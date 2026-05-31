using System;
using System.Security.Cryptography;

namespace XLua;

public class SignatureLoader
{
	private LuaEnv.CustomLoader userLoader;

	private RSACryptoServiceProvider rsa;

	private SHA1 sha;

	public SignatureLoader(string publicKey, LuaEnv.CustomLoader loader)
	{
		rsa = new RSACryptoServiceProvider();
		rsa.ImportCspBlob(Convert.FromBase64String(publicKey));
		sha = new SHA1CryptoServiceProvider();
		userLoader = loader;
	}

	private byte[] load_and_verify(ref string filepath)
	{
		byte[] array = userLoader(ref filepath);
		if (array == null)
		{
			return null;
		}
		if (array.Length < 128)
		{
			throw new InvalidProgramException(filepath + " length less than 128!");
		}
		byte[] array2 = new byte[128];
		byte[] array3 = new byte[array.Length - 128];
		Array.Copy(array, array2, 128);
		Array.Copy(array, 128, array3, 0, array3.Length);
		if (!rsa.VerifyData(array3, sha, array2))
		{
			throw new InvalidProgramException(filepath + " has invalid signature!");
		}
		return array3;
	}

	public static implicit operator LuaEnv.CustomLoader(SignatureLoader signatureLoader)
	{
		return signatureLoader.load_and_verify;
	}
}
