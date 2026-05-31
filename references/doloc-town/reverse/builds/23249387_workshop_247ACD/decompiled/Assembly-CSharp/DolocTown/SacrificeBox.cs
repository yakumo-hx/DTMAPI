using UnityEngine;

namespace DolocTown;

public class SacrificeBox : ContainerObject
{
	[SerializeField]
	protected string targetItemName;

	[SerializeField]
	protected string rewardItemName;

	[SerializeField]
	protected string dialogueNodeOnSubmit;

	private string latestItemName;

	public override int totalCapacity => 1;

	public override int lineCapacity => 1;

	public override bool ContentFilter(Item item)
	{
		if (DolocAPI.IsItemCanPutInToContainer(item))
		{
			return DolocAPI.IsItemDisposable(item);
		}
		return false;
	}

	protected override void OpenBox()
	{
		latestItemName = base.inventory.FirstItemName;
		if (!IsSubmitCompleted())
		{
			DolocAPI.EnterUI((SacrificeBoxUiState state) => state.HandleStartUpArgs(this, delegate
			{
				SaveInventory();
				if (base.inventory.FirstItemName != null && !(latestItemName == base.inventory.FirstItemName))
				{
					if (IsSubmitCompleted())
					{
						CommandDefines.BackupNumber(1f);
						CommandDefines.BackupString(rewardItemName);
					}
					else
					{
						CommandDefines.BackupNumber(0f);
					}
					DolocAPI.StartDialogueNode(dialogueNodeOnSubmit);
				}
			}));
		}
		else
		{
			CommandDefines.BackupNumber(2f);
			DolocAPI.StartDialogueNode(dialogueNodeOnSubmit);
		}
	}

	private bool IsSubmitCompleted()
	{
		return base.inventory?.FirstItemName == targetItemName;
	}
}
