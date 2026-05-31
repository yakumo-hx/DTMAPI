namespace DolocTown.GameData;

public interface IEncryptor
{
	string Encrypt(string text);

	string Decrypt(string encryptedText);
}
