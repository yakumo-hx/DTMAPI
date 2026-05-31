using DolocTown.Config.Equipment;
using UnityEngine;

namespace DolocTown;

public struct SitParams
{
	public Vector3 chairPosition;

	public Vector3 sitPosition;

	private ChairInfo chairProto;

	public bool fliped;

	public float lifeTimer => chairProto.LifeTimer;

	public int addHealth => chairProto.AddHealth;

	public int addEnergy => chairProto.AddEnergy;

	public bool save => chairProto.Save;

	public float timeScale => chairProto.TimeScale;

	public Sprite foreGroundSprite
	{
		get
		{
			if (!fliped)
			{
				return chairProto.Foreground.Asset;
			}
			return chairProto.ForegroundFlip.Asset;
		}
	}

	public bool faceRight
	{
		get
		{
			if (!fliped)
			{
				return chairProto.FaceRight;
			}
			return !chairProto.FaceRight;
		}
	}

	public SitParams(Vector3 chairPosition, Vector3 sitPosition, ChairInfo chairProto, bool fliped = false)
	{
		this.chairPosition = chairPosition;
		this.sitPosition = sitPosition;
		this.chairProto = chairProto;
		this.fliped = fliped;
	}
}
