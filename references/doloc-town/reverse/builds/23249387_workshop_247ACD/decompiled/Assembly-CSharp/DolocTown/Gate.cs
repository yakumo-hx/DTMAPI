using System;
using DolocTown.Config;
using DolocTown.Config.Room;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class Gate : Portal, IGate
{
	[SerializeField]
	private bool useIndicator;

	[SerializeField]
	private GameObject indicator;

	[SerializeField]
	private Vector2 tipOffset;

	protected PortalInfo _config;

	[SerializeField]
	private Transform agentReference;

	[SerializeField]
	private Transform motorReference;

	[SerializeField]
	private string dstId;

	private GateManager gateManager => DolocAPI.archiveHandle.cityData.gateManager;

	public bool IsDisabledNow => !DolocAPI.archiveHandle.CheckGateEnable(portalId);

	public virtual PortalInfo config
	{
		get
		{
			if (_config == null)
			{
				_config = DolocConfig.Tables.TbPortal.GetOrDefault(portalId);
			}
			return _config;
		}
	}

	protected bool hasTarget => config?.TargetId_Ref != null;

	public Vector2 remotePos
	{
		get
		{
			if (!hasTarget)
			{
				return Vector2.zero;
			}
			return config.TargetPosition;
		}
	}

	public SceneType remoteSceneType
	{
		get
		{
			if (!hasTarget)
			{
				return SceneType.NONE;
			}
			return config.TargetId_Ref.RoomType;
		}
	}

	public string transportSceneName
	{
		get
		{
			if (!hasTarget)
			{
				return "";
			}
			return config.TargetId_Ref.SceneRawName;
		}
	}

	private bool IsOpen
	{
		get
		{
			if (!hasTarget)
			{
				return false;
			}
			if (!config.UseTimeRange)
			{
				return true;
			}
			int hour = DolocAPI.archiveHandle.DateNow.Hour;
			return config.TimeRange.InRange(hour);
		}
	}

	public bool AvailableToMotor => config?.AvailableToMotor ?? false;

	public virtual bool NeedInteract => config?.NeedInteract ?? false;

	public virtual bool KeepHorizontalSpeed => config?.KeepHorizontalSpeed ?? false;

	public virtual bool KeepVerticalSpeed => config?.KeepVerticalSpeed ?? false;

	public PortalInteractKey InteractKey => config?.InteractKeyType ?? PortalInteractKey.None;

	protected virtual bool DoTransport(Action callback)
	{
		if (DolocAPI.userInput.CurrentState is DialogueState)
		{
			return false;
		}
		if (DolocAPI.gameManager.gameInitConfig.skipInteractionConditionCheck)
		{
			return _DoTransport(callback);
		}
		if (!DolocAPI.CheckAvailableInCurrentState(config.Id))
		{
			return false;
		}
		if (IsDisabledNow)
		{
			DolocAPI.ShowMessageBoxSmall(config.DisableTip);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_BUILDING_ERROR);
			return false;
		}
		if (!IsOpen)
		{
			DolocAPI.ShowMessageBoxSmall(config.TimeRange.OutRangeTip);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_BUILDING_ERROR);
			return false;
		}
		return _DoTransport(callback);
	}

	private void Start()
	{
		_config = DolocConfig.Tables.TbPortal.GetOrDefault(portalId);
		if (_config == null)
		{
			Debug.LogWarning("传送门<" + base.name + ">没有关联配置数据");
		}
		if (indicator != null)
		{
			indicator.SetActive(value: false);
		}
		if (agentReference != null)
		{
			agentReference.gameObject.SetActive(value: false);
		}
		if (motorReference != null)
		{
			motorReference.gameObject.SetActive(value: false);
		}
	}

	protected virtual bool _DoTransport(Action callback)
	{
		if (!hasTarget || !DolocAPI.agent.IsCurrentStateSupportTeleport)
		{
			return false;
		}
		float y = (config.NeedInteract ? 0f : Mathf.Max(0f, DolocAPI.AgentPosition.y - config.Position.y));
		DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited(config.Id);
		return DolocAPI.DoTransport(config.TargetId, new Vector2(0f, y), callback, config.NeedInteract);
	}

	public virtual void OnTouch()
	{
		if (!hasTarget)
		{
			return;
		}
		Action callback = null;
		if (NeedInteract)
		{
			if (useIndicator && indicator != null)
			{
				indicator.SetActive(value: true);
			}
			Vector2 vector = DolocAPI.CalcPopPosition(base.transform, 1.2f) + tipOffset;
			if (DolocAPI.IsAgentRiding && vector.y - position.y < 6f)
			{
				vector.y += 3f;
			}
			this.ShowSceneOperationTip(vector, config.EnableTip, ((IGate)this).GetInteractKeyName());
			return;
		}
		if (DolocAPI.IsAgentRiding && !AvailableToMotor)
		{
			DolocAPI.gameStateManager.agentController.GetOffMotor();
			Room room = DolocAPI.CurrentRoom;
			Vector3 pos = motorReference.position;
			callback = delegate
			{
				DolocAPI.SetMotorPosition(room, pos);
			};
		}
		DoTransport(callback);
	}

	public virtual void OnDisTouch()
	{
		if (hasTarget && NeedInteract)
		{
			if (useIndicator && indicator != null)
			{
				indicator.SetActive(value: false);
			}
			this.HideSceneOperationTip();
		}
	}

	public virtual void OnInteract()
	{
		if (hasTarget && NeedInteract)
		{
			if (DoTransport(null))
			{
				this.PushSceneOperationTipToHide();
				DolocAPI.Broadcast(OperationEventType.ENTER_BUILDING);
			}
			else
			{
				DolocAPI.outputError("传送失败..");
				this.PushSceneOperationTip();
			}
		}
	}
}
