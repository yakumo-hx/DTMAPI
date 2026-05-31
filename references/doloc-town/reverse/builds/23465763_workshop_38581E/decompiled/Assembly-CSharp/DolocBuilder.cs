using System;
using System.Collections.Generic;
using DolocTown;
using UnityEngine;

public class DolocBuilder : MonoBehaviour
{
	[SerializeField]
	public AgentLight builderLight;

	private readonly Dictionary<Type, BuilderTipBase> _lutCache = new Dictionary<Type, BuilderTipBase>();

	private BuilderTipBase _builderTipBase;

	public void Init()
	{
		builderLight.Init();
		base.gameObject.SetActive(value: false);
	}

	public void EnterHelpState(Vector2 roomPosition, Vector2Int size)
	{
		base.gameObject.SetActive(value: true);
		GridBackground gridBackground = DolocAPI.EntitySystem.Next<GridBackground>();
		gridBackground.GridPosition = BuilderUtils.GetRealGridPos(Vector2Int.zero, roomPosition);
		gridBackground.GridSize = size;
	}

	public void ExitHelpState()
	{
		_builderTipBase = null;
		base.gameObject.SetActive(value: false);
		DolocAPI.EntitySystem.Clear<GridBackground>();
	}

	public T GetBuilderTip<T>() where T : BuilderTipBase
	{
		Type typeFromHandle = typeof(T);
		if (!_lutCache.ContainsKey(typeFromHandle))
		{
			T value = Activator.CreateInstance<T>();
			_lutCache.Add(typeFromHandle, value);
		}
		_builderTipBase = _lutCache[typeFromHandle] as T;
		return _lutCache[typeFromHandle] as T;
	}

	public void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		_builderTipBase?.OnInputDeviceChanged(type);
	}

	private void LateUpdate()
	{
		_builderTipBase?.OnUpdate(Time.deltaTime);
	}
}
