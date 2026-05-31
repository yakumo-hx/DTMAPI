using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneExtraWeaponRendererInfrared : DroneExtraWeaponRenderer
{
	private GameObject entity;

	private LineRenderer lineRenderer;

	public override bool OnRender(Transform parent, DroneWeaponInfo proto)
	{
		base.OnRender(parent, proto);
		entity = DolocGameAssets.GAME_ENTITY_LINE.CreateEntity();
		if (entity == null)
		{
			return false;
		}
		lineRenderer = entity.GetComponent<LineRenderer>();
		lineRenderer.sharedMaterial = LocMaterials.GAME_MAT_2D_UNLIT;
		lineRenderer.startWidth = 0.1f;
		lineRenderer.endWidth = 0.1f;
		lineRenderer.startColor = Color.red;
		lineRenderer.endColor = Color.red;
		return true;
	}

	public override void SetShootInfos(Vector2 shootPosition, Vector2 shootDirection)
	{
		if (!(lineRenderer == null))
		{
			lineRenderer.positionCount = 2;
			lineRenderer.SetPosition(0, shootPosition);
			RaycastHit2D raycastHit2D = Physics2D.Raycast(shootPosition, shootDirection, base.gunProto.AttackDistance, (int)DolocAPI.gameConfig.groundMask | (int)DolocAPI.gameConfig.enemyMask);
			Vector2 vector = ((raycastHit2D.collider == null) ? (shootPosition + shootDirection * base.gunProto.AttackDistance) : raycastHit2D.point);
			lineRenderer.SetPosition(1, vector);
		}
	}

	public override void Dispose()
	{
		Object.Destroy(entity);
	}
}
