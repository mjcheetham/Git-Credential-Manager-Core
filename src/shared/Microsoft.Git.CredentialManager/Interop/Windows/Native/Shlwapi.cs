using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.Windows.Native
{
    public static class Shlwapi
    {
        private const string LibraryName = "shlwapi.dll";

        public const int OS_ANYSERVER = 29;

        [DllImport(LibraryName, EntryPoint="IsOS", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool IsOS(int os);
    }
}
