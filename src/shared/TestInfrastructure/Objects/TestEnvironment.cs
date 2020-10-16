// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestEnvironment : IEnvironment
    {
        private readonly IEqualityComparer<string> _pathComparer;
        private readonly IEqualityComparer<string> _envarComparer;

        public string EnvPathSeparator { get; }

        public TestEnvironment(string envPathSeparator = null, IEqualityComparer<string> pathComparer = null, IEqualityComparer<string> envarComparer = null)
        {
            // Use the current platform separators and comparison types by default
            EnvPathSeparator = envPathSeparator ?? (PlatformUtils.IsWindows() ? ";" : ":");

            _envarComparer = envarComparer ??
                             (PlatformUtils.IsWindows()
                                 ? StringComparer.OrdinalIgnoreCase
                                 : StringComparer.Ordinal);

            _pathComparer = pathComparer ??
                            (PlatformUtils.IsLinux()
                                ? StringComparer.Ordinal
                                : StringComparer.OrdinalIgnoreCase);

            EnvPathSeparator = envPathSeparator;
            Variables = new Dictionary<string, string>(_envarComparer);
            WhichFiles = new Dictionary<string, ICollection<string>>(_pathComparer);
            Symlinks = new Dictionary<string, string>(_pathComparer);
        }

        public IDictionary<string, string> Variables { get; set; }

        public IDictionary<string, ICollection<string>> WhichFiles { get; set; }

        public IDictionary<string, string> Symlinks { get; set; }

        public IEnumerable<string> Path
        {
            get
            {
                if (Variables.TryGetValue("PATH", out string value))
                {
                    return value.Split(new[] {EnvPathSeparator}, StringSplitOptions.RemoveEmptyEntries);
                }

                return new string[0];
            }

            set => Variables["PATH"] = string.Join(EnvPathSeparator, value);
        }

        #region IEnvironment

        IReadOnlyDictionary<string, string> IEnvironment.Variables => new ReadOnlyDictionary<string, string>(Variables);

        bool IEnvironment.IsDirectoryOnPath(string directoryPath)
        {
            return Path.Any(x => _pathComparer.Equals(x, directoryPath));
        }

        public void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target)
        {
            Variables["PATH"] = string.Join(EnvPathSeparator, Path);
        }

        public void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target)
        {
            Variables["PATH"] = string.Join(EnvPathSeparator, Path);
        }

        public bool TryLocateExecutable(string program, out string path)
        {
            if (WhichFiles.TryGetValue(program, out ICollection<string> paths))
            {
                path = paths.First();
                return true;
            }

            if (!System.IO.Path.HasExtension(program) && PlatformUtils.IsWindows())
            {
                // If we're testing on a Windows platform, don't have a file extension, and were unable to locate
                // the executable file.. try appending .exe.
                path = WhichFiles.TryGetValue($"{program}.exe", out paths) ? paths.First() : null;
                return !(path is null);
            }

            path = null;
            return false;
        }

        #endregion
    }
}
