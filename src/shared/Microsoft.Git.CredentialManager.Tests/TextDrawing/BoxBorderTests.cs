// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.TextDrawing;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.TextDrawing
{
    public class BoxBorderTests
    {
        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "─")]
        [InlineData(2, "┌┐")]
        [InlineData(3, "┌─┐")]
        [InlineData(4, "┌──┐")]
        [InlineData(10, "┌────────┐")]
        public void BoxBorder_DrawTop(int length, string expected)
        {
            var border = BoxBorder.Thin;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawTop(writer, length);
            }

            string actual = sb.ToString();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "─")]
        [InlineData(2, "└┘")]
        [InlineData(3, "└─┘")]
        [InlineData(4, "└──┘")]
        [InlineData(10, "└────────┘")]
        public void BoxBorder_DrawBottom(int length, string expected)
        {
            var border = BoxBorder.Thin;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawBottom(writer, length);
            }

            string actual = sb.ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BoxBorder_DrawEdge()
        {
            var border = BoxBorder.Thin;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawEdge(writer);
            }

            string actual = sb.ToString();
            Assert.Equal("│", actual);
        }

        [Fact]
        public void BoxBorder_None_DrawTop_WritesNothing()
        {
            var border = BoxBorder.None;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawTop(writer, 5);
            }

            string actual = sb.ToString();
            Assert.Equal(string.Empty, actual);
        }

        [Fact]
        public void BoxBorder_None_DrawBottom_WritesNothing()
        {
            var border = BoxBorder.None;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawBottom(writer, 5);
            }

            string actual = sb.ToString();
            Assert.Equal(string.Empty, actual);
        }

        [Fact]
        public void BoxBorder_None_DrawEdge_WritesNothing()
        {
            var border = BoxBorder.None;

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                border.DrawEdge(writer);
            }

            string actual = sb.ToString();
            Assert.Equal(string.Empty, actual);
        }
    }
}
