// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.TextDrawing;

namespace Microsoft.Git.CredentialManager
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendBox(this StringBuilder sb, Box box, int maxWidth)
        {
            using (var writer = new StringWriter(sb))
            {
                box.Draw(writer, maxWidth);
            }

            return sb;
        }

        public static StringBuilder InsertLinePrefix(this StringBuilder sb, string prefix)
        {
            int prefixLength = prefix?.Length ?? 0;

            sb.Insert(0, prefix);

            for (int i = prefixLength; i < sb.Length; i++)
            {
                if (sb[i] == '\n')
                {
                    sb.Insert(i + 1, prefix);
                    i += prefixLength;
                }
            }

            return sb;
        }
    }
}
