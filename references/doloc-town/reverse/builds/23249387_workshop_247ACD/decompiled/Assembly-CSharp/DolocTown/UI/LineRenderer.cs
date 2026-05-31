using RedSaw;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace DolocTown.UI;

public class LineRenderer : UILineRenderer, IRecyclable
{
	public float alpha
	{
		get
		{
			return color.a;
		}
		set
		{
			Color color = this.color;
			color.a = value;
			this.color = color;
		}
	}

	public void OnCreated()
	{
	}

	public void OnRecycle()
	{
		base.gameObject.SetActive(value: false);
	}

	public void OnReuse()
	{
		base.gameObject.SetActive(value: true);
	}
}
