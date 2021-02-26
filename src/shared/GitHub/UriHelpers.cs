// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace GitHub
{
    public static class UriHelpers
    {
        public static bool IsGitHubLike(Uri targetUri)
        {
            return IsGitHubLike(targetUri.Host);
        }

        public static bool IsGitHubLike(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                return false;
            }

            // GitHub.com is trivially GitHub "like"
            if (IsGitHubDotCom(hostName))
            {
                return true;
            }

            string[] domains = hostName.Split('.');

            // github[.subdomain].domain.tld
            if (domains.Length >= 3 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "github"))
            {
                return true;
            }

            // gist.github[.subdomain].domain.tld
            if (domains.Length >= 4 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "gist") &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[1], "github"))
            {
                return true;
            }

            return false;
        }

        public static bool IsGitHubDotCom(Uri targetUri)
        {
            return IsGitHubDotCom(targetUri.Host);
        }

        public static bool IsGitHubDotCom(string hostName)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(hostName, GitHubConstants.GitHubBaseUrlHost) ||
                   StringComparer.OrdinalIgnoreCase.Equals(hostName, GitHubConstants.GistBaseUrlHost);
        }

        public static Uri NormalizeUri(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsGitHubLike(uri))
            {
                throw new ArgumentException(@"URI is not a GitHub style URL", nameof (uri));
            }

            // Special case for gist.github.com which are git backed repositories under the hood.
            // Credentials for these repositories are the same as the one stored with "github.com".
            // Same for gist.github[.subdomain].domain.tld.
            int firstDot = uri.DnsSafeHost.IndexOf(".", StringComparison.OrdinalIgnoreCase);
            if (firstDot > -1)
            {
                string firstPart = uri.DnsSafeHost.Substring(0, firstDot);
                if (StringComparer.OrdinalIgnoreCase.Equals(firstPart, "gist"))
                {
                    string secondPart = uri.DnsSafeHost.Substring(firstDot + 1);
                    var ub = new UriBuilder(uri) {Host = secondPart};
                    return ub.Uri;
                }
            }

            return uri;
        }

        public static bool TryGetRepositoryFromUrl(Uri uri, out string owner, out string repo)
        {
            owner = null;
            repo = null;

            if (!IsGitHubLike(uri))
            {
                return false;
            }

            string path = uri.AbsolutePath;
            string[] parts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                owner = parts[0];
                repo = parts[1];
                return true;
            }

            return false;
        }
    }
}
