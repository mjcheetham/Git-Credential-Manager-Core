// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Commands;
using KnownGitCfg = Microsoft.Git.CredentialManager.Constants.GitConfiguration;

namespace Microsoft.AzureRepos
{
    public class AzureReposHostProvider : DisposableObject, IHostProvider, IConfigurableComponent, ICommandProvider
    {
        private readonly ICommandContext _context;
        private readonly IAzureDevOpsRestApi _azDevOps;
        private readonly IMicrosoftAuthentication _msAuth;
        private readonly IAzureDevOpsAuthorityCache _authorityCache;
        private readonly IAzureReposUserManager _userManager;

        public AzureReposHostProvider(ICommandContext context)
            : this(context, new AzureDevOpsRestApi(context), new MicrosoftAuthentication(context),
                new AzureDevOpsAuthorityCache(context), new AzureReposUserManager(context))
        {
        }

        public AzureReposHostProvider(ICommandContext context, IAzureDevOpsRestApi azDevOps,
            IMicrosoftAuthentication msAuth, IAzureDevOpsAuthorityCache authorityCache,
            IAzureReposUserManager userManager)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(azDevOps, nameof(azDevOps));
            EnsureArgument.NotNull(msAuth, nameof(msAuth));
            EnsureArgument.NotNull(authorityCache, nameof(authorityCache));
            EnsureArgument.NotNull(userManager, nameof(userManager));

            _context = context;
            _azDevOps = azDevOps;
            _msAuth = msAuth;
            _authorityCache = authorityCache;
            _userManager = userManager;
        }

        #region IHostProvider

        public string Id => "azure-repos";

        public string Name => "Azure Repos";

        public IEnumerable<string> SupportedAuthorityIds => MicrosoftAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Azure Repos,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return input.TryGetHostAndPort(out string hostName, out _)
                   && (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                       StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   UriHelpers.IsAzureDevOpsHost(hostName);
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            // Azure DevOps Server (TFS) is handled by the generic provider, which supports basic auth, and WIA detection.
            return false;
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Azure Repos. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();
            Uri orgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string orgName);

            // Determine the MS authentication authority for this organization
            _context.Trace.WriteLine($"Determining Microsoft Authentication authority for Azure DevOps organization '{orgName}'...");
            string authAuthority = await _authorityCache.GetAuthorityAsync(orgName);
            if (authAuthority is null)
            {
                // If there is no cached value we must query for it and cache it for future use
                _context.Trace.WriteLine($"No cached authority value - querying {orgUri} for authority...");
                authAuthority = await _azDevOps.GetAuthorityAsync(orgUri);
                await _authorityCache.UpdateAuthorityAsync(orgName, authAuthority);
            }
            _context.Trace.WriteLine($"Authority is '{authAuthority}'.");

            // Get the currently 'signed in' user for this remote, if one exists
            _context.Trace.WriteLine($"Looking up signed-in user for remote '{remoteUri}'...");

            // Always prefer a remote-level user before an org-level one
            string userName = _userManager.GetUser(remoteUri);
            if (userName is null)
            {
                userName = _userManager.GetUser(orgName);
            }
            else if (string.IsNullOrEmpty(userName))
            {
                // The empty string means the remote was explicitly signed-out previously
                // and we should not attempt to use the org-level user
                _context.Trace.WriteLine("Remote was previously explicitly signed-out.");
            }

            _context.Trace.WriteLine(string.IsNullOrWhiteSpace(userName)
                ? "No signed-in user found."
                : $"Signed-in user is '{userName}'.");

            // Get an AAD access token for the Azure DevOps SPS
            _context.Trace.WriteLine("Getting Azure AD access token...");
            IMicrosoftAuthenticationResult result = await _msAuth.GetTokenAsync(
                authAuthority,
                GetClientId(),
                GetRedirectUri(),
                AzureDevOpsConstants.AzureDevOpsDefaultScopes,
                userName);
            _context.Trace.WriteLineSecrets(
                $"Acquired Azure access token. Account='{result.AccountUpn}' Token='{{0}}' TokenSource='{result.TokenSource}'",
                new object[] {result.AccessToken});

            return new GitCredential(result.AccountUpn, result.AccessToken);
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            string account = input.UserName;
            var icmp = StringComparer.OrdinalIgnoreCase;

            Uri remoteUri = input.GetRemoteUri();
            string orgName = UriHelpers.GetOrganizationName(remoteUri);

            //
            // Try and mark the user as signed-in if required.
            //

            // Look for an existing org-level user sign-in
            string orgUser = _userManager.GetUser(orgName);

            // If there is no existing organization user then sign-in to the org now and
            // clear any remote-level sign-in that may exist
            if (orgUser is null)
            {
                _userManager.SignInOrganization(orgName, account);
                _userManager.SignOutRemote(remoteUri);
            }
            else
            {
                if (!icmp.Equals(orgUser, account))
                {
                    // If the organization has been signed in with a different user then explicitly with this remote URL
                    _userManager.SignInRemote(remoteUri, account);
                }
                else
                {
                    // The org-level sign-in is correct; clear any remote-level sign-in state
                    _userManager.SignOutRemote(remoteUri);
                }
            }

            return Task.CompletedTask;
        }

        public async Task EraseCredentialAsync(InputArguments input)
        {
            // Clear the authority cache in case this was the reason for failure
            Uri remoteUri = input.GetRemoteUri();
            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            await _authorityCache.EraseAuthorityAsync(orgName);

            //
            // Try and mark the user as signed-out.
            //

            // Look for existing org-level and remote-level user sign-ins
            string orgUser = _userManager.GetUser(orgName);

            // If there is an org-level sign-in then mark the remote as explicitly 'signed-out' so that we
            // prompt for a user the next attempt and do NOT inherit the org-level user.
            if (orgUser != null)
            {
                _userManager.SignOutRemote(remoteUri, isExplicit: true);
            }
            else
            {
                // If there is no org-level sign-in then just remove any remote-level sign-in
                _userManager.SignOutRemote(remoteUri);
            }
        }

        protected override void ReleaseManagedResources()
        {
            _azDevOps.Dispose();
            base.ReleaseManagedResources();
        }

        private string GetClientId()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                AzureDevOpsConstants.EnvironmentVariables.DevAadClientId,
                Constants.GitConfiguration.Credential.SectionName, AzureDevOpsConstants.GitConfiguration.Credential.DevAadClientId,
                out string clientId))
            {
                return clientId;
            }

            return AzureDevOpsConstants.AadClientId;
        }

        private Uri GetRedirectUri()
        {
            // Check for developer override value
            if (_context.Settings.TryGetSetting(
                AzureDevOpsConstants.EnvironmentVariables.DevAadRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, AzureDevOpsConstants.GitConfiguration.Credential.DevAadRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return AzureDevOpsConstants.AadRedirectUri;
        }

        #endregion

        #region IConfigurationComponent

        string IConfigurableComponent.Name => "Azure Repos provider";

        public Task ConfigureAsync(ConfigurationTarget target)
        {
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration(configurationLevel);

            if (targetConfig.TryGet(useHttpPathKey, out string currentValue) && currentValue.IsTruthy())
            {
                _context.Trace.WriteLine("Git configuration 'credential.useHttpPath' is already set to 'true' for https://dev.azure.com.");
            }
            else
            {
                _context.Trace.WriteLine("Setting Git configuration 'credential.useHttpPath' to 'true' for https://dev.azure.com...");
                targetConfig.Set(useHttpPathKey, "true");
            }

            return Task.CompletedTask;
        }

        public Task UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string useHttpPathKey = $"{KnownGitCfg.Credential.SectionName}.https://dev.azure.com.{KnownGitCfg.Credential.UseHttpPath}";

            _context.Trace.WriteLine("Clearing Git configuration 'credential.useHttpPath' for https://dev.azure.com...");

            GitConfigurationLevel configurationLevel = target == ConfigurationTarget.System
                ? GitConfigurationLevel.System
                : GitConfigurationLevel.Global;

            IGitConfiguration targetConfig = _context.Git.GetConfiguration(configurationLevel);

            // On Windows, if there is a "manager-core" entry remaining in the system config then we must not clear
            // the useHttpPath option otherwise this would break the bundled version of GCM Core in Git for Windows.
            if (!PlatformUtils.IsWindows() || target != ConfigurationTarget.System ||
                targetConfig.GetAll(helperKey).All(x => !string.Equals(x, "manager-core")))
            {
                targetConfig.Unset(useHttpPathKey);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region ICommandProvider

        void ICommandProvider.ConfigureCommand(Command rootCommand)
        {
            var clearAuthCacheCmd = new Command("clear-cache", "Clear the authority cache")
            {
                Handler = CommandHandler.Create(ClearAuthCacheCmdAsync)
            };

            rootCommand.AddCommand(clearAuthCacheCmd);
        }

        private async Task<int> ClearAuthCacheCmdAsync()
        {
            _context.Streams.Out.WriteLine("Clearing Azure DevOps authority cache...");
            await _authorityCache.ClearAsync();
            return 0;
        }

        #endregion
    }
}
