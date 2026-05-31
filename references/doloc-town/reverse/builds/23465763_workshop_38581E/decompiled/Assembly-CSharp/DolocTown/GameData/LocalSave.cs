using System;
using System.IO;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace DolocTown.GameData;

public class LocalSave : IFileDataHandler
{
	private int currentIndex;

	private IEncryptor encryptor;

	private JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.Auto,
		SerializationBinder = new TypeRedirectBinder(),
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		Error = delegate(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
		{
			Exception error = args.ErrorContext.Error;
			Debug.LogError("反序列化异常: " + error.Message);
			Debug.LogError(error.StackTrace);
			Debug.LogError(args.ErrorContext.Path);
			Debug.LogException(error);
			args.ErrorContext.Handled = true;
		}
	};

	private string dataDirPath => Path.Combine(Application.persistentDataPath, "SAVE");

	private int dataFileCount => DolocAPI.gameManager.archiveFileCount;

	private string DataFileNameFormatFormat => DolocAPI.gameManager.archiveFileNameFormat;

	private string achievementDataFileName => "achievements.data";

	private string firstPlayFlagFileName => "fp.flag";

	private string convertDataFlagFileName => "0.92.flag";

	private bool useEncryption => DolocAPI.gameManager.useEncryption;

	private string aesKey => DolocAPI.gameManager.aesKey;

	private string aesIV => DolocAPI.gameManager.aesIV;

	private string encryptPrefix => DolocAPI.gameManager.encryptPrefix;

	private string minimumSupportedVersion => DolocAPI.gameManager.minimumSupportedVersion;

	private string settingFullPath => Path.Combine(dataDirPath, "user_settings.json");

	private string rebindsFullPath => Path.Combine(dataDirPath, "key_rebinds.json");

	private string modManagerFullPath => Path.Combine(dataDirPath, "mod_infos.json");

	private string achievementDataFullPath => Path.Combine(dataDirPath, achievementDataFileName);

	private string GetDataFullPath(int index)
	{
		return Path.Combine(dataDirPath, DataFileNameFormatFormat.Format(index));
	}

	private string GetDataBackupPath(int index)
	{
		return Path.Combine(dataDirPath, DolocUtils.Format(DataFileNameFormatFormat, index + "-bak"));
	}

	private string GetDataPrevPath(int index)
	{
		return Path.Combine(dataDirPath, DolocUtils.Format(DataFileNameFormatFormat, index + "-prev"));
	}

	private string GetDataTempPath(int index)
	{
		return Path.Combine(dataDirPath, DolocUtils.Format(DataFileNameFormatFormat, index + "-tmp"));
	}

	private string GetMissionLogPath(int index)
	{
		return Path.Combine(dataDirPath, $"./.mission_logs/mission_log_{index}.data");
	}

	public LocalSave()
	{
		encryptor = new AesEncryptor(aesKey, aesIV);
		Validate();
	}

	private void Validate()
	{
		string path = Path.Combine(Application.persistentDataPath, "Media");
		if (Directory.Exists(path))
		{
			try
			{
				Directory.Delete(path, recursive: true);
			}
			catch (Exception)
			{
			}
		}
		if (useEncryption)
		{
			for (int i = 0; i < DolocAPI.gameManager.archiveFileCount; i++)
			{
				try
				{
					string dataFullPath = GetDataFullPath(i);
					if (!File.Exists(dataFullPath))
					{
						continue;
					}
					string text = "";
					using (FileStream stream = new FileStream(dataFullPath, FileMode.Open))
					{
						using StreamReader streamReader = new StreamReader(stream);
						text = streamReader.ReadToEnd();
					}
					bool flag = false;
					if (!text.StartsWith(encryptPrefix))
					{
						JObject obj = JObject.Parse(text);
						text = GetDataToStore(obj);
						flag = true;
						Debug.Log($"加密存档文件{i}成功：{dataFullPath}");
					}
					if (FixArchiveIndex(i, text, out var dataToStore))
					{
						flag = true;
					}
					if (flag)
					{
						File.WriteAllText(dataFullPath, dataToStore);
					}
				}
				catch (Exception ex2)
				{
					Debug.LogError(ex2.Message);
					Debug.LogError(ex2.StackTrace);
				}
			}
		}
		if (!MarkFlag(convertDataFlagFileName))
		{
			return;
		}
		for (int j = 0; j < dataFileCount; j++)
		{
			string dataFullPath2 = GetDataFullPath(j);
			string text2 = Path.Combine(dataDirPath, $"demo-doloc-archive-{j}.data");
			if (!File.Exists(dataFullPath2) && File.Exists(text2))
			{
				File.Move(text2, dataFullPath2);
			}
		}
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

	public bool LoadGame(int index, out ArchiveDataHandle data)
	{
		data = null;
		string dataFullPath = GetDataFullPath(index);
		if (File.Exists(dataFullPath))
		{
			if (DolocAPI.gameManager.gameInitConfig.loadDataUnsafe)
			{
				string dataToLoad = "";
				using (FileStream stream = new FileStream(dataFullPath, FileMode.Open))
				{
					using StreamReader streamReader = new StreamReader(stream);
					dataToLoad = streamReader.ReadToEnd();
				}
				dataToLoad = TryDecryptData(dataToLoad);
				data = JsonConvert.DeserializeObject<ArchiveDataHandle>(dataToLoad, jsonSettings);
			}
			else
			{
				try
				{
					string dataToLoad2 = "";
					using (FileStream stream2 = new FileStream(dataFullPath, FileMode.Open))
					{
						using StreamReader streamReader2 = new StreamReader(stream2);
						dataToLoad2 = streamReader2.ReadToEnd();
					}
					dataToLoad2 = TryDecryptData(dataToLoad2);
					try
					{
						data = JsonConvert.DeserializeObject<ArchiveDataHandle>(dataToLoad2, jsonSettings);
					}
					catch (Exception ex)
					{
						Debug.LogError($"读入存档文件{index}失败：{dataFullPath}");
						Debug.LogError(ex.Message);
						Debug.LogError(ex.StackTrace);
						Debug.LogException(ex);
						return false;
					}
				}
				catch (Exception ex2)
				{
					Debug.LogError($"打开存档文件{index}失败：{dataFullPath}");
					Debug.LogError(ex2.Message);
					Debug.LogError(ex2.StackTrace);
					Debug.LogException(ex2);
					return false;
				}
			}
		}
		Debug.Log($"读入存档文件{index}成功：{dataFullPath}");
		return true;
	}

	public bool SaveGame(ArchiveDataHandle data, int index)
	{
		data.SetArchiveIndex(index);
		string dataFullPath = GetDataFullPath(index);
		string dataPrevPath = GetDataPrevPath(index);
		string dataTempPath = GetDataTempPath(index);
		try
		{
			string dataToStore = GetDataToStore(data);
			Directory.CreateDirectory(Path.GetDirectoryName(dataFullPath) ?? string.Empty);
			using (FileStream stream = new FileStream(dataTempPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.Write(dataToStore);
			}
			if (File.Exists(dataFullPath))
			{
				File.Replace(dataTempPath, dataFullPath, dataPrevPath);
				Debug.Log($"存档{index}备份成功：{dataPrevPath}");
			}
			else
			{
				File.Move(dataTempPath, dataFullPath);
			}
			Debug.Log($"写入存档{index}成功: {dataFullPath}");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError($"写入存档文件{index}失败：{dataFullPath}");
			Debug.LogException(exception);
			try
			{
				if (File.Exists(dataTempPath))
				{
					File.Delete(dataTempPath);
				}
			}
			catch (Exception exception2)
			{
				Debug.LogWarning("临时文件清理失败：" + dataTempPath);
				Debug.LogException(exception2);
			}
			return false;
		}
	}

	public bool DuplicateGame(int index, out int targetIndex)
	{
		targetIndex = -1;
		for (int i = 0; i < dataFileCount; i++)
		{
			if (GetArchiveInfo(i) == null)
			{
				targetIndex = i;
				break;
			}
		}
		if (targetIndex < 0)
		{
			return false;
		}
		string dataFullPath = GetDataFullPath(index);
		string dataFullPath2 = GetDataFullPath(targetIndex);
		try
		{
			string dataToLoad = "";
			using (FileStream stream = new FileStream(dataFullPath, FileMode.Open))
			{
				using StreamReader streamReader = new StreamReader(stream);
				dataToLoad = streamReader.ReadToEnd();
			}
			FixArchiveIndex(targetIndex, dataToLoad, out var dataToStore);
			File.WriteAllText(dataFullPath2, dataToStore);
			Debug.Log($"复制存档{index}到{targetIndex}成功: {dataFullPath} -> {dataFullPath2}");
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"复制存档{index}到{targetIndex}失败：{dataFullPath} -> {dataFullPath2}");
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			targetIndex = -1;
			return false;
		}
		return true;
	}

	private bool FixArchiveIndex(int targetIndex, string dataToLoad, out string dataToStore)
	{
		dataToLoad = TryDecryptData(dataToLoad);
		JObject jObject = JObject.Parse(dataToLoad);
		string propertyName = "baseData";
		string propertyName2 = "archiveIndex";
		bool result = false;
		if (jObject.ContainsKey(propertyName2))
		{
			if ((int)jObject[propertyName2] != targetIndex)
			{
				result = true;
			}
			jObject[propertyName2] = JToken.FromObject(targetIndex);
		}
		if (jObject.ContainsKey(propertyName) && jObject[propertyName] is JObject jObject2)
		{
			if ((int)jObject2[propertyName2] != targetIndex)
			{
				result = true;
			}
			jObject2[propertyName2] = JToken.FromObject(targetIndex);
		}
		dataToStore = GetDataToStore(jObject);
		return result;
	}

	private string GetDataToStore(object obj)
	{
		string text = JsonConvert.SerializeObject(obj, (!useEncryption) ? Formatting.Indented : Formatting.None, jsonSettings);
		if (useEncryption)
		{
			text = encryptPrefix + encryptor.Encrypt(text);
		}
		return text;
	}

	public string GetArchiveDataAsString(ArchiveDataHandle data)
	{
		return JsonConvert.SerializeObject(data, Formatting.None, jsonSettings);
	}

	public bool DeleteGame(int index)
	{
		string dataFullPath = GetDataFullPath(index);
		string dataBackupPath = GetDataBackupPath(index);
		try
		{
			if (File.Exists(dataBackupPath))
			{
				File.Delete(dataBackupPath);
			}
			File.Move(dataFullPath, dataBackupPath);
			Debug.Log($"删除存档{index}成功: {dataFullPath}");
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"删除存档{index}失败：{dataFullPath}");
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			return false;
		}
	}

	public BaseArchiveData GetArchiveInfo(int index)
	{
		BaseArchiveData result = null;
		string dataFullPath = GetDataFullPath(index);
		if (File.Exists(dataFullPath))
		{
			try
			{
				string dataToLoad = "";
				using (FileStream stream = new FileStream(dataFullPath, FileMode.Open))
				{
					using StreamReader streamReader = new StreamReader(stream);
					dataToLoad = streamReader.ReadToEnd();
				}
				dataToLoad = TryDecryptData(dataToLoad);
				BaseArchiveData baseArchiveData = JObject.Parse(dataToLoad)["baseData"]?.ToObject<BaseArchiveData>();
				string text = baseArchiveData?.version;
				if (DolocAPI.gameManager.gameOuterConfig.ignoreMinimumVersion || (text != null && new Version(text) >= new Version(minimumSupportedVersion)))
				{
					result = baseArchiveData;
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"读入存档文件{index}基础信息失败：{dataFullPath}");
				Debug.LogError(ex.Message);
				Debug.LogError(ex.StackTrace);
				return null;
			}
		}
		return result;
	}

	public BaseArchiveData[] GetAllArchiveInfo()
	{
		BaseArchiveData[] array = new BaseArchiveData[dataFileCount];
		for (int i = 0; i < dataFileCount; i++)
		{
			array[i] = GetArchiveInfo(i);
		}
		return array;
	}

	public bool LoadUserSettings(out UserSettings settings)
	{
		settings = null;
		if (File.Exists(settingFullPath))
		{
			try
			{
				string value = "";
				using (FileStream stream = new FileStream(settingFullPath, FileMode.Open))
				{
					using StreamReader streamReader = new StreamReader(stream);
					value = streamReader.ReadToEnd();
				}
				try
				{
					settings = JsonConvert.DeserializeObject<UserSettings>(value, jsonSettings);
					string orDefault = settings.GetOrDefault<string>(UserSettingType.LANGUAGE_TEXT);
					if (orDefault == "cn")
					{
						settings.SetValue(UserSettingType.LANGUAGE_TEXT, "zh-CN", sendMessage: false);
						SaveUserSettings(settings);
					}
					else if (orDefault == "tw")
					{
						settings.SetValue(UserSettingType.LANGUAGE_TEXT, "zh-TW", sendMessage: false);
						SaveUserSettings(settings);
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("读入用户设置失败：" + settingFullPath + ", 新建默认设置");
					Debug.LogError(ex.Message);
					Debug.LogError(ex.StackTrace);
					Debug.LogException(ex);
					settings = new UserSettings();
					return false;
				}
			}
			catch (Exception ex2)
			{
				Debug.LogError("打开用户设置失败：" + settingFullPath + ", 新建默认设置");
				Debug.LogError(ex2.Message);
				Debug.LogError(ex2.StackTrace);
				Debug.LogException(ex2);
				settings = new UserSettings();
				return false;
			}
		}
		if (settings == null)
		{
			settings = new UserSettings();
			SaveUserSettings(settings);
			return true;
		}
		Debug.Log("读入用户设置文件成功：" + settingFullPath);
		return true;
	}

	public bool SaveUserSettings(UserSettings settings)
	{
		if (settings == null)
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(settingFullPath) ?? string.Empty);
			string value = JsonConvert.SerializeObject(settings, Formatting.Indented, jsonSettings);
			using (FileStream stream = new FileStream(settingFullPath, FileMode.Create))
			{
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.Write(value);
			}
			Debug.Log("写入用户设置成功: " + settingFullPath);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("写入用户设置失败：" + settingFullPath);
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			return false;
		}
	}

	public bool LoadModManager(out ModManager modManager)
	{
		if (!FileUtils.TryLoadJson<ModManager>(modManagerFullPath, jsonSettings, out modManager) || modManager == null)
		{
			modManager = new ModManager();
			SaveModManager(modManager);
			return false;
		}
		return true;
	}

	public bool SaveModManager(ModManager modManager)
	{
		if (modManager == null)
		{
			return false;
		}
		return FileUtils.TrySaveJson(modManagerFullPath, modManager, jsonSettings);
	}

	public bool LoadUserBindingOverrides(out string rebinds)
	{
		rebinds = string.Empty;
		if (File.Exists(rebindsFullPath))
		{
			try
			{
				string text = "";
				using FileStream stream = new FileStream(rebindsFullPath, FileMode.Open);
				using StreamReader streamReader = new StreamReader(stream);
				text = streamReader.ReadToEnd();
				rebinds = text;
				return !rebinds.IsNullOrEmpty();
			}
			catch (Exception ex)
			{
				Debug.LogError("打开用户按键绑定文件失败：" + rebindsFullPath);
				Debug.LogError(ex.Message);
				Debug.LogError(ex.StackTrace);
				Debug.LogException(ex);
				return false;
			}
		}
		return false;
	}

	public bool SaveUserBindingOverrides(string rebinds)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(rebindsFullPath) ?? string.Empty);
			using (FileStream stream = new FileStream(rebindsFullPath, FileMode.Create))
			{
				using StreamWriter streamWriter = new StreamWriter(stream);
				streamWriter.Write(rebinds);
			}
			Debug.Log("写入用户按键绑定文件成功: " + rebindsFullPath);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("写入用户按键绑定文件失败：" + rebindsFullPath);
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			return false;
		}
	}

	public bool IsFirstPlayGame(bool shouldMark)
	{
		return MarkFlag(firstPlayFlagFileName, shouldMark);
	}

	private bool MarkFlag(string mark, bool shouldMark = true)
	{
		string text = Path.Combine(dataDirPath, mark);
		if (File.Exists(text))
		{
			return false;
		}
		if (!shouldMark)
		{
			return true;
		}
		try
		{
			File.Create(text).Close();
			Debug.Log("首次游戏标记成功：" + text);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("首次游戏标记失败：" + text);
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			return false;
		}
	}

	public bool HasDataStartWith(string prefix)
	{
		string[] files = Directory.GetFiles(dataDirPath);
		if (files.IsNullOrEmpty())
		{
			return false;
		}
		return files.Any((string file) => Path.GetFileName(file).StartsWith(prefix));
	}
}
