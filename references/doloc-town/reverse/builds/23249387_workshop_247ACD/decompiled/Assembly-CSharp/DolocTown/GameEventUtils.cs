using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;

namespace DolocTown;

public static class GameEventUtils
{
	private static readonly GameEventType[] _event_with_int_args = new GameEventType[7]
	{
		GameEventType.HOUR_PASSED,
		GameEventType.DAY_PASSED,
		GameEventType.MONTH_PASSED,
		GameEventType.YEAR_PASSED,
		GameEventType.MAKE_MONEY,
		GameEventType.SLEEP,
		GameEventType.FISHING_VERTICAL_DISTANCE
	};

	private static readonly HashSet<GameEventType> EventsWithIntArgs = new HashSet<GameEventType>(_event_with_int_args);

	public static IEnumerable<string> GetIntArgsTypeNames()
	{
		return _event_with_int_args.Select((GameEventType x) => x.ToString());
	}

	public static bool IsEventHasIntArgs(this GameEventType type)
	{
		return EventsWithIntArgs.Contains(type);
	}

	public static MissionArgsType GetMissionArgsType(this GameEventType type)
	{
		if (type.IsEventHasIntArgs())
		{
			return MissionArgsType.INT;
		}
		switch (type)
		{
		case GameEventType.NONE:
		case GameEventType.LAUNCH_FREIGHT_DRONE:
		case GameEventType.GAME_START:
		case GameEventType.WAKE_UP:
		case GameEventType.FIRST_COMEOUT_AFTER_SLEEP:
		case GameEventType.STAND_PLATFORM:
		case GameEventType.EQUIP_DRONE:
		case GameEventType.GUN_RELOAD_FINISH:
			return MissionArgsType.NONE;
		case GameEventType.WEATHER_PROPERTY_CHANGED:
			return MissionArgsType.BOOL;
		default:
			return MissionArgsType.STRING;
		}
	}

	public static bool HasNoArgs(this GameEventType type)
	{
		return type.GetMissionArgsType() == MissionArgsType.NONE;
	}
}
