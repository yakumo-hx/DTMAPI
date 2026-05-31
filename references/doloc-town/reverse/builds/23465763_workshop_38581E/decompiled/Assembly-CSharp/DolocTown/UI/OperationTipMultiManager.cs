using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class OperationTipMultiManager : DolocUiEntity
{
	private class OperationList
	{
		private readonly NashObjectPoolEx<OperationTipMulti> _poolEx;

		private readonly List<OperationTip> _tips;

		private int index;

		public int Count => _tips.Count;

		public OperationTip Bottom
		{
			get
			{
				if (_tips.Count <= 2)
				{
					return null;
				}
				Debug.Log($"try fetch bottom index:{index} + 1");
				int num = Loop(index + 1);
				Debug.Log($"bottom index :{num}");
				if (num != -1)
				{
					return _tips[num];
				}
				return null;
			}
		}

		public OperationTip Center
		{
			get
			{
				if (_tips.Count == 0)
				{
					return null;
				}
				int num = Loop(index);
				if (num != -1)
				{
					return _tips[num];
				}
				return null;
			}
		}

		public OperationTip Top
		{
			get
			{
				if (_tips.Count <= 1)
				{
					return null;
				}
				int num = Loop(index - 1);
				if (num != -1)
				{
					return _tips[num];
				}
				return null;
			}
		}

		public OperationTip TopHided
		{
			get
			{
				if (_tips.Count <= 3)
				{
					return null;
				}
				int num = Loop(index - 2);
				if (num != -1)
				{
					return _tips[num];
				}
				return null;
			}
		}

		public OperationTip BottomHided
		{
			get
			{
				if (_tips.Count <= 3)
				{
					return null;
				}
				int num = Loop(index + 2);
				if (num != -1)
				{
					return _tips[num];
				}
				return null;
			}
		}

		private int Loop(int idx)
		{
			if (_tips.Count == 0)
			{
				return -1;
			}
			if (idx >= 0 && idx < _tips.Count)
			{
				return idx;
			}
			int num = idx % _tips.Count;
			if (num >= 0)
			{
				return num;
			}
			return num + _tips.Count;
		}

		public OperationList(NashObjectPoolEx<OperationTipMulti> pool)
		{
			_tips = new List<OperationTip>();
			_poolEx = pool;
		}

		private bool ContainsObject(object caller)
		{
			return _tips.Any((OperationTip tip) => tip.caller == caller);
		}

		public void Clear()
		{
			if (_tips.Count == 0)
			{
				return;
			}
			foreach (OperationTip tip in _tips)
			{
				_poolEx.Recycle(tip.Renderer);
			}
			_tips.Clear();
			index = -1;
		}

		public bool TryFindOperationTip(object caller, out OperationTip tip)
		{
			tip = _tips.FirstOrDefault((OperationTip t) => t.caller == caller);
			return tip != null;
		}

		public void Add(OperationTip tip)
		{
			if (ContainsObject(tip.caller))
			{
				Debug.LogWarning("OperationTipMultiManager : 重复添加提示符");
				int num = _tips.IndexOf(tip);
				if (index != num)
				{
					Debug.Log("切换顺序");
					index = num;
					Replay();
				}
				return;
			}
			if (_tips.Count == 0)
			{
				AddFirst(tip);
				return;
			}
			if (_tips.Count == 1)
			{
				AddSecond(tip);
				return;
			}
			if (_tips.Count == 2)
			{
				AddThird(tip);
				return;
			}
			tip.Renderer = _poolEx.Next;
			tip.Renderer.Callback = delegate
			{
				Remove(tip);
			};
			tip.Renderer.MoveFromRight();
			Center.Renderer.MoveUp();
			Top.Renderer.MoveUpToHide();
			int num2 = index + 1;
			_tips.Insert(num2, tip);
			index = num2;
		}

		public void AddFirst(OperationTip tip)
		{
			_tips.Add(tip);
			tip.Renderer = _poolEx.Next;
			tip.Renderer.Callback = delegate
			{
				Remove(tip);
			};
			tip.Renderer.MoveFromRight();
			index = 0;
		}

		public void AddSecond(OperationTip tip)
		{
			_tips.Add(tip);
			tip.Renderer = _poolEx.Next;
			tip.Renderer.Callback = delegate
			{
				Remove(tip);
			};
			tip.Renderer.MoveFromRight();
			Center.Renderer.MoveUp();
			index = 1;
		}

		public void AddThird(OperationTip tip)
		{
			_tips.Add(tip);
			tip.Renderer = _poolEx.Next;
			tip.Renderer.Callback = delegate
			{
				Remove(tip);
			};
			tip.Renderer.MoveFromRight();
			Center.Renderer.MoveUp();
			Top.Renderer.MoveFromBottom();
			tip.Renderer.MoveFromRight();
			index++;
		}

		public void Replay()
		{
			Center?.Renderer.MoveFromRight();
			if (_tips.Count >= 2)
			{
				Top?.Renderer.MoveUp();
			}
			if (_tips.Count >= 3)
			{
				Bottom?.Renderer.MoveFromBottom();
			}
		}

		private int NearestIndex(int index)
		{
			if (_tips.Count == 0)
			{
				return -1;
			}
			int num = Loop(index - 1);
			if (num != -1)
			{
				return num;
			}
			num = Loop(index + 1);
			if (num != -1)
			{
				return num;
			}
			return -1;
		}

		public void MoveUp()
		{
			if (_tips.Count <= 1)
			{
				return;
			}
			if (_tips.Count == 2)
			{
				index = Loop(index + 1);
				Replay();
				return;
			}
			OperationTip center = Center;
			OperationTip top = Top;
			OperationTip bottom = Bottom;
			if (_tips.Count == 3)
			{
				top.Renderer.MoveFromBottom();
				center.Renderer.MoveUp();
				bottom.Renderer.MoveCenter();
				index = Loop(index + 1);
			}
			else
			{
				OperationTip bottomHided = BottomHided;
				top.Renderer.MoveUpToHide();
				center.Renderer.MoveUp();
				bottom.Renderer.MoveCenter();
				index = Loop(index + 1);
				Debug.Log("MoveUp : " + index);
				bottomHided.Renderer.MoveFromBottom();
			}
		}

		public void MoveDown()
		{
			if (_tips.Count <= 1)
			{
				return;
			}
			if (_tips.Count == 2)
			{
				index = Loop(index - 1);
				Replay();
				return;
			}
			OperationTip center = Center;
			OperationTip top = Top;
			OperationTip bottom = Bottom;
			if (_tips.Count == 3)
			{
				top.Renderer.MoveCenter();
				center.Renderer.MoveDown();
				bottom.Renderer.MoveFromTop();
				index = Loop(index - 1);
			}
			else
			{
				OperationTip topHided = TopHided;
				top.Renderer.MoveCenter();
				center.Renderer.MoveDown();
				bottom.Renderer.MoveDownToHide();
				index = Loop(index - 1);
				Debug.Log("MoveDown : " + index);
				topHided.Renderer.MoveFromTop();
			}
		}

		public void Remove(OperationTip tip)
		{
			if (_tips.IndexOf(tip) < 0)
			{
				return;
			}
			if (!IsShowing(tip))
			{
				_Remove(tip);
				return;
			}
			OperationTip center = Center;
			if (tip == center)
			{
				_Remove(tip);
				index = Loop(index - 1);
				Replay();
				return;
			}
			OperationTip bottom = Bottom;
			if (tip == bottom)
			{
				_Remove(tip);
				index = _tips.IndexOf(Center);
				Replay();
				return;
			}
			OperationTip top = Top;
			if (tip == top)
			{
				_Remove(tip);
				index = _tips.IndexOf(Center);
				Replay();
			}
			else
			{
				_Remove(tip);
				index = _tips.IndexOf(center);
				Replay();
			}
		}

		private bool IsShowing(OperationTip tip)
		{
			OperationTip center = Center;
			OperationTip bottom = Bottom;
			OperationTip top = Top;
			if (center != tip && bottom != tip)
			{
				return top == tip;
			}
			return true;
		}

		private void _Remove(OperationTip tip)
		{
			_tips.Remove(tip);
			_poolEx.Recycle(tip.Renderer);
		}
	}

	private abstract class PositionHandle
	{
		public abstract Vector2 Position { get; }
	}

	private class StaticPosition : PositionHandle
	{
		private readonly Vector2 position;

		public override Vector2 Position => position;

		public StaticPosition(Vector2 position)
		{
			this.position = position;
		}
	}

	private class DynamicPosition : PositionHandle
	{
		private readonly Func<Vector2> positionGetter;

		public override Vector2 Position => positionGetter();

		public DynamicPosition(Func<Vector2> positionGetter)
		{
			this.positionGetter = positionGetter;
		}
	}

	private class OperationTip
	{
		public readonly object caller;

		public readonly PositionHandle positionHandle;

		private readonly Sprite buttonIcon;

		private readonly string prompt;

		private OperationTipMulti _renderer;

		public OperationTipMulti Renderer
		{
			get
			{
				return _renderer;
			}
			set
			{
				if (!(_renderer != null))
				{
					if (value != null)
					{
						value.KeyIcon = buttonIcon;
						value.Prompt = prompt;
					}
					_renderer = value;
				}
			}
		}

		public bool IsEmpty => caller == null;

		public OperationTip(object caller, PositionHandle handle, Sprite buttonIcon, string prompt)
		{
			this.caller = caller;
			positionHandle = handle;
			this.buttonIcon = buttonIcon;
			this.prompt = prompt;
			_renderer = null;
		}
	}

	private NashObjectPoolEx<OperationTipMulti> pool;

	private OperationList list;

	private PositionHandle positionHandle;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_OPERATION_TIP_IN_SCENE_MULTI);
		pool = new NashObjectPoolEx<OperationTipMulti>(asset, base.transform);
		list = new OperationList(pool);
	}

	private void Update()
	{
		if (list != null && list.Count != 0 && positionHandle != null)
		{
			base.position = DolocAPI.WorldToScreen(positionHandle.Position);
		}
	}

	public void Clear()
	{
		list.Clear();
	}

	public void ShowOperationTip(object caller, Vector2 ws, string prompt, string key)
	{
		Sprite keyIcon = GetKeyIcon(key);
		OperationTip tip = new OperationTip(caller, new StaticPosition(ws), keyIcon, prompt);
		list.Add(tip);
		UpdatePositionHandle();
	}

	public void ShowOperationTip(object caller, Func<Vector2> ws, string prompt, string key)
	{
		Sprite keyIcon = GetKeyIcon(key);
		OperationTip tip = new OperationTip(caller, new DynamicPosition(ws), keyIcon, prompt);
		list.Add(tip);
		UpdatePositionHandle();
	}

	private void UpdatePositionHandle()
	{
		if (list.Count != 0)
		{
			positionHandle = list.Center.positionHandle;
		}
	}

	public void HideOperationTip(object caller)
	{
		if (list.TryFindOperationTip(caller, out var tip))
		{
			list.Remove(tip);
			UpdatePositionHandle();
		}
	}

	public void PushOperationTip(object caller)
	{
		if (list.TryFindOperationTip(caller, out var tip))
		{
			if (list.Count == 1)
			{
				tip.Renderer.PushVertical();
			}
			else
			{
				tip.Renderer.PushHorizontal();
			}
		}
	}

	public void ScrollUp()
	{
		list.MoveUp();
		UpdatePositionHandle();
	}

	public void ScrollDown()
	{
		list.MoveDown();
		UpdatePositionHandle();
	}

	private Sprite GetKeyIcon(string key)
	{
		if (DolocAPI.GetActionKeyIconGroup(DolocAPI.UserInput.DeviceType, key, out var iconGroup))
		{
			return null;
		}
		return iconGroup.smallIcon;
	}

	public void MoveUp()
	{
		list.MoveUp();
	}

	public void MoveDown()
	{
		list.MoveDown();
	}
}
