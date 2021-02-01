// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    public interface IAzureDevOpsAuthorityCache
    {
        /// <summary>
        /// Lookup the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        /// <returns>Authority for the organization, or null if not found.</returns>
        Task<string> GetAuthorityAsync(string orgName);

        /// <summary>
        /// Updates the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        /// <param name="authority">New authority value.</param>
        Task UpdateAuthorityAsync(string orgName, string authority);

        /// <summary>
        /// Erase the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        Task EraseAuthorityAsync(string orgName);

        /// <summary>
        /// Erase all cached authorities for all Azure DevOps organizations.
        /// </summary>
        Task ClearAsync();
    }

    public class AzureDevOpsAuthorityCache : IAzureDevOpsAuthorityCache
    {
        private readonly ITrace _trace;
        private readonly IScopedTransactionalStore _iniStore;

        public AzureDevOpsAuthorityCache(ICommandContext context)
            : this(context.Trace, new IniFileStore(context.FileSystem, new IniSerializer(), Path.Combine(
                context.FileSystem.UserDataDirectoryPath,
                AzureDevOpsConstants.AzReposDataDirectoryName,
                AzureDevOpsConstants.AzReposDataStoreName))) { }

        public AzureDevOpsAuthorityCache(ITrace trace, IScopedTransactionalStore iniStore)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(iniStore, nameof(iniStore));

            _trace = trace;
            _iniStore = iniStore;
        }

        public async Task<string> GetAuthorityAsync(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Looking up cached authority for organization '{orgName}'...");

            await _iniStore.ReloadAsync();
            if (_iniStore.TryGetValue(GetAuthorityKey(orgName), out string authority))
            {
                return authority;
            }

            return null;
        }

        public async Task UpdateAuthorityAsync(string orgName, string authority)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Updating cached authority for '{orgName}' to '{authority}'...");

            await _iniStore.ReloadAsync();
            _iniStore.SetValue(GetAuthorityKey(orgName), authority);
            await _iniStore.CommitAsync();
        }

        public async Task EraseAuthorityAsync(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Removing cached authority for '{orgName}'...");
            await _iniStore.ReloadAsync();
            _iniStore.Remove(GetAuthorityKey(orgName));
            await _iniStore.CommitAsync();
        }

        public async Task ClearAsync()
        {
            _trace.WriteLine("Removing all cached authorities...");

            await _iniStore.ReloadAsync();

            IEnumerable<string> orgScopes = _iniStore.GetSectionScopes("org");
            foreach (var orgName in orgScopes)
            {
                _iniStore.Remove(GetAuthorityKey(orgName));
            }

            await _iniStore.CommitAsync();
        }

        private static string GetAuthorityKey(string orgName)
        {
            return $"org.{orgName}.authority";
        }
    }
}
