using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/farm/scene_progress_bar", DolocGameAssets.GAME_ENTITY_SCENE_PROGRESSBAR)]
public class ProgressBarRenderer : GameEntity
{
	[SerializeField]
	private SpriteRenderer sp;

	private float _progress;

	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			sp.material.SetFloat("_Progress", value);
			_progress = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		sp.material = new Material(sp.sharedMaterial);
	}

	public void Render(Vector3 positionWS, float progress)
	{
		base.transform.position = positionWS;
		Progress = progress;
	}

	private void OnDestroy()
	{
		Object.Destroy(sp.material);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
	}
}
