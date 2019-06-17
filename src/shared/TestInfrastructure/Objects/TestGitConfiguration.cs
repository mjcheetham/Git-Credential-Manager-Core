using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGitConfiguration : IGitConfiguration
    {
        public string RepositoryPath { get; set; }

        public IDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

        #region IGitConfiguration

        string IGitConfiguration.RepositoryPath => RepositoryPath;

        string IGitConfiguration.this[string name] => ((IGitConfiguration)this).GetString(name);

        string IGitConfiguration.GetString(string name)
        {
            return Values.TryGetValue(name, out string value) ? value : null;
        }

        void IDisposable.Dispose() { }

        #endregion
    }
}
