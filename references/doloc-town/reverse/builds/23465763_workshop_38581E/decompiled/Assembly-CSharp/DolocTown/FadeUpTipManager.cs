using Cysharp.Threading.Tasks;
using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FadeUpTipManager
{
	private readonly NashObjectPoolEx<FadeUpTip> pool;

	public FadeUpTipManager(Transform container)
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_FADEUPTIP);
		pool = new NashObjectPoolEx<FadeUpTip>(asset, container, 10);
		pool.OnCreate = delegate(FadeUpTip tip)
		{
			tip.recycle = pool.Recycle;
		};
	}

	public void Clear()
	{
		pool.RecycleAll();
	}

	public void RaiseUp(Vector2 pos, Sprite icon, Ease ease, float duration, float popDistance, bool filpX)
	{
		pool.Next.RaiseUp(pos, icon, ease, duration, popDistance, filpX);
	}

	public void MoveTo(Vector2 from, Vector2 to, Sprite icon, Ease ease, float duartion)
	{
		pool.Next.MoveTo(from, to, icon, ease, duartion);
	}

	public void RaiseDown(Vector2 pos, Sprite icon, Ease ease, float duration, float popDistance)
	{
		pool.Next.RaiseDown(pos, icon, ease, duration, popDistance);
	}

	public async UniTaskVoid RaiseSpriteArray(Vector2 pos, Sprite[] icons, Ease ease, float duration, float popDistance, bool flipX)
	{
		foreach (Sprite icon in icons)
		{
			pool.Next.RaiseUp(pos, icon, ease, duration, popDistance, flipX);
			await UniTask.Delay((int)(duration * 1000f));
		}
	}
}
