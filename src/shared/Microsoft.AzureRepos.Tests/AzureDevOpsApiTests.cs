// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureDevOpsApiTests
    {
        private const string CommonAuthority = "https://login.microsoftonline.com/common";
        private const string OrganizationsAuthority = "https://login.microsoftonline.com/organizations";

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NullUri_ThrowsException()
        {
            var api = new AzureDevOpsRestApi(new TestCommandContext());

            await Assert.ThrowsAsync<ArgumentNullException>(() => api.GetAuthorityAsync(null));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NoNetwork_ThrowsException()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var httpHandler = new TestHttpMessageHandler {SimulateNoNetwork = true};

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            await Assert.ThrowsAsync<HttpRequestException>(() => api.GetAuthorityAsync(uri));
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_NoHeaders_ReturnsCommonAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = CommonAuthority;

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_WwwAuthenticateBearer_ReturnsAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = "https://login.microsoftonline.com/test-authority";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_WwwAuthenticateMultiple_ReturnsBearerAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            const string expectedAuthority = "https://login.microsoftonline.com/test-authority";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.WwwAuthenticate.ParseAdd("Bearer");
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");
            httpResponse.Headers.WwwAuthenticate.ParseAdd("NTLM [test-challenge-string]");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantAad_ReturnsAadAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantId = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantId:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = {{AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId.ToString("D")}}
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantMultiple_ReturnsFirstAadAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantId1 = Guid.NewGuid();
            var msaTenantId  = Guid.Empty;
            var aadTenantId2 = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantId1:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers =
                {
                    {AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId1.ToString("D")},
                    {AzureDevOpsConstants.VssResourceTenantHeader, msaTenantId.ToString("D")},
                    {AzureDevOpsConstants.VssResourceTenantHeader, aadTenantId2.ToString("D")},
                }
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_VssResourceTenantMsa_ReturnsOrganizationsAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var msaTenantId = Guid.Empty;

            // This is only the case because we're using MSA pass-through.. in the future, if and when we
            // move away from MSA pass-through, this should be the common authority.
            const string expectedAuthority = OrganizationsAuthority;

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Headers = {{AzureDevOpsConstants.VssResourceTenantHeader, msaTenantId.ToString("D")}}
            };

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public async Task AzureDevOpsRestApi_GetAuthorityAsync_BothWwwAuthAndVssResourceHeaders_ReturnsWwwAuthAuthority()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");
            var aadTenantIdWwwAuth = Guid.NewGuid();
            var aadTenantIdVssRes = Guid.NewGuid();

            string expectedAuthority = $"https://login.microsoftonline.com/{aadTenantIdWwwAuth:D}";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            httpResponse.Headers.Add(AzureDevOpsConstants.VssResourceTenantHeader, aadTenantIdVssRes.ToString("D"));
            httpResponse.Headers.WwwAuthenticate.ParseAdd($"Bearer authorization_uri={expectedAuthority}");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, httpResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var api = new AzureDevOpsRestApi(context);

            string actualAuthority = await api.GetAuthorityAsync(uri);

            Assert.Equal(expectedAuthority, actualAuthority);
        }


        [Theory]
        [InlineData(null, false, null)]
        [InlineData("NotBearer", false, null)]
        [InlineData("Bearer", false, null)]
        [InlineData("Bearer foobar", false, null)]
        [InlineData("Bearer authorization_uri=https://example.com", true, "https://example.com")]
        public void AzureDevOpsRestApi_TryGetAuthorityFromHeader(string headerValue, bool expectedResult, string expectedAuthority)
        {
            var header = headerValue is null ? null : AuthenticationHeaderValue.Parse(headerValue);
            bool actualResult = AzureDevOpsRestApi.TryGetAuthorityFromHeader(header, out string actualAuthority);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedAuthority, actualAuthority);
        }
    }
}
