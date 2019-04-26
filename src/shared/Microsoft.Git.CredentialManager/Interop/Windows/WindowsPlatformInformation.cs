// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.Interop.Windows.Native;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsPlatformInformation : PlatformInformation
    {
        public WindowsPlatformInformation()
        {
            PlatformUtils.EnsureWindows();

            if (Shlwapi.IsOS(Shlwapi.OS_ANYSERVER))
            {
                OperatingSystemName = "Windows Server";
            }
            else
            {
                OperatingSystemName = "Windows";
            }

            OperatingSystemVersion = Environment.OSVersion.Version.ToString();
        }

        public override string OperatingSystemName { get; }

        public override string OperatingSystemVersion { get; }
    }
}
