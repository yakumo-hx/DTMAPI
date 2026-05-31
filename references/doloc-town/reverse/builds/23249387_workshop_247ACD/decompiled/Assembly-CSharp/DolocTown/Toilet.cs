using System;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.TechTree;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class Toilet : Equipment, IGatherableEquipment, IAnimalToilet, IAnimalInteractable, IAnimalMoodAffector
{
	private readonly EquipmentFuncToilet func;

	[JsonProperty]
	[DebugInfo("数量", AllowEdit = true)]
	[DebugOnValueChanged("RefreshView")]
	private int count;

	private readonly GameEntitySlot<EquipmentParticleSystemRenderer> effectsHandle = new GameEntitySlot<EquipmentParticleSystemRenderer>("stink");

	private int __animal_counter;

	public bool IsGatherable => IsToiletFull;

	public override bool IsValid
	{
		get
		{
			if (base.IsValid)
			{
				return func != null;
			}
			return false;
		}
	}

	private Sprite currentSprite
	{
		get
		{
			if (count != 0)
			{
				return func.FullSprite.Asset;
			}
			return proto.Sprite;
		}
	}

	public int MoodContribution
	{
		get
		{
			if (!IsToiletFull)
			{
				return 0;
			}
			return DolocAPI.GlobalParameter.AnimalMoodContributionFullToilet;
		}
	}

	public Room AnimalInteractableRoom => base.Host.CurrentRoom;

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public Vector2Int AnimalInteractablePosition => base.Anchor + new Vector2Int(proto.CoverSize.x / 2, 0);

	public bool IsAnimalInteractableLocked { get; set; }

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

	public bool IsToiletFull => count >= func.Capacity;

	public bool IsToiletEmpty => count == 0;

	public Item[] Gather()
	{
		if (count <= 0)
		{
			return Array.Empty<Item>();
		}
		Item item = DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefFaeces, count);
		DolocAPI.AddTechExp(TechPointType.ANIMAL, count);
		count = 0;
		RefreshView();
		return new Item[1] { item };
	}

	public Toilet(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncToilet)proto.Function;
	}

	[JsonConstructor]
	public Toilet(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, int count)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			if (proto.Function is EquipmentFuncToilet equipmentFuncToilet)
			{
				func = equipmentFuncToilet;
			}
			this.count = count;
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		RefreshView();
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		effectsHandle.Release();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (count > 0)
		{
			this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationClear, DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				this.CreateDropItem(DolocAPI.GlobalParameter.ItemRefFaeces, shouldRender: true, sendMessage: false);
			}
			DolocAPI.AddTechExp(TechPointType.ANIMAL, count);
			count = 0;
			RefreshView();
		}
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
		this.PushSceneOperationTipToHide();
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		for (int i = 0; i < count; i++)
		{
			this.CreateDropItem(DolocAPI.GlobalParameter.ItemRefFaeces, shouldRender: true, sendMessage: false);
		}
		DolocAPI.AddTechExp(TechPointType.ANIMAL, count);
	}

	private void RefreshView()
	{
		if (!base.IsRender || base.Renderer == null)
		{
			return;
		}
		if (IsToiletFull)
		{
			effectsHandle.Do(delegate(EquipmentParticleSystemRenderer R)
			{
				R.Play(base.PositionCenter);
			});
		}
		else
		{
			effectsHandle.Release();
		}
		base.Renderer.Sprite = currentSprite;
	}

	public void Excrete()
	{
		count++;
		if (base.IsRender)
		{
			RefreshView();
		}
	}
}
