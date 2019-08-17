// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : HostProvider
    {
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IAzureReposAuthorityCache _authorityCache;
        private readonly IAzureReposUserManager _userManager;

        public AzureReposHostProvider(ICommandContext context)
            : this(context,
                new AzureDevOpsRestApi(context),
                new MicrosoftAuthentication(context),
                AzureDevOpsConstants.CreateIniDataStore(context?.FileSystem))
        { }

        public AzureReposHostProvider(
            ICommandContext context,
            IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth,
            ITransactionalValueStore<string, string> dataStore)
            : this(context, azDevOps, msAuth,
                new AzureReposAuthorityCache(context?.Trace, dataStore),
                new AzureReposUserManager(context?.Trace, dataStore))
        { }

        public AzureReposHostProvider(
            ICommandContext context,
            IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth,
            IAzureReposAuthorityCache authorityCache,
            IAzureReposUserManager userManager)
            : base(context)
        {
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(authorityCache, nameof(authorityCache));
            EnsureArgument.NotNull(userManager, nameof(userManager));

            _azDevOps = azDevOps;
            _msAuth = msAuth;
            _authorityCache = authorityCache;
            _userManager = userManager;
        }

        #region HostProvider

        public override string Id => "azure-repos";

        public override string Name => "Azure Repos";

        public override IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

        public override bool IsSupported(InputArguments input)
        {
            // We do not support unencrypted HTTP communications to Azure Repos,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                   StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   UriHelpers.IsAzureDevOpsHost(input.Host);
        }

        public override string GetCredentialKey(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri);
            return $"git:{orgUri.AbsoluteUri}";
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            Debug.Assert(!Context.Settings.GetIsAccessTokenModeEnabled(), "Should only be creating a PAT credential in PAT-mode.");

            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            JsonWebToken accessToken = await GetAccessTokenAsync(remoteUri);
            string atUser = accessToken.GetAzureUserName();
            Context.Trace.WriteLineSecrets($"Acquired Azure access token. User='{atUser}' Token='{{0}}'", new object[] {accessToken.EncodedToken});

            var patScopes = new[]
            {
                AzureDevOpsConstants.PersonalAccessTokenScopes.ReposWrite,
                AzureDevOpsConstants.PersonalAccessTokenScopes.ArtifactsRead
            };

            Context.Trace.WriteLine($"Creating Azure DevOps PAT for organization '{orgName}' with scopes '{string.Join(", ", patScopes)}'...");

            string pat = await _azDevOps.CreatePersonalAccessTokenAsync(
                orgUri,
                accessToken,
                patScopes);
            Context.Trace.WriteLineSecrets("PAT created. PAT='{0}'", new object[] {pat});

            return new GitCredential(Constants.PersonalAccessTokenUserName, pat);
        }

        public override async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            EnsureSecureProtocol(input);

            // In AT-only mode we just return the AAD AT directly.
            if (Context.Settings.GetIsAccessTokenModeEnabled())
            {
                Context.Trace.WriteLine("Azure Access Token (AT) mode is enabled.");

                Uri remoteUri = input.GetRemoteUri();

                JsonWebToken accessToken = await GetAccessTokenAsync(remoteUri);
                string atUser = accessToken.GetAzureUserName();
                Context.Trace.WriteLineSecrets($"Acquired Azure access token. User='{atUser}' Token='{{0}}'", new object[] {accessToken.EncodedToken});

                return new GitCredential(atUser, accessToken.EncodedToken);
            }

            Context.Trace.WriteLine("Azure DevOps Personal Access Token (PAT) mode is enabled.");
            return await base.GetCredentialAsync(input);
        }

        public override Task StoreCredentialAsync(InputArguments input)
        {
            if (Context.Settings.GetIsAccessTokenModeEnabled())
            {
                Context.Trace.WriteLine("Azure Access Token (AT) mode is enabled.");

                // Mark the specified user as signed-in to the particular remote
                Uri remoteUri = input.GetRemoteUri();
                string userName = input.UserName;
                _userManager.SignIn(remoteUri, userName);

                // Nothing to store here.. the MSAuth component will have already stored the AT and RT for us.
                return Task.CompletedTask;
            }

            Context.Trace.WriteLine("Azure DevOps Personal Access Token (PAT) mode is enabled.");
            return base.StoreCredentialAsync(input);
        }

        public override Task EraseCredentialAsync(InputArguments input)
        {
            // We should clear out the cached authority for this organization in case the reason for
            // the authentication failure was using old or incorrect data to generate the credentials.
            Uri remoteUri = input.GetRemoteUri();
            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            _authorityCache.EraseAuthority(orgName);

            if (Context.Settings.GetIsAccessTokenModeEnabled())
            {
                Context.Trace.WriteLine("Azure Access Token (AT) mode is enabled.");
                // We should explicitly mark the specific remote as being 'signed-out' to force other attempts to sign-in
                // to not use an existing user or refresh token.
                _userManager.SignOut(remoteUri);

                // Nothing more to erase here.. the lack of an existing signed-in user will force the MSAuth component create a new AT and RT.
                return Task.CompletedTask;
            }

            Context.Trace.WriteLine("Azure DevOps Personal Access Token (PAT) mode is enabled.");
            return base.EraseCredentialAsync(input);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _azDevOps.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Private methods

        private async Task<JsonWebToken> GetAccessTokenAsync(Uri remoteUri)
        {
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);
            Context.Trace.WriteLine($"Acquiring Azure access token for organization '{orgName}'...");

            // Determine the MS authentication authority for this organization
            string authority = _authorityCache.GetAuthority(orgName);
            if (authority is null)
            {
                Context.Trace.WriteLine("No authority found in cache; querying server...");
                authority = await _azDevOps.GetAuthorityAsync(orgUri);

                // Update our cache
                _authorityCache.UpdateAuthority(orgName, authority);
            }
            Context.Trace.WriteLine($"Authority for '{orgName}' is '{authority}'.");

            string userName = _userManager.GetUser(remoteUri);
            if (userName is null)
            {
                Context.Trace.WriteLine("No existing user is signed-in.");
            }
            else
            {
                Context.Trace.WriteLine($"Found existing signed-in user: '{userName}'.");
            }

            // Get an AAD access token for the Azure DevOps SPS
            JsonWebToken accessToken = await _msAuth.GetAccessTokenAsync(
                authority,
                AzureDevOpsConstants.AadClientId,
                AzureDevOpsConstants.AadRedirectUri,
                AzureDevOpsConstants.AadResourceId,
                remoteUri,
                userName);

            return accessToken;
        }

        #endregion
    }
}
