// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Interop.MacOS
{
    public class PlistTests
    {
        [Fact]
        public void Plist_TryParsePlist_ValidPlist_ReturnsTrueAndDictionary()
        {
            var expectedDict = new Dictionary<string, string>
            {
                ["Foo"] = "Bar",
                ["Soup"] = "Fish",
                ["Hello"] = "World",
            };

            var text =
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
            <plist version=""1.0"">
                <dict>
                    <key>Foo</key>
                    <string>Bar</string>
                    <key>Soup</key>
                    <string>Fish</string>
                    <key>Hello</key>
                    <string>World</string>
                </dict>
            </plist>";

            bool result;
            IDictionary<string, string> actualDict;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                result = Plist.TryParse(stream, out actualDict);
            }

            Assert.True(result);
            Assert.Equal(expectedDict, actualDict);
        }

        [Fact]
        public void Plist_TryParsePlist_InvalidPlist_ReturnsFalse()
        {
            var text =
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <notaplist>
                <huh>
                    <h2>Heading Level Two!</h2>
                </huh>
            </notaplist>";

            bool result;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                result = Plist.TryParse(stream, out _);
            }

            Assert.False(result);
        }
    }
}
