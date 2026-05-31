using System;
using System.Collections.Generic;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Item;
using DolocTown.Config.TechTree;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.AI.LinearTask;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class Animal : IHasIndex
{
	public const string PROTO_NAME_CHICKEN = "chicken";

	public readonly AnimalInfo proto;

	private readonly Vector2 widthOffset;

	private readonly Counter tuCounter;

	public bool isPassingTime;

	private bool _isInvoked;

	private readonly Counter _invokeCounter = new Counter(3);

	private readonly Counter _moodEmotionCounter = new Counter(30);

	private readonly Counter _envRefreshCounter = new Counter(60);

	private AnimalRenderer _renderer;

	[JsonProperty]
	[DebugInfo("数据序号")]
	private int dataIdx;

	[JsonProperty]
	[DebugInfo("小动物名称", AllowEdit = true)]
	[DebugOnValueChanged("OnNameChanged")]
	public string customName;

	[JsonProperty]
	public DateInfo birthdayInfo;

	[JsonProperty]
	[DebugInfo("小动物状态")]
	public AnimalData data;

	public Room homeRoom;

	public Room currentRoom;

	[JsonProperty]
	[DebugInfo("所在网格坐标")]
	[DebugGroup("环境信息")]
	public Vector2Int positionCell;

	[JsonProperty]
	[DebugInfo("是否处于繁育状态")]
	public bool isBreeding;

	[JsonProperty]
	public Counter breedingCounter;

	[JsonProperty]
	private readonly Dictionary<string, int> husbandryValues;

	[JsonProperty]
	[DebugInfo("逃跑计数器")]
	private int escapeCounter;

	[JsonProperty]
	[DebugInfo("心情结算计数器")]
	private Counter moodCounter;

	[JsonProperty]
	[DebugInfo("是否需要排便")]
	private bool needExcrete;

	[DebugInfo("绘制所有可抵达区域", AllowEdit = true)]
	public bool drawAllAvailablePositions;

	private string _tmp_homeRoomId;

	private string _tmp_currentRoomId;

	private bool isInitAfterLoadData;

	[DebugInfo("是否输出心情值调试信息", AllowEdit = true)]
	public bool debugMoodInfo;

	private AnimalRoomEnv _currentRoomEnv;

	private readonly AnimalMover _mover = new AnimalMover();

	private readonly AnimalJumper _jumper = new AnimalJumper();

	private bool _dontJumpFlag;

	private Vector2Int _fleePositionCache;

	public const int MAX_VALUE = 100;

	public const float MAX_VALUE_METABOLISM = 180f;

	public const float MAX_VALUE_RECIPROCAL = 0.01f;

	private readonly Counter metabolismCDCounter = new Counter(32);

	[DebugInfo("控制器")]
	public AnimalController controller { get; private set; }

	[DebugInfo("宽度")]
	[DebugGroup("原型数据")]
	public int width => proto.Size;

	public int InteractPriority
	{
		get
		{
			if (isSleep)
			{
				return -1;
			}
			int num = (ShowFondleFlag ? 1 : 0);
			int num2 = ((proto.ManualMetabolism && NeedMetabolism) ? 1 : 0);
			return num + num2;
		}
	}

	public AnimalRenderer Renderer
	{
		get
		{
			return _renderer;
		}
		set
		{
			if ((object)value == null)
			{
				if (!(_renderer == null))
				{
					AnimalRenderer renderer = _renderer;
					_renderer.OnDisTouch();
					_renderer.animal = null;
					_renderer = null;
					OnUnRender(renderer);
				}
			}
			else
			{
				_renderer = value;
				_renderer.animal = this;
				OnRender();
			}
		}
	}

	[JsonProperty]
	[DebugInfo("原型ID")]
	[DebugGroup("原型数据")]
	public string protoName => proto.Id;

	[JsonProperty]
	[DebugInfo("所在房间ID")]
	[DebugGroup("环境信息")]
	private string currentRoomId => currentRoom?.RoomId;

	[JsonProperty]
	[DebugInfo("家园房间ID")]
	[DebugGroup("环境信息")]
	private string homeRoomId => homeRoom?.RoomId;

	[JsonProperty]
	[DebugInfo("是否正在睡眠")]
	public bool isSleep { get; private set; }

	[DebugInfo("繁育进度")]
	private string breedingProcess => $"{breedingCounter.Value}/{proto.BreedDuration}";

	public bool isEscaped { get; private set; }

	[DebugInfo("畜牧产出")]
	private string HusbandryInfo
	{
		get
		{
			List<string> list = new List<string>();
			foreach (var (arg, num2) in husbandryValues)
			{
				list.Add($"{arg}[{num2}]");
			}
			return string.Join(";", list);
		}
	}

	[DebugInfo("生日")]
	[DebugGroup("UI信息")]
	public string BirthdayInfo => DolocUtils.Format(DolocConfig.StaticTexts.UiAnimalBirthday, birthdayInfo.GetDateSimpleInfo());

	[DebugInfo("年龄")]
	[DebugGroup("UI信息")]
	public string AgeInfo
	{
		get
		{
			int num = DolocAPI.archiveHandle.DateNow.TotalDays - birthdayInfo.TotalDays + 1;
			int num2 = num / DolocAPI.GlobalParameter.Month2Day;
			int num3 = num2 / DolocAPI.GlobalParameter.Year2Month;
			if (num2 <= 0)
			{
				return DolocConfig.StaticTexts.UiAnimalAgeDay.Format(num);
			}
			if (num3 <= 0)
			{
				return DolocConfig.StaticTexts.UiAnimalAgeMonth.Format(num2);
			}
			num2 %= DolocAPI.GlobalParameter.Year2Month;
			return DolocConfig.StaticTexts.UiAnimalAgeYear.Format(num3) + ((num2 > 0) ? DolocConfig.StaticTexts.UiAnimalAgeMonth.Format(num2) : string.Empty);
		}
	}

	[DebugInfo("饱食度进度")]
	[DebugGroup("UI信息")]
	[DebugProgressBar(null)]
	public float EnergyProcess
	{
		get
		{
			if (!(Energy >= 100f))
			{
				return Energy * 0.01f;
			}
			return 1f;
		}
	}

	[DebugInfo("心情值进度")]
	[DebugGroup("UI信息")]
	[DebugProgressBar(null)]
	public float MoodProcess => (float)Mood * 0.01f;

	[DebugInfo("状态")]
	[DebugGroup("UI信息")]
	public AnimalState CurrentState
	{
		get
		{
			if (isBreeding)
			{
				if (!data.isChild)
				{
					return AnimalState.Breeding;
				}
				return AnimalState.Incubate;
			}
			if (Mood >= DolocAPI.GlobalParameter.AnimalUnhappyThreshold)
			{
				return AnimalState.Happy;
			}
			if (Mood < DolocAPI.GlobalParameter.AnimalWeaknessThreshold)
			{
				return AnimalState.Weakness;
			}
			return AnimalState.Unhappy;
		}
	}

	public bool IsInfoVisible => CurrentState != AnimalState.Incubate;

	public int index
	{
		get
		{
			return dataIdx;
		}
		set
		{
			dataIdx = value;
		}
	}

	public bool isDeserializationValid => proto != null;

	[DebugInfo("饱食度值", AllowEdit = true)]
	public float Energy
	{
		get
		{
			return data.energy;
		}
		set
		{
			data.energy = Mathf.Clamp(value, 0f, 100f);
		}
	}

	[DebugInfo("代谢值", AllowEdit = true)]
	public float Metabolism
	{
		get
		{
			return data.metabolism;
		}
		set
		{
			data.metabolism = Mathf.Clamp(value, 0f, 180f);
			if (IsRender)
			{
				RefreshMetabolismFlag();
			}
		}
	}

	public bool IsRender => Renderer != null;

	public bool IsHungry => Energy < (float)proto.HungryThreshold;

	public bool ShouldExcrete
	{
		get
		{
			if (Energy >= proto.ExcreteCost)
			{
				return needExcrete;
			}
			return false;
		}
	}

	public bool NeedBreed
	{
		get
		{
			AnimalData animalData = data;
			if (animalData.isAdult && animalData.growth >= 100f && Energy >= proto.BreedEnergyCost && ((IAnimalHost)currentRoom).CountAnimal(protoName, isAdult: true) > 1)
			{
				return ((IAnimalHost)homeRoom).CheckAnimalSpace(proto.Space);
			}
			return false;
		}
	}

	public bool NeedMetabolism
	{
		get
		{
			if (NeedMetabolismWithoutMood)
			{
				return IsMoodSatisfiedProduce;
			}
			return false;
		}
	}

	public bool NeedMetabolismWithoutMood
	{
		get
		{
			if (data.isAdult)
			{
				return Metabolism >= 100f;
			}
			return false;
		}
	}

	public bool IsAdult => data.isAdult;

	public bool IsInHome => currentRoom == homeRoom;

	public bool IsInHouse => currentRoom.IsInHouse;

	public bool ShouldNotEscape => Mood >= DolocAPI.GlobalParameter.AnimalWeaknessThreshold;

	public bool CanInvokeNow
	{
		get
		{
			if (Renderer == null)
			{
				return false;
			}
			if (Renderer.IsJumping)
			{
				return false;
			}
			if (IsFlee)
			{
				return false;
			}
			return !isSleep;
		}
	}

	public string Title
	{
		get
		{
			if (!customName.IsNullOrEmpty())
			{
				return customName;
			}
			return proto.Title;
		}
		set
		{
			customName = ((value == proto.DefaulInputName || value == proto.Title) ? "" : value);
		}
	}

	public string DescriptionInSack
	{
		get
		{
			if (!IsAdult)
			{
				return proto.ChildDescriptionInSack;
			}
			return proto.AdultDescriptionInSack;
		}
	}

	public bool IsMoodSatisfiedProduce
	{
		get
		{
			if (DolocAPI.gameManager.gameInitConfig.ignoreAnimalMoodWhenProduce)
			{
				return true;
			}
			return Mood >= proto.ProduceRequireMood;
		}
	}

	[DebugInfo("心情值", AllowEdit = true)]
	public int Mood
	{
		get
		{
			return data.mood;
		}
		set
		{
			data.mood = Mathf.Clamp(value, 0, 100);
			if (ShouldNotEscape && escapeCounter > 0)
			{
				escapeCounter = 0;
			}
		}
	}

	public IEnumerable<Vector2Int> CoverPositions => proto.OccupiedPositions.Offset(positionCell);

	public Sprite Icon
	{
		get
		{
			if (!IsAdult)
			{
				return proto.UiChildSprite.Asset;
			}
			return proto.UiSprite.Asset;
		}
	}

	public float PositionZ
	{
		get
		{
			float num = ((homeRoom is TemplateRoomInHouse templateRoomInHouse) ? ((float)templateRoomInHouse.Building.index * 0.01f) : 0f);
			return -0.3f - (float)index * 0.001f - num;
		}
	}

	public Vector3 PositionWSOfCell => CalcPositionOfCell(positionCell);

	public Vector3 PositionWSOfCellNoOffset => CalcPositionOfCell(positionCell, addWidthOffset: false);

	public Vector3 PositionDropItem
	{
		get
		{
			Vector3 positionWSOfCell = PositionWSOfCell;
			positionWSOfCell.y += 1.5f;
			return positionWSOfCell;
		}
	}

	public bool ShowFondleFlag
	{
		get
		{
			if (!data.hasFondled)
			{
				return Mood < DolocAPI.GlobalParameter.AnimalMoodShowFlagThreshold;
			}
			return false;
		}
	}

	[DebugInfo("繁育值", AllowEdit = true)]
	public float DEBUG_BreedingValue
	{
		get
		{
			return data.growth;
		}
		set
		{
			data.growth = Mathf.Clamp(value, 0f, 100f);
		}
	}

	[DebugInfo("是否已经逃跑", AllowEdit = true)]
	public bool DEBUG_IsEscaped
	{
		get
		{
			return isEscaped;
		}
		set
		{
			isEscaped = value;
		}
	}

	[DebugInfo("成长状态", AllowEdit = true)]
	public AnimalMatureType DEBUG_MatureType
	{
		get
		{
			return data.matureType;
		}
		set
		{
			data.matureType = value;
		}
	}

	[DebugGroup("AI控制")]
	[DebugInfo("控制状态")]
	public string DEBUG_CurrentAIState => controller.CurrentStateName;

	public bool isMoving { get; private set; }

	[DebugInfo("小动物是否正在被驱赶")]
	public bool IsFlee { get; private set; }

	public bool isCurrentRoomClosed
	{
		get
		{
			if (currentRoom is TemplateRoomInHouse templateRoomInHouse)
			{
				return templateRoomInHouse.Building.isClosed;
			}
			return false;
		}
	}

	public AnimalJumper Jumper => _jumper;

	public AnimalRoomEnv CurrentEnv
	{
		get
		{
			if (_currentRoomEnv != null)
			{
				return _currentRoomEnv;
			}
			Vector2Int[] positions = AnimalUtils.FindAvailablePositions(currentRoom, positionCell, width, 5000, "重新构建小动物环境");
			_currentRoomEnv = new AnimalRoomEnv(currentRoom, positions);
			return _currentRoomEnv;
		}
	}

	public AnimalRoomEnv HomeEnv
	{
		get
		{
			if (_currentRoomEnv.room == homeRoom)
			{
				return _currentRoomEnv;
			}
			if (!AnimalUtils.GetTransitionPoint(homeRoom, currentRoom, out var pt))
			{
				Debug.LogWarning("Animal.HomeEnv: 无法获取小动物的家园环境");
				return null;
			}
			Vector2Int[] positions = AnimalUtils.FindAvailablePositions(homeRoom, pt, proto.Size, 5000, "Animal.HomeEnv");
			return new AnimalRoomEnv(homeRoom, positions);
		}
	}

	public Vector2Int AroundPosition => _currentRoomEnv.GetAroundPosition(positionCell);

	public Vector2Int RandomPosition => _currentRoomEnv.GetRandomPosition(positionCell);

	public Room AnotherRoom
	{
		get
		{
			if (currentRoom == null || homeRoom == null)
			{
				return null;
			}
			if (currentRoom == homeRoom)
			{
				if (currentRoom != DolocAPI.archiveHandle.MainFarm)
				{
					return DolocAPI.archiveHandle.MainFarm;
				}
				return null;
			}
			if (currentRoom == DolocAPI.archiveHandle.MainFarm)
			{
				return homeRoom;
			}
			return DolocAPI.archiveHandle.MainFarm;
		}
	}

	private void OnNameChanged(string newName)
	{
		Debug.Log("测试，小动物名称更换为:" + newName);
	}

	[DebugButton("改名", ButtonType = DebugButtonType.GroupToolbar)]
	[DebugGroup("UI信息")]
	private void ChangeName()
	{
		InvokeChangeNameInputBox();
	}

	private Vector3 CalcPositionOfCell(Vector2Int positionCell, bool addWidthOffset = true)
	{
		Vector2 vector = (addWidthOffset ? widthOffset : Vector2.zero);
		Vector3 result = ((currentRoom == null) ? (positionCell * DolocTransform.TILE_WORLD_SIZE + vector) : (currentRoom.Geometry.CalcWorldPosition(positionCell) + vector));
		result.z = PositionZ;
		return result;
	}

	public Animal(AnimalInfo proto, DateInfo birthdayInfo)
	{
		isInitAfterLoadData = true;
		this.proto = proto;
		this.birthdayInfo = birthdayInfo;
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		positionCell = default(Vector2Int);
		data = default(AnimalData);
		data.mood = 70;
		data.energy = proto.DefaultEnergy;
		currentRoom = null;
		controller = new AnimalController(this);
		widthOffset = new Vector2((float)proto.Size * 0.5f * 1.5f, 0f);
		breedingCounter = new Counter(proto.BreedDuration);
		husbandryValues = new Dictionary<string, int>();
		moodCounter = new Counter(DolocAPI.GlobalParameter.AnimalMoodUpdateInterval * DolocAPI.GlobalParameter.TULength);
	}

	[JsonConstructor]
	public Animal(int dataIdx, string protoName, string customName, DateInfo birthdayInfo, AnimalData data, Vector2Int positionCell, string homeRoomId, string currentRoomId, bool isSleep, bool isBreeding, Counter breedingCounter, Dictionary<string, int> husbandryValues, int escapeCounter, Counter moodCounter, bool needExcrete = false)
	{
		if (!DolocConfig.Tables.TbAnimal.DataMap.TryGetValue(protoName, out var value))
		{
			proto = null;
			return;
		}
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		this.dataIdx = dataIdx;
		proto = value;
		this.data = data;
		this.customName = customName;
		this.birthdayInfo = birthdayInfo;
		this.positionCell = positionCell;
		widthOffset = new Vector2((float)value.Size * 0.5f * 1.5f, 0f);
		_tmp_currentRoomId = currentRoomId;
		_tmp_homeRoomId = homeRoomId;
		this.isSleep = isSleep;
		this.isBreeding = isBreeding;
		this.breedingCounter = breedingCounter ?? new Counter(value.BreedDuration);
		this.husbandryValues = husbandryValues ?? new Dictionary<string, int>();
		this.escapeCounter = escapeCounter;
		this.needExcrete = needExcrete;
		this.moodCounter = moodCounter ?? new Counter(DolocAPI.GlobalParameter.AnimalMoodUpdateInterval * DolocAPI.GlobalParameter.TULength);
		this.moodCounter.ValidateInterval(DolocAPI.GlobalParameter.AnimalMoodUpdateInterval * DolocAPI.GlobalParameter.TULength);
		if (isBreeding && isEscaped)
		{
			isEscaped = false;
		}
	}

	public void AfterLoadData()
	{
		if (!isInitAfterLoadData)
		{
			isInitAfterLoadData = true;
			controller = new AnimalController(this);
			homeRoom = DolocAPI.GetRoom(_tmp_homeRoomId);
			currentRoom = DolocAPI.GetRoom(_tmp_currentRoomId);
			if (currentRoom != null)
			{
				ResetEnvToTargetRoom(currentRoom);
			}
		}
	}

	public bool TryCostEnergy(float value)
	{
		value = (isSleep ? (value * DolocAPI.GlobalParameter.AnimalEnergyCostPercentInNight) : value);
		return data.TryCostEnergy(value);
	}

	[DebugButton("召唤至室外")]
	[DebugGroup("环境信息")]
	public void CallToOutside()
	{
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.MainFarm;
		if (currentRoom != mainFarm)
		{
			if (!AnimalUtils.GetTransitionPoint(mainFarm, currentRoom, out var pt))
			{
				pt = mainFarm.Geometry.groundPositions.Choice();
			}
			CallToRoom(mainFarm, pt);
		}
	}

	[DebugButton("召唤至室内")]
	[DebugGroup("环境信息")]
	public void CallToHome()
	{
		Vector2Int position = homeRoom.Geometry.groundPositions.Choice();
		CallToRoom(homeRoom, position);
	}

	public void ToggleAnimalIndoorsStatus()
	{
		if (currentRoom == homeRoom)
		{
			CallToOutside();
		}
		else
		{
			CallToHome();
		}
	}

	public void CallToRoom(Room room, Vector2Int position)
	{
		if (room != null && room != currentRoom)
		{
			EnterRoom(room, checkRoomClosed: false);
			controller.ChangeTask(LinearTask.WaitFrames(2));
			if (isSleep)
			{
				WakeUp();
			}
			if (IsRender)
			{
				DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.CONFUSE);
			}
			positionCell = position + new Vector2Int(UnityEngine.Random.Range(-2, 2), 0);
			if (IsRender)
			{
				Renderer.position = PositionWSOfCell;
			}
		}
	}

	public void InvokeChangeNameInputBox(Action onConfirm = null)
	{
		DolocAPI.EnterUI((RenamingUiState state) => state.HandleStartUpArgs(DolocConfig.StaticTexts.AnimalInputAnimalName, proto.DefaulInputName, proto.Title, delegate(string text)
		{
			Title = text;
			onConfirm?.Invoke();
		}, DolocAPI.GlobalParameter.InputPlayerNameMaxLength));
	}

	private void OnRender()
	{
		Vector3 positionWSOfCell = PositionWSOfCell;
		Renderer.position = positionWSOfCell;
		Renderer.animatorController = (data.isAdult ? proto.Animator.Asset : proto.ChildAnimator.Asset);
		Renderer.EmotionOffset = proto.GetEmotionOffset(data.isAdult);
		Renderer.SetVisible(value: true);
		Renderer.PlayAnimation(_GetCurrentAnimation());
		Renderer.ShowSleepEffects = isSleep;
		Renderer.BoxColliderSize = (data.isAdult ? proto.ColliderSizeAdult : proto.ColliderSizeChild);
		if (proto.ManualMetabolism)
		{
			Renderer.ShowMetabolismFlag = NeedMetabolism;
		}
		Renderer.ShowFondleFlag = ShowFondleFlag;
		DolocAPI.Sound.RegisterGameObject(Renderer.gameObject);
		_dontJumpFlag = controller.CheckTaskType<AnimalJump>();
	}

	private void RefreshAI()
	{
		controller = new AnimalController(this);
	}

	private void RefreshMetabolismFlag()
	{
		if (IsRender && proto.ManualMetabolism)
		{
			Renderer.ShowMetabolismFlag = NeedMetabolism;
		}
	}

	private void RefreshFondleFlag()
	{
		if (IsRender)
		{
			Renderer.ShowFondleFlag = ShowFondleFlag;
		}
	}

	private string _GetCurrentAnimation()
	{
		if (isSleep)
		{
			return "sleep";
		}
		return "idle";
	}

	private void OnUnRender(AnimalRenderer renderer)
	{
		if (IsFlee)
		{
			StopFlee();
		}
		DolocAPI.Sound.UnregisterGameObject(renderer.gameObject);
		DolocAPI.ClearEmotions(renderer.transform);
		DolocAPI.EntitySystem.Recycle(renderer);
	}

	public void Update()
	{
		if (_envRefreshCounter.Tick())
		{
			RefreshCurrentEnv();
		}
		if (IsFlee)
		{
			return;
		}
		if (_isInvoked)
		{
			if (_invokeCounter.Tick())
			{
				_isInvoked = false;
			}
			return;
		}
		if (isBreeding)
		{
			_UpdateBreeding(shouldRender: true);
			return;
		}
		if (tuCounter.Tick())
		{
			WeatherType id = currentRoom.CurrentWeatherInfo.Id;
			_UpdateData(id);
		}
		_EnsurePositionValid();
		controller.UpdatePerSec();
	}

	public void UpdateNoRender()
	{
		if (_envRefreshCounter.Tick())
		{
			RefreshCurrentEnv();
		}
		if (_isInvoked)
		{
			_isInvoked = false;
		}
		if (isBreeding)
		{
			_UpdateBreeding();
			return;
		}
		if (tuCounter.Tick())
		{
			WeatherType id = currentRoom.CurrentWeatherInfo.Id;
			_UpdateData(id);
		}
		_EnsurePositionValid();
		controller.UpdatePerSec();
	}

	private void _EnsurePositionValid()
	{
		if (!AnimalUtils.IsPositionWalkable(currentRoom, positionCell, width) || _currentRoomEnv.IsEmpty)
		{
			MoveToValidPosition();
		}
	}

	private void _UpdateBreeding(bool shouldRender = false)
	{
		if (tuCounter.Tick() && breedingCounter.Tick())
		{
			if (data.isChild)
			{
				birthdayInfo = DolocAPI.archiveHandle.DateNow;
				DolocAPI.BroadcastString(GameEventType.ANIMAL_BIRTH, protoName);
				DolocAPI.AddTechExp(TechPointType.ANIMAL, proto.BreedTechPoint);
				DolocAPI.archiveHandle.RecordCollection(CollectionType.Animal, protoName);
			}
			isBreeding = false;
			if (shouldRender)
			{
				RefreshRenderer();
			}
		}
	}

	public void RefreshRenderer()
	{
		if (DolocAPI.CurrentRoom == currentRoom && !isPassingTime && !isBreeding)
		{
			if (Renderer == null)
			{
				Renderer = DolocAPI.EntitySystem.Next<AnimalRenderer>();
				Renderer.animal = this;
			}
		}
		else if (Renderer != null)
		{
			Renderer = null;
		}
	}

	private void _UpdateData(WeatherType weatherType)
	{
		if (data.isChild && ++data.growthCounter >= proto.GrowInterval)
		{
			data.growthCounter = 0;
			Grow();
		}
		_UpdateMood(weatherType);
		data.excreteCounter++;
		data.eatCounter++;
		if (data.isAdult)
		{
			_UpdateDataAdult();
		}
	}

	public void OnThunder()
	{
		Mood -= DolocAPI.GlobalParameter.AnimalThunderMoodDecrease;
		if (IsRender)
		{
			DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.SCARED);
		}
	}

	private void _UpdateDataAdult()
	{
		data.breedingCounter++;
		data.metabolismCounter++;
		if (data.metabolismCounter >= proto.MetabolismInterval)
		{
			data.metabolismCounter = 0;
			if (TryCostEnergy(proto.MetabolismCost))
			{
				Metabolism += proto.MetabolismIncrease;
			}
		}
		if (!(data.growth < 100f))
		{
			return;
		}
		data.growthCounter++;
		if (data.growthCounter >= proto.FertilityInterval)
		{
			data.growthCounter = 0;
			if (TryCostEnergy(proto.FertilityCost))
			{
				data.growth = Mathf.Min(100f, data.growth + proto.FertilityIncrease);
			}
		}
	}

	public void __OnDayChanged()
	{
		RefreshAI();
		_currentRoomEnv.Refresh(positionCell, width);
		if (data.matureType == AnimalMatureType.PreAdult)
		{
			data.matureType = AnimalMatureType.Adult;
			DolocAPI.BroadcastString(GameEventType.ANIMAL_GROW_UP, protoName);
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Animal, protoName);
		}
		data.hasFondled = false;
		_UpdateEscapeCounter();
	}

	public void __OnWeatherChanged(WeatherType type)
	{
		controller.ChangeTask(LinearTask.WaitFrames(UnityEngine.Random.Range(1, 3)));
	}

	public void __OnFenceChanged(Room changedRoom)
	{
		if (currentRoom == changedRoom)
		{
			RefreshCurrentEnv();
			if (controller.CheckTaskType<AnimalMove>())
			{
				controller.ChangeTask(LinearTask.WaitFrames(2));
			}
		}
	}

	public void __OnTerrainExtent(Room changedRoom, Vector2Int offset)
	{
		if (currentRoom != changedRoom)
		{
			return;
		}
		positionCell += offset;
		if (AnimalUtils.IsPositionWalkable(currentRoom, positionCell, width))
		{
			RefreshCurrentEnv();
			controller.RefreshWork();
			if (IsRender)
			{
				Renderer.position2d = PositionWSOfCell;
			}
		}
		else
		{
			MoveToValidPosition();
		}
	}

	public void __OnPlatformChanged(Room changedRoom)
	{
		if (currentRoom == changedRoom)
		{
			if (AnimalUtils.IsPositionWalkable(currentRoom, positionCell, width))
			{
				RefreshCurrentEnv();
				controller.RefreshWork();
			}
			else
			{
				MoveToValidPosition();
			}
		}
	}

	public void __OnBuildingChanged(Room changedRoom)
	{
		if (changedRoom == currentRoom)
		{
			if (AnimalUtils.IsPositionWalkable(currentRoom, positionCell, width))
			{
				RefreshCurrentEnv();
				controller.RefreshWork();
			}
			else
			{
				MoveToValidPosition();
			}
		}
	}

	private void MoveToValidPosition()
	{
		if (currentRoom.Geometry.GetNearestGroundPositionCanPlaceAgent(positionCell, out var groundPosition))
		{
			positionCell = groundPosition;
		}
		else
		{
			positionCell = currentRoom.Geometry.GetNearestGroundPosition(positionCell);
		}
		if (IsRender)
		{
			Renderer.position2d = PositionWSOfCell;
		}
		RefreshCurrentEnv();
		controller.RefreshWork();
	}

	public override string ToString()
	{
		return (customName.IsNullOrEmpty() ? proto.DefaulInputName : customName) + "(" + protoName + ")";
	}

	public void DEBUG_SetHomeClosed(bool value)
	{
		if (homeRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			templateRoomInHouse.Building.SetClosed(value);
		}
	}

	[DebugButton("关闭家园房间")]
	[DebugGroup("环境信息")]
	public void DEBUG_CloseHome()
	{
		DEBUG_SetHomeClosed(value: true);
	}

	[DebugButton("打开家园房间")]
	[DebugGroup("环境信息")]
	public void DEBUG_OpenHome()
	{
		DEBUG_SetHomeClosed(value: false);
	}

	[DebugButton("重置排泄周期")]
	public void DEBUG_ResetExcreteInterval()
	{
		data.excreteCounter = proto.ExcreteInterval;
	}

	[DebugButton("重置进食周期")]
	public void DEBUG_ResetEatInterval()
	{
		data.eatCounter = proto.EatInterval;
	}

	[DebugButton("重置繁育周期")]
	public void DEBUG_ResetBreedInterval()
	{
		data.breedingCounter = 100;
	}

	[DebugButton("重置代谢周期")]
	public void DEBUG_ResetMetabolismInterval()
	{
		data.metabolismCounter = proto.MetabolismInterval;
	}

	[DebugButton("模拟畜牧输出")]
	public void DEBUG_SimulateOutput()
	{
		CountItem[] array = SimulateProduceAsItems();
		for (int i = 0; i < array.Length; i++)
		{
			CountItem countItem = array[i];
			Debug.Log($"<color=#ffff00>模拟输出: {countItem.itemCount}个{countItem.itemName}</color>");
		}
	}

	[DebugButton("检查畜牧贡献度值")]
	public void DEBUG_CheckHusbandryContribution()
	{
		if (husbandryValues.Count == 0)
		{
			Debug.Log("当前小动物没有畜牧贡献度值");
			return;
		}
		foreach (KeyValuePair<string, int> husbandryValue in husbandryValues)
		{
			Debug.Log($"<color=#ffff00>畜牧贡献度: {husbandryValue.Key} - {husbandryValue.Value}</color>");
		}
	}

	[DebugButton("删除动物", ButtonType = DebugButtonType.GroupToolbar)]
	[DebugGroup("")]
	public void DEBUG_RemoveThisAnimal()
	{
		if (isBreeding)
		{
			Debug.LogError("繁育状态的小动物不可删除");
		}
		else
		{
			((IAnimalHost)homeRoom).RemoveAnimal(this);
		}
	}

	public void DEBUG_SetHusbandryValue(string key, int value)
	{
		if (string.IsNullOrEmpty(key))
		{
			Debug.LogError("畜牧贡献度键不能为空");
			return;
		}
		if (value < 0)
		{
			Debug.LogError("畜牧贡献度值不能小于0");
			return;
		}
		husbandryValues[key] = value;
		Debug.Log($"设置畜牧贡献度: {key} - {value}");
	}

	public void DEBUG_SetAdult(bool value)
	{
		if (data.isAdult != value)
		{
			data.matureType = (value ? AnimalMatureType.Adult : AnimalMatureType.Child);
			if (IsRender)
			{
				Renderer.animatorController = (value ? proto.Animator.Asset : proto.ChildAnimator.Asset);
			}
		}
	}

	public void OnReceiveEvent(AnimalEvent evt)
	{
		if (evt.Type == AnimalEventType.CHICKEN_NEST_BROKEN)
		{
			_HandleChickenNestBroken(evt);
		}
	}

	private void _HandleChickenNestBroken(AnimalEvent evt)
	{
		if (!CanInvokeNow || isBreeding || !IsRender || !data.isAdult || protoName != "chicken")
		{
			return;
		}
		ChickenNest value = ((GameEventArgs<ChickenNest>)evt.Args).value;
		evt.Use();
		Vector2Int targetPosition = value.Anchor + new Vector2Int(value.proto.CoverSize.x / 2, 0);
		DolocAPI.RaiseEmotion(Renderer.transform, EmotionName.ANGRY);
		if (this.GenTask_JourneyToPosition(targetPosition, out var task))
		{
			task.Do(delegate
			{
				Face(DolocAPI.AgentPosition);
			});
			controller.ChangeTask(task.WrapAnimalTask(this));
		}
		else
		{
			task = LinearTask.StartWith.Do(delegate
			{
				Face(DolocAPI.AgentPosition);
			});
			controller.ChangeTask(task.WrapAnimalTask(this));
		}
	}

	public void Grow()
	{
		data.growthCounter = 0;
		if (TryCostEnergy(proto.GrowCost))
		{
			data.growth += proto.GrowIncrease;
			if (!(data.growth < 100f))
			{
				data.matureType = AnimalMatureType.PreAdult;
				data.growth = 0f;
			}
		}
	}

	private bool TryCreateNewAnimal()
	{
		Animal animal = ((IAnimalHost)currentRoom).CreateAnimal(proto, positionCell, shouldRender: false);
		if (animal == null)
		{
			return false;
		}
		animal.isBreeding = true;
		animal.breedingCounter.Reset();
		animal.isPassingTime = isPassingTime;
		return true;
	}

	public bool CanBreedNow()
	{
		if (data.matureType != AnimalMatureType.Adult)
		{
			return false;
		}
		if (data.growth < 100f)
		{
			return false;
		}
		if (Energy < proto.BreedEnergyCost)
		{
			return false;
		}
		return ((IAnimalHost)homeRoom).CheckAnimalSpace(proto.Space);
	}

	public bool _Breed()
	{
		if (!CanBreedNow())
		{
			return false;
		}
		if (!TryCreateNewAnimal())
		{
			Debug.LogError("Animal._Breed: 创建小动物失败，请检查原因");
			return false;
		}
		data.growth = 0f;
		TryCostEnergy(proto.BreedEnergyCost);
		isBreeding = true;
		if (isEscaped)
		{
			isEscaped = false;
		}
		breedingCounter.Reset();
		RefreshRenderer();
		return true;
	}

	public void Fondle()
	{
		if (isSleep)
		{
			return;
		}
		if (!data.hasFondled)
		{
			if (DolocAPI.AgentEquipmentManager.IsShepherdActive(proto.Id, out var moodIncrease))
			{
				DolocAPI.RaiseInstantPSEffects(DolocAPI.agent.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
			}
			Mood += proto.MoodIncreaseFondle + moodIncrease;
			DolocAPI.AddTechExp(TechPointType.ANIMAL, DolocAPI.GlobalParameter.AnimalTechpointFondle);
		}
		DolocAPI.RaiseEmotionLimited(Renderer.transform, EmotionName.LOVE);
		PlayAnimalSound();
		data.hasFondled = true;
		RefreshFondleFlag();
	}

	public void PlayAnimalSound()
	{
		DolocAPI.Sound.PostSoundEvent(data.isChild ? proto.SoundEventChild : proto.SoundEvent);
	}

	private void _UpdateEscapeCounter()
	{
		if (!ShouldNotEscape && !isBreeding)
		{
			if (!EscapeCheck(out var probability))
			{
				Debug.Log($"逃跑判定失败,当前概率{probability:F1}");
				escapeCounter++;
			}
			else
			{
				Debug.Log($"逃跑判定成功,当前概率{probability:F1}");
				isEscaped = true;
			}
		}
	}

	private bool EscapeCheck(out float probability)
	{
		probability = proto.EscapeProbabilityBase;
		probability += proto.EscapeProbabilityIncrease * (float)escapeCounter;
		if (!(probability >= 1f))
		{
			return RandomUtils.Dice(probability);
		}
		return true;
	}

	private void _UpdateMood(WeatherType weatherType)
	{
		if (!isSleep && moodCounter.Tick())
		{
			int num = 0;
			StringBuilder stringBuilder = new StringBuilder();
			if (weatherType.IsMalignantWeather() || weatherType.IsRainyWeather())
			{
				num += DolocAPI.GlobalParameter.AnimalMoodContributionWeather;
				stringBuilder.AppendLine($"恶劣天气影响心情 {DolocAPI.GlobalParameter.AnimalMoodContributionWeather}");
			}
			if (Energy < (float)proto.MoodDecreaseThreshold)
			{
				num += DolocAPI.GlobalParameter.AnimalMoodContributionHungry;
				stringBuilder.AppendLine($"饥饿影响心情 {DolocAPI.GlobalParameter.AnimalMoodContributionHungry}");
			}
			else if (Mood < proto.MaxNatureMood)
			{
				num += DolocAPI.GlobalParameter.AnimalMoodContributionFull;
				stringBuilder.AppendLine($"饱食影响心情 {DolocAPI.GlobalParameter.AnimalMoodContributionFull}");
			}
			Vector2Int offset = positionCell + new Vector2Int(width / 2, 0);
			IEnumerable<Vector2Int> positions = proto.MoodAffectPositions.Offset(offset);
			int moodContribution = AnimalUtils.GetMoodContribution(currentRoom, positions);
			num += moodContribution;
			if (moodContribution != 0)
			{
				stringBuilder.AppendLine($"周围设备影响心情 {moodContribution}");
			}
			stringBuilder.Append($"最终心情变化: {num}");
			if (debugMoodInfo)
			{
				Debug.Log($"小动物<{ToString()}>心情值更新:\n{stringBuilder}");
			}
			Mood += num;
			if (IsRender && num > 3 && IsMoodSatisfiedProduce && RandomUtils.Dice(0.3f))
			{
				DolocAPI.RaiseEmotionLimited(Renderer.transform, EmotionName.PROUD);
			}
		}
	}

	public void RefreshCurrentEnv()
	{
		_currentRoomEnv?.Refresh(positionCell, width);
	}

	private void ResetEnvToTargetRoom(Room currentRoom)
	{
		Vector2Int[] positions = AnimalUtils.FindAvailablePositions(currentRoom, positionCell, width, 5000, "Animal.RefreshEnv");
		_currentRoomEnv = new AnimalRoomEnv(currentRoom, positions);
	}

	private bool EnsureRoomReleationship(Room room)
	{
		if (room == null)
		{
			return false;
		}
		if (room.Type != RoomType.Farm)
		{
			return false;
		}
		return room.IsInHouse != currentRoom.IsInHouse;
	}

	public void EnterRoom(Room nextRoom, bool checkRoomClosed = true)
	{
		if (nextRoom == null || nextRoom == currentRoom)
		{
			return;
		}
		if (currentRoom == null)
		{
			Vector2Int position = (nextRoom.IsInHouse ? nextRoom.Geometry.CalcCellPosition(nextRoom.Geometry.DefaultEntryPosition) : nextRoom.Geometry.groundPositions.Choice());
			SetCurrentRoom(nextRoom, position);
		}
		if (checkRoomClosed && currentRoom.IsRoomClosed())
		{
			Debug.LogError("Animal.EnterRoom: 当前房间已关闭，无法进行房间切换");
			return;
		}
		if (!EnsureRoomReleationship(nextRoom))
		{
			Debug.Log("Animal.EnterRoom: 要去的房间" + nextRoom.Title + "不合法");
			return;
		}
		Room room = currentRoom;
		Vector2Int position2;
		if (room.IsInHouse)
		{
			Vector2 entryPosition = nextRoom.DM_building.GetBuilding(room.Title).EntryPosition;
			position2 = nextRoom.Geometry.CalcCellPosition(entryPosition);
		}
		else
		{
			Vector2 defaultEntryPosition = nextRoom.Geometry.DefaultEntryPosition;
			position2 = nextRoom.Geometry.CalcCellPosition(defaultEntryPosition);
		}
		SetCurrentRoom(nextRoom, position2);
		RefreshRenderer();
	}

	public void SetCurrentRoom(Room room, Vector2Int position)
	{
		if (room != null)
		{
			currentRoom = room;
			positionCell = position;
			ResetEnvToTargetRoom(room);
		}
	}

	public void SetHomeRoom(Room room)
	{
		if (room.Type == RoomType.Farm)
		{
			homeRoom = room;
		}
	}

	public void StartMove(Vector2Int dest)
	{
		isMoving = true;
		Vector3 positionWSOfCell = PositionWSOfCell;
		float walkSpeed = ((Mathf.Abs(dest.x - positionCell.x) >= proto.RunThreshold) ? proto.RunSpeed : proto.MoveSpeed);
		_mover.SetMoveInfo(positionWSOfCell, positionCell, dest, walkSpeed);
	}

	public bool Move()
	{
		if (!isMoving)
		{
			return true;
		}
		bool result = _mover.Move(out positionCell);
		if (IsRender)
		{
			Renderer.MoveTo(_mover.positionWS);
			Renderer.PlayAnimation("move");
		}
		return result;
	}

	public void StopMove()
	{
		if (isMoving)
		{
			isMoving = false;
		}
		if (IsRender)
		{
			Renderer.StopMove();
		}
	}

	public bool StartJump(Vector2Int dest)
	{
		if (!AnimalUtils.IsPositionWalkable(currentRoom, dest, width))
		{
			controller.RefreshWork();
			return false;
		}
		Vector3 vector = (IsRender ? Renderer.position : PositionWSOfCell);
		Vector3 vector2 = CalcPositionOfCell(dest);
		positionCell = dest;
		_jumper.SetJumpInfo(vector, vector2, 0.63f, proto.JumpHeight);
		if (IsRender)
		{
			Renderer.Jump(vector2);
		}
		return true;
	}

	public bool Jump()
	{
		if (_dontJumpFlag)
		{
			_dontJumpFlag = false;
			return true;
		}
		if (IsRender)
		{
			Renderer.StopJump();
		}
		return true;
	}

	public void StopFlee()
	{
		IsFlee = false;
		positionCell = _fleePositionCache;
		controller.RefreshWork();
		if (Renderer != null)
		{
			Renderer.PlayAnimation("idle");
		}
	}

	public bool Flee()
	{
		if (IsFlee)
		{
			return false;
		}
		if (!IsRender || Renderer.IsJumping)
		{
			return false;
		}
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		if (!AnimalUtils.TryGetFleePosition(currentRoom, this, agentRoomCellPosition, positionCell, out _fleePositionCache))
		{
			return false;
		}
		IsFlee = true;
		Vector2 dest = currentRoom.Geometry.CalcWorldPosition(_fleePositionCache);
		Renderer.PlayAnimation("move");
		Renderer.MoveTo(dest, proto.RunSpeed, StopFlee);
		return true;
	}

	public bool CheckEatInterval()
	{
		if (data.eatCounter < proto.EatInterval)
		{
			return false;
		}
		data.eatCounter = 0;
		controller.ResetRoomSearcherStatus<IFeeder>();
		return true;
	}

	public bool CheckExcreteInterval()
	{
		if (data.excreteCounter < proto.ExcreteInterval)
		{
			return false;
		}
		needExcrete = true;
		data.excreteCounter = 0;
		controller.ResetRoomSearcherStatus<IAnimalToilet>();
		return true;
	}

	public bool CheckBreedInterval()
	{
		if (data.breedingCounter < 100)
		{
			return false;
		}
		data.breedingCounter = 0;
		controller.ResetRoomSearcherStatus<IAnimalLivestockNursery>();
		return true;
	}

	public void Eat(int feedCount, string name = null, bool playAnimation = true)
	{
		data.AddEnergy(feedCount);
		data.eatCounter = 0;
		if (IsRender)
		{
			Renderer.Eat(playAnimation);
		}
		HandleHusbandry(name);
	}

	private void HandleHusbandry(string name)
	{
		if (name.IsNullOrEmpty())
		{
			return;
		}
		TbHusbandry.ContributionInfo[] contributionInfos = DolocConfig.Tables.TbHusbandry.GetContributionInfos(protoName, name);
		for (int i = 0; i < contributionInfos.Length; i++)
		{
			TbHusbandry.ContributionInfo contributionInfo = contributionInfos[i];
			if (!husbandryValues.TryAdd(contributionInfo.outputId, contributionInfo.contribution))
			{
				husbandryValues[contributionInfo.outputId] += contributionInfo.contribution;
			}
		}
	}

	public bool Excrete()
	{
		if (!TryCostEnergy(proto.ExcreteCost))
		{
			return false;
		}
		needExcrete = false;
		return true;
	}

	public bool CheckMetabolismInterval()
	{
		return metabolismCDCounter.Tick();
	}

	private CountItem[] _BaseProduce()
	{
		ItemSpawnInfo spawnLut_Ref = proto.ProduceSpawnEntry.SpawnLut_Ref;
		if (spawnLut_Ref == null)
		{
			return Array.Empty<CountItem>();
		}
		return spawnLut_Ref.SpawnItems(proto.ProduceSpawnEntry.CountRange.MinCount, proto.ProduceSpawnEntry.CountRange.MaxCount);
	}

	private CountItem[] _SpecialProduce()
	{
		List<CountItem> list = new List<CountItem>();
		Queue<(string, int)> queue = new Queue<(string, int)>();
		foreach (KeyValuePair<string, int> husbandryValue in husbandryValues)
		{
			if (DolocConfig.Tables.TbHusbandry.TryGetThreshold(protoName, husbandryValue.Key, out var threshold) && husbandryValue.Value >= threshold)
			{
				CountItem[] array = DolocConfig.Tables.TbHusbandry.GenOutput(protoName, husbandryValue.Key);
				if (array == null || array.Length == 0)
				{
					Debug.LogError("Animal._SpecialProduce: 未知的产出物\"" + husbandryValue.Key + "\"");
					continue;
				}
				queue.Enqueue((husbandryValue.Key, threshold));
				list.AddRange(array);
			}
		}
		while (queue.Count > 0)
		{
			(string, int) tuple = queue.Dequeue();
			if (proto.ManualMetabolism)
			{
				husbandryValues[tuple.Item1] = 0;
			}
			else
			{
				husbandryValues[tuple.Item1] -= tuple.Item2;
			}
		}
		return list.ToArray();
	}

	private CountItem[] _SimulateSpecialProduce()
	{
		List<CountItem> list = new List<CountItem>();
		foreach (KeyValuePair<string, int> husbandryValue in husbandryValues)
		{
			if (DolocConfig.Tables.TbHusbandry.TryGetThreshold(protoName, husbandryValue.Key, out var threshold) && husbandryValue.Value >= threshold)
			{
				CountItem[] array = DolocConfig.Tables.TbHusbandry.GenOutput(protoName, husbandryValue.Key);
				if (array == null || array.Length == 0)
				{
					Debug.LogError("Animal._SpecialProduce: 未知的产出物\"" + husbandryValue.Key + "\"");
				}
				else
				{
					list.AddRange(array);
				}
			}
		}
		return list.ToArray();
	}

	public CountItem[] ProduceAsItems()
	{
		if (Metabolism < 100f || !IsMoodSatisfiedProduce)
		{
			return Array.Empty<CountItem>();
		}
		DolocAPI.BroadcastString(GameEventType.ANIMAL_PRODUCE, protoName);
		DolocAPI.AddTechExp(TechPointType.ANIMAL, proto.ProduceTechPoint);
		if (proto.ManualMetabolism)
		{
			Metabolism = 0f;
		}
		else
		{
			Metabolism -= 100f;
		}
		RefreshMetabolismFlag();
		List<CountItem> list = new List<CountItem>();
		list.AddRange(_BaseProduce());
		list.AddRange(_SpecialProduce());
		return list.ToArray();
	}

	public CountItem[] SimulateProduceAsItems()
	{
		List<CountItem> list = new List<CountItem>();
		list.AddRange(_BaseProduce());
		list.AddRange(_SimulateSpecialProduce());
		return list.ToArray();
	}

	public void Produce()
	{
		CountItem[] countItems = ProduceAsItems();
		((IDropItemHost)currentRoom).CreateDropItemAnimated(countItems, (Vector2)PositionDropItem, shouldSendMsg: true);
	}

	private void GenerateDropItem(Item item)
	{
		if (IsRender)
		{
			DolocAPI.GenerateDropItem(currentRoom, item, Renderer.position + new Vector3(0f, 1.5f));
			return;
		}
		Room room = currentRoom;
		Vector3 vector = PositionWSOfCell + new Vector3(0f, 1.5f);
		((IDropItemHost)room).CreateDropItemNoRender(item, (Vector2)vector, shouldSendMsg: true);
	}

	public bool Invoke()
	{
		if (!CanInvokeNow)
		{
			return false;
		}
		_invokeCounter.Reset();
		_isInvoked = true;
		return true;
	}

	public void Face(Vector2 targetPosition)
	{
		if (IsRender)
		{
			Renderer.FaceRight = Renderer.position.x < targetPosition.x;
		}
	}

	public void Sleep()
	{
		if (!isSleep)
		{
			isSleep = true;
			if (IsRender)
			{
				Renderer.PlayAnimation("sleep", force: true);
				Renderer.ShowSleepEffects = true;
			}
		}
	}

	public void WakeUp()
	{
		if (isSleep)
		{
			isSleep = false;
			if (IsRender)
			{
				Renderer.PlayAnimation("idle");
				Renderer.ShowSleepEffects = false;
			}
		}
	}

	public void Wink()
	{
		if (IsRender)
		{
			Renderer.PlayAnimation("wink");
		}
	}
}
