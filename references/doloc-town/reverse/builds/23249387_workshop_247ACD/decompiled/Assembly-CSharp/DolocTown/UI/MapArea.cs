using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MapArea : DolocUiObject
{
	[SerializeField]
	private string mapId;

	[SerializeField]
	private string areaId;

	private CanvasGroup canvasGroup;

	private MapAreaInfo _proto;

	[HideInInspector]
	public UnityEvent OnPointerEnter = new UnityEvent();

	[HideInInspector]
	public UnityEvent OnPointerExit = new UnityEvent();

	public MapAreaInfo proto
	{
		get
		{
			if (_proto == null)
			{
				_proto = DolocConfig.Tables.TbMapArea.Get(mapId, areaId);
			}
			return _proto;
		}
	}

	public string AreaId => areaId;

	public MapAreaType AreaType => proto.MapAreaType;

	public string Label => proto?.Label ?? "";

	public MapPoint mapPoint { get; private set; }

	[HideInInspector]
	public Selectable selectable => mapPoint.button;

	public Vector2 centerPos
	{
		get
		{
			if (AreaType != MapAreaType.Fixpoint)
			{
				return base.position + new Vector2(base.size.x / 2f, base.size.y / 2f);
			}
			return base.position;
		}
	}

	public Vector2 topCenterPos
	{
		get
		{
			if (AreaType != MapAreaType.Fixpoint)
			{
				return base.position + new Vector2(base.size.x / 2f, base.size.y);
			}
			return base.position;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (AreaType == MapAreaType.Label)
		{
			GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_CITY_MAP_POINT);
			mapPoint = Object.Instantiate(asset, base.transform).GetComponent<MapPoint>();
			mapPoint.Init();
			mapPoint.rectTransform.anchorMin = Vector2.zero;
			mapPoint.rectTransform.anchorMax = Vector2.one;
			mapPoint.rectTransform.pivot = Vector2.zero;
			mapPoint.positionLocal = Vector2.zero;
			mapPoint.size = Vector2.zero;
			mapPoint.onPointerEnter.AddListener(delegate
			{
				OnPointerEnter.Invoke();
			});
			mapPoint.onPointerExit.AddListener(delegate
			{
				OnPointerExit.Invoke();
			});
		}
	}

	public Vector2 GetPosByPercent(float xPercent, float yPercent)
	{
		MapAreaType areaType = AreaType;
		if (areaType == MapAreaType.Room || areaType == MapAreaType.HiddenArea)
		{
			return base.position + new Vector2(base.size.x * xPercent, base.size.y * yPercent) / DolocAPI.screenManager.scaleFactor;
		}
		return base.position;
	}
}
