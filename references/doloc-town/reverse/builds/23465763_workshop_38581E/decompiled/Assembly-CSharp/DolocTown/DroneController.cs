using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneController
{
	private bool _forceHide;

	private readonly DolocUserInput userInput;

	private readonly RSTimer faceTimer = new RSTimer(3f);

	private readonly RSTimer autoReloadingTimer = new RSTimer(5f);

	private readonly Counter emptyWeaponAlertCounter = new Counter(5);

	private readonly RSTimer lowPowerAlertCDTimer = new RSTimer(3f);

	private readonly RSTimer escapeCombatTimer = new RSTimer(5f);

	private DroneCross shootingCross;

	private DroneCrossDir shootingCrossDir;

	private Vector2 shootingDirCache;

	private bool updateCrossDir;

	private Vector2 shootPositionByJoyStick;

	private bool isAutoAimingValid;

	private Vector2 lastAutoAimPosition;

	private RSTimer autoAimCheckTimer = new RSTimer();

	private DolocInputDeviceType deviceType;

	private bool faceFollowMouse;

	private bool shouldUpdate;

	private bool isSelected;

	private bool couldAlertLowPower;

	public bool ForceHide
	{
		get
		{
			return _forceHide;
		}
		set
		{
			if (CurrentDrone?.renderer != null)
			{
				CurrentDrone.renderer.SetVisible(!value);
			}
			_forceHide = value;
		}
	}

	public bool isCombat { get; private set; }

	private bool IsFireKeyPushed
	{
		get
		{
			if (deviceType.IsJoystick())
			{
				if (!userInput.NormalFire && !userInput.NormalFireInProgress)
				{
					if (DolocAPI.userSettings.joystickShootByDir)
					{
						return userInput.AssistMove != Vector2.zero;
					}
					return false;
				}
				return true;
			}
			if (!userInput.NormalFire)
			{
				return userInput.NormalFireInProgress;
			}
			return true;
		}
	}

	public Drone CurrentDrone { get; private set; }

	private void SetupDroneRenderer(Drone drone, DroneRenderer droneRenderer, bool shouldRender)
	{
		shouldRender &= !ForceHide;
		DroneStruct structure = drone.structure;
		RuntimeAnimatorController asset = structure.proto.Animator.Asset;
		if (asset != null)
		{
			droneRenderer.animatorController = asset;
			droneRenderer.PlayAnimation("idle");
		}
		else
		{
			droneRenderer.animatorController = null;
			droneRenderer.sprite = structure.proto.Sprite.Asset;
		}
		droneRenderer.SwordCollider.SetSwordColliderEnabled(value: false);
		Vector2 size = structure.proto.Sprite.Asset.rect.size;
		droneRenderer.SetShootOffset(structure.GetWeaponSlotPosition(size));
		droneRenderer.ClearComponents();
		DroneSlot[] slots = structure.slots;
		foreach (DroneSlot droneSlot in slots)
		{
			if (!droneSlot.IsEmpty && droneSlot.proto.IsVisual)
			{
				IDroneComponentItem droneComponentItem = droneSlot.item as IDroneComponentItem;
				float x = ((float)(droneSlot.proto.VisualPivot.x - droneComponentItem.ComponentPivot.x) - size.x * 0.5f) * 0.125f;
				float num = ((float)(droneSlot.proto.VisualPivot.y - droneComponentItem.ComponentPivot.y) - size.y * 0.5f) * 0.125f;
				DroneComponentRenderer renderer = droneRenderer.RenderComponent(droneComponentItem?.ComponentSprite, new Vector3(x, num, ZOffsetOfType(droneSlot.proto.VisualType) - num));
				drone._OnRenderComponent(droneSlot, renderer);
			}
		}
		droneRenderer.SetFollowTarget(DolocAPI.DroneFollowTarget);
		droneRenderer.ResetPosition();
		droneRenderer.SetVisible(shouldRender);
		droneRenderer.shouldLightUp = shouldRender && DolocAPI.archiveHandle.ShouldLightUp;
		static float ZOffsetOfType(SlotVisualSuitableType type)
		{
			return type switch
			{
				SlotVisualSuitableType.Front => -0.1f, 
				SlotVisualSuitableType.Bottom => 0f, 
				SlotVisualSuitableType.Back => 0.1f, 
				_ => 0f, 
			};
		}
	}

	public DroneController(DolocUserInput userInput)
	{
		this.userInput = userInput;
		couldAlertLowPower = true;
		shootingCross = DolocGameAssets.GAME_ENTITY_DRONE_CROSS.CreateEntity<DroneCross>();
		shootingCross.SetVisible(value: false);
		shootingCrossDir = DolocGameAssets.GAME_ENTITY_DRONE_CROSS_DIR.CreateEntity<DroneCrossDir>();
		shootingCrossDir.SetVisible(value: false);
	}

	public void SetSelected(bool value)
	{
		isSelected = value;
		shootingCrossDir.IsSelected = value;
		if (isSelected)
		{
			if (CurrentDrone.weapon is DroneWeaponGun)
			{
				shootingCross.SetVisible(!updateCrossDir || CurrentDrone.weapon._autoBattle);
				shootingCrossDir.SetVisible(updateCrossDir);
			}
			shootingCross.position2d = DolocAPI.WorldToScreen(CurrentDrone.renderer.position2d);
		}
		else
		{
			shootingCross.SetVisible(value: false);
			shootingCrossDir.SetVisible(value: false);
		}
	}

	public void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		deviceType = type;
		if (CurrentDrone != null)
		{
			updateCrossDir = type.IsJoystick() && DolocAPI.userSettings.joystickShootByDir;
			shootingCross.SetVisible((!updateCrossDir && isSelected) || CurrentDrone.weapon._autoBattle);
			shootingCrossDir.SetVisible(updateCrossDir && isSelected);
		}
	}

	public Vector2 ResetRendererPosition()
	{
		if (CurrentDrone == null)
		{
			return Vector2.zero;
		}
		CurrentDrone.renderer.ResetPosition();
		return CurrentDrone.renderer.position2d;
	}

	public void OnUpdate(float dt)
	{
		if (shouldUpdate)
		{
			UpdateDroneDirection(dt);
			CurrentDrone.OnUpdate(dt);
			HandleUserInput(dt);
			UpdateCrossPosition(dt);
			UpdateCrossDir();
		}
	}

	public void OnFixedUpdate(float dt)
	{
		if (shouldUpdate)
		{
			EscapingCombat(dt);
			CurrentDrone.OnFixedUpdate(dt);
			if (!couldAlertLowPower && lowPowerAlertCDTimer.Tick(dt))
			{
				couldAlertLowPower = true;
			}
		}
	}

	private void UpdateCrossDir()
	{
		if (updateCrossDir)
		{
			shootingCrossDir.Position = CurrentDrone.renderer.ShootPosition;
			Vector2 normalized = userInput.AssistMove.normalized;
			if (normalized != Vector2.zero)
			{
				shootingDirCache = normalized;
			}
			shootingCrossDir.Direction = normalized;
		}
	}

	private void UpdateCrossPosition(float dt)
	{
		if (CurrentDrone.weapon._autoBattle)
		{
			if (CurrentDrone.Env.TryGetNearestMonsterPosToAgent(30f, out var pos))
			{
				shootPositionByJoyStick = DolocAPI.WorldToScreen(pos);
				shootingCross.position2d = pos;
				shootingCross.SetVisible(value: true);
				return;
			}
			shootingCross.SetVisible(value: false);
		}
		if (!shootingCross.isVisible)
		{
			return;
		}
		if (isAutoAimingValid && autoAimCheckTimer.Tick(dt))
		{
			isAutoAimingValid = false;
		}
		if (DolocAPI.cursorManager.HiddenCursorByGamepad)
		{
			if (DolocAPI.userSettings.autoAim && isAutoAimingValid)
			{
				shootingCross.position2d = lastAutoAimPosition;
			}
			else
			{
				shootingCross.position2d = DolocAPI.ScreenToWorld(shootPositionByJoyStick);
			}
		}
		else if (DolocAPI.userSettings.autoAim && isAutoAimingValid)
		{
			shootingCross.position2d = lastAutoAimPosition;
		}
		else
		{
			shootingCross.position2d = DolocAPI.ScreenToWorld(userInput.MousePosition);
			shootPositionByJoyStick = userInput.MousePosition;
		}
	}

	private void HandleUserInput(float dt)
	{
		if (userInput.NormalSelectedDrone && deviceType != 0 && !isSelected)
		{
			DolocAPI.uiSystem.inventoryQuick.SelectDrone();
		}
		if (CurrentDrone.weapon.IsGun)
		{
			_HandleUserInputGun(dt);
			if (DolocAPI.cursorManager.HiddenCursorByGamepad)
			{
				shootPositionByJoyStick += userInput.AssistMove * (600f * dt);
				shootPositionByJoyStick.x = Mathf.Clamp(shootPositionByJoyStick.x, 0f, Screen.width);
				shootPositionByJoyStick.y = Mathf.Clamp(shootPositionByJoyStick.y, 0f, Screen.height);
			}
		}
		else if (CurrentDrone.weapon.IsSword)
		{
			_HandleUserInputSword(dt);
		}
	}

	private void _HandleUserInputGun(float dt)
	{
		if (userInput.NormalSwitchAutoFire)
		{
			CurrentDrone.weapon.SwitchAutoFire();
		}
		if (CurrentDrone.weapon._autoBattle)
		{
			AutoBattle();
			if (!shootingCross.isVisible && isSelected)
			{
				shootingCross.SetVisible(value: true);
			}
			if (IsFireKeyPushed && isSelected)
			{
				CurrentDrone.weapon.SwitchAutoFire();
			}
		}
		else if (isSelected)
		{
			if (!updateCrossDir && !shootingCross.isVisible)
			{
				shootingCross.SetVisible(value: true);
			}
			if (IsFireKeyPushed)
			{
				_ManualFire();
			}
		}
		if (CurrentDrone.weapon.NeedReload)
		{
			if (CurrentDrone.weapon.IsEmpty)
			{
				CurrentDrone.weapon.Reload();
			}
			else if (autoReloadingTimer.Tick(dt))
			{
				CurrentDrone.weapon.Reload();
			}
		}
	}

	private void _HandleUserInputSword(float dt)
	{
		if (userInput.NormalSwitchAutoFire)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrSwordCannotAutofire);
		}
		if (!isSelected)
		{
			return;
		}
		if (userInput.NormalFire)
		{
			if (CurrentDrone.weapon.IsSwordReadyNow || CurrentDrone.renderer.IsDash || Vector2.Distance(CurrentDrone.renderer.position2d, DolocAPI.AgentPosition) > 10f)
			{
				return;
			}
			if (!CurrentDrone.TryCostPower(CurrentDrone.weapon.weaponParams.PowerCost))
			{
				if (couldAlertLowPower)
				{
					DolocAPI.RaiseInstantAnimEffects(CurrentDrone.renderer.position2d, InstAnimEffectType.LOW_POWER, Vector2.right, flip: false, LocMaterials.GAME_MAT_2D_UNLIT, "SceneUI");
					couldAlertLowPower = false;
				}
			}
			else
			{
				CurrentDrone.weapon.StartSwordReady(GetSwordAttackDir());
			}
		}
		else if (userInput.NormalFireInProgress)
		{
			if (CurrentDrone.weapon.IsSwordReadyNow)
			{
				CurrentDrone.weapon.SwordReady(dt);
				if (Vector2.Distance(CurrentDrone.renderer.position2d, DolocAPI.AgentPosition) > 30f)
				{
					CurrentDrone.weapon.TrySwordAttack(GetSwordAttackDir());
					OnStartCombat();
				}
			}
		}
		else
		{
			CurrentDrone.weapon.TrySwordAttack(GetSwordAttackDir());
			OnStartCombat();
		}
	}

	private void AutoBattle()
	{
		Drone currentDrone = CurrentDrone;
		if (currentDrone.weapon.IsSword)
		{
			return;
		}
		BulletProto bulletProto = currentDrone.weapon.proto.BulletProto;
		float b = (bulletProto.isCircle ? bulletProto.colliderRadius : bulletProto.colliderSize.y);
		b = Mathf.Min(1f, b);
		if (!currentDrone.Env.TryGetNearestMonsterPosToAgent(currentDrone.weapon.weaponParams.AttackDistance, b, out var pos))
		{
			return;
		}
		Vector2 normalized = (pos - currentDrone.renderer.ShootPosition).normalized;
		if (currentDrone.weapon.GunAttack(normalized, isAuto: true))
		{
			OnStartCombat();
			autoReloadingTimer.Reset();
			if (normalized.x != 0f)
			{
				currentDrone.renderer.FaceRight = normalized.x > 0f;
			}
			shootingCross.PlayAttackTween();
		}
	}

	private void UpdateDroneDirection(float dt)
	{
		if (CurrentDrone.weapon._autoBattle || !faceFollowMouse)
		{
			return;
		}
		if (faceTimer.Tick(dt))
		{
			faceFollowMouse = false;
			CurrentDrone.renderer.AutoChangeDir = true;
		}
		if (deviceType == DolocInputDeviceType.KeyboardMouse)
		{
			Vector2 vector = DolocAPI.ScreenToWorld(userInput.MousePosition);
			CurrentDrone.renderer.FaceRight = vector.x - CurrentDrone.renderer.position.x > 0f;
			return;
		}
		Vector2 normalized = userInput.AssistMove.normalized;
		if (normalized.x != 0f)
		{
			CurrentDrone.renderer.FaceRight = normalized.x > 0f;
		}
	}

	private Vector2 GetShootingDir()
	{
		Vector2 shootPosition = CurrentDrone.renderer.ShootPosition;
		if (DolocAPI.userSettings.autoAim)
		{
			float attackDistance = CurrentDrone.weapon.weaponParams.AttackDistance;
			autoAimCheckTimer.Reset();
			isAutoAimingValid = CurrentDrone.Env.TryGetNearestMonsterPosToAgent(attackDistance, out var pos);
			if (isAutoAimingValid)
			{
				lastAutoAimPosition = pos;
				return (pos - shootPosition).normalized;
			}
		}
		if (deviceType == DolocInputDeviceType.KeyboardMouse)
		{
			return (DolocAPI.ScreenToWorld(userInput.MousePosition) - shootPosition).normalized;
		}
		if (DolocAPI.userSettings.joystickShootByDir)
		{
			Vector2 normalized = DolocAPI.UserInput.AssistMove.normalized;
			if (normalized == Vector2.zero)
			{
				return shootingDirCache;
			}
			shootingDirCache = normalized;
			return normalized;
		}
		return (DolocAPI.ScreenToWorld(shootPositionByJoyStick) - shootPosition).normalized;
	}

	private Vector2 GetSwordAttackDir()
	{
		if (deviceType == DolocInputDeviceType.KeyboardMouse)
		{
			return (DolocAPI.ScreenToWorld(userInput.MousePosition) - CurrentDrone.renderer.position2d).normalized;
		}
		if (userInput.AssistMove != Vector2.zero)
		{
			return userInput.AssistMove.normalized;
		}
		return new Vector2(CurrentDrone.renderer.FaceRight ? 1 : (-1), 0f);
	}

	private void _ManualFire()
	{
		if (CurrentDrone.IsEmptyWeapon)
		{
			if (emptyWeaponAlertCounter.Tick())
			{
				DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.UiOperationErrEmptyDrone);
			}
		}
		else if (!CurrentDrone.weapon.IsFireNotAvailable)
		{
			faceFollowMouse = true;
			faceTimer.Reset();
			CurrentDrone.renderer.AutoChangeDir = false;
			if (CurrentDrone.weapon.GunAttack(GetShootingDir()))
			{
				shootingCross.PlayAttackTween();
				OnStartCombat();
			}
		}
	}

	public void UnloadDrone()
	{
		if (CurrentDrone != null)
		{
			CurrentDrone.Dispose();
			CurrentDrone.renderer.SetVisible(value: false);
			SetCurrentDrone(null);
		}
	}

	public void RunDrone(DroneStruct droneStructure, bool shouldRender = true)
	{
		if (droneStructure != null)
		{
			if (CurrentDrone != null && CurrentDrone.structure == droneStructure)
			{
				Debug.Log("RunDrone: 无人机已经初始化，无需再次初始化");
				return;
			}
			Debug.Log("RunDrone: 初始化新的无人机实例");
			CurrentDrone?.Dispose();
			Drone drone = new Drone(droneStructure, DolocAPI.droneRenderer, DolocAPI.battleSystem.droneEnv);
			SetCurrentDrone(drone);
			SetupDroneRenderer(drone, DolocAPI.droneRenderer, shouldRender);
		}
	}

	public void OnPause()
	{
		CurrentDrone?.OnPause();
	}

	public void OnResume()
	{
		CurrentDrone?.OnResume();
		if (shouldUpdate && CurrentDrone != null)
		{
			DroneStruct structure = CurrentDrone.structure;
			if (structure.IsComponentChanged())
			{
				UnloadDrone();
				DolocAPI.RunDrone(structure);
			}
		}
	}

	public void SetCurrentDrone(Drone drone)
	{
		CurrentDrone = drone;
		shouldUpdate = drone != null;
	}

	public void SetDroneFollower(Transform follower)
	{
		CurrentDrone?.renderer.SetFollowTarget(follower);
	}

	private void OnStartCombat()
	{
		isCombat = true;
		escapeCombatTimer.Reset();
	}

	private void EscapingCombat(float dt)
	{
		if (isCombat && escapeCombatTimer.Tick(dt))
		{
			isCombat = false;
			OnEscapeCombat();
		}
	}

	private void OnEscapeCombat()
	{
	}
}
