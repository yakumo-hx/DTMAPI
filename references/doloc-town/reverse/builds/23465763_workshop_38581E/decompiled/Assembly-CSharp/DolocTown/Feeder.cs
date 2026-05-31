using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class Feeder : Equipment, IFeeder, IAnimalInteractable
{
	private readonly EquipmentFuncFeeder _func;

	[JsonProperty]
	[DebugInfo("剩余饲料")]
	private int feederCount;

	private int __animal_counter;

	public override bool IsDirty => feederCount > 0;

	public float progress
	{
		get
		{
			if (feederCount != _func.Capacity)
			{
				return (float)feederCount / (float)_func.Capacity;
			}
			return 1f;
		}
	}

	public override bool CanInteractContinues
	{
		get
		{
			if (CheckCurrentItemCanInteract(out var _))
			{
				return progress < 1f;
			}
			return false;
		}
	}

	public Vector2 BarPosition => base.PositionTop;

	public int FeederPriority => 0;

	public Room AnimalInteractableRoom => base.Host.CurrentRoom;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public Vector2Int AnimalInteractablePosition
	{
		get
		{
			if (proto.CoverSize.x <= 3)
			{
				return base.Anchor;
			}
			return new Vector2Int(base.Anchor.x + proto.CoverSize.x / 2, base.Anchor.y);
		}
	}

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool IsAnimalInteractableLocked { get; set; }

	public bool IsFeederEmpty => feederCount == 0;

	public int AnimalCounter
	{
		get
		{
			return __animal_counter;
		}
		set
		{
			__animal_counter = Mathf.Max(0, value);
		}
	}

	public Feeder(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		if (!(proto.Function is EquipmentFuncFeeder func))
		{
			Debug.LogError($"Feeder proto is not EquipmentFuncFeeder: {proto}");
			return;
		}
		_func = func;
		feederCount = 0;
	}

	[JsonConstructor]
	public Feeder(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, int feederCount = 0)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			if (!(proto.Function is EquipmentFuncFeeder func))
			{
				Debug.LogError($"Feeder proto is not EquipmentFuncFeeder: {proto}");
				return;
			}
			_func = func;
			this.feederCount = feederCount;
		}
	}

	private void UpdateBarRenderer(ProgressBarRenderer renderer)
	{
		if (!(renderer == null))
		{
			renderer.position = BarPosition;
			renderer.SetVisible(value: true);
			if (feederCount <= 0)
			{
				renderer.Progress = -1f;
			}
			else
			{
				renderer.Progress = progress;
			}
		}
	}

	private void UpdateBarRendererOnlyVisible()
	{
		ProgressBarRenderer progressBarRenderer = base.Renderer.FetchRenderComponent<ProgressBarRenderer>();
		if (!(progressBarRenderer == null) && progressBarRenderer.isVisible)
		{
			UpdateBarRenderer(progressBarRenderer);
		}
	}

	private void UpdateBarRenderer()
	{
		ProgressBarRenderer renderComponent = base.Renderer.GetRenderComponent<ProgressBarRenderer>();
		UpdateBarRenderer(renderComponent);
	}

	private void UpdateSprite()
	{
		if (base.IsRender)
		{
			base.Renderer.Sprite = ((feederCount > 0) ? _func.FullSprite.Asset : proto.Sprite);
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		UpdateSprite();
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		base.Renderer.RemoveRenderComponent<ProgressBarRenderer>();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationFill, DolocAPI.UserInput.GlobalInteractActionName);
		UpdateBarRenderer();
	}

	public bool IsAnimalFeeds(ItemInfo item, out int energy)
	{
		return DolocConfig.Tables.TbFeed.IsFeederFeeds(item?.Id, out energy);
	}

	private bool CheckCurrentItemCanInteract(out int energy)
	{
		return IsAnimalFeeds(DolocAPI.SelectedItem?.proto, out energy);
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem == null)
		{
			return;
		}
		if (!CheckCurrentItemCanInteract(out var energy))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrNotFeeds, selectedItem.title));
			return;
		}
		if (feederCount >= _func.Capacity)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrFeederFull);
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			PushTip();
			if (selectedItem.CostSelf(out var _, showFadeUpIcon: false))
			{
				feederCount += energy;
				UpdateBarRenderer();
				UpdateSprite();
			}
		});
	}

	public void AddFeeds(ItemInfo item)
	{
		if (item != null && IsAnimalFeeds(item, out var energy))
		{
			feederCount += energy;
			if (base.IsRender)
			{
				UpdateBarRendererOnlyVisible();
				UpdateSprite();
			}
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
		if (base.Renderer != null)
		{
			base.Renderer.RemoveRenderComponent<ProgressBarRenderer>();
		}
	}

	public int TakeFeeds(int require, out string name)
	{
		name = null;
		int result = require;
		if (feederCount > require)
		{
			feederCount -= require;
		}
		else
		{
			result = feederCount;
			feederCount = 0;
		}
		if (base.IsRender)
		{
			UpdateSprite();
			UpdateBarRendererOnlyVisible();
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.FEEDS_DEBRIS);
		}
		return result;
	}
}
