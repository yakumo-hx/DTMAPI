using UnityEngine;

namespace DolocTown;

public class CutsceneObject : InteractableObject
{
	[SerializeField]
	private bool simpleMode = true;

	[SerializeField]
	private string dialogueObjectId;

	[SerializeField]
	private string dialogueNode;

	private CutsceneObjectLogic logic;

	private int interactTimes;

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		if (!simpleMode && (!base.archiveData.TryGetLogicEntity<CutsceneObjectLogic>(this, out logic) || logic.dialogueObjectId != dialogueObjectId))
		{
			base.archiveData.UnregisterLogicEntity(this, logic);
			logic = new CutsceneObjectLogic(base.roomId, base.guid, dialogueObjectId);
			base.archiveData.RegisterLogicEntity(this, logic);
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (simpleMode)
		{
			DolocAPI.StartDialogueNode(dialogueNode);
			return;
		}
		CutsceneObjectLogic cutsceneObjectLogic = logic;
		if (cutsceneObjectLogic != null && cutsceneObjectLogic.IsValid)
		{
			logic.TryAddInteractCount();
			if (logic.TryGetCurrentDialogueNode(out var node))
			{
				DolocAPI.StartDialogueNode(node);
			}
		}
	}
}
