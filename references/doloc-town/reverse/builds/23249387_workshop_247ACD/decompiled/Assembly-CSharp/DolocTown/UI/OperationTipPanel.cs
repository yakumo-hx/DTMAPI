using Cysharp.Threading.Tasks;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class OperationTipPanel : DolocUiRecyclableObject
{
	[SerializeField]
	private float expandDelay = 0.08f;

	private ObjectPool<OperationTip> tipPool;

	private ContentSizeFitter fitter;

	public bool isActive { get; private set; }

	private OperationTip firstTip => tipPool.Instances[0];

	private OperationTip lastTip
	{
		get
		{
			if (tipPool.ActiveCount <= 0)
			{
				return null;
			}
			return tipPool?.Instances[^1];
		}
	}

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_OPERATIONTIP);
		tipPool = new ObjectPool<OperationTip>(asset, base.transform, usePreset: true);
		fitter = GetComponent<ContentSizeFitter>();
	}

	public void Render(string[] keys, string[] contents, bool hide = true)
	{
		int num = keys.Length;
		tipPool.CheckCount(num);
		for (int i = 0; i < num; i++)
		{
			OperationTip operationTip = tipPool.Instances[i];
			operationTip.transform.localScale = (hide ? Vector3.up : Vector3.one);
			operationTip.transform.SetSiblingIndex(i);
			operationTip.Render(keys[i], contents[i]);
		}
	}

	private async UniTask Show()
	{
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		isActive = true;
		await UniTask.NextFrame();
		SetVisible(value: true);
		for (int i = 0; i < tipPool.Instances.Count; i++)
		{
			if (i < tipPool.Instances.Count)
			{
				tipPool.Instances[i].Show();
				await UniTask.Delay((int)(expandDelay * 1000f));
			}
		}
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
	}

	public void Show(string[] keys, string[] contents)
	{
		Render(keys, contents);
		Show().Forget();
	}

	public void Show(Vector2 localPos, string[] keys, string[] contents)
	{
		Show(keys, contents);
		base.positionLocal = localPos;
	}

	public async UniTaskVoid Hide()
	{
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		isActive = false;
		int cnt = tipPool.Instances.Count;
		for (int i = 0; i < cnt; i++)
		{
			if (isActive)
			{
				return;
			}
			if (i < tipPool.Instances.Count)
			{
				tipPool.Instances[i].HideNotInvisible();
				await UniTask.Delay((int)(expandDelay * 1000f));
			}
		}
		await UniTask.WaitUntil(() => lastTip != null && !lastTip.isActive);
		if (!isActive)
		{
			SetVisible(value: false);
		}
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
	}
}
