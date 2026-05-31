using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BuildingSlot : CraftSlot<BuildingData>
{
	[SerializeField]
	protected Text coverDesc;

	[SerializeField]
	protected Text innerDesc;

	public override void Render(BuildingData data)
	{
		base.Render(data);
		coverDesc.text = data.coverDescription;
		innerDesc.text = data.innerDescription;
	}
}
