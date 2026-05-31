using UnityEngine;

namespace DolocTown;

public class StorageBox : ContainerObject
{
	[SerializeField]
	private int _totalCapacity;

	[SerializeField]
	private int _lineCapacity;

	public override int totalCapacity => _totalCapacity;

	public override int lineCapacity => _lineCapacity;

	public override bool ContentFilter(Item item)
	{
		return DolocAPI.IsItemCanPutInToContainer(item);
	}

	protected override void OpenBox()
	{
		DolocAPI.EnterUI((FarmCaseUiState state) => state.HandleStartUpArgs(this, SaveInventory));
	}
}
