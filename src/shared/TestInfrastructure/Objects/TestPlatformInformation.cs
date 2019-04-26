// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestPlatformInformation : IPlatformInformation
    {
        public string ApplicationVersion { get; set; } = "2.0";
        public string ApplicationCommit { get; set; } = "0123456789abcdef0123456789abcdef01234567";
        public string OperatingSystemName { get; set; } = "TestOS";
        public string OperatingSystemVersion { get; set; } = "1.0";
        public string RuntimeName { get; set; } = "TestRuntime";
        public string RuntimeVersion { get; set; } = "1.0";
        public string CpuArchitecture { get; set; } = "TestArch";
    }
}
