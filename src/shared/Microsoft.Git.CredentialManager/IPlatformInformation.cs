// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents information about the platform (OS and runtime).
    /// </summary>
    public interface IPlatformInformation
    {
        string ApplicationVersion { get; }

        string ApplicationCommit { get; }

        string OperatingSystemName { get; }

        string OperatingSystemVersion { get; }

        string RuntimeName { get; }

        string RuntimeVersion { get; }

        string CpuArchitecture { get; }
    }
}
