// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Linq;
using Microsoft.Git.CredentialManager.TextDrawing;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.TextDrawing
{
    public class BoxContentTests
    {
        [Fact]
        public void BoxContent_GetWrappedLines_SplitOnExistingWhiteSpace()
        {
            string text = "this is some text that is short";

            string[] expectedLines =
            {
                "this is",
                "some",
                "text",
                "that is",
                "short"
            };

            var boxContent = new BoxContent(text);

            string[] actualLines = boxContent.GetWrappedLines(7).ToArray();

            Assert.Equal(expectedLines, actualLines);
        }

        [Fact]
        public void BoxContent_GetWrappedLines_NoWhiteSpaceSplit()
        {
            string text = "this-is-some-text-that-has-no-white-space-or-obvious-split-points";

            string[] expectedLines =
            {
                "this-is-so",
                "me-text-th",
                "at-has-no-",
                "white-spac",
                "e-or-obvio",
                "us-split-p",
                "oints"
            };

            var boxContent = new BoxContent(text);

            string[] actualLines = boxContent.GetWrappedLines(10).ToArray();

            Assert.Equal(expectedLines, actualLines);
        }

        [Fact]
        public void BoxContent_GetWrappedLines_NewLinesAlreadyShort()
        {
            string text = "this is some text that\n" +
                          "already has line breaks\n" +
                          "where each line is short\n" +
                          "enough not to be split.";

            string[] expectedLines =
            {
                "this is some text that",
                "already has line breaks",
                "where each line is short",
                "enough not to be split."
            };

            var boxContent = new BoxContent(text);

            string[] actualLines = boxContent.GetWrappedLines(25).ToArray();

            Assert.Equal(expectedLines, actualLines);
        }

        [Fact]
        public void BoxContent_GetWrappedLines_NewLinesTooLong()
        {
            string text = "this is some text that already has line breaks\n" +
                          "but there are some lines\n" +
                          "that are short enough not to be split, and also some\n" +
                          "lines that are.";

            string[] expectedLines =
            {
                "this is some text that",
                "already has line breaks",
                "but there are some lines",
                "that are short enough not",
                "to be split, and also",
                "some",
                "lines that are."
            };

            var boxContent = new BoxContent(text);

            string[] actualLines = boxContent.GetWrappedLines(25).ToArray();

            Assert.Equal(expectedLines, actualLines);
        }
    }
}
