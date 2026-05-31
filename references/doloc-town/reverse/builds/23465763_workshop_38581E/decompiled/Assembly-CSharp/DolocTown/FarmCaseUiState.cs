using System;

namespace DolocTown;

public class FarmCaseUiState : ContainerBaseUiState
{
	private int currentSkinIndex;

	protected override bool ignoreSubmitWhenClick => true;

	protected override bool disableItemLocked => false;

	private Case farmCase => base.container as Case;

	public bool HandleStartUpArgs(IContainer container, Action onExit, string info)
	{
		base.container = container;
		base.onExit = onExit;
		base.panel.containerWidget.SetInfo(info);
		Case box = container as Case;
		if (box != null)
		{
			base.panel.containerWidget.SetRenamingCallback(delegate
			{
				DolocAPI.EnterUI((RenamingUiState state) => state.HandleStartUpArgs(base.staticTexts.UiTipRename, box.title, box.proto.Title, delegate(string name)
				{
					box.title = name;
					base.panel.containerWidget.SetTitle(name);
				}, DolocAPI.GlobalParameter.InputPlayerNameMaxLength));
			});
		}
		return container?.inventory != null;
	}

	protected override bool HandleInput(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
			return true;
		}
		if (isItemSlotSelected && base.HandleInput(deltaTime))
		{
			return true;
		}
		return false;
	}

	private void OnColorTagClick(int index)
	{
		if (farmCase != null)
		{
			farmCase.SetSkinIndex(index);
		}
	}

	protected override void Register()
	{
		base.Register();
		base.containerWidget.containerColorTagUI.SetClickCallbacks(OnColorTagClick);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.containerWidget.containerColorTagUI.RemoveCallbacks();
	}

	protected override void OnContainerItemRender(int index, Item item)
	{
		if (index < base.containerWidget.slots.Count)
		{
			bool grayed = false;
			base.containerWidget.slots[index].grayed = grayed;
		}
	}

	protected override void Show()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEventShow);
		base.Show();
		RefreshColorTag();
		base.panel.RebuildNavigation();
	}

	private void RefreshColorTag()
	{
		if (farmCase != null)
		{
			currentSkinIndex = farmCase.skinIndex;
			if (farmCase.func.SkinCount > 1)
			{
				base.containerWidget.containerColorTagUI.Render(farmCase.func.SkinColor);
				base.containerWidget.containerColorTagUI.Show();
				base.containerWidget.containerColorTagUI.FireClick(currentSkinIndex);
				base.panel.RebuildLayout();
			}
			else
			{
				base.containerWidget.containerColorTagUI.Hide();
			}
		}
	}
}
