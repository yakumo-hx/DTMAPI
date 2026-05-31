using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class RoomInfo : BeanBase
{
	public const int __ID__ = -331726884;

	public string Id { get; private set; }

	public string SceneName { get; private set; }

	public bool IsInhouse { get; private set; }

	public RoomSpawnInfo SpawnInfo { get; private set; }

	public RoomConstructInfo ConstructInfo { get; private set; }

	public bool DisableMotor { get; private set; }

	public RoomInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["scene_name"].IsString)
		{
			throw new SerializationException();
		}
		SceneName = _json["scene_name"];
		if (!_json["is_inhouse"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsInhouse = _json["is_inhouse"];
		if (!_json["spawn_info"].IsObject)
		{
			throw new SerializationException();
		}
		SpawnInfo = RoomSpawnInfo.DeserializeRoomSpawnInfo(_json["spawn_info"]);
		if (!_json["construct_info"].IsObject)
		{
			throw new SerializationException();
		}
		ConstructInfo = RoomConstructInfo.DeserializeRoomConstructInfo(_json["construct_info"]);
		if (!_json["disableMotor"].IsBoolean)
		{
			throw new SerializationException();
		}
		DisableMotor = _json["disableMotor"];
	}

	public RoomInfo(string id, string scene_name, bool is_inhouse, RoomSpawnInfo spawn_info, RoomConstructInfo construct_info, bool disableMotor)
	{
		Id = id;
		SceneName = scene_name;
		IsInhouse = is_inhouse;
		SpawnInfo = spawn_info;
		ConstructInfo = construct_info;
		DisableMotor = disableMotor;
	}

	public static RoomInfo DeserializeRoomInfo(JSONNode _json)
	{
		return new RoomInfo(_json);
	}

	public override int GetTypeId()
	{
		return -331726884;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpawnInfo?.Resolve(_tables);
		ConstructInfo?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SpawnInfo?.TranslateText(translator);
		ConstructInfo?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SceneName:" + SceneName + ",IsInhouse:" + IsInhouse + ",SpawnInfo:" + SpawnInfo?.ToString() + ",ConstructInfo:" + ConstructInfo?.ToString() + ",DisableMotor:" + DisableMotor + ",}";
	}
}
