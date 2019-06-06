// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public class MacOSCrypto : ICryptography
    {
        public byte[] DecryptDataForCurrentUser(byte[] data)
        {
            return data;
        }

        public byte[] EncryptDataForCurrentUser(byte[] data)
        {
            return data;
        }
    }
}
