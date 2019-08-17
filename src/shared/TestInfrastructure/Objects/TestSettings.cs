// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestSettings : ISettings
    {
        public IDictionary<string, string> EnvironmentValues { get; set; }
            = new Dictionary<string, string>();

        public IDictionary<(string section, string property), IEnumerable<string>> GitConfigurationValues { get; set; }
            = new Dictionary<(string section, string property), IEnumerable<string>>();

        public bool IsDebuggingEnabled { get; set; }

        public bool IsTerminalPromptsEnabled { get; set; } = true;

        public string Trace { get; set; }

        public bool IsSecretTracingEnabled { get; set; }

        public bool IsMsalTracingEnabled { get; set; }

        public string ProviderOverride { get; set; }

        public string LegacyAuthorityOverride { get; set; }

        public bool IsWindowsIntegratedAuthenticationEnabled { get; set; } = true;

        public bool IsCertificateVerificationEnabled { get; set; } = true;

        public Uri ProxyConfiguration { get; set; }

        public bool IsDeprecatedProxyConfiguration { get; set; }

        #region ISettings

        public string RepositoryPath { get; set; }

        public Uri RemoteUri { get; set; }

        bool ISettings.TryGetEnvironmentValue(string name, out string value)
        {
            return EnvironmentValues.TryGetValue(name, out value);
        }

        IEnumerable<string> ISettings.GetGitConfigurationValues(string section, string property)
        {
            if (GitConfigurationValues.TryGetValue((section, property), out IEnumerable<string> values))
            {
                return values;
            }

            return Enumerable.Empty<string>();
        }

        bool ISettings.IsDebuggingEnabled => IsDebuggingEnabled;

        bool ISettings.IsTerminalPromptsEnabled => IsTerminalPromptsEnabled;

        bool ISettings.GetTracingEnabled(out string value)
        {
            value = Trace;
            return Trace != null;
        }

        bool ISettings.IsSecretTracingEnabled => IsSecretTracingEnabled;

        bool ISettings.IsMsalTracingEnabled => IsMsalTracingEnabled;

        string ISettings.ProviderOverride => ProviderOverride;

        string ISettings.LegacyAuthorityOverride => LegacyAuthorityOverride;

        bool ISettings.IsWindowsIntegratedAuthenticationEnabled => IsWindowsIntegratedAuthenticationEnabled;

        bool ISettings.IsCertificateVerificationEnabled => IsCertificateVerificationEnabled;

        Uri ISettings.GetProxyConfiguration(out bool isDeprecatedConfiguration)
        {
            isDeprecatedConfiguration = IsDeprecatedProxyConfiguration;
            return ProxyConfiguration;
        }

        #endregion
    }
}
