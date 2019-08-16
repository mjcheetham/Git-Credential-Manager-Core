// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.TextDrawing
{
    public struct BoxPadding : IEquatable<BoxPadding>
    {
        public static readonly BoxPadding Zero = new BoxPadding();

        public BoxPadding(int v, int h) : this(v, h, v, h) { }

        public BoxPadding(int t, int r, int b, int l)
        {
            Top = t;
            Right = r;
            Bottom = b;
            Left = l;
        }

        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
        public readonly int Left;

        public bool Equals(BoxPadding other)
        {
            return Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;
        }

        public override bool Equals(object obj)
        {
            return obj is BoxPadding other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Top;
                hashCode = (hashCode * 397) ^ Right;
                hashCode = (hashCode * 397) ^ Bottom;
                hashCode = (hashCode * 397) ^ Left;
                return hashCode;
            }
        }
    }
}
