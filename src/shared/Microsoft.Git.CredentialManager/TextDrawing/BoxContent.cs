// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.TextDrawing
{
    public class BoxContent
    {
        public BoxContent(string innerText,
            HorizontalAlignment hAlign = HorizontalAlignment.Left)
        {
            InnerText = innerText;
            HorizontalAlignment = hAlign;
        }

        public string InnerText { get; set; }

        public HorizontalAlignment HorizontalAlignment { get; set; }

        public IEnumerable<string> GetWrappedLines(int maxWidth)
        {
            var lines = new List<string>(
                InnerText.Split(new[] {"\n", "\r\n"}, StringSplitOptions.None)
            );

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (line.Length > maxWidth)
                {
                    SplitLine(line, maxWidth, out string first, out string second);
                    yield return first;

                    // Add the second line back into the candidate list (it may still be too long)
                    lines.Insert(i + 1, second);
                }
                else
                {
                    yield return line;
                }
            }
        }

        private void SplitLine(string line, int maxLength, out string first, out string second)
        {
            first = line.Substring(0, maxLength);

            // Check if we did a simple maxLength split we'd split on a whitespace already
            if (line.Length > maxLength + 1 && char.IsWhiteSpace(line[maxLength]))
            {
                // Split the second part after the whitespace character we have split on
                second = line.Substring(maxLength + 1);
                return;
            }

            // Try and find a whitespace character before maxWidth to split on
            int whiteSpace = Array.FindLastIndex(first.ToCharArray(), char.IsWhiteSpace);
            if (whiteSpace > 0)
            {
                first = line.Substring(0, whiteSpace);
                second = line.Substring(whiteSpace + 1); // +1 to skip the whitespace character we are splitting on
                return;
            }

            // Do a simple split at the max length
            second = line.Substring(maxLength);
        }
    }
}
