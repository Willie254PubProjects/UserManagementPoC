namespace UserManagementPoC.Shared.Security.Contracts;

public interface IEncryptionService
{
    string Encrypt(string plainText, out string ivBase64);
    string Decrypt(string cipherText, string ivBase64);

}