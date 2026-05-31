using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ShowerRoom : Equipment
{
	private readonly EquipmentFuncShowerRoom _showerRoom;

	public ShowerRoom(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		_showerRoom = (EquipmentFuncShowerRoom)proto.Function;
	}

	[JsonConstructor]
	private ShowerRoom(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			_showerRoom = (EquipmentFuncShowerRoom)proto.Function;
		}
	}

	private void ApplyEffects()
	{
		DolocAPI.AddBuff(_showerRoom.BuffId);
		if (!DolocAPI.archiveHandle.farmData.agentData.CheckHasShoweredToday())
		{
			int num = (int)(_showerRoom.HealthRecv * (float)DolocAPI.archiveHandle.farmData.agentData.maxHealth);
			int healthGap = DolocAPI.archiveHandle.farmData.agentData.HealthGap;
			if (healthGap > num)
			{
				DolocAPI.AddHealth(num);
			}
			else
			{
				int value = num - healthGap;
				DolocAPI.archiveHandle._SetOverflowHealth(value);
				DolocAPI.AddHealth(healthGap);
			}
			int num2 = (int)(_showerRoom.EnergyRecv * (float)DolocAPI.archiveHandle.farmData.agentData.maxEnergy);
			int energyGap = DolocAPI.archiveHandle.farmData.agentData.EnergyGap;
			if (energyGap > num2)
			{
				DolocAPI.AddEnergy(num2);
				return;
			}
			int value2 = num2 - energyGap;
			DolocAPI.archiveHandle._SetOverflowEnergy(value2);
			DolocAPI.AddEnergy(energyGap);
		}
	}

	private async UniTask _Animation()
	{
		IDialogueEntity player = DolocAPI.GetDialogueTargetViewOrDefault("player");
		await player.WalkTo(new Vector2(base.PositionCenter.x, player.WorldPosition.y), player.DefaultWalkSpeed);
		await UniTask.Delay(750);
		player.SetEntityVisible(value: false);
		base.Renderer.Sr.ToggleAlphaMask(_showerRoom.Mask.Asset);
		SingleSpriteRender backRenderer = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
		backRenderer.sprite = proto.Sprite;
		backRenderer.position2d = base.PositionBottom;
		backRenderer.SortingLayerName = "Platform";
		backRenderer.SpriteRenderer.ToggleAlphaMask(_showerRoom.Mask.Asset, shouldRevert: true);
		await UniTask.Delay(750);
		ContinuesParticleEffects effects = DolocAPI.RaiseContinuesPS(base.PositionTop - new Vector3(0f, 0.75f, 0f), ContinuesParticleEffectsType.SHOWER_BATH);
		ContinuesParticleEffects effects2 = DolocAPI.RaiseContinuesPS(base.PositionCenter, ContinuesParticleEffectsType.SHOWER_MIST);
		await UniTask.Delay(4500);
		effects.Stop();
		effects2.Stop();
		await UniTask.Delay(1000);
		player.SetWorldPosition(base.PositionBottom);
		player.SetEntityVisible(value: true);
		DolocAPI.EntitySystem.Recycle(backRenderer);
		base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
	}

	private async UniTaskVoid PlayAnimation()
	{
		await UniTask.Delay(1);
		await CutSceneState.PlayTask(_Animation());
		ApplyEffects();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(DolocConfig.StaticTexts.UiOperationShower);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (!this.TryCostWater(_showerRoom.WaterCost))
		{
			this.PushSceneOperationTip();
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWaterAround);
			return;
		}
		this.PushSceneOperationTipToHide();
		DolocAPI.agent._Interact(delegate
		{
			PlayAnimation().Forget();
		});
	}
}
