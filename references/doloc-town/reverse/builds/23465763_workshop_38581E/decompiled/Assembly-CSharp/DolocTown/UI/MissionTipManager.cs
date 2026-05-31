using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class MissionTipManager : DolocBasicTip
{
	[SerializeField]
	private Transform slotRoot;

	[SerializeField]
	private float spacing = 12f;

	[SerializeField]
	private int maxShowNum = 3;

	private Vector2 itemSize;

	private readonly List<string> _bufferOrder = new List<string>();

	private readonly Dictionary<string, MissionTip> _missionTipsBuffer = new Dictionary<string, MissionTip>();

	private readonly List<MissionTip> _destroyList = new List<MissionTip>();

	private ObjectPool<MissionTip> _pool;

	private int CurFirstMissionIndex
	{
		get
		{
			if (_bufferOrder.Count <= maxShowNum)
			{
				return 0;
			}
			return _bufferOrder.Count - maxShowNum - 1;
		}
	}

	private bool tipsUnfold
	{
		get
		{
			return DolocAPI.archiveHandle.farmData.agentData.unfoldMissionTips;
		}
		set
		{
			DolocAPI.archiveHandle.farmData.agentData.unfoldMissionTips = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		GameObject uI_ELEMENT_MISSIONTIPEX = LocPfbs.UI_ELEMENT_MISSIONTIPEX;
		itemSize = uI_ELEMENT_MISSIONTIPEX.GetComponent<RectTransform>().sizeDelta;
		_pool = new ObjectPool<MissionTip>(LocPfbs.UI_ELEMENT_MISSIONTIPEX, slotRoot);
	}

	protected override void OnClick()
	{
		if (_pool.ActiveCount != 0)
		{
			tipsUnfold = !tipsUnfold;
			slotRoot.gameObject.SetActive(tipsUnfold);
		}
	}

	protected override void OnHover()
	{
		string text = ((_pool.ActiveCount == 0) ? base.staticTexts.UiTipNoMission : (DolocAPI.archiveHandle.farmData.agentData.unfoldMissionTips ? base.staticTexts.UiTipShowMissionTip : base.staticTexts.UiTipHideMissionTip));
		this.HoverTextSmall(text, UIAlignmentType.RightBottom, UIAlignmentType.RightTop);
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		slotRoot.gameObject.SetActive(tipsUnfold && base.gameObject.activeSelf);
	}

	public void AddMissionTip(string id, bool useSound)
	{
		if (!_bufferOrder.Contains(id))
		{
			if (RenderNewTip(id, useSound))
			{
				_bufferOrder.Add(id);
			}
			if (_bufferOrder.Count > maxShowNum)
			{
				RemoveMissionTip(_bufferOrder[CurFirstMissionIndex]);
			}
		}
	}

	public void FinishMission(string id)
	{
		if (_bufferOrder.Contains(id))
		{
			_bufferOrder.Remove(id);
		}
		RemoveMissionTip(id);
		AddLatelyMission();
	}

	public void RefreshTip(string id)
	{
		if (DolocAPI.archiveHandle.QueryMission(id, out var mission) && _missionTipsBuffer.TryGetValue(id, out var value))
		{
			value.Render(mission.Title, mission.Tip, mission.BriefStatus);
		}
	}

	public void RefreshAllTips()
	{
		foreach (string key in _missionTipsBuffer.Keys)
		{
			RefreshTip(key);
		}
	}

	public void Clear()
	{
		_bufferOrder.Clear();
		_missionTipsBuffer.Clear();
		_destroyList.Clear();
		_pool.RecycleAll();
	}

	private Vector2 CalculatePosition(int index)
	{
		float y = (0f - (itemSize.y + spacing)) * (float)index;
		return new Vector2(0f, y);
	}

	private void UpdateAllMissionPos()
	{
		for (int i = 0; i < _missionTipsBuffer.Count; i++)
		{
			Vector2 target = CalculatePosition(i);
			int index = _bufferOrder.Count - _missionTipsBuffer.Count + i;
			MissionTip missionTip = _missionTipsBuffer[_bufferOrder[index]];
			if (!target.Equals(missionTip.positionLocal))
			{
				missionTip.MoveToTargetPos(target);
			}
		}
	}

	private bool RenderNewTip(string id, bool useSound)
	{
		if (DolocAPI.archiveHandle.QueryMission(id, out var mission))
		{
			MissionTip tip = _pool.Next;
			_missionTipsBuffer.Add(id, tip);
			DolocButtonComponent orCreateButton = tip.gameObject.GetOrCreateButton();
			orCreateButton.onClick.RemoveAllListeners();
			orCreateButton.onClick.AddListener(delegate
			{
				DolocAPI.EnterUI((MissionPanelUiState state) => state.HandleStartUpArgs(id));
			});
			orCreateButton.onPointerEnter.RemoveAllListeners();
			orCreateButton.onPointerEnter.AddListener(delegate
			{
				tip.HoverTextSmall(base.staticTexts.UiTipMissionShowDetails, UIAlignmentType.RightBottom, UIAlignmentType.RightTop);
			});
			orCreateButton.onPointerExit.RemoveAllListeners();
			orCreateButton.onPointerExit.AddListener(tip.HideHoverBox);
			string title = mission.Title;
			string tip2 = mission.Tip;
			tip.Render(title, tip2, mission.BriefStatus);
			tip.positionLocal = CalculatePosition(_missionTipsBuffer.Count + _destroyList.Count - 1);
			tip.ShowAtCurrent();
			if (useSound)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_MISSION_MESSAGE);
			}
			return true;
		}
		return false;
	}

	private void RemoveMissionTip(string id)
	{
		if (!_missionTipsBuffer.ContainsKey(id))
		{
			return;
		}
		MissionTip tip = _missionTipsBuffer[id];
		_destroyList.Add(tip);
		tip.Hide(delegate
		{
			_pool.Recycle(tip);
			_destroyList.Remove(tip);
			if (_destroyList.Count == 0)
			{
				UpdateAllMissionPos();
			}
		});
		_missionTipsBuffer.Remove(id);
	}

	private void AddLatelyMission()
	{
		if (_missionTipsBuffer.Count < maxShowNum && _bufferOrder.Count >= maxShowNum)
		{
			string id = _bufferOrder[_bufferOrder.Count - _missionTipsBuffer.Count - 1];
			RenderNewTip(id, useSound: true);
		}
	}
}
