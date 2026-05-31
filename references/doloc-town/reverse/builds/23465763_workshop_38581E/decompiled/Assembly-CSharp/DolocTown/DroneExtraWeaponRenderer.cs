using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public abstract class DroneExtraWeaponRenderer
{
	public static DroneExtraWeaponRenderer FallbackRenderer { get; } = new DroneExtraWeaponRendererDefault();


	protected DroneWeaponInfo gunProto { get; private set; }

	protected Transform parent { get; private set; }

	public static DroneExtraWeaponRenderer CreateRenderer(string id)
	{
		Debug.Log("额外组件ID:" + id);
		if (id == "infrared")
		{
			Debug.Log("渲染红外线");
			return new DroneExtraWeaponRendererInfrared();
		}
		return new DroneExtraWeaponRendererDefault();
	}

	public virtual bool OnRender(Transform parent, DroneWeaponInfo gunProto)
	{
		this.parent = parent;
		this.gunProto = gunProto;
		return true;
	}

	public virtual void SetShootInfos(Vector2 shootPosition, Vector2 shootDirection)
	{
	}

	public virtual void Dispose()
	{
	}
}
