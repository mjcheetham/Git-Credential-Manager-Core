// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestFileSystem : IFileSystem
    {
        public IDictionary<string, Stream> Files { get; set; }
        public ISet<string> Directories { get; set; }
        public string CurrentDirectory { get; set; } = Path.GetTempPath();

        #region IFileSystem

        bool IFileSystem.FileExists(string path)
        {
            return Files.ContainsKey(path);
        }

        bool IFileSystem.DirectoryExists(string path)
        {
            return Directories.Contains(path);
        }

        string IFileSystem.GetCurrentDirectory()
        {
            return CurrentDirectory;
        }

        Stream IFileSystem.OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return Files[path];
        }

        public void CreateDirectory(string path)
        {
            Directories.Add(path);
        }

        #endregion
    }
}
