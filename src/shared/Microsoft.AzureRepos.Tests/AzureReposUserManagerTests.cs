// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposUserManagerTests
    {
        #region GetUser

        [Fact]
        public void AzureReposUserManager_GetUser_NullUri_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.GetUser((Uri)null));
        }

        [Fact]
        public void AzureReposUserManager_GetUser_NullOrg_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.GetUser((string)null));
        }

        [Fact]
        public void AzureReposUserManager_GetUser_Remote_NoUser_ReturnsNull()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            string actual = cache.GetUser(remote);

            Assert.Null(actual);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_Org_NoUser_ReturnsNull()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            string actual = cache.GetUser(orgName);

            Assert.Null(actual);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_Remote_User_ReturnsUser()
        {
            const string expectedUserName = "user1";
            const string otherUserName = "user2";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = otherUserName;
            store.PersistedStore[GetRemoteUserKey(remote)] = expectedUserName;

            string actualUserName = cache.GetUser(remote);

            Assert.Equal(expectedUserName, actualUserName);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_Org_User_ReturnsUser()
        {
            const string expectedUserName = "user1";
            const string otherUserName = "user2";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = expectedUserName;
            store.PersistedStore[GetRemoteUserKey(remote)] = otherUserName;

            string actualUserName = cache.GetUser(orgName);

            Assert.Equal(expectedUserName, actualUserName);
        }

        #endregion

        #region SignIn

        [Fact]
        public void AzureReposUserManager_SignInRemote_NullUri_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignInRemote(null, "user"));
        }

        [Fact]
        public void AzureReposUserManager_SignInOrg_NullOrg_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignInOrganization(null, "user"));
        }

        [Fact]
        public void AzureReposUserManager_SignInRemote_NoUser_SetsRemoteKey()
        {
            const string expectedUser = "user1";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignInRemote(remote, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignInRemote_ExistingUser_SetsRemoteKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.SignInRemote(remote, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignInOrg_NoUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignInOrganization(orgName, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignInOrg_ExistingUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.SignInOrganization(orgName, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        #endregion

        #region SignOut

        [Fact]
        public void AzureReposUserManager_SignOutRemote_NullUri_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignOutRemote(null));
        }

        [Fact]
        public void AzureReposUserManager_SignOutOrg_NullOrg_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignOutOrganization(null));
        }

        [Fact]
        public void AzureReposUserManager_SignOutRemote_NoUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOutRemote(remote);

            Assert.False(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string _));
        }

        [Fact]
        public void AzureReposUserManager_SignOutRemote_ExistingUser_RemovesRemoteKey()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.SignOutRemote(remote);

            Assert.False(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
        }

        [Fact]
        public void AzureReposUserManager_SignOutRemote_Explicit_ExistingUser_SetsRemoteKeyEmptyString()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.SignOutRemote(remote, isExplicit: true);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(string.Empty, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignOutOrg_NoUser_DoesNothing()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOutOrganization(orgName);

            Assert.False(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string _));
        }

        [Fact]
        public void AzureReposUserManager_SignOutOrg_ExistingUser_RemovesOrgKey()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.SignOutOrganization(orgName);

            Assert.False(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
        }

        #endregion

        #region GetUsers

        [Fact]
        public void AzureReposUserManager_GetRemoteUsers_NoUsers_ReturnsEmpty()
        {
            var expected = new Dictionary<Uri, string>();

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            IDictionary<Uri, string> actual = cache.GetRemoteUsers();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetOrgUsers_NoUsers_ReturnsEmpty()
        {
            var expected = new Dictionary<string, string>();

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            IDictionary<string, string> actual = cache.GetOrganizationUsers();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetRemoteUsers_Users_ReturnsRemoteUsers()
        {
            const string org1 = "org1";
            const string org2 = "org2";
            var remote1 = new Uri("https://dev.azure.com/org/_git/repo1");
            var remote2 = new Uri("https://dev.azure.com/org/_git/repo2");

            var expected = new Dictionary<Uri, string>
            {
                [remote1] = "user1",
                [remote2] = "user2",
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetRemoteUserKey(remote1)] = "user1";
            store.PersistedStore[GetRemoteUserKey(remote2)] = "user2";
            store.PersistedStore[GetOrgUserKey(org1)] = "user3";
            store.PersistedStore[GetOrgUserKey(org2)] = "user4";

            IDictionary<Uri, string> actual = cache.GetRemoteUsers();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetOrgUsers_Users_ReturnsOrgUsers()
        {
            const string org1 = "org1";
            const string org2 = "org2";
            var remote1 = new Uri("https://dev.azure.com/org/_git/repo1");
            var remote2 = new Uri("https://dev.azure.com/org/_git/repo2");

            var expected = new Dictionary<string, string>
            {
                [org1] = "user3",
                [org2] = "user4",
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetRemoteUserKey(remote1)] = "user1";
            store.PersistedStore[GetRemoteUserKey(remote2)] = "user2";
            store.PersistedStore[GetOrgUserKey(org1)] = "user3";
            store.PersistedStore[GetOrgUserKey(org2)] = "user4";

            IDictionary<string, string> actual = cache.GetOrganizationUsers();

            Assert.Equal(expected, actual);
        }

        #endregion

        #region Helpers

        private static string GetOrgUserKey(string orgName)
        {
            return $"org.{orgName}.user";
        }

        private static string GetRemoteUserKey(Uri uri)
        {
            return $"remote.{uri}.user";
        }

        #endregion
    }
}
