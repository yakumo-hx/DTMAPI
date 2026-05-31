using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PlatformSlot : CraftSlot<PlatformData>
{
	[SerializeField]
	private Text desc;

	[SerializeField]
	private Text cost;

	public override void Render(PlatformData data)
	{
		base.Render(data);
		desc.text = data.description;
		cost.text = data.itemTiles;
	}
}
