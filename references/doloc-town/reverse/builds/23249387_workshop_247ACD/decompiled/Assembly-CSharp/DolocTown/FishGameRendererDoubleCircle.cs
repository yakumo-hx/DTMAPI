using UnityEngine;

namespace DolocTown;

public class FishGameRendererDoubleCircle : DolocObject
{
	[SerializeField]
	private SpriteRenderer spriteRendererOfProgressBar;

	[SerializeField]
	private Transform fishEntity;

	[SerializeField]
	private LineRenderer fishLine;

	[SerializeField]
	private Color catchingColor;

	[SerializeField]
	private Color fleeingColor;

	private static readonly int ProgressId = Shader.PropertyToID("_Progress");

	public float Progress
	{
		set
		{
			if (spriteRendererOfProgressBar != null)
			{
				spriteRendererOfProgressBar.sharedMaterial.SetFloat(ProgressId, value);
			}
		}
	}

	public bool IsCatchingNow
	{
		set
		{
			spriteRendererOfProgressBar.color = (value ? catchingColor : fleeingColor);
		}
	}

	public Vector2 FishPosition
	{
		get
		{
			return fishEntity.localPosition;
		}
		set
		{
			fishEntity.localPosition = value;
		}
	}

	public Vector2 FishRodPosition
	{
		set
		{
			fishLine.positionCount = 2;
			fishLine.SetPosition(0, (Vector3)value + base.transform.position);
			fishLine.SetPosition(1, fishEntity.transform.position);
		}
	}
}
