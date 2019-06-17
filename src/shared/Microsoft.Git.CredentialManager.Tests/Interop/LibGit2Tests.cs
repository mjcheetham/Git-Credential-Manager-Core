// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop
{
    public class LibGit2Tests
    {
        [Fact]
        public void LibGit2_GetRepositoryPath_NotInsideRepository_ReturnsNull()
        {
            var git = new CredentialManager.Interop.LibGit2();
            string randomPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
            Directory.CreateDirectory(randomPath);

            string repositoryPath = git.GetRepositoryPath(randomPath);

            Assert.Null(repositoryPath);
        }

        [Fact]
        public void LibGit2_GetConfiguration_ReturnsConfiguration()
        {
            var git = new CredentialManager.Interop.LibGit2();
            using (var config = git.GetConfiguration())
            {
                Assert.NotNull(config);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_Exists_ReturnsString()
        {
            var git = new CredentialManager.Interop.LibGit2();
            using (var config = git.GetConfiguration())
            {
                string value = config.GetString("user.name");
                Assert.NotNull(value);
            }
        }

        [Fact]
        public void LibGit2Configuration_GetString_DoesNotExists_ReturnsNull()
        {
            var git = new CredentialManager.Interop.LibGit2();
            using (var config = git.GetConfiguration())
            {
                string randomKey = $"{Guid.NewGuid():N}.{Guid.NewGuid():N}";
                string value = config.GetString(randomKey);
                Assert.Null(value);
            }
        }
    }
}
