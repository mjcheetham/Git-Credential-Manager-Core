// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Security.Cryptography;

namespace Microsoft.Git.CredentialManager
{
    public enum CryptographicProtectionScope
    {
        CurrentUser,
        LocalMachine,
    }

    public interface ICryptography
    {
        byte[] Protect(byte[] bytes, CryptographicProtectionScope scope);

        byte[] Unprotect(byte[] bytes, CryptographicProtectionScope scope);
    }

    public class Cryptography : ICryptography
    {
        public byte[] Protect(byte[] bytes, CryptographicProtectionScope scope)
        {
#if NETFRAMEWORK
            return ProtectedData.Protect(bytes, null, ToDpapiScope(scope));
#else
            return bytes;
#endif
        }

        public byte[] Unprotect(byte[] bytes, CryptographicProtectionScope scope)
        {
#if NETFRAMEWORK
            return ProtectedData.Unprotect(bytes, null, ToDpapiScope(scope));
#else
            return bytes;
#endif
        }

#if NETFRAMEWORK
        private static DataProtectionScope ToDpapiScope(CryptographicProtectionScope scope)
        {
            switch (scope)
            {
                case CryptographicProtectionScope.CurrentUser:
                    return DataProtectionScope.CurrentUser;
                case CryptographicProtectionScope.LocalMachine:
                    return DataProtectionScope.LocalMachine;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }
#endif
    }
}
