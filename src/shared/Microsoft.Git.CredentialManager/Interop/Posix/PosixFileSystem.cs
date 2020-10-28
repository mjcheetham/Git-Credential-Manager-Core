// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;

namespace Microsoft.Git.CredentialManager.Interop.Posix
{
    public class PosixFileSystem : FileSystem
    {
        private readonly bool _isCaseSensitive;

        public PosixFileSystem()
        {
            // Compute this now and save the result for future use
            _isCaseSensitive = IsCaseSensitive();
        }

        public override bool IsSamePath(string a, string b)
        {
            // TODO: resolve symlinks
            a = Path.GetFileName(a);
            b = Path.GetFileName(b);

            return _isCaseSensitive
                ? StringComparer.Ordinal.Equals(a, b)
                : StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }

        private static bool IsCaseSensitive()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string fileLower = Path.Combine(tempDir, "file");
            string fileUpper = Path.Combine(tempDir, "FILE");
            Directory.CreateDirectory(tempDir);
            File.Create(fileLower);
            return !File.Exists(fileUpper);
        }
    }
}
