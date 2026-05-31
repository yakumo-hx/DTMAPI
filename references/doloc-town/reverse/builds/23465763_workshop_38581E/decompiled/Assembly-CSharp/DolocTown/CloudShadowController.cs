using System.Collections.Generic;
using DolocTown.Config.Weather;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class CloudShadowController : DolocObject
{
	[SerializeField]
	private Vector2 _cloudAreaStart = new Vector2(-50f, -50f);

	[SerializeField]
	private Vector2 _cloudAreaSize = new Vector2(100f, 100f);

	[SerializeField]
	private Vector2 _spawnRange = new Vector2(10f, 20f);

	[SerializeField]
	[Min(1f)]
	private int _maxCloudCount = 3;

	[SerializeField]
	private Color _defaultShadowColor = new Color(0.5f, 0.5f, 0.5f, 1f);

	private Color _currentShadowColor;

	[SerializeField]
	private Vector2Int _spawnIntervalRange = new Vector2Int(30, 60);

	private bool _shouldUpdate = true;

	private bool _shouldUpdateCloudColors = true;

	private readonly List<CloudShadow> _cloudShadows = new List<CloudShadow>();

	private readonly RSTimer _timer = new RSTimer(5f);

	private readonly RSTimer _timerBorderCheck = new RSTimer(5f);

	private readonly RSTimer _statusCheck = new RSTimer(20f);

	private readonly Counter _spawnCounter = new Counter();

	private readonly Queue<CloudShadow> _recycleQueue = new Queue<CloudShadow>();

	private Room _currentRoom;

	private float LeftBorder => _cloudAreaStart.x;

	private float RightBorder => _cloudAreaStart.x + _cloudAreaSize.x;

	private float BottomBorder => _cloudAreaStart.y;

	private float TopBorder => _cloudAreaStart.y + _cloudAreaSize.y;

	private Vector2 RandomPosition
	{
		get
		{
			float x = Random.Range(LeftBorder, RightBorder);
			float y = Random.Range(BottomBorder, TopBorder);
			return new Vector2(x, y);
		}
	}

	private Vector2 RandomSpawnPosition
	{
		get
		{
			float x = Random.Range(_spawnRange.x, _spawnRange.y);
			float y = Random.Range(BottomBorder, TopBorder);
			return new Vector2(x, y);
		}
	}

	private int RandomInterval => Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);

	protected override void __Init()
	{
		base.__Init();
		_spawnCounter.SetInterval(1);
		_currentShadowColor = _defaultShadowColor;
		_shouldUpdate = false;
	}

	public void OnEnterRoom(Room targetRoom)
	{
		if (targetRoom is DungeonRoom)
		{
			if (!(_currentRoom is DungeonRoom))
			{
				Disable();
			}
			_currentRoom = targetRoom;
			return;
		}
		if (targetRoom.IsInHouse)
		{
			_shouldUpdateCloudColors = false;
			HideCloud(isInitial: true);
		}
		else if (_currentRoom is DungeonRoom)
		{
			Enable(isInitial: true);
			SpawnCloud();
			_spawnCounter.SetInterval(RandomInterval);
		}
		else
		{
			_shouldUpdateCloudColors = true;
			UpdateCloudColors(isInitial: true);
			if (!_shouldUpdate)
			{
				Enable(isInitial: true);
			}
		}
		_currentRoom = targetRoom;
	}

	public void UpdateCloudColorsIfNeed()
	{
		if (_shouldUpdateCloudColors)
		{
			UpdateCloudColors();
		}
	}

	public void SetShadowColor(Color c, bool transit, float duration = 3f)
	{
		_currentShadowColor = c;
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			cloudShadow.SetShadowColor(c, transit, duration);
		}
	}

	private void UpdateCloudColors(bool isInitial = false)
	{
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			cloudShadow.SetTimeInfo(dayProcess, currentWeatherType, isInitial);
		}
	}

	public void Disable()
	{
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			DolocAPI.EntitySystem.Recycle(cloudShadow);
		}
		_cloudShadows.Clear();
		_shouldUpdate = false;
	}

	public void Enable(bool isInitial = false)
	{
		Debug.Log("启用云朵阴影控制器");
		if (!_shouldUpdate)
		{
			UpdateCloudColors(isInitial);
			_shouldUpdate = true;
		}
	}

	public void HideCloud(bool isInitial)
	{
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			cloudShadow.SetShadowIntensity(0f, isInitial);
		}
	}

	public void ShowCloud(float process, WeatherType weatherType, bool isInitial = true)
	{
		if (_cloudShadows.Count == 0)
		{
			return;
		}
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			cloudShadow.SetTimeInfo(process, weatherType, isInitial);
		}
	}

	public void RefreshPosition()
	{
		if (_cloudShadows.Count == 0)
		{
			return;
		}
		foreach (CloudShadow cloudShadow in _cloudShadows)
		{
			cloudShadow.position2d = RandomPosition;
		}
	}

	public void Update()
	{
		if (!_shouldUpdate || _cloudShadows.Count == 0)
		{
			return;
		}
		if (_timerBorderCheck.Tick(Time.deltaTime))
		{
			foreach (CloudShadow cloudShadow2 in _cloudShadows)
			{
				if (cloudShadow2.transform.position.x > RightBorder)
				{
					_recycleQueue.Enqueue(cloudShadow2);
				}
			}
			while (_recycleQueue.Count > 0)
			{
				CloudShadow cloudShadow = _recycleQueue.Dequeue();
				_cloudShadows.Remove(cloudShadow);
				DolocAPI.EntitySystem.Recycle(cloudShadow);
			}
		}
		if (_statusCheck.Tick(Time.deltaTime))
		{
			UpdateCloudColorsIfNeed();
		}
	}

	public void FixedUpdate()
	{
		if (_shouldUpdate && _timer.Tick(Time.fixedDeltaTime) && _spawnCounter.Tick())
		{
			_spawnCounter.SetInterval(RandomInterval);
			SpawnCloud();
		}
	}

	public void SpawnCloud()
	{
		if (_cloudShadows.Count >= _maxCloudCount)
		{
			return;
		}
		CloudShadow cloudShadow = DolocAPI.EntitySystem.Next<CloudShadow>();
		if (cloudShadow == null || cloudShadow.gameObject == null)
		{
			Debug.LogWarning("生成云朵阴影失败");
			return;
		}
		cloudShadow.SetShadowColor(_currentShadowColor);
		_cloudShadows.Add(cloudShadow);
		cloudShadow.position2d = RandomSpawnPosition;
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		if (_shouldUpdateCloudColors)
		{
			cloudShadow.SetTimeInfo(dayProcess, currentWeatherType);
		}
		else
		{
			cloudShadow.SetShadowIntensity(0f, isInitial: true);
		}
	}
}
