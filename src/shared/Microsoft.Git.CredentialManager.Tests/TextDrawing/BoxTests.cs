// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.TextDrawing;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.TextDrawing
{
    public class BoxTests
    {
        [Fact]
        public void Box_Draw_ThinBorder_Padding_AlignContentCenter_LongContent()
        {
            string text = "This is a test which draws a box with a thin border, containing text that is centered in " +
                          "both the horizontal and vertical directions. The box has an outer margin and an inner padding.\n" +
                          "\n" +
                          "This is the second paragraph in the box.";

            var box = new Box
            {
                Border = BoxBorder.Thin,
                Content = new BoxContent(text)
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Padding = new BoxPadding(1, 4)
            };

            string expectedOutput =
                "┌─────────────────────────────────────────────────────────────────────┐ \n" +
                "│                                                                     │ \n" +
                "│        This is a test which draws a box with a thin border,         │ \n" +
                "│     containing text that is centered in both the horizontal and     │ \n" +
                "│    vertical directions. The box has an outer margin and an inner    │ \n" +
                "│                              padding.                               │ \n" +
                "│                                                                     │ \n" +
                "│              This is the second paragraph in the box.               │ \n" +
                "│                                                                     │ \n" +
                "└─────────────────────────────────────────────────────────────────────┘ \n";

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                box.Draw(writer, 72);
            }

            string actualOutput = sb.ToString();
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void Box_Draw_ThinBorder_AlignLeft_Padding_ShortContent()
        {
            string text = "This is short content.";

            var box = new Box
            {
                Border = BoxBorder.Thin,
                Content = new BoxContent(text)
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Padding = new BoxPadding(1, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            string expectedOutput =
                "┌──────────────────────────────┐                                        \n" +
                "│                              │                                        \n" +
                "│    This is short content.    │                                        \n" +
                "│                              │                                        \n" +
                "└──────────────────────────────┘                                        \n";

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                box.Draw(writer, 72);
            }

            string actualOutput = sb.ToString();
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void Box_Draw_ThinBorder_AlignCenter_Padding_ShortContent()
        {
            string text = "This is short content.";

            var box = new Box
            {
                Border = BoxBorder.Thin,
                Content = new BoxContent(text)
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Padding = new BoxPadding(1, 4),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            string expectedOutput =
                "                    ┌──────────────────────────────┐                    \n" +
                "                    │                              │                    \n" +
                "                    │    This is short content.    │                    \n" +
                "                    │                              │                    \n" +
                "                    └──────────────────────────────┘                    \n";

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                box.Draw(writer, 72);
            }

            string actualOutput = sb.ToString();
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void Box_Draw_ThinBorder_AlignRight_Padding_ShortContent()
        {
            string text = "This is short content.";

            var box = new Box
            {
                Border = BoxBorder.Thin,
                Content = new BoxContent(text)
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                Padding = new BoxPadding(1, 4),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            string expectedOutput =
                "                                        ┌──────────────────────────────┐\n" +
                "                                        │                              │\n" +
                "                                        │    This is short content.    │\n" +
                "                                        │                              │\n" +
                "                                        └──────────────────────────────┘\n";

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                box.Draw(writer, 72);
            }

            string actualOutput = sb.ToString();
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
