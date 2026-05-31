using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RecipeSlot : CraftSlot<RecipeData>
{
	[SerializeField]
	private Text count;

	[SerializeField]
	private Text desc;

	[SerializeField]
	private Transform storageTrans;

	[SerializeField]
	private Text storage;

	public override void Render(RecipeData data)
	{
		base.Render(data);
		count.text = data.outputCount;
		desc.text = data.typeInfo;
		if (data.storageInfo.IsNullOrEmpty())
		{
			storageTrans.gameObject.SetActive(value: false);
		}
		else
		{
			storage.text = data.storageInfo;
			storageTrans.gameObject.SetActive(value: true);
		}
		base.grayed = data.soldOut;
		if (base.grayed)
		{
			craftIcon.SetActive(value: false);
		}
	}
}
