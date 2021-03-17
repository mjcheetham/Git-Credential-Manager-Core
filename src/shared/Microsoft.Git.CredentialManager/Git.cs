// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IGit
    {
        /// <summary>
        /// Get the version of Git.
        /// </summary>
        /// <returns>Git version.</returns>
        GitVersion Version { get; }

        /// <summary>
        /// Return the path to the current repository, or null if this instance is not
        /// scoped to a Git repository.
        /// </summary>
        /// <returns>Absolute path to the current Git repository, or null.</returns>
        string GetCurrentRepository();

        /// <summary>
        /// Get all remotes for the current repository.
        /// </summary>
        /// <returns>Names of all remotes in the current repository.</returns>
        IEnumerable<GitRemote> GetRemotes();

        /// <summary>
        /// Get the configuration object.
        /// </summary>
        /// <returns>Git configuration.</returns>
        IGitConfiguration GetConfiguration();

        /// <summary>
        /// Run a Git helper process which expects and returns key-value maps
        /// </summary>
        /// <param name="args">Arguments to the executable</param>
        /// <param name="standardInput">key-value map to pipe into stdin</param>
        /// <returns>stdout from helper executable as key-value map</returns>
        Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput);
    }

    public sealed class GitVersion : IComparable<GitVersion>, IEquatable<GitVersion>
    {
        public static bool TryParse(string str, out GitVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(str)) return false;

            string[] parts = str.Split('.');
            if (parts.Length < 3) return false;

            if (!int.TryParse(parts[0], out int major)) return false;
            if (!int.TryParse(parts[1], out int minor)) return false;
            if (!int.TryParse(parts[2], out int patch)) return false;

            version = new GitVersion(major, minor, patch);
            return true;
        }

        public GitVersion(int major, int minor, int patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public bool Equals(GitVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is GitVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                return hashCode;
            }
        }

        public int CompareTo(GitVersion other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var majorCmp = Major.CompareTo(other.Major);
            if (majorCmp != 0) return majorCmp;
            var minorCmp = Minor.CompareTo(other.Minor);
            if (minorCmp != 0) return minorCmp;
            return Patch.CompareTo(other.Patch);
        }

        public static bool operator <(GitVersion a, GitVersion b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(GitVersion a, GitVersion b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.CompareTo(b) > 0;
        }

        public static bool operator ==(GitVersion a, GitVersion b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.CompareTo(b) == 0;
        }

        public static bool operator >=(GitVersion a, GitVersion b) => !(a < b);

        public static bool operator <=(GitVersion a, GitVersion b) => !(a > b);

        public static bool operator !=(GitVersion a, GitVersion b) => !(a == b);
    }

    public class GitRemote
    {
        public GitRemote(string name, string fetchUrl, string pushUrl)
        {
            Name = name;
            FetchUrl = fetchUrl;
            PushUrl = pushUrl;
        }

        public string Name { get; }
        public string FetchUrl { get; }
        public string PushUrl { get; }
    }

    public class GitProcess : IGit
    {
        private readonly ITrace _trace;
        private readonly string _gitPath;
        private readonly string _workingDirectory;

        private GitVersion _version;

        public GitProcess(ITrace trace, string gitPath, string workingDirectory = null)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNullOrWhiteSpace(gitPath, nameof(gitPath));

            _trace = trace;
            _gitPath = gitPath;
            _workingDirectory = workingDirectory;
        }

        public GitVersion Version => _version ?? (_version = GetVersion());

        public IGitConfiguration GetConfiguration()
        {
            return new GitProcessConfiguration(_trace, this);
        }

        public string GetCurrentRepository()
        {
            using (var git = CreateProcess("rev-parse --absolute-git-dir"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        return data.TrimEnd();
                    case 128: // Not inside a Git repository
                        return null;
                    default:
                        _trace.WriteLine($"Failed to get current Git repository (exit={git.ExitCode})");
                        throw CreateGitException(git, "Failed to get current Git repository");
                }
            }
        }

        public IEnumerable<GitRemote> GetRemotes()
        {
            using (var git = CreateProcess("remote -v show"))
            {
                git.Start();
                // To avoid deadlocks, always read the output stream first and then wait
                // TODO: don't read in all the data at once; stream it
                string data = git.StandardOutput.ReadToEnd();
                string stderr = git.StandardError.ReadToEnd();
                git.WaitForExit();

                switch (git.ExitCode)
                {
                    case 0: // OK
                        break;
                    case 128 when stderr.Contains("not a git repository"): // Not inside a Git repository
                        yield break;
                    default:
                        _trace.WriteLine($"Failed to enumerate Git remotes (exit={git.ExitCode})");
                        throw CreateGitException(git, "Failed to enumerate Git remotes");
                }

                string[] lines = data.Split('\n');

                // Remotes are always output in groups of two (fetch and push)
                for (int i = 0; i + 1 < lines.Length; i += 2)
                {
                    // The fetch URL is written first, followed by the push URL
                    string[] fetchLine = lines[i].Split();
                    string[] pushLine = lines[i + 1].Split();

                    // Remote name is always first (and should match between fetch/push)
                    string remoteName = fetchLine[0];

                    // The next part, if present, is the URL
                    string fetchUrl = null;
                    string pushUrl = null;
                    if (fetchLine.Length > 1 && !string.IsNullOrWhiteSpace(fetchLine[1])) fetchUrl = fetchLine[1].TrimEnd();
                    if (pushLine.Length > 1 && !string.IsNullOrWhiteSpace(pushLine[1]))   pushUrl  = pushLine[1].TrimEnd();

                    yield return new GitRemote(remoteName, fetchUrl, pushUrl);
                }
            }
        }

        public Process CreateProcess(string args)
        {
            var psi = new ProcessStartInfo(_gitPath, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = _workingDirectory
            };

            return new Process {StartInfo = psi};
        }

        // This code was originally copied from
        // src/shared/Microsoft.Git.CredentialManager/Authentication/AuthenticationBase.cs
        // That code is for GUI helpers in this codebase, while the below is for
        // communicating over Git's stdin/stdout helper protocol. The GUI helper
        // protocol will one day use a different IPC mechanism, whereas this code
        // has to follow what upstream Git does.
        public async Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput = null)
        {
            var procStartInfo = new ProcessStartInfo(_gitPath)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            var process = Process.Start(procStartInfo);
            if (process is null)
            {
                throw new Exception($"Failed to start Git helper '{args}'");
            }

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
                // some helpers won't continue until they see EOF
                // cf git-credential-cache
                process.StandardInput.Close();
            }

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit());
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                if (!resultDict.TryGetValue("error", out string errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
        }

        private GitVersion GetVersion()
        {
            using (var git = CreateProcess("--version"))
            {
                git.Start();
                string data = git.StandardOutput.ReadToEnd();
                git.WaitForExit();

                if (git.ExitCode != 0)
                {
                    _trace.WriteLine($"Failed to get Git version (exit={git.ExitCode})");
                    throw CreateGitException(git, "Failed to get Git version");
                }

                string versionStr = data.TrimEnd();

                if (!GitVersion.TryParse(versionStr, out var version))
                {
                    throw new Exception($"Failed to parse Git version: '{versionStr}'");
                }

                return version;
            }
        }

        public static GitException CreateGitException(Process git, string message)
        {
            string gitMessage = git.StandardError.ReadToEnd();
            throw new GitException(message, gitMessage, git.ExitCode);
        }
    }

    public class GitException : Exception
    {
        public string GitErrorMessage { get; }

        public int ExitCode { get; }

        public GitException(string message, string gitErrorMessage, int exitCode)
            : base(message)
        {
            GitErrorMessage = gitErrorMessage;
            ExitCode = exitCode;
        }
    }

    public static class GitExtensions
    {
        /// <summary>
        /// Returns true if the current Git instance is scoped to a local repository.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>True if inside a local Git repository, false otherwise.</returns>
        public static bool IsInsideRepository(this IGit git)
        {
            return !string.IsNullOrWhiteSpace(git.GetCurrentRepository());
        }
    }
}
