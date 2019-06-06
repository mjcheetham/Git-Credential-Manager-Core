// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using Microsoft.Identity.Client;

namespace Microsoft.Git.CredentialManager.Authentication
{
    /// <summary>
    /// Custom token cache which will find and use the latest installed version of Visual Studio's MSAL and/or ADAL caches.
    /// </summary>
    /// <remarks>
    /// Sharing the VS token caches will help reduce the number of sign-in prompts by enabling re-use of stored access tokens.
    /// This can be useful when Git is being used from both VS Team Explorer and the command line.
    /// </remarks>
    internal class VisualStudioTokenCache
    {
        private const string KnownMsalCachePath = @".IdentityService\msal.cache"; // VS2019 (MSAL)
        private const string KnownAdalCachePath = @".IdentityService\IdentityServiceAdalCache.cache"; // VS2017/2019 (ADAL)

        private readonly ICommandContext _context;
        private readonly string _vsMsalCachePath;
        private readonly string _vsAdalCachePath;
        private readonly object _lock = new object();

        public VisualStudioTokenCache(ICommandContext context)
        {
            _context = context;

            if (TryFindVisualStudioCachePaths(out _vsMsalCachePath, out _vsAdalCachePath))
            {
                _context.Trace.WriteLine("Using Visual Studio token caches:");

                if (!(_vsMsalCachePath is null))
                {
                    _context.Trace.WriteLine($"  * {_vsMsalCachePath} (MSAL)");
                }

                if (!(_vsAdalCachePath is null))
                {
                    _context.Trace.WriteLine($"  * {_vsAdalCachePath} (ADAL)");
                }
            }
            else
            {
                _context.Trace.WriteLine("No Visual Studio token caches were found.");
            }
        }

        public void Register(IPublicClientApplication app)
        {
            app.UserTokenCache.SetBeforeAccess(OnBeforeAccess);
            app.UserTokenCache.SetAfterAccess(OnAfterAccess);
        }

        private static bool TryFindVisualStudioCachePaths(out string msalCachePath, out string adalCachePath)
        {
            bool foundCache = false;
            msalCachePath = null;
            adalCachePath = null;

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string candidateMsalPath = Path.Combine(localAppDataPath, KnownMsalCachePath);
            if (File.Exists(candidateMsalPath))
            {
                msalCachePath = candidateMsalPath;
                foundCache = true;
            }

            string candidateAdalPath = Path.Combine(localAppDataPath, KnownAdalCachePath);
            if (File.Exists(candidateAdalPath))
            {
                adalCachePath = candidateAdalPath;
                foundCache = true;
            }

            return foundCache;
        }

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            lock (_lock)
            {
                try
                {
                    if (TryReadProtectedFile(_vsMsalCachePath, out byte[] msalData))
                    {
                        args.TokenCache.DeserializeMsalV3(msalData);
                    }

                    if (TryReadProtectedFile(_vsAdalCachePath, out byte[] adalData))
                    {
                        args.TokenCache.DeserializeAdalV3(adalData);
                    }
                }
                catch (Exception ex)
                {
                    _context.Trace.WriteLine("Reading token cache failed!");
                    _context.Trace.WriteException(ex);
                }
            }
        }

        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_vsMsalCachePath))
                    {
                        byte[] msalData = args.TokenCache.SerializeMsalV3();
                        WriteProtectedFile(_vsMsalCachePath, msalData);
                    }

                    if (File.Exists(_vsAdalCachePath))
                    {
                        byte[] adalData = args.TokenCache.SerializeAdalV3();
                        WriteProtectedFile(_vsMsalCachePath, adalData);
                    }
                }
                catch (Exception ex)
                {
                    _context.Trace.WriteLine("Writing token cache failed!");
                    _context.Trace.WriteException(ex);
                }
            }
        }

        private bool TryReadProtectedFile(string path, out byte[] data)
        {
            if (_context.FileSystem.FileExists(path))
            {
                byte[] encryptedData = File.ReadAllBytes(path);

                data = _context.Cryptography.DecryptDataForCurrentUser(encryptedData);

                return true;
            }

            data = null;
            return false;
        }

        private void WriteProtectedFile(string path, byte[] data)
        {
            string dirPath = Path.GetDirectoryName(path);
            if (!_context.FileSystem.DirectoryExists(dirPath))
            {
                _context.FileSystem.CreateDirectory(dirPath);
            }

            byte[] encryptedData = _context.Cryptography.EncryptDataForCurrentUser(data);

            File.WriteAllBytes(path, encryptedData);
        }
    }
}
