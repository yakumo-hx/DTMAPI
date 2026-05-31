using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class LongPressTipRenderer : DolocUiObject
{
	[SerializeField]
	private Text info;

	[SerializeField]
	private Image progressBarImage;

	private Material material;

	private static readonly int Progress1 = Shader.PropertyToID("_Progress");

	public float Progress
	{
		set
		{
			material.SetFloat(Progress1, Mathf.Clamp01(value));
		}
	}

	public string Info
	{
		set
		{
			info.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		material = new Material(progressBarImage.material);
		progressBarImage.material = material;
	}

	private void OnDestroy()
	{
		Object.Destroy(material);
	}
}
