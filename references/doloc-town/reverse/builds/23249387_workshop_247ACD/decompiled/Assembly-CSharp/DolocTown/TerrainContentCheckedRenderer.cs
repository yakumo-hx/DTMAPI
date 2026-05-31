using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class TerrainContentCheckedRenderer
{
	private readonly DolocBorderRenderer _border;

	private readonly SimpleTextTip _entityTip;

	public TerrainContentCheckedRenderer()
	{
		_border = DolocAPI.uiSystem.GetFromPoolInScene<DolocBorderRenderer>();
		_entityTip = DolocAPI.uiSystem.GetFromPoolInScene<SimpleTextTip>();
		_entityTip.Hide();
	}

	public void OnContentChoose(string content, Vector2 pos, Vector2 size)
	{
		_border.position = pos;
		_border.SizeDelta = DolocAPI.TileSizeToScreenSize(size);
		_entityTip.Description = content;
		float num = (float)DolocAPI.worldResolution.y / 1080f * 48f;
		_entityTip.position = pos + new Vector2(0f, 0f - num);
		if (string.IsNullOrEmpty(content))
		{
			_entityTip.Hide();
		}
		else
		{
			_entityTip.Show();
		}
	}

	public void SetVisible(bool visible)
	{
		_border.SetVisible(visible);
		if (visible)
		{
			_entityTip.Show();
		}
		else
		{
			_entityTip.Hide();
		}
	}

	public void Dispose()
	{
		DolocAPI.uiSystem.RecycleToPoolInScene(_border);
		DolocAPI.uiSystem.RecycleToPoolInScene(_entityTip);
	}
}
