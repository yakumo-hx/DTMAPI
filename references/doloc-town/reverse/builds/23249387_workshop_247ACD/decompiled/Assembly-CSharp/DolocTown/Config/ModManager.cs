using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DolocTown.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;
using Steamworks;
using UnityEngine;

namespace DolocTown.Config;

[JsonObject(MemberSerialization.OptIn)]
public class ModManager
{
	private class LocalModUploadPlanCache
	{
		public ulong workshopId;

		public WorkshopUploadPlan plan;
	}

	[JsonProperty]
	private Dictionary<string, ModInfo> modInfos;

	private const string ModFolderName = "MODS";

	private const string ModInfoFileName = "info.json";

	private const string WorkshopInfoFileName = "workshop.json";

	private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.Auto,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
	};

	private static Dictionary<string, Type> _configTableTypesByFile;

	private List<ModInfo> sortedModInfos;

	private readonly SteamWorkshopUploader uploader = new SteamWorkshopUploader();

	private readonly Dictionary<string, LocalModUploadPlanCache> localModUploadPlanCache = new Dictionary<string, LocalModUploadPlanCache>();

	private readonly Dictionary<string, List<Action<WorkshopUploadPlan>>> pendingLocalModUploadPlanCallbacks = new Dictionary<string, List<Action<WorkshopUploadPlan>>>();

	private readonly Queue<ModInfo> pendingLocalModUploadPlanQueue = new Queue<ModInfo>();

	private readonly HashSet<string> queuedLocalModUploadPlanKeys = new HashSet<string>();

	private string resolvingLocalModUploadPlanKey;

	private Callback<SteamUGCQueryCompleted_t> queryCallback;

	private static Dictionary<string, Type> configTableTypesByFile
	{
		get
		{
			if (_configTableTypesByFile == null)
			{
				_configTableTypesByFile = CreateConfigTableTypesByFile();
			}
			return _configTableTypesByFile;
		}
	}

	public ModInfo[] EnabledMods { get; private set; }

	public Dictionary<string, ModInfo> CachedSprites { get; private set; } = new Dictionary<string, ModInfo>();


	public Dictionary<string, List<ModInfo>> CachedConfigs { get; private set; } = new Dictionary<string, List<ModInfo>>();


	public List<ModInfo> SortedModInfos
	{
		get
		{
			if (sortedModInfos.IsNullOrEmpty() || sortedModInfos.Count != modInfos.Count)
			{
				sortedModInfos = GetAllValidModInfos();
			}
			return sortedModInfos;
		}
	}

	public bool HasPlayerOverride { get; private set; }

	public bool HasPlayerHairOverride { get; private set; }

	public bool HasPlayerBodyOverride { get; private set; }

	public bool HasEnabledMods => !EnabledMods.IsNullOrEmpty();

	public string ModsRoot => Path.Combine(Application.persistentDataPath, "MODS");

	[JsonConstructor]
	public ModManager(Dictionary<string, ModInfo> modInfos = null)
	{
		this.modInfos = modInfos ?? new Dictionary<string, ModInfo>();
		ReloadMods();
	}

	public ModManager()
		: this(null)
	{
	}

	public void ReloadMods()
	{
		if (!Directory.Exists(ModsRoot))
		{
			Directory.CreateDirectory(ModsRoot);
		}
		localModUploadPlanCache.Clear();
		HashSet<string> hashSet = new HashSet<string>();
		string[] directories = Directory.GetDirectories(ModsRoot);
		foreach (string dir in directories)
		{
			if (TryLoadMod(dir, string.Empty, ModSourceType.Local, out var info))
			{
				hashSet.Add(info.id);
			}
		}
		foreach (PublishedFileId_t subscribedMod in GetSubscribedMods())
		{
			if (GetSubscribedModDirectory(subscribedMod, out var dir2) && TryLoadMod(dir2, subscribedMod.m_PublishedFileId.ToString(), ModSourceType.Workshop, out var info2))
			{
				hashSet.Add(info2.id);
			}
		}
		directories = modInfos.Keys.ToArray();
		foreach (string text in directories)
		{
			if (!hashSet.Contains(text))
			{
				modInfos[text].ClearCache();
				modInfos.Remove(text);
			}
		}
		UpdateCache();
	}

	private bool TryLoadMod(string dir, string overrideId, ModSourceType source, out ModInfo info)
	{
		info = null;
		string text = Path.Combine(dir, "info.json");
		if (!File.Exists(text))
		{
			return false;
		}
		string name = new DirectoryInfo(dir).Name;
		try
		{
			string text2 = File.ReadAllText(text);
			ulong legacyWorkshopId = 0uL;
			if (source == ModSourceType.Local)
			{
				text2 = TryMigrateData(dir, text, text2, out legacyWorkshopId);
			}
			ModManifest modManifest = JsonConvert.DeserializeObject<ModManifest>(text2);
			if (modManifest == null || string.IsNullOrEmpty(name))
			{
				Debug.LogError("[MOD] Invalid manifest: " + text);
				return false;
			}
			ulong workshopId = 0uL;
			if (source == ModSourceType.Workshop)
			{
				ulong.TryParse(overrideId, NumberStyles.None, CultureInfo.InvariantCulture, out workshopId);
			}
			else if (!ModInfo.TryLoadWorkshopInfo(dir, out workshopId))
			{
				workshopId = legacyWorkshopId;
			}
			string text3 = source.ToString() + "." + (overrideId.IsNullOrEmpty() ? name : overrideId);
			modInfos.TryAdd(text3, new ModInfo(text3, enabled: false, source));
			info = modInfos[text3];
			info.Init(dir, modManifest, source, workshopId);
			return true;
		}
		catch (Exception arg)
		{
			Debug.LogError($"[MOD] Failed to load mod manifest: {text}\n{arg}");
			return false;
		}
	}

	private static string TryMigrateData(string dir, string manifestPath, string manifestJson, out ulong legacyWorkshopId)
	{
		legacyWorkshopId = 0uL;
		JObject jObject = JObject.Parse(manifestJson);
		JToken jToken = jObject["workshopId"] ?? jObject["workshop_id"];
		bool flag = false;
		if (jToken != null)
		{
			ulong.TryParse(jToken.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out legacyWorkshopId);
			string path = Path.Combine(dir, "workshop.json");
			if (!File.Exists(path) && !FileUtils.TrySaveJson(path, new ModWorkshopInfo(legacyWorkshopId)))
			{
				return manifestJson;
			}
			flag = true;
		}
		flag |= RemoveLegacyManifestField(jObject, "workshopId");
		flag |= RemoveLegacyManifestField(jObject, "workshop_id");
		flag |= RemoveLegacyManifestField(jObject, "priority");
		flag |= EnsureLocalizedManifestField(jObject, "localized_name", string.Empty);
		if (!(flag | EnsureLocalizedManifestField(jObject, "localized_description", string.Empty)))
		{
			return manifestJson;
		}
		string text = jObject.ToString(Formatting.Indented);
		if (!FileUtils.TrySaveText(manifestPath, text))
		{
			return manifestJson;
		}
		return text;
	}

	private static bool RemoveLegacyManifestField(JObject manifestObj, string fieldName)
	{
		return manifestObj.Remove(fieldName);
	}

	private static bool EnsureLocalizedManifestField(JObject manifestObj, string fieldName, string fallbackValue)
	{
		bool result = false;
		JObject jObject = manifestObj[fieldName] as JObject;
		if (jObject == null)
		{
			jObject = (JObject)(manifestObj[fieldName] = new JObject());
			result = true;
		}
		string[] uploadL10nIds = ModManifest.GetUploadL10nIds();
		foreach (string propertyName in uploadL10nIds)
		{
			JToken? jToken2 = jObject[propertyName];
			if (jToken2 == null || jToken2.Type != JTokenType.String)
			{
				jObject[propertyName] = fallbackValue ?? string.Empty;
				result = true;
			}
		}
		return result;
	}

	public void UpdateCache()
	{
		EnabledMods = GetAllEnabledModInfos();
		CachedSprites.Clear();
		CachedConfigs.Clear();
		for (int num = EnabledMods.Length - 1; num >= 0; num--)
		{
			ModInfo modInfo = EnabledMods[num];
			modInfo.UpdateCache();
			foreach (KeyValuePair<string, Sprite> sprite in modInfo.sprites)
			{
				CachedSprites[sprite.Key] = modInfo;
			}
			foreach (KeyValuePair<string, List<string>> config in modInfo.configs)
			{
				CachedConfigs.TryAdd(config.Key, new List<ModInfo>());
				CachedConfigs[config.Key].Add(modInfo);
			}
		}
		UpdateFlags();
	}

	private void UpdateFlags()
	{
		HasPlayerOverride = false;
		HasPlayerHairOverride = false;
		HasPlayerBodyOverride = false;
		bool flag = false;
		bool flag2 = false;
		ModInfo[] enabledMods = EnabledMods;
		foreach (ModInfo modInfo in enabledMods)
		{
			if (!flag && !flag2)
			{
				if (modInfo.HasPlayerOverride)
				{
					HasPlayerOverride = true;
					flag2 = true;
				}
				else if (modInfo.HasPlayerHairOverride || modInfo.HasPlayerBodyOverride)
				{
					HasPlayerHairOverride |= modInfo.HasPlayerHairOverride;
					HasPlayerBodyOverride |= modInfo.HasPlayerBodyOverride;
					flag = true;
				}
			}
			else if (flag)
			{
				HasPlayerHairOverride |= modInfo.HasPlayerHairOverride;
				HasPlayerBodyOverride |= modInfo.HasPlayerBodyOverride;
			}
		}
	}

	public JSONNode LoadWithMods(string file, JSONNode baseJson)
	{
		if (!CachedConfigs.TryGetValue(file, out var value))
		{
			return baseJson;
		}
		JSONArray asArray = baseJson.AsArray;
		foreach (ModInfo item in value)
		{
			if (!item.configs.TryGetValue(file, out var value2))
			{
				continue;
			}
			foreach (string item2 in value2)
			{
				if (!File.Exists(item2))
				{
					continue;
				}
				try
				{
					JSONArray asArray2 = JSON.Parse(File.ReadAllText(item2)).AsArray;
					if (!TryValidateMergedConfig(file, asArray2, out var error))
					{
						Debug.LogError("[MOD] Mod config validation failed, skip merge: " + item2 + "\nError: " + error);
						continue;
					}
					string keyName = (file.Contains("localization_tbtextmapper") ? "key" : "id");
					MergeArray(asArray, asArray2, file.Contains("mod_tbmod"), keyName);
				}
				catch (Exception)
				{
					Debug.LogError("[MOD] Mod config load failed: " + item2);
				}
			}
		}
		return asArray;
	}

	private static Dictionary<string, Type> CreateConfigTableTypesByFile()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		PropertyInfo[] properties = typeof(Tables).GetProperties(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < properties.Length; i++)
		{
			Type propertyType = properties[i].PropertyType;
			if (!(propertyType.GetConstructor(new Type[1] { typeof(JSONNode) }) == null))
			{
				string @namespace = propertyType.Namespace;
				if (!string.IsNullOrEmpty(@namespace) && @namespace.StartsWith("DolocTown.Config.", StringComparison.Ordinal))
				{
					string key = @namespace.Substring(@namespace.LastIndexOf('.') + 1).ToLowerInvariant() + "_" + propertyType.Name.ToLowerInvariant();
					dictionary[key] = propertyType;
				}
			}
		}
		return dictionary;
	}

	private static bool TryGetConfigTableType(string file, out Type tableType)
	{
		return configTableTypesByFile.TryGetValue(file, out tableType);
	}

	private static bool TryValidateMergedConfig(string file, JSONArray mergedArray, out string error)
	{
		error = null;
		if (!TryGetConfigTableType(file, out var tableType))
		{
			error = "Config table type not found for file: " + file;
			return false;
		}
		try
		{
			Activator.CreateInstance(tableType, new object[1] { mergedArray });
			return true;
		}
		catch (TargetInvocationException ex)
		{
			Exception innerException = ex.InnerException;
			error = ((innerException == null) ? ex.ToString() : innerException.ToString());
			return false;
		}
		catch (Exception ex2)
		{
			error = ex2.ToString();
			return false;
		}
	}

	private void MergeArray(JSONArray baseArray, JSONArray modArray, bool ignoreId, string keyName)
	{
		if (ignoreId)
		{
			foreach (JSONNode child in modArray.Children)
			{
				if (child.AsObject != null)
				{
					baseArray.Add(child);
				}
			}
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < baseArray.Count; i++)
		{
			JSONObject asObject = baseArray[i].AsObject;
			if (!(asObject == null))
			{
				string text = asObject[keyName];
				if (!string.IsNullOrEmpty(text))
				{
					dictionary[text] = i;
				}
			}
		}
		foreach (JSONNode child2 in modArray.Children)
		{
			JSONObject asObject2 = child2.AsObject;
			if (asObject2 == null)
			{
				throw new InvalidDataException("[MOD] Mod config row must be a JSON object.");
			}
			string text2 = asObject2["id"];
			if (string.IsNullOrEmpty(text2))
			{
				baseArray.Add(asObject2);
				continue;
			}
			if (dictionary.TryGetValue(text2, out var value))
			{
				baseArray[value] = asObject2;
				continue;
			}
			baseArray.Add(asObject2);
			dictionary[text2] = baseArray.Count - 1;
		}
	}

	public void ToggleModEnabled(ModInfo modInfo)
	{
		modInfo.ToggleEnabled();
		sortedModInfos.Remove(modInfo);
		int num = sortedModInfos.FindIndex((ModInfo m) => !m.enabled);
		if (num < 0)
		{
			sortedModInfos.Add(modInfo);
		}
		else
		{
			sortedModInfos.Insert(num, modInfo);
		}
		RefreshPriority();
	}

	public int MovePrev(ModInfo modInfo)
	{
		return Move(modInfo, -1);
	}

	public int MoveNext(ModInfo modInfo)
	{
		return Move(modInfo, 1);
	}

	public bool CanMovePrev(ModInfo modInfo)
	{
		return CanMove(modInfo, -1);
	}

	public bool CanMoveNext(ModInfo modInfo)
	{
		return CanMove(modInfo, 1);
	}

	private int Move(ModInfo modInfo, int diff)
	{
		if (!CanMove(modInfo, diff))
		{
			return -1;
		}
		int num = SortedModInfos.IndexOf(modInfo);
		int num2 = num + diff;
		List<ModInfo> list = SortedModInfos;
		int index = num;
		List<ModInfo> list2 = SortedModInfos;
		int index2 = num2;
		ModInfo modInfo2 = SortedModInfos[num2];
		ModInfo modInfo3 = SortedModInfos[num];
		ModInfo modInfo5 = (list[index] = modInfo2);
		modInfo5 = (list2[index2] = modInfo3);
		RefreshPriority();
		return num2;
	}

	private bool CanMove(ModInfo modInfo, int diff)
	{
		int num = SortedModInfos.IndexOf(modInfo);
		if (num < 0)
		{
			return false;
		}
		int num2 = num + diff;
		if (num2 >= 0)
		{
			return num2 < SortedModInfos.Count;
		}
		return false;
	}

	private void RefreshPriority()
	{
		int num = 0;
		for (int num2 = SortedModInfos.Count - 1; num2 >= 0; num2--)
		{
			ModInfo modInfo = SortedModInfos[num2];
			if (modInfo.enabled)
			{
				modInfo.priority = num;
				num++;
			}
			else
			{
				modInfo.priority = -1;
			}
		}
	}

	private bool TryGetModInfo(string id, out ModInfo modInfo)
	{
		return modInfos.TryGetValue(id, out modInfo);
	}

	private bool CheckModEnabled(string id)
	{
		if (TryGetModInfo(id, out var modInfo))
		{
			return modInfo.enabled;
		}
		return false;
	}

	public void SetModEnabled(string id, bool enabled)
	{
		if (modInfos.TryGetValue(id, out var value))
		{
			value.enabled = enabled;
		}
	}

	public List<ModInfo> GetAllValidModInfos()
	{
		return (from x in modInfos.Values.ToArray()
			orderby x.priority descending
			select x).ToList();
	}

	public ModInfo[] GetAllEnabledModInfos()
	{
		return (from x in modInfos.Values.ToArray()
			where CheckModEnabled(x.id)
			orderby x.priority descending
			select x).ToArray();
	}

	public bool LoadSpriteFromFile(string address, out Sprite asset)
	{
		asset = null;
		if (!CachedSprites.TryGetValue(address, out var value))
		{
			return false;
		}
		return value.sprites.TryGetValue(address, out asset);
	}

	public List<PublishedFileId_t> GetSubscribedMods()
	{
		try
		{
			uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
			PublishedFileId_t[] array = new PublishedFileId_t[numSubscribedItems];
			SteamUGC.GetSubscribedItems(array, numSubscribedItems);
			return array.ToList();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return new List<PublishedFileId_t>();
		}
	}

	public void QuerySubscribedMods()
	{
		UGCQueryHandle_t handle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Subscribed, EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc, SteamUtils.GetAppID(), SteamUtils.GetAppID(), 1u);
		SteamUGC.SetReturnMetadata(handle, bReturnMetadata: true);
		queryCallback = Callback<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
		SteamUGC.SendQueryUGCRequest(handle);
	}

	private void OnQueryCompleted(SteamUGCQueryCompleted_t result)
	{
		for (uint num = 0u; num < result.m_unNumResultsReturned; num++)
		{
			SteamUGC.GetQueryUGCResult(result.m_handle, num, out var _);
		}
	}

	private bool GetSubscribedModDirectory(PublishedFileId_t item, out string dir)
	{
		if (SteamUGC.GetItemInstallInfo(item, out var _, out dir, 260u, out var _))
		{
			return true;
		}
		return false;
	}

	private static string GetLocalModUploadPlanKey(ModInfo modInfo)
	{
		return modInfo?.id;
	}

	private bool TryGetCachedLocalModUploadPlan(ModInfo modInfo, out WorkshopUploadPlan plan)
	{
		plan = null;
		if (modInfo == null || modInfo.source != 0)
		{
			return false;
		}
		string localModUploadPlanKey = GetLocalModUploadPlanKey(modInfo);
		if (localModUploadPlanKey.IsNullOrEmpty())
		{
			return false;
		}
		if (!localModUploadPlanCache.TryGetValue(localModUploadPlanKey, out var value))
		{
			return false;
		}
		if (value.workshopId != modInfo.workshopId)
		{
			return false;
		}
		plan = value.plan;
		return plan != null;
	}

	private void CacheLocalModUploadPlan(ModInfo modInfo, WorkshopUploadPlan plan)
	{
		if (modInfo != null && plan != null && modInfo.source == ModSourceType.Local)
		{
			string localModUploadPlanKey = GetLocalModUploadPlanKey(modInfo);
			if (!localModUploadPlanKey.IsNullOrEmpty())
			{
				localModUploadPlanCache[localModUploadPlanKey] = new LocalModUploadPlanCache
				{
					workshopId = modInfo.workshopId,
					plan = plan
				};
			}
		}
	}

	private void ProcessNextLocalModUploadPlanRequest()
	{
		if (!resolvingLocalModUploadPlanKey.IsNullOrEmpty())
		{
			return;
		}
		while (pendingLocalModUploadPlanQueue.Count > 0)
		{
			ModInfo modInfo = pendingLocalModUploadPlanQueue.Dequeue();
			string key = GetLocalModUploadPlanKey(modInfo);
			if (key.IsNullOrEmpty())
			{
				continue;
			}
			queuedLocalModUploadPlanKeys.Remove(key);
			if (!pendingLocalModUploadPlanCallbacks.ContainsKey(key))
			{
				continue;
			}
			if (TryGetCachedLocalModUploadPlan(modInfo, out var plan2))
			{
				List<Action<WorkshopUploadPlan>> list = pendingLocalModUploadPlanCallbacks[key];
				pendingLocalModUploadPlanCallbacks.Remove(key);
				foreach (Action<WorkshopUploadPlan> item in list)
				{
					item?.Invoke(plan2);
				}
				continue;
			}
			resolvingLocalModUploadPlanKey = key;
			uploader.ResolveUploadPlan(modInfo, delegate(WorkshopUploadPlan plan)
			{
				CacheLocalModUploadPlan(modInfo, plan);
				if (pendingLocalModUploadPlanCallbacks.TryGetValue(key, out var value))
				{
					pendingLocalModUploadPlanCallbacks.Remove(key);
					foreach (Action<WorkshopUploadPlan> item2 in value)
					{
						item2?.Invoke(plan);
					}
				}
				resolvingLocalModUploadPlanKey = null;
				ProcessNextLocalModUploadPlanRequest();
			});
			break;
		}
	}

	public bool TryGetResolvedLocalModUploadPlan(ModInfo modInfo, out WorkshopUploadPlan plan)
	{
		return TryGetCachedLocalModUploadPlan(modInfo, out plan);
	}

	public void ResolveLocalModUploadPlan(ModInfo modInfo, Action<WorkshopUploadPlan> onResolved)
	{
		if (modInfo == null || modInfo.source != 0)
		{
			return;
		}
		if (TryGetCachedLocalModUploadPlan(modInfo, out var plan))
		{
			onResolved?.Invoke(plan);
			return;
		}
		string localModUploadPlanKey = GetLocalModUploadPlanKey(modInfo);
		if (!localModUploadPlanKey.IsNullOrEmpty())
		{
			pendingLocalModUploadPlanCallbacks.TryAdd(localModUploadPlanKey, new List<Action<WorkshopUploadPlan>>());
			pendingLocalModUploadPlanCallbacks[localModUploadPlanKey].Add(onResolved);
			if (!queuedLocalModUploadPlanKeys.Contains(localModUploadPlanKey) && resolvingLocalModUploadPlanKey != localModUploadPlanKey)
			{
				pendingLocalModUploadPlanQueue.Enqueue(modInfo);
				queuedLocalModUploadPlanKeys.Add(localModUploadPlanKey);
			}
			ProcessNextLocalModUploadPlanRequest();
		}
	}

	public void UploadLocalMod(ModInfo modInfo, WorkshopUploadPlan plan, Action<WorkshopUploadResult> onCompleted)
	{
		if (modInfo == null || modInfo.source != 0)
		{
			return;
		}
		uploader.UploadMod(modInfo, plan, delegate(WorkshopUploadResult result)
		{
			if (result.success && result.workshopId != 0L)
			{
				modInfo.SetWorkshopId(result.workshopId);
				CacheLocalModUploadPlan(modInfo, new WorkshopUploadPlan(WorkshopUploadMode.Update, result.workshopId));
				DolocAPI.dataPersistenceManager?.SaveModManager(this);
			}
			onCompleted?.Invoke(result);
		});
	}
}
