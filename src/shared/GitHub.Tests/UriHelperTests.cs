// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Xunit;

namespace GitHub.Tests
{
    public class UriHelperTests
    {
        [Theory]
        [InlineData("https://github.com", true)]
        [InlineData("https://gitHUB.CoM", true)]
        [InlineData("https://GITHUB.COM", true)]
        [InlineData("https://foogithub.com", false)]
        [InlineData("https://api.github.com", false)]
        public void UriHelpers_IsGitHubDotCom(string input, bool expected)
        {
            Assert.Equal(expected, UriHelpers.IsGitHubDotCom(new Uri(input)));
        }

        [Theory]
        [InlineData("https://github.com", "https://github.com/")]
        [InlineData("https://gist.github.com", "https://github.com/")]
        [InlineData("https://github.com/owner/repo", "https://github.com/owner/repo")]
        [InlineData("https://gist.github.com/owner/repo", "https://github.com/owner/repo")]
        public void UriHelpers_NormalizeUri(string input, string expected)
        {
            Uri actualUri = UriHelpers.NormalizeUri(new Uri(input));
            Assert.Equal(expected, actualUri.ToString());
        }

        [Theory]
        [InlineData("github.com", true)]
        [InlineData("github.con", false)] // No support of phony similar tld.
        [InlineData("gist.github.con", false)] // No support of phony similar tld.
        [InlineData("foogithub.com", false)] // No support of non github.com domains.
        [InlineData("api.github.com", false)] // No support of github.com subdomains.
        [InlineData("gist.github.com", true)] // Except gists.
        [InlineData("GiST.GitHub.Com", true)]
        [InlineData("GitHub.Com", true)]
        [InlineData("github.my-company-server.com", true)]
        [InlineData("gist.github.my-company-server.com", true)]
        [InlineData("gist.my-company-server.com", false)]
        [InlineData("my-company-server.com", false)]
        [InlineData("github.my.company.server.com", true)]
        [InlineData("foogithub.my-company-server.com", false)]
        [InlineData("api.github.my-company-server.com", false)]
        [InlineData("gist.github.my.company.server.com", true)]
        [InlineData("GitHub.My-Company-Server.Com", true)]
        [InlineData("GiST.GitHub.My-Company-Server.com", true)]
        public void UriHelpers_IsGitHubLike(string input, bool expected)
        {
            Assert.Equal(expected, UriHelpers.IsGitHubLike(input));
        }

        [Theory]
        [InlineData("https://github.com/owner/repo", "owner", "repo")]
        [InlineData("https://github.com/owner/repo/", "owner", "repo")]
        [InlineData("https://GITHUB.COM/oWnEr/RePo/", "oWnEr", "RePo")]
        [InlineData("https://gist.github.com/owner/repo", "owner", "repo")]
        [InlineData("https://github.example.com/owner/repo", "owner", "repo")]
        [InlineData("https://gist.github.example.com/owner/repo", "owner", "repo")]
        public void UriHelpers_TryGetRepositoryFromUrl_Valid(string input, string expectedOwner, string expectedRepo)
        {
            var uri = new Uri(input);
            Assert.True(UriHelpers.TryGetRepositoryFromUrl(uri, out string owner, out string repo));
            Assert.Equal(expectedOwner, owner);
            Assert.Equal(expectedRepo, repo);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("https://example.com/owner/repo")]
        [InlineData("https://github.com")]
        [InlineData("https://github.com/")]
        public void UriHelpers_TryGetRepositoryFromUrl_Invalid(string input)
        {
            var uri = new Uri(input);
            Assert.False(UriHelpers.TryGetRepositoryFromUrl(uri, out string owner, out string repo));
            Assert.Null(owner);
            Assert.Null(repo);
        }
    }
}
