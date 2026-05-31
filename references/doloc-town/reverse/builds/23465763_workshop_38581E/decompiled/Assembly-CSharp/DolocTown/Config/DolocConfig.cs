using System;
using DolocTown.Config.Localization;
using SimpleJSON;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DolocTown.Config;

public class DolocConfig
{
	private enum BoolValue
	{
		TRUE,
		FALSE
	}

	private static Tables _tables;

	private static bool _isInitialized;

	public static Tables Tables
	{
		get
		{
			if (_tables != null)
			{
				return _tables;
			}
			_tables = new Tables(Loader);
			_tables.MergeModExtension();
			return _tables;
		}
	}

	public static TbStaticText StaticTexts
	{
		get
		{
			if (_isInitialized)
			{
				return Tables.TbStaticText;
			}
			Debug.LogError("table is empty!");
			return new TbStaticText(Loader("localization_statictexts"));
		}
	}

	private static JSONNode Loader(string file)
	{
		AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>("Configs/" + file + ".json");
		JSONNode jSONNode = JSON.Parse(handle.WaitForCompletion().text);
		Addressables.Release(handle);
		if (DolocAPI.UseMods)
		{
			jSONNode = DolocAPI.modManager.LoadWithMods(file, jSONNode);
		}
		return jSONNode;
	}

	public static bool Init()
	{
		if (_isInitialized)
		{
			return true;
		}
		try
		{
			_tables = new Tables(Loader);
			_isInitialized = true;
		}
		catch (Exception ex)
		{
			if (ex.InnerException != null)
			{
				Debug.LogException(ex.InnerException);
				Debug.LogError(ex.Message);
				Debug.LogError(ex.StackTrace);
			}
			Debug.LogException(ex);
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			throw;
		}
		return _isInitialized;
	}

	public static void Reload()
	{
		_tables = new Tables(Loader);
		_tables.MergeModExtension();
	}

	public static string GetL10nText(string l10nKey, bool useHoldPlaceOnErr = true)
	{
		if (l10nKey == null)
		{
			return "";
		}
		string text = Tables.TbL10nText.GetOrDefault(l10nKey)?.Text ?? "";
		if (text.IsNullOrEmpty() && useHoldPlaceOnErr)
		{
			text = "missing:<" + l10nKey + ">";
		}
		return text;
	}

	public static string GetEnumText<T>(T enumValue) where T : Enum
	{
		string key = enumValue.GetType().Name + "." + enumValue.ToString();
		return Tables.TbL10nText.GetOrDefault(key)?.Text ?? enumValue.ToString();
	}

	public static string GetBoolValueText(bool value)
	{
		if (!value)
		{
			return GetEnumText(BoolValue.FALSE);
		}
		return GetEnumText(BoolValue.TRUE);
	}
}
