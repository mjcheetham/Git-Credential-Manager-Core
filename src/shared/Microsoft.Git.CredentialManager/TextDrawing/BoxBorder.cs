// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;

namespace Microsoft.Git.CredentialManager.TextDrawing
{
    public class BoxBorder
    {
        public static readonly BoxBorder None = new BoxBorder();
        public static readonly BoxBorder Thin = new BoxBorder('─', '│', '┌', '┐', '┘', '└');
        public static readonly BoxBorder Thick = new BoxBorder('━', '┃', '┏', '┓', '┛', '┗');

        private BoxBorder() {}

        private BoxBorder(char h, char v, char tl, char tr, char br, char bl)
        {
            Horizontal = h;
            Vertical = v;
            TopLeft = tl;
            TopRight = tr;
            BottomRight = br;
            BottomLeft = bl;
        }

        public char Horizontal { get; set; }

        public char Vertical { get; set; }

        public char TopLeft { get; set; }

        public char TopRight { get; set; }

        public char BottomRight { get; set; }

        public char BottomLeft { get; set; }

        public int Thickness => ReferenceEquals(this, None) ? 0 : 1;

        public void DrawTop(TextWriter writer, int length)
        {
            if (Thickness > 0 && length > 0)
            {
                if (length == 1)
                {
                    writer.Write(Horizontal);
                }
                else
                {
                    string line = string.Empty;
                    if (length > 2)
                    {
                        line = new string(Horizontal, length - 2);
                    }

                    writer.Write(TopLeft);
                    writer.Write(line);
                    writer.Write(TopRight);
                }
            }
        }

        public void DrawEdge(TextWriter writer)
        {
            if (Thickness > 0)
            {
                writer.Write(Vertical);
            }
        }

        public void DrawBottom(TextWriter writer, int length)
        {
            if (Thickness > 0 && length > 0)
            {
                if (length == 1)
                {
                    writer.Write(Horizontal);
                }
                else
                {
                    string line = string.Empty;
                    if (length > 2)
                    {
                        line = new string(Horizontal, length - 2);
                    }

                    writer.Write(BottomLeft);
                    writer.Write(line);
                    writer.Write(BottomRight);
                }
            }
        }
    }
}
