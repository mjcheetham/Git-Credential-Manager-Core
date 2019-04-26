// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop.MacOS
{
    public class MacOSPlatformInformationTests
    {
        [Fact]
        public void MacOSPlatformInformation_TryReadVersionPlist_ValidPlist_ReturnsTrueAndVersion()
        {
            const string testPlistPath = "/tmp/version.plist";
            const string expectedVersion = "42.42.42";

            var plistText =
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
            <plist version=""1.0"">
                <dict>
                    <key>ProductBuildVersion</key>
                    <string>12A123</string>
                    <key>ProductCopyright</key>
                    <string>19xx-20xx Company Inc.</string>
                    <key>ProductName</key>
                    <string>Cool OS</string>
                    <key>ProductUserVisibleVersion</key>
                    <string>Domestic House Cat</string>
                    <key>ProductVersion</key>
                    <string>42.42.42</string>
                    <key>iOSSupportVersion</key>
                    <string>12.2</string>
                </dict>
            </plist>";

            var plistData = Encoding.UTF8.GetBytes(plistText);
            var fileSystem = new TestFileSystem
            {
                Files = {[testPlistPath] = new MemoryStream(plistData)}
            };

            var platformInfo = new MacOSPlatformInformation(fileSystem);

            bool result = platformInfo.TryReadVersionPlist(testPlistPath, out string actualVersion);

            Assert.True(result);
            Assert.Equal(expectedVersion, actualVersion);
        }

        [Fact]
        public void MacOSPlatformInformation_TryReadVersionPlist_MissingProductVersionKey_ReturnsFalse()
        {
            const string testPlistPath = "/tmp/version.plist";

            var plistText =
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
            <plist version=""1.0"">
                <dict>
                    <key>NotTheKeyYouAreLookingFor</key>
                    <string>MoveAlongMoveAlong</string>
                </dict>
            </plist>";

            var plistData = Encoding.UTF8.GetBytes(plistText);
            var fileSystem = new TestFileSystem
            {
                Files = {[testPlistPath] = new MemoryStream(plistData)}
            };

            var platformInfo = new MacOSPlatformInformation(fileSystem);

            bool result = platformInfo.TryReadVersionPlist(testPlistPath, out _);

            Assert.False(result);
        }

        [PlatformFact(Platform.MacOS)]
        public void MacOSPlatformInformation_TryReadVersionPlist_MissingPlist_ReturnsFalse()
        {
            const string testPlistPath = "/tmp/version.plist";

            var fileSystem = new TestFileSystem();

            var platformInfo = new MacOSPlatformInformation(fileSystem);

            bool result = platformInfo.TryReadVersionPlist(testPlistPath, out _);

            Assert.False(result);
        }
    }
}
