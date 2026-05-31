using UnityEngine;

namespace DolocTown;

[GameEntityManager("/global/cloud_shadows", DolocGameAssets.GAME_ENTITY_CLOUD_SHADOW, CustomManagement = true)]
public class CloudShadow : ProjectiveShadow
{
	[SerializeField]
	private bool _shouldMove = true;

	[SerializeField]
	private float _moveSpeed = 1f;

	protected override void __Init()
	{
		base.__Init();
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		component.color = component.color.Alpha(0f);
		component.sharedMaterial = DolocAPI.GetAsset<Material>(DolocGameAssets.GAME_MAT_PERSPECTIVE_SHADOW);
		Vector3 vector = base.transform.position;
		vector.z = 15f;
		base.transform.position = vector;
	}

	public override void OnRecycle()
	{
		if (!(this == null) && !(base.gameObject == null))
		{
			base.OnRecycle();
		}
	}

	public override void OnReuse()
	{
		base.OnReuse();
		SpriteRenderer component = base.gameObject.GetComponent<SpriteRenderer>();
		if (component != null)
		{
			component.color = component.color.Alpha(0f);
		}
	}

	private void FixedUpdate()
	{
		if (_shouldMove)
		{
			base.transform.position += Vector3.right * (_moveSpeed * Time.fixedDeltaTime);
		}
	}
}
