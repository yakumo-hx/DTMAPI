using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class PortalInfo : BeanBase
{
	public const int __ID__ = -1328482771;

	public string Id { get; private set; }

	public MarkPointInfo Id_Ref { get; private set; }

	public string TargetId { get; private set; }

	public MarkPointInfo TargetId_Ref { get; private set; }

	public bool AvailableToNpc { get; private set; }

	public bool AvailableToMotor { get; private set; }

	public bool KeepHorizontalSpeed { get; private set; }

	public bool KeepVerticalSpeed { get; private set; }

	public bool NeedInteract { get; private set; }

	public PortalInteractKey InteractKeyType { get; private set; }

	public string EnableTip { get; private set; }

	public string EnableTip_l10n_key { get; }

	public string DisableTip { get; private set; }

	public string DisableTip_l10n_key { get; }

	public RoomTimeRange TimeRange { get; private set; }

	public Vector2 Position => Id_Ref.Position;

	public Vector2 TargetPosition => TargetId_Ref.Position;

	public bool UseTimeRange { get; private set; }

	public string SceneRawName => Id_Ref.SceneRawName;

	public string RoomId => Id_Ref.RoomId;

	public PortalInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["target_id"].IsString)
		{
			throw new SerializationException();
		}
		TargetId = _json["target_id"];
		if (!_json["available_to_npc"].IsBoolean)
		{
			throw new SerializationException();
		}
		AvailableToNpc = _json["available_to_npc"];
		if (!_json["available_to_motor"].IsBoolean)
		{
			throw new SerializationException();
		}
		AvailableToMotor = _json["available_to_motor"];
		if (!_json["keep_horizontal_speed"].IsBoolean)
		{
			throw new SerializationException();
		}
		KeepHorizontalSpeed = _json["keep_horizontal_speed"];
		if (!_json["keep_vertical_speed"].IsBoolean)
		{
			throw new SerializationException();
		}
		KeepVerticalSpeed = _json["keep_vertical_speed"];
		if (!_json["need_interact"].IsBoolean)
		{
			throw new SerializationException();
		}
		NeedInteract = _json["need_interact"];
		if (!_json["interact_key_type"].IsNumber)
		{
			throw new SerializationException();
		}
		InteractKeyType = (PortalInteractKey)_json["interact_key_type"].AsInt;
		if (!_json["enable_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		EnableTip_l10n_key = _json["enable_tip"]["key"];
		if (!_json["enable_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		EnableTip = _json["enable_tip"]["text"];
		if (!_json["disable_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DisableTip_l10n_key = _json["disable_tip"]["key"];
		if (!_json["disable_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DisableTip = _json["disable_tip"]["text"];
		if (!_json["time_range"].IsObject)
		{
			throw new SerializationException();
		}
		TimeRange = RoomTimeRange.DeserializeRoomTimeRange(_json["time_range"]);
	}

	public PortalInfo(string id, string target_id, bool available_to_npc, bool available_to_motor, bool keep_horizontal_speed, bool keep_vertical_speed, bool need_interact, PortalInteractKey interact_key_type, string enable_tip, string disable_tip, RoomTimeRange time_range)
	{
		Id = id;
		TargetId = target_id;
		AvailableToNpc = available_to_npc;
		AvailableToMotor = available_to_motor;
		KeepHorizontalSpeed = keep_horizontal_speed;
		KeepVerticalSpeed = keep_vertical_speed;
		NeedInteract = need_interact;
		InteractKeyType = interact_key_type;
		EnableTip = enable_tip;
		DisableTip = disable_tip;
		TimeRange = time_range;
	}

	public static PortalInfo DeserializePortalInfo(JSONNode _json)
	{
		return new PortalInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1328482771;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(Id);
		TargetId_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(TargetId);
		TimeRange?.Resolve(_tables);
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		EnableTip = translator(EnableTip_l10n_key, EnableTip);
		DisableTip = translator(DisableTip_l10n_key, DisableTip);
		TimeRange?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",TargetId:" + TargetId + ",AvailableToNpc:" + AvailableToNpc + ",AvailableToMotor:" + AvailableToMotor + ",KeepHorizontalSpeed:" + KeepHorizontalSpeed + ",KeepVerticalSpeed:" + KeepVerticalSpeed + ",NeedInteract:" + NeedInteract + ",InteractKeyType:" + InteractKeyType.ToString() + ",EnableTip:" + EnableTip + ",DisableTip:" + DisableTip + ",TimeRange:" + TimeRange?.ToString() + ",}";
	}

	private void PostResolve()
	{
		UseTimeRange = TimeRange.StartTime != TimeRange.EndTime;
	}
}
