using System;
using System.Collections.Generic;
using System.IO;
using DolocTown.Config.Mod;
using DolocTown.Config.Player;
using DolocTown.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace DolocTown.Config;

[JsonObject(MemberSerialization.OptIn)]
public class ModInfo
{
	private const string PreviewImageFileName = "preview.png";

	private const string IconImageFileName = "icon.png";

	private const string ManifestFileName = "info.json";

	private const string WorkshopInfoFileName = "workshop.json";

	private const string IgnoreFolderName = "_ignore";

	[JsonProperty]
	public string id;

	[JsonProperty]
	public bool enabled;

	[JsonProperty]
	public int priority;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public ModSourceType source;

	public string savedTitle;

	public Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

	public Dictionary<string, List<string>> configs = new Dictionary<string, List<string>>();

	public bool HasPlayerOverride { get; private set; }

	public bool HasPlayerHairOverride { get; private set; }

	public bool HasPlayerBodyOverride { get; private set; }

	public ulong workshopId { get; private set; }

	[JsonProperty]
	public string title => manifest?.Title ?? id;

	public string author => manifest?.author ?? id;

	public string description => manifest?.Description ?? id;

	public Sprite icon { get; private set; }

	public string previewPath { get; private set; }

	public string iconPath { get; private set; }

	public string rootPath { get; private set; }

	public ModManifest manifest { get; private set; }

	public string[] tags => manifest?.tags ?? Array.Empty<string>();

	private void UpdateFlags()
	{
		UpdatePlayerFlags();
	}

	private void UpdatePlayerFlags()
	{
		HasPlayerOverride = true;
		HasPlayerHairOverride = true;
		HasPlayerBodyOverride = true;
		foreach (PlayerAnimationFrameInfo data in DolocConfig.Tables.TbPlayerAnimationFrame.DataList)
		{
			for (int i = 0; i < data.FrameCount; i++)
			{
				string arg = (data.OverrideId.IsNullOrEmpty() ? data.Id : data.OverrideId);
				if (HasPlayerOverride && !sprites.ContainsKey($"anim_player_{arg}_{i}"))
				{
					HasPlayerOverride = false;
				}
				if (HasPlayerHairOverride && !sprites.ContainsKey($"anim_player_hair_{arg}_{i}"))
				{
					HasPlayerHairOverride = false;
				}
				if (HasPlayerBodyOverride && !sprites.ContainsKey($"anim_player_body_{arg}_{i}"))
				{
					HasPlayerBodyOverride = false;
				}
				if (!HasPlayerOverride && !HasPlayerHairOverride && !HasPlayerBodyOverride)
				{
					break;
				}
			}
		}
	}

	[JsonConstructor]
	public ModInfo(string id, bool enabled, ModSourceType source, int priority = -1, string title = "")
	{
		this.id = id;
		this.enabled = enabled;
		this.source = source;
		this.priority = priority;
		savedTitle = title;
	}

	public void Init(string rootPath, ModManifest manifest, ModSourceType source, ulong workshopId)
	{
		this.rootPath = rootPath;
		this.source = source;
		this.manifest = manifest;
		this.workshopId = workshopId;
		previewPath = Path.Combine(rootPath, "preview.png");
		iconPath = Path.Combine(rootPath, "icon.png");
		icon = CreateSpriteFromFile(iconPath);
		savedTitle = title;
		ClearCache();
	}

	public void SetWorkshopId(ulong workshopId)
	{
		this.workshopId = workshopId;
		FileUtils.TrySaveJson(GetWorkshopInfoPath(), new ModWorkshopInfo(workshopId));
	}

	public static bool TryLoadWorkshopInfo(string rootPath, out ulong workshopId)
	{
		workshopId = 0uL;
		string text = Path.Combine(rootPath, "workshop.json");
		if (!File.Exists(text))
		{
			return false;
		}
		try
		{
			workshopId = JsonConvert.DeserializeObject<ModWorkshopInfo>(File.ReadAllText(text))?.workshopId ?? 0;
			return true;
		}
		catch (Exception arg)
		{
			Debug.LogError($"[MOD] Failed to load workshop info: {text}\n{arg}");
			return false;
		}
	}

	private string GetWorkshopInfoPath()
	{
		return Path.Combine(rootPath, "workshop.json");
	}

	public void ToggleEnabled()
	{
		enabled = !enabled;
	}

	public void UpdateCache()
	{
		ClearCache();
		if (Directory.Exists(rootPath))
		{
			LoadConfigs();
			LoadSprites();
			UpdateFlags();
		}
	}

	public void ClearCache()
	{
		foreach (Sprite value in sprites.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		sprites.Clear();
		configs.Clear();
	}

	private void LoadSprites()
	{
		string text = Path.Combine(rootPath, "Content");
		if (!Directory.Exists(rootPath))
		{
			return;
		}
		foreach (string filesExcludingDir in FileUtils.GetFilesExcludingDirs(text, "*.png", "_ignore"))
		{
			try
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filesExcludingDir);
				Sprite sprite = CreateSpriteFromFile(filesExcludingDir);
				if (sprite != null)
				{
					sprites[fileNameWithoutExtension] = sprite;
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"[MOD] <{id}> Sprite load failed: {filesExcludingDir}\n{arg}");
			}
		}
	}

	private Sprite CreateSpriteFromFile(string pngPath)
	{
		if (!File.Exists(pngPath))
		{
			return null;
		}
		byte[] data = File.ReadAllBytes(pngPath);
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
		texture2D.LoadImage(data, markNonReadable: false);
		texture2D.filterMode = FilterMode.Point;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Vector2 pivot = new Vector2(0.5f, 0.5f);
		float pixelsPerUnit = 8f;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pngPath);
		string text = Path.ChangeExtension(pngPath, ".json");
		if (configs.TryGetValue(fileNameWithoutExtension, out var value) && value.Count > 0)
		{
			text = value[0];
		}
		if (File.Exists(text))
		{
			try
			{
				ModSpriteMeta modSpriteMeta = JsonConvert.DeserializeObject<ModSpriteMeta>(File.ReadAllText(text));
				if (modSpriteMeta != null)
				{
					pivot = modSpriteMeta.CalNormalizedPivot(texture2D.width, texture2D.height);
					if (modSpriteMeta.pixelsPerUnit > 0f)
					{
						pixelsPerUnit = modSpriteMeta.pixelsPerUnit;
					}
				}
			}
			catch (Exception arg)
			{
				Debug.LogWarning($"[MOD] <{id}> Sprite meta parse failed: {text}\n{arg}");
			}
		}
		else
		{
			bool flag = false;
			foreach (ModImageSettingInfo data2 in DolocConfig.Tables.TbModImageSetting.DataList)
			{
				if (fileNameWithoutExtension.StartsWith(data2.Id))
				{
					pivot = (data2.UseBottomCenterAsPivot ? new Vector2((float)Mathf.FloorToInt((float)texture2D.width / 2f) / (float)texture2D.width, 2f / (float)texture2D.height) : data2.DefaultPivot);
					flag = true;
					break;
				}
			}
			if (!flag && (fileNameWithoutExtension.StartsWith("sprite_") || fileNameWithoutExtension.StartsWith("anim_")))
			{
				pivot = new Vector2((float)Mathf.FloorToInt((float)texture2D.width / 2f) / (float)texture2D.width, 2f / (float)texture2D.height);
			}
		}
		Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
		return Sprite.Create(texture2D, rect, pivot, pixelsPerUnit, 0u, SpriteMeshType.Tight, Vector4.zero, generateFallbackPhysicsShape: true);
	}

	private void LoadConfigs()
	{
		string path = Path.Combine(rootPath, "Content");
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string filesExcludingDir in FileUtils.GetFilesExcludingDirs(path, "*.json", "_ignore"))
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filesExcludingDir);
			configs.TryAdd(fileNameWithoutExtension, new List<string>());
			configs[fileNameWithoutExtension].Add(filesExcludingDir);
		}
	}
}
