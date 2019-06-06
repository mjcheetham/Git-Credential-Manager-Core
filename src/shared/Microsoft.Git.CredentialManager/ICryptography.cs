namespace Microsoft.Git.CredentialManager
{
    public interface ICryptography
    {
        byte[] DecryptDataForCurrentUser(byte[] data);

        byte[] EncryptDataForCurrentUser(byte[] data);
    }
}
