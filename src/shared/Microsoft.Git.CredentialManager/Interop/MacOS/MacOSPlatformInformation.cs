// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public class MacOSPlatformInformation : PlatformInformation
    {
        private const string ServerVersionPlistPath = "/System/Library/CoreServices/ServerVersion.plist";
        private const string ClientVersionPlistPath = "/System/Library/CoreServices/SystemVersion.plist";

        private readonly IFileSystem _fileSystem;

        public MacOSPlatformInformation(IFileSystem fileSystem)
        {
            PlatformUtils.EnsureMacOS();
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));

            _fileSystem = fileSystem;

            string osName;
            string osVersion;

            if (TryReadVersionPlist(ServerVersionPlistPath, out osVersion))
            {
                osName = "macOS Server";
            }
            else if (TryReadVersionPlist(ClientVersionPlistPath, out osVersion))
            {
                osName = "macOS";
            }
            else
            {
                // Unable to get version
                osName = "macOS";
                osVersion = "?";
            }

            OperatingSystemName = osName;
            OperatingSystemVersion = osVersion;
        }

        public override string OperatingSystemName { get; }

        public override string OperatingSystemVersion { get; }

        public bool TryReadVersionPlist(string path, out string version)
        {
            // Rather than shelling out to `sw_vers` or `defaults` to get the OS version,
            // just read the plist file that `sw_vers` itself reads.
            // Note that we also cannot use `uname` because it's version info is about
            // the Darwin kernel, not the 'marketing name' of macOS.
            //
            // Example version plist:
            //
            // <plist version="1.0">
            //     <dict>
            //         <key>ProductBuildVersion</key>
            //         <string>18E226</string>
            //         <key>ProductCopyright</key>
            //         <string>1983-2019 Apple Inc.</string>
            //         <key>ProductName</key>
            //         <string>Mac OS X</string>
            //         <key>ProductUserVisibleVersion</key>
            //         <string>10.14.4</string>
            //         <key>ProductVersion</key>
            //         <string>10.14.4</string>
            //         <key>iOSSupportVersion</key>
            //         <string>12.2</string>
            //     </dict>
            // </plist>

            version = null;

            if (_fileSystem.FileExists(path))
            {
                var stream = _fileSystem.OpenFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (stream)
                {
                    return Plist.TryParse(stream, out var dict) && dict.TryGetValue("ProductVersion", out version);
                }
            }

            return false;
        }
    }
}
