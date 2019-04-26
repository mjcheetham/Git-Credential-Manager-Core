// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net;
using System.Net.Http;
using Moq;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class HttpClientFactoryTests
    {
        [Fact]
        public void HttpClientFactory_GetClient_SetsDefaultHeaders()
        {
            const string expectedUserAgent = "Git-Credential-Manager/3.14159 (TestOS v99; MIPS) CLR/TestCLR-42.42";

            var platformInfo = new Mock<IPlatformInformation>();
            platformInfo.SetupGet(x => x.RuntimeName).Returns("TestCLR");
            platformInfo.SetupGet(x => x.RuntimeVersion).Returns("42.42");
            platformInfo.SetupGet(x => x.CpuArchitecture).Returns("MIPS");
            platformInfo.SetupGet(x => x.ApplicationVersion).Returns("3.14159");
            platformInfo.SetupGet(x => x.OperatingSystemName).Returns("TestOS");
            platformInfo.SetupGet(x => x.OperatingSystemVersion).Returns( "v99");

            var factory = new HttpClientFactory(Mock.Of<ITrace>(), Mock.Of<ISettings>(), new TestStandardStreams(), platformInfo.Object);

            HttpClient client = factory.CreateClient();

            Assert.NotNull(client);
            Assert.Equal(expectedUserAgent, client.DefaultRequestHeaders.UserAgent.ToString());
            Assert.True(client.DefaultRequestHeaders.CacheControl.NoCache);
        }

        [Fact]
        public void HttpClientFactory_GetClient_MultipleCalls_ReturnsNewInstance()
        {
            var factory = new HttpClientFactory(Mock.Of<ITrace>(), Mock.Of<ISettings>(), new TestStandardStreams(), new TestPlatformInformation());

            HttpClient client1 = factory.CreateClient();
            HttpClient client2 = factory.CreateClient();

            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_NoProxy_ReturnsFalseOutNull()
        {
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";
            var repoRemoteUri = new Uri(repoRemote);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath
            };
            var httpFactory = new HttpClientFactory(Mock.Of<ITrace>(), settings, Mock.Of<IStandardStreams>(), new TestPlatformInformation());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.False(result);
            Assert.Null(proxy);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyNoCredentials_ReturnsTrueOutProxyWithUrlDefaultCredentials()
        {
            const string repoPath = "/tmp/repos/foo";
            const string repoRemote = "https://remote.example.com/foo.git";
            var repoRemoteUri = new Uri(repoRemote);

            string proxyConfigString = "https://proxy.example.com/git";
            string expectedProxyUrl = proxyConfigString;

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = new Uri(proxyConfigString)
            };
            var httpFactory = new HttpClientFactory(Mock.Of<ITrace>(), settings, Mock.Of<IStandardStreams>(), new TestPlatformInformation());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            var configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(expectedProxyUrl, configuredProxyUrl.ToString());

            AssertDefaultCredentials(proxy.Credentials);
        }

        [Fact]
        public void HttpClientFactory_TryCreateProxy_ProxyWithCredentials_ReturnsTrueOutProxyWithUrlConfiguredCredentials()
        {
            const string proxyScheme = "https";
            const string proxyUser   = "john.doe";
            const string proxyPass   = "letmein";
            const string proxyHost   = "proxy.example.com/git";
            const string repoPath    = "/tmp/repos/foo";
            const string repoRemote  = "https://remote.example.com/foo.git";

            string proxyConfigString = $"{proxyScheme}://{proxyUser}:{proxyPass}@{proxyHost}";
            string expectedProxyUrl  = $"{proxyScheme}://{proxyHost}";
            var repoRemoteUri = new Uri(repoRemote);

            var settings = new TestSettings
            {
                RemoteUri = repoRemoteUri,
                RepositoryPath = repoPath,
                ProxyConfiguration = new Uri(proxyConfigString)
            };
            var httpFactory = new HttpClientFactory(Mock.Of<ITrace>(), settings, Mock.Of<IStandardStreams>(), new TestPlatformInformation());

            bool result = httpFactory.TryCreateProxy(out IWebProxy proxy);

            Assert.True(result);
            Assert.NotNull(proxy);
            var configuredProxyUrl = proxy.GetProxy(repoRemoteUri);
            Assert.Equal(expectedProxyUrl, configuredProxyUrl.ToString());

            Assert.NotNull(proxy.Credentials);
            Assert.IsType<NetworkCredential>(proxy.Credentials);
            var configuredCredentials = (NetworkCredential) proxy.Credentials;
            Assert.Equal(proxyUser, configuredCredentials.UserName);
            Assert.Equal(proxyPass, configuredCredentials.Password);
        }

        private static void AssertDefaultCredentials(ICredentials credentials)
        {
            var netCred = (NetworkCredential) credentials;

            Assert.Equal(string.Empty, netCred.Domain);
            Assert.Equal(string.Empty, netCred.UserName);
            Assert.Equal(string.Empty, netCred.Password);
        }
    }
}
