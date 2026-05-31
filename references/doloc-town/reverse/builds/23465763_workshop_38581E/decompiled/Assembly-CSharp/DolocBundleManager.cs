using System;
using System.Collections;
using System.Collections.Generic;
using DolocTown;
using DolocTown.Config;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using DolocTown.TreeGraph;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DolocBundleManager : MonoBehaviour
{
	private delegate bool DataLoader();

	[Header("预加载资源标签")]
	[SerializeField]
	private AssetLabelReference preloadLabel;

	public readonly SimpleDatabase<EffectsConfigSO> effects = new SimpleDatabase<EffectsConfigSO>();

	public DolocAssetCache cache { get; private set; }

	public SimpleDatabase<RoomProto> rooms { get; private set; }

	public SimpleDatabase<LampProto> lamps { get; private set; }

	public CityRoomDatabase cityRooms { get; private set; }

	public TechTreeDatabase techTrees { get; private set; }

	public SimpleDatabase<MissionGraph> missionChains { get; private set; }

	public SimpleDatabase<GameProcessGraph> gameProcessGraphs { get; private set; }

	public SimpleDatabase<WaterParamSO> WaterTemplates { get; private set; }

	public SimpleDatabase<NpcScheduleGraph> npcSchedules { get; private set; }

	public SimpleDatabase<RoutineGraph> routineGraphs { get; private set; }

	public DungeonDatabase dungeons { get; private set; }

	public SimpleDatabase<BulletProto> bullets { get; private set; }

	public SimpleDatabase<BulletMoverProto> bulletMovers { get; private set; }

	public MonsterDatabase monsters { get; private set; }

	public SimpleDatabase<SkillProto> skills { get; private set; }

	public MotionParamSO motionParam { get; private set; }

	public GameConfigSO gameConfig { get; private set; }

	public DRockEffectsConfig effectsConfig { get; private set; }

	public void LoadAsync(Action<bool> callback)
	{
		Debug.Log("正在加载游戏资源");
		cache = new DolocAssetCache(preloadLabel);
		DolocCoroutineLoader dolocCoroutineLoader = new DolocCoroutineLoader(delegate
		{
			Debug.Log("所有的AB包加载完毕");
			StartCoroutine(InitDataAsync(callback));
		});
		Debug.Log("加载所有的Game资源");
		dolocCoroutineLoader.AddCoroutine(cache.Load(dolocCoroutineLoader.OnCoroutineDone));
		dolocCoroutineLoader.Start(this);
	}

	private IEnumerator InitDataAsync(Action<bool> callback)
	{
		Debug.Log("准备开始同步加载剩余数据");
		Debug.Log("初始化静态数据..");
		bool valid = true;
		DataLoader[] array = new DataLoader[17]
		{
			LoadLubanConfigs, LoadRooms, LoadTechTrees, LoadLamps, LoadCityRooms, LoadMissionChains, LoadGameProcessGraphs, LoadNpcSchedules, LoadRoutineGraphs, LoadWaterTemplates,
			LoadDungeons, LoadMonsters, LoadBullets, LoadBulletMovers, LoadSkills, LoadGameParamSettings, LoadEffects
		};
		DataLoader[] array2 = array;
		foreach (DataLoader loader in array2)
		{
			yield return null;
			valid &= loader();
		}
		callback(valid);
	}

	private bool LoadLubanConfigs()
	{
		Debug.Log("加载配置表..");
		bool num = DolocConfig.Init();
		if (!num)
		{
			Debug.LogError("加载配置表失败");
			return num;
		}
		Debug.Log("加载配置表成功");
		return num;
	}

	private bool LoadRooms()
	{
		try
		{
			Debug.Log("正在加载所有的房间");
			rooms = new SimpleDatabase<RoomProto>();
			int num = 0;
			RoomSO[] allAssetsOfType = cache.GetAllAssetsOfType<RoomSO>();
			foreach (RoomSO roomSO in allAssetsOfType)
			{
				if (roomSO.CreateProto(out var proto))
				{
					rooms.AddData(proto.name, proto);
					continue;
				}
				Debug.LogError("房间\"" + roomSO.name + "\"加载失败");
				num++;
			}
			Debug.Log($"房间数据库生成成功,成功加载{rooms.totalCount}个房间,{num}条失败记录");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载房间数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadTechTrees()
	{
		try
		{
			Debug.Log("正在加载所有的科技树");
			Debug.Log("加载科技树数据..");
			TechTreeGraph[] allAssetsOfType = cache.GetAllAssetsOfType<TechTreeGraph>();
			if (allAssetsOfType == null || allAssetsOfType.Length == 0)
			{
				Debug.LogError("科技树配置文件缺失!");
				return false;
			}
			List<TreeGraph<TechNodeProto>> list = new List<TreeGraph<TechNodeProto>>();
			TechTreeGraph[] array = allAssetsOfType;
			foreach (TechTreeGraph techTreeGraph in array)
			{
				if (!(techTreeGraph == null))
				{
					TreeGraph<TechNodeProto> item = techTreeGraph.CreateTree(LocSprites.UI_ICON_DEFAULT24);
					list.Add(item);
				}
			}
			techTrees = new TechTreeDatabase();
			techTrees.LoadTechTrees(list.ToArray());
			Debug.Log($"科技树数据库生成成功,共有{techTrees.totalCount}个科技树");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载科技树配置文件失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadLamps()
	{
		try
		{
			Debug.Log("正在加载所有灯光数据");
			Debug.Log("加载灯光数据..");
			lamps = new SimpleDatabase<LampProto>();
			LampSo[] allAssetsOfType = cache.GetAllAssetsOfType<LampSo>();
			foreach (LampSo lampSo in allAssetsOfType)
			{
				if (!lampSo.CreateProto(out var proto))
				{
					Debug.LogError("灯光数据\"" + lampSo.name + "\"加载失败");
				}
				else
				{
					lamps.AddData(lampSo.name, proto);
				}
			}
			Debug.Log($"灯光数据库生成成功,共有{lamps.totalCount}个数据");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载灯光数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadCityRooms()
	{
		try
		{
			Debug.Log("正在构建城镇场景数据库");
			Debug.Log("加载城镇场景数据..");
			cityRooms = new CityRoomDatabase();
			RoomSO[] allAssetsOfType = cache.GetAllAssetsOfType<RoomSO>();
			foreach (RoomSO roomSO in allAssetsOfType)
			{
				if (roomSO.roomType == RoomType.City)
				{
					if (roomSO.CreateProto(out var proto))
					{
						cityRooms.AddData(proto);
					}
					else
					{
						Debug.LogError("城镇场景" + roomSO.name + "构建失败");
					}
				}
			}
			cityRooms.BuildPath(DolocConfig.Tables.TbPortal.DataMap, DolocConfig.Tables.TbInwalkableArea.DataMap);
			Debug.Log($"城镇场景数据库构建完毕, 共加载{cityRooms.totalCount}个场景");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("构建城镇场景数据库失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadWaterTemplates()
	{
		try
		{
			Debug.Log("加载水体模板数据..");
			WaterTemplates = new SimpleDatabase<WaterParamSO>();
			WaterParamSO[] allAssetsOfType = cache.GetAllAssetsOfType<WaterParamSO>();
			foreach (WaterParamSO waterParamSO in allAssetsOfType)
			{
				WaterTemplates.AddData(waterParamSO.name.ToLower(), waterParamSO);
			}
			Debug.Log($"水体模板数据加载完毕, 共加载{WaterTemplates.totalCount}个模板");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载水资源模板失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadGameProcessGraphs()
	{
		try
		{
			Debug.Log("正在加载游戏流程图");
			Debug.Log("加载游戏流程图");
			gameProcessGraphs = new SimpleDatabase<GameProcessGraph>();
			GameProcessGraph[] allAssetsOfType = cache.GetAllAssetsOfType<GameProcessGraph>();
			foreach (GameProcessGraph gameProcessGraph in allAssetsOfType)
			{
				if (gameProcessGraph.IsGraphInvalid)
				{
					Debug.LogWarning("无效流程图:\"" + gameProcessGraph.name + "\"");
				}
				else
				{
					gameProcessGraphs.AddData(gameProcessGraph.name, gameProcessGraph);
				}
			}
			Debug.Log($"加载游戏流程图成功, 共加载{gameProcessGraphs.totalCount}个流程图");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载游戏流程图失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadNpcSchedules()
	{
		try
		{
			Debug.Log("正在加载NPC日程数据");
			Debug.Log("加载NPC日程数据..");
			npcSchedules = new SimpleDatabase<NpcScheduleGraph>();
			NpcScheduleGraph[] allAssetsOfType = cache.GetAllAssetsOfType<NpcScheduleGraph>();
			foreach (NpcScheduleGraph npcScheduleGraph in allAssetsOfType)
			{
				npcSchedules.AddData(npcScheduleGraph.name, npcScheduleGraph);
			}
			Debug.Log($"NPC日程数据加载完毕,共有{npcSchedules.totalCount}条记录");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载NPC日程数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadRoutineGraphs()
	{
		try
		{
			Debug.Log("正在加载事务计划表");
			Debug.Log("加载事务计划表..");
			routineGraphs = new SimpleDatabase<RoutineGraph>();
			RoutineGraph[] allAssetsOfType = cache.GetAllAssetsOfType<RoutineGraph>();
			foreach (RoutineGraph routineGraph in allAssetsOfType)
			{
				routineGraphs.AddData(routineGraph.name, routineGraph);
			}
			Debug.Log($"加载事务计划表成功, 共加载{routineGraphs.totalCount}个事务计划表");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载事务计划表失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadMissionChains()
	{
		try
		{
			Debug.Log("正在加载任务链数据");
			Debug.Log("加载任务链数据..");
			missionChains = new SimpleDatabase<MissionGraph>();
			MissionGraph[] allAssetsOfType = cache.GetAllAssetsOfType<MissionGraph>();
			foreach (MissionGraph missionGraph in allAssetsOfType)
			{
				if (missionGraph.primeNode != null)
				{
					missionChains.AddData(missionGraph.name, missionGraph);
				}
			}
			Debug.Log($"任务链数据加载完毕, 共加载{missionChains.totalCount}个任务链数据");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载任务链数据库失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadDungeons()
	{
		try
		{
			Debug.Log("正在构建地牢数据库");
			Debug.Log("加载地牢数据..");
			dungeons = new DungeonDatabase();
			DungeonSO[] allAssetsOfType = cache.GetAllAssetsOfType<DungeonSO>();
			DungeonSO[] array = allAssetsOfType;
			foreach (DungeonSO dungeonSO in array)
			{
				if (dungeonSO.CreateProto(out var proto))
				{
					dungeons.AddDungeonProto(proto);
				}
				else
				{
					Debug.LogError("地牢<" + dungeonSO.name + ">加载失败");
				}
			}
			Debug.Log($"地牢数据库构建完成, 共加载{allAssetsOfType.Length}个地牢场景");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("构建地牢数据库失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadBullets()
	{
		try
		{
			Debug.Log("正在加载子弹数据");
			Debug.Log("加载子弹数据..");
			bullets = new SimpleDatabase<BulletProto>();
			BulletSO[] allAssetsOfType = cache.GetAllAssetsOfType<BulletSO>();
			for (int i = 0; i < allAssetsOfType.Length; i++)
			{
				if (allAssetsOfType[i].CreateProto(out var proto))
				{
					bullets.AddData(proto.name, proto);
				}
			}
			Debug.Log($"子弹数据加载完毕,共有{bullets.totalCount}条记录");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载子弹数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadBulletMovers()
	{
		try
		{
			Debug.Log("正在加载子弹移动器数据");
			Debug.Log("加载子弹移动器数据..");
			bulletMovers = new SimpleDatabase<BulletMoverProto>();
			BulletMoverConfig[] allAssetsOfType = cache.GetAllAssetsOfType<BulletMoverConfig>();
			foreach (BulletMoverConfig bulletMoverConfig in allAssetsOfType)
			{
				BulletMoverProto bulletMoverProto = bulletMoverConfig.CreateProto();
				if (bulletMoverProto != null)
				{
					bulletMovers.AddData(bulletMoverConfig.name, bulletMoverProto);
				}
			}
			Debug.Log($"子弹移动器数据加载完毕,共有{bulletMovers.totalCount}条记录");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载子弹移动器数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadMonsters()
	{
		try
		{
			Debug.Log("正在加载怪物数据库");
			Debug.Log("加载怪物数据..");
			monsters = new MonsterDatabase();
			MonsterSO[] allAssetsOfType = cache.GetAllAssetsOfType<MonsterSO>();
			for (int i = 0; i < allAssetsOfType.Length; i++)
			{
				if (allAssetsOfType[i].CreateProto(out var proto))
				{
					monsters.AddMonster(proto);
				}
			}
			Debug.Log($"怪物数据库加载完毕,共有{monsters.TotalCount}条记录");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载怪物数据库失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadSkills()
	{
		try
		{
			Debug.Log("正在加载技能数据库");
			Debug.Log("加载技能数据..");
			skills = new SimpleDatabase<SkillProto>();
			SkillSO[] allAssetsOfType = cache.GetAllAssetsOfType<SkillSO>();
			for (int i = 0; i < allAssetsOfType.Length; i++)
			{
				if (allAssetsOfType[i].CreateProto(out var proto))
				{
					skills.AddData(proto.id, proto);
				}
			}
			Debug.Log($"技能数据库加载完毕,共有{skills.totalCount}个技能对象");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载技能失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadEffects()
	{
		try
		{
			Debug.Log("正在加载特效数据");
			Debug.Log("加载特效数据..");
			EffectsConfigSO[] allAssetsOfType = cache.GetAllAssetsOfType<EffectsConfigSO>();
			foreach (EffectsConfigSO effectsConfigSO in allAssetsOfType)
			{
				effects.AddData(effectsConfigSO.name, effectsConfigSO);
			}
			Debug.Log($"特效数据加载完毕, 共加载{effects.totalCount}个特效");
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载特效数据失败");
			Debug.LogException(exception);
			return false;
		}
	}

	private bool LoadSettings<T>(string path, string title, out T receiver) where T : ScriptableObject
	{
		Debug.Log("正在加载配置:" + title);
		Debug.Log("加载配置:" + title + "..");
		receiver = Resources.Load<T>(path);
		if (receiver == null)
		{
			Debug.LogError("加载配置:" + title + "失败");
			return false;
		}
		return true;
	}

	private bool LoadGameParamSettings()
	{
		try
		{
			GameConfigSO receiver;
			DRockEffectsConfig receiver2;
			MotionParamSO receiver3;
			int result = 1 & (LoadSettings<GameConfigSO>("Configs/gameCfg", "全局游戏配置", out receiver) ? 1 : 0) & (LoadSettings<DRockEffectsConfig>("Configs/effectsCfg", "特效配置", out receiver2) ? 1 : 0) & (LoadSettings<MotionParamSO>("Configs/agentMotionParam", "主角运动参数", out receiver3) ? 1 : 0);
			gameConfig = receiver;
			effectsConfig = receiver2;
			motionParam = receiver3;
			return (byte)result != 0;
		}
		catch (Exception exception)
		{
			Debug.LogError("加载游戏全局配置失败");
			Debug.LogException(exception);
			return false;
		}
	}
}
