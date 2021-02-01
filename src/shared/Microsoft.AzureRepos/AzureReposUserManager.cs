// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    /// <summary>
    /// Manages association of users and Git remotes for Azure Repos.
    /// </summary>
    public interface IAzureReposUserManager
    {
        /// <summary>
        /// Get the identifier of a user signed-in to the given organization.
        /// </summary>
        /// <param name="orgName">Organization name to query a signed-in user for.</param>
        /// <returns>User identifier signed-in to the remote, or null if no user is signed-in.</returns>
        string GetUser(string orgName);

        /// <summary>
        /// Get the identifier of a user signed-in to the given remote URL.
        /// </summary>
        /// <param name="remoteUri">Remote URL to query a signed-in user for.</param>
        /// <returns>User identifier signed-in to the remote, or null if no user is signed-in.</returns>
        string GetUser(Uri remoteUri);

        /// <summary>
        /// Get all users that have been signed-in at the organization level.
        /// </summary>
        /// <returns>Users signed-in by organization.</returns>
        IDictionary<string, string> GetOrganizationUsers();

        /// <summary>
        /// Get all users that have been signed-in at the remote URL level.
        /// </summary>
        /// <returns>Users signed-in by remote URL.</returns>
        IDictionary<Uri, string> GetRemoteUsers();

        /// <summary>
        /// Sign-in a user to the given organization.
        /// </summary>
        /// <param name="orgName">Organization to sign the user in to.</param>
        /// <param name="userName">User identifier to sign-in to the target.</param>
        void SignInOrganization(string orgName, string userName);

        /// <summary>
        /// Sign-in a user to the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to sign the user in to.</param>
        /// <param name="userName">User identifier to sign-in to the target.</param>
        void SignInRemote(Uri remoteUri, string userName);

        /// <summary>
        /// Sign-out a user from the given remote URI.
        /// </summary>
        /// <param name="orgName">Organization to sign the user out of.</param>
        void SignOutOrganization(string orgName);

        /// <summary>
        /// Sign-out a user from the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to sign the user out of.</param>
        /// <param name="isExplicit">Mark the remote as explicitly signed-out</param>
        void SignOutRemote(Uri remoteUri, bool isExplicit = false);
    }

    public class AzureReposUserManager : IAzureReposUserManager
    {
        private readonly ITrace _trace;
        private readonly IScopedTransactionalStore _iniStore;

        public AzureReposUserManager(ICommandContext context)
            : this(context.Trace, new IniFileStore(context.FileSystem, new IniSerializer(), Path.Combine(
                context.FileSystem.UserDataDirectoryPath,
                AzureDevOpsConstants.AzReposDataDirectoryName,
                AzureDevOpsConstants.AzReposDataStoreName))) { }

        public AzureReposUserManager(ITrace trace, IScopedTransactionalStore iniStore)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(iniStore, nameof(iniStore));

            _trace = trace;
            _iniStore = iniStore;
        }

        public string GetUser(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _iniStore.ReloadAsync();

            string key = GetOrgUserKey(orgName);

            _trace.WriteLine($"Looking for user signed-in to organization '{orgName}'...");
            if (_iniStore.TryGetValue(key, out string userName))
            {
                return userName;
            }

            return null;
        }

        public string GetUser(Uri remoteUri)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            _iniStore.ReloadAsync();

            string key = GetRemoteUserKey(remoteUri);

            _trace.WriteLine($"Looking for user signed-in to remote URL '{remoteUri}'...");
            if (_iniStore.TryGetValue(key, out string userName))
            {
                return userName;
            }

            return null;
        }

        public IDictionary<string, string> GetOrganizationUsers()
        {
            var dict = new Dictionary<string, string>();

            _iniStore.ReloadAsync();

            IEnumerable<string> orgNames = _iniStore.GetSectionScopes("org");
            foreach (string orgName in orgNames)
            {
                string orgUserKey = GetOrgUserKey(orgName);
                if (_iniStore.TryGetValue(orgUserKey, out string orgUser))
                {
                    dict[orgName] = orgUser;
                }
            }

            return dict;
        }

        public IDictionary<Uri, string> GetRemoteUsers()
        {
            var dict = new Dictionary<Uri, string>();

            _iniStore.ReloadAsync();

            IEnumerable<string> remotes = _iniStore.GetSectionScopes("remote");
            foreach (string remoteUrl in remotes)
            {
                if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out Uri remoteUri))
                {
                    continue;
                }

                string orgUserKey = GetRemoteUserKey(remoteUri);
                if (_iniStore.TryGetValue(orgUserKey, out string remoteUser))
                {
                    dict[remoteUri] = remoteUser;
                }
            }

            return dict;
        }

        public void SignInOrganization(string orgName, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _iniStore.ReloadAsync();

            string key = GetOrgUserKey(orgName);

            _trace.WriteLine($"Signing-in user '{userName}' to organization '{orgName}'...");
            _iniStore.SetValue(key, userName);

            _iniStore.CommitAsync();
        }

        public void SignInRemote(Uri remoteUri, string userName)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            _iniStore.ReloadAsync();

            string key = GetRemoteUserKey(remoteUri);

            _trace.WriteLine($"Signing-in user '{userName}' to remote URL '{remoteUri}'...");
            _iniStore.SetValue(key, userName);

            _iniStore.CommitAsync();
        }

        public void SignOutOrganization(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _iniStore.ReloadAsync();

            string key = GetOrgUserKey(orgName);

            _trace.WriteLine($"Signing-out of organization '{orgName}'...");
            _iniStore.Remove(key);

            _iniStore.CommitAsync();
        }

        public void SignOutRemote(Uri remoteUri, bool isExplicit = false)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            _iniStore.ReloadAsync();

            string key = GetRemoteUserKey(remoteUri);

            if (isExplicit)
            {
                // Use the empty string value to signal an explicitly signed-out user
                _trace.WriteLine($"Explicitly signing-out of remote URL '{remoteUri}'...");
                _iniStore.SetValue(key, string.Empty);
            }
            else
            {
                _trace.WriteLine($"Signing-out of remote URL '{remoteUri}'...");
                _iniStore.Remove(key);
            }

            _iniStore.CommitAsync();
        }

        private static string GetOrgUserKey(string orgName)
        {
            return $"org.{orgName}.user";
        }

        private static string GetRemoteUserKey(Uri uri)
        {
            return $"remote.{uri}.user";
        }
    }
}
