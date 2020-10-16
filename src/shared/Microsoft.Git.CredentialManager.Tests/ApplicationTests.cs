// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class ApplicationTests
    {
        [Fact]
        public async Task Application_ConfigureAsync_HelperSet_DoesNothing()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext();
            IConfigurableComponent application = new Application(context, executablePath);

            context.Git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                emptyHelper, gcmConfigName
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.GlobalConfiguration.Dictionary);
            Assert.True(context.Git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperSetWithOthersPreceding_DoesNothing()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext();
            IConfigurableComponent application = new Application(context, executablePath);

            context.Git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                "foo", "bar", emptyHelper, gcmConfigName
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.GlobalConfiguration.Dictionary);
            Assert.True(context.Git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(4, actualValues.Count);
            Assert.Equal("foo", actualValues[0]);
            Assert.Equal("bar", actualValues[1]);
            Assert.Equal(emptyHelper, actualValues[2]);
            Assert.Equal(gcmConfigName, actualValues[3]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperSetWithOthersFollowing_ClearsEntriesSetsHelper()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext();
            IConfigurableComponent application = new Application(context, executablePath);

            context.Git.GlobalConfiguration.Dictionary[key] = new List<string>
            {
                "bar", emptyHelper, executablePath, "foo"
            };

            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.GlobalConfiguration.Dictionary);
            Assert.True(context.Git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_ConfigureAsync_HelperNotSet_SetsHelper()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext();
            IConfigurableComponent application = new Application(context, executablePath);
            await application.ConfigureAsync(ConfigurationTarget.User);

            Assert.Single(context.Git.GlobalConfiguration.Dictionary);
            Assert.True(context.Git.GlobalConfiguration.Dictionary.TryGetValue(key, out var actualValues));
            Assert.Equal(2, actualValues.Count);
            Assert.Equal(emptyHelper, actualValues[0]);
            Assert.Equal(gcmConfigName, actualValues[1]);
        }

        [Fact]
        public async Task Application_UnconfigureAsync_HelperSet_RemovesEntries()
        {
            const string emptyHelper = "";
            const string gcmConfigName = "manager-core";
            const string executablePath = "/usr/local/share/gcm-core/git-credential-manager-core";
            string key = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";

            var context = new TestCommandContext();
            IConfigurableComponent application = new Application(context, executablePath);

            context.Git.GlobalConfiguration.Dictionary[key] = new List<string> {emptyHelper, gcmConfigName};

            await application.UnconfigureAsync(ConfigurationTarget.User);

            Assert.Empty(context.Git.GlobalConfiguration.Dictionary);
        }
    }
}
