using System.Collections.Generic;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class SensitiveWordChecker
{
	private List<string> sensitiveWordList = new List<string>();

	private SensitiveWordFilter filter;

	private IEncryptor encryptor;

	private string aesKey => DolocAPI.gameManager.aesKey;

	private string aesIV => DolocAPI.gameManager.aesIV;

	private string encryptPrefix => DolocAPI.gameManager.encryptPrefix;

	public SensitiveWordChecker()
	{
		encryptor = new AesEncryptor(aesKey, aesIV);
		TextAsset textAsset = Resources.Load<TextAsset>("Other/sensitive_words_lines");
		if (textAsset == null)
		{
			Debug.LogError("敏感词文件不存在！");
			return;
		}
		string text = TryDecryptData(textAsset.text);
		sensitiveWordList = new List<string>(text.Split('\n'));
		filter = new SensitiveWordFilter(sensitiveWordList);
	}

	public bool ContainsSensitiveWord(string text)
	{
		return filter.ContainsSensitiveWord(text);
	}

	private string TryDecryptData(string dataToLoad)
	{
		if (dataToLoad.StartsWith(encryptPrefix))
		{
			string encryptedText = dataToLoad[encryptPrefix.Length..];
			dataToLoad = encryptor.Decrypt(encryptedText);
		}
		return dataToLoad;
	}
}
