// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Linq;

namespace Microsoft.Git.CredentialManager.TextDrawing
{
    public class Box
    {
        public BoxPadding Padding { get; set; } = BoxPadding.Zero;
        public BoxBorder Border { get; set; } = BoxBorder.None;
        public BoxContent Content { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

        public void Draw(TextWriter writer, int canvasWidth)
        {
            // Calculate the maximum allowed width for the inner content
            int maxContentWidth = canvasWidth - Padding.Left - Padding.Right - Border.Thickness * 2;

            // Wrap the content into lines that are bounded by the maximum allowed content width
            string[] contentLines = Content.GetWrappedLines(maxContentWidth).ToArray();

            // Calculate the actual width of the content
            int contentWidth = contentLines.Max(x => x.Length);

            // Calculate the width of the box up-to and include the border
            int boxWidth = contentWidth + Padding.Left + Padding.Right + Border.Thickness * 2;

            // Calculate how much slack around the box (including border) we have to the canvas edge
            int slackWidth = canvasWidth - boxWidth;

            // Split the slack width into left and right margins depending on desired alignment
            string lMargin;
            string rMargin;
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    lMargin = string.Empty;
                    rMargin = new string(' ', slackWidth);
                    break;
                case HorizontalAlignment.Center:
                    lMargin = new string(' ', slackWidth / 2);
                    rMargin = lMargin;
                    break;
                case HorizontalAlignment.Right:
                    lMargin = new string(' ', slackWidth);
                    rMargin = string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var lPad = new string(' ', Padding.Left);
            var rPad = new string(' ', Padding.Right);

            var emptyContentLine = new string(' ', contentWidth);

            void DrawEmptyContentLines(int numLines)
            {
                for (int i = 0; i < numLines; i++)
                {
                    writer.Write(lMargin);
                    Border.DrawEdge(writer);
                    writer.Write(lPad);
                    writer.Write(emptyContentLine);
                    writer.Write(rPad);
                    Border.DrawEdge(writer);
                    writer.Write(rMargin);
                    writer.WriteLine();
                }
            }

            string GetAlignedContentLine(string line)
            {
                switch (Content.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        return line.PadRight(contentWidth);
                    case HorizontalAlignment.Center:
                        int slack = contentWidth - line.Length;
                        return line.PadLeft(line.Length + slack / 2)
                                   .PadRight(contentWidth);
                    case HorizontalAlignment.Right:
                        return line.PadLeft(contentWidth);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Draw top border
            if (Border.Thickness > 0)
            {
                writer.Write(lMargin);
                Border.DrawTop(writer, boxWidth);
                writer.Write(rMargin);
                writer.WriteLine();
            }

            // Draw top padding
            DrawEmptyContentLines(Padding.Top);

            // Draw content
            foreach (string line in contentLines)
            {
                writer.Write(lMargin);
                Border.DrawEdge(writer);
                writer.Write(lPad);
                writer.Write(GetAlignedContentLine(line));
                writer.Write(rPad);
                Border.DrawEdge(writer);
                writer.Write(rMargin);
                writer.WriteLine();
            }

            // Draw bottom padding
            DrawEmptyContentLines(Padding.Top);

            // Draw bottom border
            if (Border.Thickness > 0)
            {
                writer.Write(lMargin);
                Border.DrawBottom(writer, boxWidth);
                writer.Write(rMargin);
                writer.WriteLine();
            }
        }
    }
}
