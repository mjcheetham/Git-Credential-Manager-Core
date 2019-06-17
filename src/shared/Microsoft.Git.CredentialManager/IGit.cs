// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public interface IGitConfiguration : IDisposable
    {
        /// <summary>
        /// Path to repository if this configuration object is also scoped to a repository, otherwise null.
        /// </summary>
        string RepositoryPath { get; }

        /// <summary>
        /// Get the value of a configuration entry as a string, or null if not found.
        /// Equivalent to <seealso cref="GetString"/>.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        string this[string name] { get; }

        /// <summary>
        /// Get the value of a configuration entry as a string, or null if not found.
        /// </summary>
        /// <param name="name">Configuration entry name.</param>
        /// <returns>Configuration entry value, or null if not found.</returns>
        string GetString(string name);
    }

    public interface IGit : IDisposable
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system, user, and optionally a specified repository.
        /// </summary>
        /// <param name="repositoryPath">Optional repository path from which to load local configuration.</param>
        /// <returns>Git configuration snapshot.</returns>
        IGitConfiguration GetConfiguration(string repositoryPath);

        /// <summary>
        /// Resolve the given path to a containing repository, or null if the path is not inside a Git repository.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Git repository root path, or null if <paramref name="path"/> is not inside of a Git repository.</returns>
        string GetRepositoryPath(string path);
    }

    public static class GitExtensions
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system and user.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>Git configuration snapshot.</returns>
        public static IGitConfiguration GetConfiguration(this IGit git) => git.GetConfiguration(null);

        /// <summary>
        /// Get the value of a configuration entry as a string, or null if not found.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="property">Configuration property name.</param>
        /// <returns>Configuration entry value, or null if not found.</returns>
        public static string GetString(this IGitConfiguration config, string section, string property)
        {
            return config.GetString($"{section}.{property}");
        }

        /// <summary>
        /// Get the value of a scoped configuration entry as a string, or null if not found.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <param name="section">Configuration section name.</param>
        /// <param name="scope">Configuration section scope.</param>
        /// <param name="property">Configuration property name.</param>
        /// <returns>Configuration entry value, or null if not found.</returns>
        public static string GetString(this IGitConfiguration config, string section, string scope, string property)
        {
            if (scope is null)
            {
                return config.GetString(section, property);
            }

            return config.GetString($"{section}.{scope}.{property}");
        }
    }
}
