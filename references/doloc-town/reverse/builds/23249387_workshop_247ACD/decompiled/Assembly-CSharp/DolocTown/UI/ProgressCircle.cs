using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ProgressCircle : DolocUiObject
{
	[SerializeField]
	private Image image;

	private float _progress;

	private static readonly int Progress1 = Shader.PropertyToID("_Progress");

	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			image.material.SetFloat(Progress1, value);
			_progress = value;
		}
	}

	public Color Color
	{
		get
		{
			return image.color;
		}
		set
		{
			image.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		image.material = new Material(image.material);
	}

	private void OnDestroy()
	{
		Object.Destroy(image.material);
	}
}
