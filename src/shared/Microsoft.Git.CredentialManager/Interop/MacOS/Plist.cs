// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public static class Plist
    {
        public static bool TryParse(Stream stream, out IDictionary<string, string> plist)
        {
            // Example plist XML:
            //
            // <?xml version="1.0" encoding="UTF-8"?>
            // <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            // <plist version="1.0">
            //     <dict>
            //         <key>Foo</key>
            //         <string>Bar</string>
            //     </dict>
            // </plist>
            try
            {
                var xdoc = XDocument.Load(stream);

                // Ensure this is a plist
                if (xdoc.Root?.Name != "plist")
                {
                    plist = null;
                    return false;
                }

                var elements = xdoc.XPathSelectElements("//plist/dict/*").ToArray();

                plist = elements.Select(x => x.Value).TakePairs().ToDictionary(x => x.Item1, x => x.Item2);
                return true;
            }
            catch (Exception)
            {
                plist = null;
                return false;
            }
        }
    }
}
