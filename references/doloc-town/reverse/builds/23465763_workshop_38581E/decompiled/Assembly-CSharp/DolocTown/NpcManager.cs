using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.NPC;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class NpcManager
{
	private readonly Dictionary<string, Npc> npcList = new Dictionary<string, Npc>();

	public IEnumerable<Npc> AllNpcs => npcList.Values;

	public int Count => npcList.Count;

	[JsonProperty("npcs")]
	private Npc[] _AllNpcs => npcList.Values.ToArray();

	public NpcManager()
	{
	}

	[JsonConstructor]
	protected NpcManager(Npc[] npcs)
	{
		if (npcs == null)
		{
			return;
		}
		foreach (Npc npc in npcs)
		{
			if (npc.isValid)
			{
				npcList.Add(npc.NpcName, npc);
			}
		}
		foreach (NpcInfo data in DolocConfig.Tables.TbNpc.DataList)
		{
			if (data.ShouldPreload)
			{
				AddNpc(new Npc(data));
			}
		}
	}

	public void PauseNpcMovers()
	{
		foreach (Npc value in npcList.Values)
		{
			if (!(value.Renderer == null))
			{
				value.Renderer.mover.SetPaused(value: true);
				if (value.Renderer.CheckAnimation("move"))
				{
					value.Renderer.PlayAnimation("idle", force: false);
				}
			}
		}
	}

	public void ResumeNpcMovers()
	{
		foreach (Npc value in npcList.Values)
		{
			if (!(value.Renderer == null))
			{
				value.Renderer.mover.SetPaused(value: false);
				value.UpdateAnimationByMoving();
			}
		}
	}

	public void InvokeNpcSchedule(string debugInfo = null)
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		if (debugInfo == null)
		{
			debugInfo = string.Empty;
		}
		foreach (Npc value in npcList.Values)
		{
			value.InvokeSchedule(dateNow, currentWeatherType);
		}
	}

	public void InvokeNpcSchedule(WeatherType weatherType)
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		foreach (Npc value in npcList.Values)
		{
			value.InvokeSchedule(dateNow, weatherType);
		}
	}

	public bool PutNpcToScene(string npcName, string sceneName)
	{
		if (!npcList.TryGetValue(npcName, out var value))
		{
			return false;
		}
		value.ManualSetPosition(sceneName);
		return true;
	}

	public IEnumerable<Npc> GetNpcsInScene(int sceneId)
	{
		if (!SceneUtils.QuerySceneName(sceneId, out var sceneName))
		{
			yield break;
		}
		foreach (Npc value in npcList.Values)
		{
			if (value.sceneName == sceneName)
			{
				yield return value;
			}
		}
	}

	public bool IsNpcAtMarkPoint(string npcName, string markName, float threshold = 5f)
	{
		if (!npcList.TryGetValue(npcName, out var value))
		{
			return false;
		}
		return value.IsAtMarkPoint(markName, threshold);
	}

	public void UpdatePerSec()
	{
		foreach (Npc value in npcList.Values)
		{
			value.UpdatePerSec();
		}
	}

	public void UpdatePerSecNoRender()
	{
		foreach (Npc value in npcList.Values)
		{
			value.UpdatePerSecNoRender();
		}
	}

	public void AfterPassTime()
	{
		foreach (Npc value in npcList.Values)
		{
			value.AfterPassTime();
		}
	}

	public void OnAgentRoomChanged(Room targetRoom)
	{
		foreach (Npc value in npcList.Values)
		{
			value.OnPlayerSceneChanged(targetRoom.SceneRawName);
		}
	}

	public bool QueryNpc(string name, out Npc data)
	{
		return npcList.TryGetValue(name, out data);
	}

	public bool AddNpc(Npc npc)
	{
		return npcList.TryAdd(npc.NpcName, npc);
	}
}
