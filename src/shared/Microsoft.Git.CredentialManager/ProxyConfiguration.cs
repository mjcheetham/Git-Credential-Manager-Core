// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public class ProxyConfiguration
    {
        public ProxyConfiguration(Uri proxyUri, string bypassHosts = null, bool isDeprecatedSource = false)
        {
            Uri = proxyUri;
            BypassHosts = bypassHosts;
            IsDeprecatedSource = isDeprecatedSource;
        }

        /// <summary>
        /// Proxy URI including any basic HTTP authentication information.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// True if the proxy configuration source is deprecated, false otherwise.
        /// </summary>
        public bool IsDeprecatedSource { get; }

        /// <summary>
        /// Comma-separated list of hosts to bypass the proxy.
        /// </summary>
        public string BypassHosts { get; }
    }
}
