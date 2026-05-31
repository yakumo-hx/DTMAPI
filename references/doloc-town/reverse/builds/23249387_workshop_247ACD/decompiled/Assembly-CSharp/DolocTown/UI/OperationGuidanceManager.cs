using System.Collections.Generic;
using RedSaw;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DolocTown.UI;

public class OperationGuidanceManager : DolocUIPanel
{
	private struct GuidanceInfo
	{
		public OperationEventType eventType;

		public string textKey;

		public string actionName;

		public GuidanceInfo(OperationEventType eventType, string textKey, string actionName)
		{
			this.eventType = eventType;
			this.textKey = textKey;
			this.actionName = actionName;
		}
	}

	private ObjectPool<OperationGuidanceSlot> pool;

	[SerializeField]
	private Transform container;

	private Vector2 itemSize;

	private Dictionary<OperationEventType, OperationGuidanceSlot> guidanceSlots = new Dictionary<OperationEventType, OperationGuidanceSlot>();

	private Dictionary<OperationEventType, GuidanceInfo> guidanceBuffer = new Dictionary<OperationEventType, GuidanceInfo>();

	private List<OperationEventType> _sort = new List<OperationEventType>();

	private int maxShowNum => 3;

	private float spacing => 8f;

	private Vector2 startPoint => Vector2.zero;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_GUIDANCE);
		pool = new ObjectPool<OperationGuidanceSlot>(asset, container, usePreset: true);
		base.gameObject.SetActive(value: true);
		DolocAPI.UserInput.BindDeviceChangedCallback(OnDeviceChanged);
		DolocAPI.UserInput.BindActionChangedCallback(OnActionChanged);
	}

	private void OnDeviceChanged(DolocInputDeviceType deviceType)
	{
		foreach (OperationGuidanceSlot instance in pool.Instances)
		{
			instance.OnRefresh(deviceType);
		}
	}

	private void OnActionChanged(object sender, InputActionChange change)
	{
		foreach (OperationGuidanceSlot instance in pool.Instances)
		{
			instance.OnRefresh(DolocAPI.UserInput.DeviceType);
		}
	}

	private void OnDestroy()
	{
		DolocAPI.UserInput.RemoveDeviceChangedCallback(OnDeviceChanged);
		DolocAPI.UserInput.RemoveActionChangedCallback(OnActionChanged);
	}

	public void RegisterEvent(string gameEvent, string textKey, string actionName)
	{
		OperationEventType operationEventType = gameEvent.ConvertToEnumOrDefault<OperationEventType>();
		if (!_sort.Contains(operationEventType))
		{
			_sort.Add(operationEventType);
			GuidanceInfo guidanceInfo = new GuidanceInfo(operationEventType, textKey, actionName);
			if (guidanceSlots.Count < maxShowNum)
			{
				GetNewGuidance(guidanceInfo, isLast: false);
			}
			else
			{
				guidanceBuffer.Add(operationEventType, guidanceInfo);
			}
		}
	}

	public void SendMessage(OperationEventType eventType)
	{
		FinishOperation(eventType);
	}

	public void Clear()
	{
		pool.RecycleAll();
		_sort.Clear();
		guidanceSlots.Clear();
		guidanceBuffer.Clear();
	}

	private void FinishOperation(OperationEventType eventType)
	{
		guidanceBuffer.Remove(eventType);
		_sort.Remove(eventType);
		if (guidanceSlots.TryGetValue(eventType, out var slot))
		{
			guidanceSlots.Remove(eventType);
			slot.Hide(delegate
			{
				pool.Recycle(slot);
				UpdateAllSlotPos();
			});
			ReplenishSlot();
		}
	}

	private void GetNewGuidance(GuidanceInfo info, bool isLast)
	{
		OperationGuidanceSlot next = pool.Next;
		guidanceSlots.Add(info.eventType, next);
		itemSize = next.SizeDelta;
		next.Render(info.eventType, info.textKey, info.actionName);
		next.positionLocal = CalculatePosition(isLast ? guidanceSlots.Count : (guidanceSlots.Count - 1));
		next.ShowAtCurrent();
	}

	private Vector2 CalculatePosition(int index)
	{
		float y = (0f - (itemSize.y + spacing)) * (float)index;
		return new Vector2(0f, y) + startPoint;
	}

	private void UpdateAllSlotPos()
	{
		for (int i = 0; i < guidanceSlots.Count; i++)
		{
			Vector2 target = CalculatePosition(i);
			OperationGuidanceSlot operationGuidanceSlot = guidanceSlots[_sort[i]];
			if (!target.Equals(operationGuidanceSlot.positionLocal))
			{
				operationGuidanceSlot.MoveToTargetPos(target);
			}
		}
	}

	private void ReplenishSlot()
	{
		if (guidanceSlots.Count < maxShowNum && guidanceBuffer.Count > 0)
		{
			GuidanceInfo info = guidanceBuffer[_sort[guidanceSlots.Count]];
			GetNewGuidance(info, isLast: true);
			guidanceBuffer.Remove(info.eventType);
		}
	}
}
