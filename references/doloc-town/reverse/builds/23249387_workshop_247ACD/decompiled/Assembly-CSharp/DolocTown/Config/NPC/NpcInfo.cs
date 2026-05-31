using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.General;
using DolocTown.Config.Room;
using DolocTown.Config.Time;
using DolocTown.GameData;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class NpcInfo : BeanBase
{
	public readonly Dictionary<string, TextContainer> Alias_Index = new Dictionary<string, TextContainer>();

	public const int __ID__ = 266055906;

	public Dictionary<string, Vector2[]> streetPoints = new Dictionary<string, Vector2[]>();

	public string Id { get; private set; }

	public int SortingOrder { get; private set; }

	public bool ShouldPreload { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public TextContainer[] Alias { get; private set; }

	public float WalkSpeed { get; private set; }

	public bool DisableActing { get; private set; }

	public AnimatorAsset AnimatorAsset { get; private set; }

	public Vector2Int SpriteSize { get; private set; }

	public bool ScheduleInitialState { get; private set; }

	public NpcScheduleAsset ScheduleAsset { get; private set; }

	public int InvokeDuration { get; private set; }

	public string InitialMarkPoint { get; private set; }

	public MarkPointInfo InitialMarkPoint_Ref { get; private set; }

	public string QuitMarkPoint { get; private set; }

	public MarkPointInfo QuitMarkPoint_Ref { get; private set; }

	public string InitialDialogueEntry { get; private set; }

	public string GiftDialogue { get; private set; }

	public int LikingLvLimit { get; private set; }

	public int OrderInBirthdayWisher { get; private set; }

	public bool IsBirthdayAmbience { get; private set; }

	public TimeRange OnlineTimeRange { get; private set; }

	public RuntimeAnimatorController Animator => AnimatorAsset.Asset;

	public INpcScheduleGraph Schedule => ScheduleAsset.Asset;

	public Vector2 RealSize => new Vector2(SpriteSize.x, SpriteSize.y) * 0.125f;

	public Vector2 EmotionOffset => RealSize * new Vector2(0.5f, 0.9f);

	public Dictionary<string, Vector2[]> StreetPoints
	{
		get
		{
			if (streetPoints.Count == 0)
			{
				streetPoints = LoadStreetPoints();
			}
			return streetPoints;
		}
	}

	public NpcInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sorting_order"].IsNumber)
		{
			throw new SerializationException();
		}
		SortingOrder = _json["sorting_order"];
		if (!_json["should_preload"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShouldPreload = _json["should_preload"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		JSONNode jSONNode = _json["alias"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Alias = new TextContainer[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TextContainer textContainer = TextContainer.DeserializeTextContainer(child);
			Alias[num++] = textContainer;
		}
		TextContainer[] alias = Alias;
		foreach (TextContainer textContainer2 in alias)
		{
			Alias_Index.Add(textContainer2.Id, textContainer2);
		}
		if (!_json["walk_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		WalkSpeed = _json["walk_speed"];
		if (!_json["disable_acting"].IsBoolean)
		{
			throw new SerializationException();
		}
		DisableActing = _json["disable_acting"];
		if (!_json["animator_asset"].IsObject)
		{
			throw new SerializationException();
		}
		AnimatorAsset = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator_asset"]));
		if (!_json["sprite_size"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["sprite_size"]));
		if (!_json["schedule_initial_state"].IsBoolean)
		{
			throw new SerializationException();
		}
		ScheduleInitialState = _json["schedule_initial_state"];
		if (!_json["schedule_asset"].IsObject)
		{
			throw new SerializationException();
		}
		ScheduleAsset = ExternalTypeUtil.NpcScheduleAssetConverter(CfgNpcScheduleAsset.DeserializeCfgNpcScheduleAsset(_json["schedule_asset"]));
		if (!_json["invoke_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		InvokeDuration = _json["invoke_duration"];
		if (!_json["initial_mark_point"].IsString)
		{
			throw new SerializationException();
		}
		InitialMarkPoint = _json["initial_mark_point"];
		if (!_json["quit_mark_point"].IsString)
		{
			throw new SerializationException();
		}
		QuitMarkPoint = _json["quit_mark_point"];
		if (!_json["initial_dialogue_entry"].IsString)
		{
			throw new SerializationException();
		}
		InitialDialogueEntry = _json["initial_dialogue_entry"];
		if (!_json["gift_dialogue"].IsString)
		{
			throw new SerializationException();
		}
		GiftDialogue = _json["gift_dialogue"];
		if (!_json["liking_lv_limit"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingLvLimit = _json["liking_lv_limit"];
		if (!_json["order_in_birthday_wisher"].IsNumber)
		{
			throw new SerializationException();
		}
		OrderInBirthdayWisher = _json["order_in_birthday_wisher"];
		if (!_json["is_birthday_ambience"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsBirthdayAmbience = _json["is_birthday_ambience"];
		if (!_json["online_time_range"].IsObject)
		{
			throw new SerializationException();
		}
		OnlineTimeRange = TimeRange.DeserializeTimeRange(_json["online_time_range"]);
	}

	public NpcInfo(string id, int sorting_order, bool should_preload, string title, TextContainer[] alias, float walk_speed, bool disable_acting, AnimatorAsset animator_asset, Vector2Int sprite_size, bool schedule_initial_state, NpcScheduleAsset schedule_asset, int invoke_duration, string initial_mark_point, string quit_mark_point, string initial_dialogue_entry, string gift_dialogue, int liking_lv_limit, int order_in_birthday_wisher, bool is_birthday_ambience, TimeRange online_time_range)
	{
		Id = id;
		SortingOrder = sorting_order;
		ShouldPreload = should_preload;
		Title = title;
		Alias = alias;
		TextContainer[] alias2 = Alias;
		foreach (TextContainer textContainer in alias2)
		{
			Alias_Index.Add(textContainer.Id, textContainer);
		}
		WalkSpeed = walk_speed;
		DisableActing = disable_acting;
		AnimatorAsset = animator_asset;
		SpriteSize = sprite_size;
		ScheduleInitialState = schedule_initial_state;
		ScheduleAsset = schedule_asset;
		InvokeDuration = invoke_duration;
		InitialMarkPoint = initial_mark_point;
		QuitMarkPoint = quit_mark_point;
		InitialDialogueEntry = initial_dialogue_entry;
		GiftDialogue = gift_dialogue;
		LikingLvLimit = liking_lv_limit;
		OrderInBirthdayWisher = order_in_birthday_wisher;
		IsBirthdayAmbience = is_birthday_ambience;
		OnlineTimeRange = online_time_range;
	}

	public static NpcInfo DeserializeNpcInfo(JSONNode _json)
	{
		return new NpcInfo(_json);
	}

	public override int GetTypeId()
	{
		return 266055906;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TextContainer[] alias = Alias;
		for (int i = 0; i < alias.Length; i++)
		{
			alias[i]?.Resolve(_tables);
		}
		InitialMarkPoint_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(InitialMarkPoint);
		QuitMarkPoint_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(QuitMarkPoint);
		OnlineTimeRange?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		TextContainer[] alias = Alias;
		for (int i = 0; i < alias.Length; i++)
		{
			alias[i]?.TranslateText(translator);
		}
		OnlineTimeRange?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SortingOrder:" + SortingOrder + ",ShouldPreload:" + ShouldPreload + ",Title:" + Title + ",Alias:" + StringUtil.CollectionToString(Alias) + ",WalkSpeed:" + WalkSpeed + ",DisableActing:" + DisableActing + ",AnimatorAsset:" + AnimatorAsset?.ToString() + ",SpriteSize:" + SpriteSize.ToString() + ",ScheduleInitialState:" + ScheduleInitialState + ",ScheduleAsset:" + ScheduleAsset?.ToString() + ",InvokeDuration:" + InvokeDuration + ",InitialMarkPoint:" + InitialMarkPoint + ",QuitMarkPoint:" + QuitMarkPoint + ",InitialDialogueEntry:" + InitialDialogueEntry + ",GiftDialogue:" + GiftDialogue + ",LikingLvLimit:" + LikingLvLimit + ",OrderInBirthdayWisher:" + OrderInBirthdayWisher + ",IsBirthdayAmbience:" + IsBirthdayAmbience + ",OnlineTimeRange:" + OnlineTimeRange?.ToString() + ",}";
	}

	public NpcScheduleResult QueryScheduleResult(NpcScheduleParams status)
	{
		if (Schedule != null && Schedule.QuerySchedule(status, out var result))
		{
			return result;
		}
		return new NpcScheduleResult(null, NpcScheduleWork.Street);
	}

	public bool TryGetRandomPositionInScene(string sceneName, out Vector2 point)
	{
		if (!StreetPoints.TryGetValue(sceneName, out var value))
		{
			point = Vector2.zero;
			return false;
		}
		point = value[UnityEngine.Random.Range(0, value.Length)];
		return true;
	}

	private Dictionary<string, Vector2[]> LoadStreetPoints()
	{
		Dictionary<string, List<Vector2>> dictionary = new Dictionary<string, List<Vector2>>();
		foreach (string portalPoint in Schedule.GetPortalPoints())
		{
			MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(portalPoint);
			if (!string.IsNullOrEmpty(orDefault?.SceneRawName))
			{
				dictionary.TryAdd(orDefault.SceneRawName, new List<Vector2>());
				dictionary[orDefault.SceneRawName].Add(orDefault.Position);
			}
		}
		return dictionary.ToDictionary((KeyValuePair<string, List<Vector2>> pair) => pair.Key, (KeyValuePair<string, List<Vector2>> pair) => pair.Value.ToArray());
	}
}
