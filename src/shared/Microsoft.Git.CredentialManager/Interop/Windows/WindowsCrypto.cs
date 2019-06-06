// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Security.Cryptography;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsCrypto : ICryptography
    {
        public byte[] DecryptDataForCurrentUser(byte[] data)
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        }

        public byte[] EncryptDataForCurrentUser(byte[] data)
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }
    }
}
