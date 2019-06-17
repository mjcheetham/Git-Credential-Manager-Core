// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop
{
    public static class U8StringConverter
    {
        // throwOnInvalidBytes: Be liberal in what we accept and conservative in what we return.
        private static readonly Encoding NativeEncoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        private static readonly Encoding ManagedEncoding = new UTF8Encoding(false, throwOnInvalidBytes: false);

        public static unsafe IntPtr ToNative(string str)
        {
            if (str == null)
            {
                return IntPtr.Zero;
            }

            int length = NativeEncoding.GetByteCount(str);
            var buffer = (byte*)Marshal.AllocHGlobal(length + 1).ToPointer();

            if (length > 0)
            {
                fixed (char* pValue = str)
                {
                    NativeEncoding.GetBytes(pValue, str.Length, buffer, length);
                }
            }
            buffer[length] = 0;

            return new IntPtr(buffer);
        }

        public static unsafe string FromNative(byte* buf)
        {
            byte* end = buf;

            if (buf == null)
            {
                return null;
            }

            if (*buf == '\0')
            {
                return string.Empty;
            }

            while (*end != '\0')
            {
                end++;
            }

            return new string((sbyte*)buf, 0, (int)(end - buf), ManagedEncoding);
        }
    }
}
