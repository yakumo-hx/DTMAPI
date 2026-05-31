using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public class Crop
{
	[JsonProperty]
	public CropData data;

	[JsonProperty]
	private CropGeneFunction[] functions;

	public SeedInfo seedProto { get; protected set; }

	public PlantBasin plantBasin { get; private set; }

	public CropGeneInfo[] geneProtos { get; private set; }

	[JsonProperty]
	private string seedName => seedProto.Id;

	[JsonProperty]
	private string[] geneNames => geneProtos.Select((CropGeneInfo x) => x.Id).ToArray();

	public bool IsValid => seedProto != null;

	public CropDecorator CropDecorator { get; private set; }

	public bool hasGrowed { get; set; }

	public float lastAddition { get; set; }

	public float lastFertilizerAddition { get; set; }

	public Sprite CurrentSprite
	{
		get
		{
			if (isDead)
			{
				if (data.currentLevel != 0)
				{
					return seedProto.SeedType_Ref.PlantWitherAsset.Asset;
				}
				return seedProto.SeedType_Ref.SeedWitherAsset.Asset;
			}
			return seedProto.GetLevelSprite(CurrentLevel);
		}
	}

	[DebugInfo("作物名", Color = "#df426e")]
	public string SeedId => seedProto.Id;

	[DebugInfo("作物贴图")]
	public Sprite CropSrite => CurrentSprite;

	[DebugInfo("是否成熟", Color = "#ee3046")]
	public bool isMature
	{
		get
		{
			CropData cropData = data;
			if (cropData.isMature)
			{
				return !cropData.isDead;
			}
			return false;
		}
	}

	[DebugInfo("是否死亡", Color = "#ee3046")]
	public bool isDead => data.isDead;

	[DebugInfo("是否湿润", Color = "#4876bb")]
	public bool isMoist => data.isMoist;

	[DebugInfo("是否被污染", Color = "#fff971")]
	public bool IsPolluted => data.isPolluted;

	[DebugInfo("作物当前生长阶段")]
	public int CurrentLevel => data.currentLevel;

	[DebugInfo("作物当前生长阶段")]
	public CropLevelData CurrentLevelData => seedProto.GetLevelData(CurrentLevel);

	[DebugInfo("当前生长值信息")]
	public string GrowthInfo => $"阶段:{data.currentLevel}/{seedProto.MatureLevel} 阶段生长:{data.currentGrowthValue}/{CurrentLevelData.GrowthValue}";

	[DebugInfo("估算生长时间/基于上一轮成长值")]
	public string TotalGrowthDays
	{
		get
		{
			if (LastGrowth <= 0f)
			{
				return "未知";
			}
			float totalGrowthValue = TotalGrowthValue;
			int num = Mathf.RoundToInt(totalGrowthValue / LastGrowth);
			float num2 = DolocAPI.GlobalParameter.Tu2GameDay(num);
			return $"预计时间单位:{num} = {num2:F2}天 = {totalGrowthValue}(总生长值) / {LastGrowth}(上一轮成长值)";
		}
	}

	[DebugInfo("当前生命值信息")]
	public string HealthInfo => $"{data.currentHealthValue}/{seedProto.HealthValue}";

	public float HealthProgress => data.currentHealthValue / seedProto.HealthValue;

	[DebugInfo("作物类型")]
	public SeedTypeInfo cropType => seedProto.SeedType_Ref;

	[DebugInfo("作物是否已经被采集过")]
	public bool hasHarvested => data.harvestTimes > 0;

	[DebugInfo("总生长值")]
	public float TotalGrowthValue => seedProto.LevelDatas.Sum((CropLevelData x) => x.GrowthValue);

	[DebugInfo("当前生长值")]
	public float CurrentGrowthValue
	{
		get
		{
			float num = data.currentGrowthValue;
			if (data.currentLevel == 0)
			{
				return num;
			}
			for (int i = 0; i < data.currentLevel; i++)
			{
				num += (float)seedProto.GetLevelData(i).GrowthValue;
			}
			return num;
		}
	}

	[DebugInfo("总生长进度")]
	public float TotalGrowthProgress => CurrentGrowthValue / TotalGrowthValue;

	[DebugInfo("剩余可采集次数")]
	public int RemainingHarvestTimes => data.lifespan;

	[DebugInfo("是否为基因作物")]
	public virtual bool IsGeneCrop => !geneProtos.IsNullOrEmpty();

	public CropRenderer Renderer { get; set; }

	public float LastGrowth { get; protected set; }

	public int OriginMoodContribution => 0;

	public int OriginMaxLifespan => seedProto.Lifespan;

	public int MoodContribution => CropDecorator.DctMoodContribution;

	public int MaxLifespan => CropDecorator.DctMaxLifespan;

	public Crop(SeedInfo seedProto, PlantBasin plantBasin, CropGeneInfo[] geneProtos)
	{
		this.seedProto = seedProto;
		this.plantBasin = plantBasin;
		this.geneProtos = geneProtos;
		data = new CropData(seedProto.Lifespan, seedProto.HealthValue);
		RebuildFunctions();
	}

	[JsonConstructor]
	protected Crop(string seedName, CropData data, string[] geneNames, CropGeneFunction[] functions = null)
	{
		if (!string.IsNullOrEmpty(seedName) && DolocAPI.QuerySeedProto(seedName, out var proto))
		{
			seedProto = proto;
			plantBasin = null;
			this.data = data;
			this.data.currentLevel = Mathf.Clamp(this.data.currentLevel, 0, seedProto.MatureLevel);
			geneProtos = RebuildGeneProtos(geneNames);
			this.functions = ValidateGeneFunctions(geneProtos, functions);
		}
	}

	public virtual void AfterLoadData(PlantBasin basin)
	{
		plantBasin = basin;
		CropGeneFunction[] array = functions;
		foreach (CropGeneFunction obj in array)
		{
			obj.BindCrop(this);
			obj.AfterLoadData();
		}
		CropDecorator = new CropDecorator(this, functions);
	}

	private CropGeneInfo[] RebuildGeneProtos(string[] names)
	{
		if (names == null)
		{
			return seedProto?.NatureGenes_Ref ?? Array.Empty<CropGeneInfo>();
		}
		if (names.Length == 0)
		{
			return Array.Empty<CropGeneInfo>();
		}
		List<CropGeneInfo> list = new List<CropGeneInfo>();
		foreach (string key in names)
		{
			if (DolocConfig.Tables.TbCropGene.DataMap.TryGetValue(key, out var value))
			{
				list.Add(value);
			}
		}
		return list.ToArray();
	}

	private CropGeneFunction[] ValidateGeneFunctions(CropGeneInfo[] geneProtos, CropGeneFunction[] functions)
	{
		List<CropGeneFunction> list = new List<CropGeneFunction>();
		foreach (CropGeneInfo proto in geneProtos)
		{
			CropGeneFunction cropGeneFunction = functions?.FirstOrDefault((CropGeneFunction f) => f.geneId == proto.Id);
			if (cropGeneFunction != null)
			{
				list.Add(cropGeneFunction);
				continue;
			}
			CropGeneFunction cropGeneFunction2 = proto.CreateFunction();
			if (cropGeneFunction2 != null)
			{
				list.Add(cropGeneFunction2);
			}
		}
		return list.ToArray();
	}

	public void RebuildFunctions()
	{
		functions = ((!geneProtos.IsNullOrEmpty()) ? geneProtos.CreateFunctions() : Array.Empty<CropGeneFunction>());
		CropGeneFunction[] array = functions;
		foreach (CropGeneFunction obj in array)
		{
			obj.BindCrop(this);
			obj.OnCreate();
		}
		CropDecorator = new CropDecorator(this, functions);
	}

	public void Swing()
	{
		if (!(Renderer == null) && data.currentLevel != 0 && cropType.RandomSwing)
		{
			if (RandomUtils.Dice(0.5f))
			{
				Renderer.SwingOnTouch();
			}
			else
			{
				Renderer.SwingLightOnTouch();
			}
		}
	}

	public void UpdateRenderer(bool animateGrow = false)
	{
		if (!(Renderer == null))
		{
			Renderer.UpdateMatureRenderer();
			if (data.isDead)
			{
				Renderer.MoistRenderer.SetVisible(value: false);
			}
			else
			{
				Renderer.SetMoist(data.isMoist, data.isPolluted);
			}
			if (animateGrow)
			{
				Renderer.ToNext(CurrentSprite);
			}
			else
			{
				Renderer.Sprite = CurrentSprite;
			}
		}
	}

	public bool FugusAcidification(bool shouldRender)
	{
		SeedInfo acidificationSeed_Ref = seedProto.SeedType_Ref.AcidificationSeed_Ref;
		if (acidificationSeed_Ref == null || data.currentLevel == 0)
		{
			return false;
		}
		if (seedProto.Id == acidificationSeed_Ref.Id)
		{
			return true;
		}
		seedProto = acidificationSeed_Ref;
		data.lifespan = seedProto.Lifespan;
		data.currentGrowthValue = 0f;
		data.isDead = false;
		plantBasin.currentSeed.ClearCloneMark();
		geneProtos = DolocAPI.GlobalParameter.ClonedCropGeneGroup_Ref;
		RebuildFunctions();
		if (shouldRender)
		{
			UpdateRenderer(animateGrow: true);
		}
		return true;
	}

	public bool OnFell(ItemTool tool, Vector2 position)
	{
		if (tool.functionTool.ToolType != ToolType.SICKLE)
		{
			return false;
		}
		if (plantBasin == null)
		{
			return false;
		}
		plantBasin.Harvest();
		return true;
	}

	protected void UpdatePollutedStatus(bool isMoist, bool shouldRender)
	{
		if (!isMoist)
		{
			data.isPolluted = false;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist: false);
			}
		}
	}

	public bool _CheckPolluted(bool isMoist, bool shouldRender)
	{
		UpdatePollutedStatus(isMoist, shouldRender);
		if (data.isPolluted)
		{
			return false;
		}
		RefreshMoistStatus(isMoist, shouldRender);
		return true;
	}

	public void RefreshMoistStatus(bool isMoist, bool shouldRender)
	{
		if (data.isMoist != isMoist)
		{
			data.isMoist = isMoist;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist, data.isPolluted);
			}
		}
	}

	public void OverrideCropData(Func<CropData, CropData> func)
	{
		SetCropData(func(data));
	}

	public virtual bool _GrowBack(bool shouldClearGrowth)
	{
		if (data.isDead || data.currentLevel == 0)
		{
			return false;
		}
		data.currentLevel--;
		data.isMature = data.currentLevel == seedProto.MatureLevel;
		if (shouldClearGrowth)
		{
			data.currentGrowthValue = 0f;
		}
		return true;
	}

	public virtual bool _GrowForward(bool shouldClearGrowth)
	{
		if (data.isDead || data.currentLevel == seedProto.MatureLevel)
		{
			return false;
		}
		data.currentLevel++;
		data.isMature = data.currentLevel == seedProto.MatureLevel;
		if (shouldClearGrowth)
		{
			data.currentGrowthValue = 0f;
		}
		return true;
	}

	public void Damage(float dmg, bool shouldRender, bool delayHurtAnim = false)
	{
		if (dmg <= 0f)
		{
			return;
		}
		data.currentHealthValue -= dmg;
		if (data.currentHealthValue <= 0f)
		{
			data.isDead = true;
		}
		if (data.isDead)
		{
			CropDecorator.OnCropDead(shouldRender);
		}
		if (shouldRender)
		{
			if (data.isDead)
			{
				Renderer.SetMoist(isMoist: false);
				Renderer.UpdateMatureRenderer();
				Renderer.ToNext(CurrentSprite);
			}
			else
			{
				Renderer.RenderHurt(delayHurtAnim);
			}
		}
	}

	public void Kill(bool shouldRender)
	{
		Damage(seedProto.HealthValue + 1f, shouldRender);
	}

	public float TakeGrowthValue(float require)
	{
		if (data.isDead || data.isMature)
		{
			return 0f;
		}
		if (data.currentGrowthValue < require)
		{
			float currentGrowthValue = data.currentGrowthValue;
			data.currentGrowthValue = 0f;
			return currentGrowthValue;
		}
		data.currentGrowthValue -= require;
		return require;
	}

	public bool CheckOxygen(Vector2Int agentPositionCell, out int value)
	{
		return CropDecorator.CheckOxygen(agentPositionCell, out value);
	}

	public string GetDescription()
	{
		return geneProtos.GetDescription(seedProto?.NatureGenes);
	}

	public void DEBUG_SetLevel(bool shouldRender, int lv)
	{
		lv = Mathf.Clamp(lv, 0, seedProto.MatureLevel);
		if (data.currentLevel == lv)
		{
			return;
		}
		bool num = lv > data.currentLevel;
		data.currentLevel = lv;
		data.isMature = lv == seedProto.MatureLevel;
		data.currentGrowthValue = 0f;
		if (num)
		{
			CropDecorator.OnCropLevelUp(shouldRender);
			if (data.isMature)
			{
				CropDecorator.OnCropMature(shouldRender);
			}
		}
		else
		{
			CropDecorator.OnCropLevelDown(shouldRender);
		}
		UpdateRenderer(animateGrow: true);
	}

	public bool _ThunderEvent_MatureEndYam(bool shouldRender)
	{
		if (seedProto.Id != DolocAPI.GlobalParameter.ItemRefSeedEndyam)
		{
			return false;
		}
		if (!data.isMature || data.isDead)
		{
			return false;
		}
		DolocAPI.BroadcastString(GameEventType.CROP_THUNDER_EVENT, seedProto.Id);
		plantBasin.ClearCrop();
		plantBasin.CreateDropItem(DolocAPI.GlobalParameter.ItemRefRoastedEndyam, shouldRender, sendMessage: true);
		if (plantBasin is PlantBasinSimple plantBasinSimple)
		{
			plantBasinSimple.MarkAsReeachMatureStage();
			plantBasinSimple.Host.RemoveEquipment(plantBasinSimple);
		}
		return true;
	}

	public void SetCropData(CropData data)
	{
		this.data = data;
	}

	public bool OriginRegrow(bool shouldRender)
	{
		if (data.isDead || data.lifespan <= 1)
		{
			return false;
		}
		data.lifespan--;
		data.isMoist = false;
		data.isMature = false;
		data.currentLevel = seedProto.RepeatLevel;
		data.currentGrowthValue = 0f;
		data.currentHealthValue = seedProto.HealthValue;
		if (shouldRender)
		{
			UpdateRenderer(animateGrow: true);
		}
		return true;
	}

	public bool OriginGrowBack(bool shouldRender, bool shouldClearGrowth)
	{
		if (data.isDead || data.currentLevel == 0)
		{
			return false;
		}
		data.currentLevel--;
		data.isMature = data.currentLevel == seedProto.MatureLevel;
		if (shouldClearGrowth)
		{
			data.currentGrowthValue = 0f;
		}
		return true;
	}

	public bool OriginGrowForward(bool shouldRender, bool shouldClearGrowth)
	{
		if (data.isDead || data.currentLevel == seedProto.MatureLevel)
		{
			return false;
		}
		data.currentLevel++;
		data.isMature = data.currentLevel == seedProto.MatureLevel;
		if (shouldClearGrowth)
		{
			data.currentGrowthValue = 0f;
		}
		return true;
	}

	public void OriginWater(bool shouldRender)
	{
		if (data.isDead)
		{
			return;
		}
		if (data.isPolluted)
		{
			data.isMoist = true;
			data.isPolluted = false;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist: true);
				Swing();
			}
		}
		else
		{
			if (data.isMoist)
			{
				return;
			}
			data.isMoist = true;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist: true);
				if (RandomUtils.Dice(0.5f))
				{
					Renderer.SwingLightOnTouch();
				}
			}
		}
	}

	public void OriginProtected(bool shouldRender)
	{
		if (!data.isDead && data.isPolluted)
		{
			data.isPolluted = false;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(data.isMoist);
			}
			CropGeneFunction[] array = functions;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnProtected(shouldRender);
			}
		}
	}

	public void _ApplyGrowth(float growth, bool shouldRender = false)
	{
		if (data.isMature)
		{
			return;
		}
		data.currentGrowthValue += growth;
		LastGrowth = growth;
		if (!(data.currentGrowthValue < (float)CurrentLevelData.GrowthValue))
		{
			if (++data.currentLevel >= seedProto.MatureLevel)
			{
				data.isMature = true;
			}
			data.currentGrowthValue = 0f;
			CropDecorator.OnCropLevelUp(shouldRender);
			if (data.isMature)
			{
				CropDecorator.OnCropMature(shouldRender);
			}
			if (shouldRender)
			{
				UpdateRenderer(animateGrow: true);
			}
		}
	}

	public void Grow(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		if (!data.isDead && _CheckPolluted(isMoist, shouldRender) && isMoist)
		{
			hasGrowed = true;
			float num = functions.CalcGrowthValue() + addition + fertilizerAddition;
			if (num > 0f)
			{
				_ApplyGrowth(num, shouldRender);
			}
		}
	}

	public void OriginUpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		Grow(shouldRender, isMoist, addition, fertilizerAddition);
	}

	public void OriginUpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		if (data.isDead)
		{
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist: false);
			}
			return;
		}
		if (isProtected)
		{
			if (data.isPolluted)
			{
				data.isPolluted = false;
				if (shouldRender && Renderer != null)
				{
					Renderer.SetMoist(isMoist);
				}
			}
			Grow(shouldRender, isMoist, addition, fertilizerAddition);
			return;
		}
		if (!data.isPolluted)
		{
			data.isPolluted = true;
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(data.isMoist, isPolluted: true);
				Swing();
			}
		}
		data.currentHealthValue -= damage * seedProto.DamageRate;
		if (data.currentHealthValue <= 0f)
		{
			if (!FugusAcidification(shouldRender))
			{
				data.isDead = true;
				data.isMoist = false;
				if (shouldRender)
				{
					UpdateRenderer(animateGrow: true);
				}
				CropDecorator.OnCropDead(shouldRender);
			}
		}
		else if (shouldRender)
		{
			Renderer.RenderHurt();
		}
	}

	public void OriginUpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		if (data.isDead)
		{
			if (shouldRender && Renderer != null)
			{
				Renderer.SetMoist(isMoist: false);
			}
			return;
		}
		if (isProtected)
		{
			if (data.isPolluted)
			{
				data.isPolluted = false;
				if (shouldRender && Renderer != null)
				{
					Renderer.SetMoist(isMoist);
				}
			}
			Grow(shouldRender, isMoist, addition, fertilizerAddition);
			return;
		}
		RefreshMoistStatus(isMoist, shouldRender);
		data.currentHealthValue -= damage * seedProto.DamageRate;
		if (data.currentHealthValue <= 0f)
		{
			data.isDead = true;
			data.isMoist = false;
			if (shouldRender && Renderer != null)
			{
				UpdateRenderer(animateGrow: true);
			}
			CropDecorator.OnCropDead(shouldRender);
		}
		else if (shouldRender && Renderer != null)
		{
			Renderer.RenderHurt();
		}
	}

	public void OriginThunder(bool shouldRender)
	{
		if (_ThunderEvent_MatureEndYam(shouldRender) || isDead)
		{
			return;
		}
		data.currentHealthValue -= DolocAPI.GlobalParameter.ThunderDamgeToCrop;
		if (data.currentHealthValue <= 0f)
		{
			data.isDead = true;
		}
		if (data.isDead)
		{
			CropDecorator.OnCropDead(shouldRender);
		}
		if (shouldRender)
		{
			if (data.isDead)
			{
				UpdateRenderer(animateGrow: true);
			}
			else
			{
				Renderer.RenderHurt();
			}
		}
	}

	public virtual bool OriginCheckGrowthMonth(bool shouldRender)
	{
		if (DolocAPI.gameManager.gameInitConfig.ignorePlantLimit)
		{
			return true;
		}
		if (plantBasin == null)
		{
			return false;
		}
		if (plantBasin.CurrentRoom.RoomEffectInfo.IgnoreSeason)
		{
			return true;
		}
		if (seedProto.SeedType_Ref.UseRoomEffect && plantBasin.CurrentRoom.RoomEffectInfo.IgnoreSeasonFungus)
		{
			return true;
		}
		if (data.isDead)
		{
			return true;
		}
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		int month = dateNow.Month;
		int[] growthMonths = seedProto.GrowthMonths;
		int value = ((month - 1 == 0) ? DolocAPI.GlobalParameter.Year2Month : (month - 1));
		if (growthMonths.Length != 0 && !growthMonths.Contains(month) && (data.currentLevel == 0 || !growthMonths.Contains(value) || dateNow.Hour >= DolocAPI.GlobalParameter.EventRefreshClock))
		{
			if (data.isMature)
			{
				OriginGenCropOutput(putInBackpack: false);
				if (plantBasin is PlantBasinSimple plantBasinSimple)
				{
					Kill(shouldRender);
					plantBasinSimple.DestroyImmediately();
					return false;
				}
			}
			Kill(shouldRender);
		}
		return true;
	}

	public virtual void OriginGenCropOutput(bool putInBackpack)
	{
		if (seedProto == null || plantBasin == null)
		{
			return;
		}
		RangedItem[] cropOutputs;
		if (geneProtos.IsNullOrEmpty())
		{
			cropOutputs = seedProto.CropOutputs;
			for (int i = 0; i < cropOutputs.Length; i++)
			{
				RangedItem rangedItem = cropOutputs[i];
				for (int j = 0; j < rangedItem.randomCount; j++)
				{
					plantBasin.PlaceItemInBagOrCreateDropItem(rangedItem.itemName, putInBackpack, sendMessage: true);
				}
			}
			return;
		}
		cropOutputs = seedProto.CropOutputs;
		for (int i = 0; i < cropOutputs.Length; i++)
		{
			RangedItem item2 = cropOutputs[i];
			int num = PassOutputData(new CropOutputData(item2.randomCount), item2).CalcFinalOutput();
			if (!DolocAPI.QueryItemProto(item2.itemName, out var proto))
			{
				continue;
			}
			if (proto.Function is ItemFunctionCrop)
			{
				for (int k = 0; k < num; k++)
				{
					Item item3 = DolocAPI.GenerateItem(proto);
					if (item3 is ItemCrop itemCrop)
					{
						if (!plantBasin.currentSeed.IsCloned)
						{
							itemCrop.geneGroup.SetGenes(geneProtos);
						}
						else
						{
							itemCrop.geneGroup.SetGenes(DolocAPI.GlobalParameter.ClonedCropGeneGroup_Ref);
						}
					}
					plantBasin.PlaceItemInBagOrCreateDropItem(item3, putInBackpack, sendMessage: true);
				}
			}
			else
			{
				for (int l = 0; l < num; l++)
				{
					plantBasin.PlaceItemInBagOrCreateDropItem(item2.itemName, putInBackpack, sendMessage: true);
				}
			}
		}
		CropOutputData PassOutputData(CropOutputData data, RangedItem item)
		{
			CropGeneFunction[] array = functions;
			for (int m = 0; m < array.Length; m++)
			{
				data = array[m].HandleOutputData(data, item);
			}
			return data;
		}
	}

	public bool OriginTryGetNeedWaterOrClear(out bool value)
	{
		value = false;
		return false;
	}

	public bool Regrow(bool shouldRender)
	{
		return CropDecorator.DctRegrow(shouldRender);
	}

	public bool GrowBack(bool shouldRender, bool shouldClearGrowth)
	{
		return CropDecorator.DctGrowBack(shouldRender, shouldClearGrowth);
	}

	public bool GrowForward(bool shouldRender, bool shouldClearGrowth)
	{
		return CropDecorator.DctGrowForward(shouldRender, shouldClearGrowth);
	}

	public void Water(bool shouldRender, bool invokeCallback)
	{
		CropDecorator.DctWater(shouldRender, invokeCallback);
	}

	public void Protected(bool shouldRender)
	{
		CropDecorator.DctProtected(shouldRender);
	}

	public void UpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		CropDecorator.DctUpdateNormal(shouldRender, isMoist, addition, fertilizerAddition);
	}

	public void UpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		CropDecorator.DctUpdateAcidRain(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public void UpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		CropDecorator.DctUpdateScorchSun(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public void Thunder(bool shouldRender)
	{
		CropDecorator.DctThunder(shouldRender);
	}

	public void GenCropOutput(bool putInBackpack)
	{
		CropDecorator.DctGenCropOutput(putInBackpack);
	}

	public bool CheckGrowthMonth(bool shouldRender)
	{
		return CropDecorator.DctCheckGrowthMonth(shouldRender);
	}

	public ItemSeed RebuildSeed()
	{
		if (functions.IsNullOrEmpty())
		{
			if (!(DolocAPI.GenerateItem(seedProto.Id) is ItemSeed itemSeed))
			{
				return null;
			}
			itemSeed.geneGroup.Clear();
			return itemSeed;
		}
		if (!(DolocAPI.GenerateItem(seedProto.Id) is ItemSeed itemSeed2))
		{
			return null;
		}
		itemSeed2.geneGroup.SetGenes(geneProtos);
		return itemSeed2;
	}

	public void OnRender()
	{
		CropDecorator.OnRender();
	}

	public void OnUnRender()
	{
		CropDecorator?.OnUnRender();
	}

	public void AfterClearWither(bool shouldRender)
	{
		CropDecorator.AfterClearWither(shouldRender);
	}

	public void AfterHarvest(bool shouldRender)
	{
		CropDecorator.AfterHarvest(shouldRender);
	}

	public bool TryGetNeedWaterOrClear(out bool value)
	{
		return CropDecorator.DctTryGetNeedWaterOrClear(out value);
	}
}
