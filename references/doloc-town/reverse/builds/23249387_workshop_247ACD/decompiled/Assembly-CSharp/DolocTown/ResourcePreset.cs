using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class ResourcePreset : ObjectPreset
{
	[SerializeField]
	public string resourceName;

	[SerializeField]
	[Range(0f, 10f)]
	public int initGrowthLevel;

	[SerializeField]
	public bool disableRefresh;

	private Sprite _sprite;

	private ResourceInfo resourceData => DolocConfig.Tables.TbResource.GetOrDefault(resourceName);

	private bool validLevel
	{
		get
		{
			if (isGrowth)
			{
				return initGrowthLevel <= resourceData.MaxLevel;
			}
			return false;
		}
	}

	private bool isGrowth => resourceData.MaxLevel > 0;

	public override Sprite Sprite => _sprite;

	public override Vector2Int GridSize
	{
		get
		{
			if (resourceData == null)
			{
				return Vector2Int.one;
			}
			return resourceData.Size;
		}
	}

	public override string PresetObjectName
	{
		get
		{
			if (resourceData == null)
			{
				return "未知资源";
			}
			return resourceData.Id;
		}
	}

	private void Start()
	{
		base.gameObject.SetActive(value: false);
	}

	public override bool CreatePreset(out RoomPresetObjectSO preset)
	{
		if (resourceData == null)
		{
			preset = null;
			return false;
		}
		initGrowthLevel = (isGrowth ? Mathf.Min(initGrowthLevel, resourceData.MaxLevel) : 0);
		preset = new RoomPresetObjectSO(RoomPresetObjectType.Resource, resourceData.Id, LocalGridPosition, initGrowthLevel, disableRefresh);
		return true;
	}
}
