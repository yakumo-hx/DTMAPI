using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DolocTown.GameData;

public class AesEncryptor : IEncryptor
{
	private readonly string key;

	private readonly string iv;

	public AesEncryptor(string key, string iv)
	{
		this.key = key;
		this.iv = iv;
	}

	public string Encrypt(string text)
	{
		using Aes aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(key);
		aes.IV = Encoding.UTF8.GetBytes(iv);
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
		using MemoryStream memoryStream = new MemoryStream();
		using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
		{
			using StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.Write(text);
		}
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	public string Decrypt(string encryptedText)
	{
		using Aes aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(key);
		aes.IV = Encoding.UTF8.GetBytes(iv);
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
		using MemoryStream stream = new MemoryStream(Convert.FromBase64String(encryptedText));
		using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using StreamReader streamReader = new StreamReader(stream2);
		return streamReader.ReadToEnd();
	}
}
