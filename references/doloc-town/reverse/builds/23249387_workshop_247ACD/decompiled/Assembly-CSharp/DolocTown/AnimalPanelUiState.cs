using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class AnimalPanelUiState : PageUiStateBase<AnimalPanel, AnimalFullInfoData>
{
	private IAnimalHost host;

	private List<Animal> currentAnimals;

	private Animal currentAnimal;

	private int currentIndex;

	private int prevIndex;

	protected override int totalCapacity => currentAnimals.Count;

	public bool HandleStartUpArgs(Room room)
	{
		host = room;
		return host != null;
	}

	protected override AnimalFullInfoData[] DataGetter(int start, int end)
	{
		List<AnimalFullInfoData> list = new List<AnimalFullInfoData>();
		int count = currentAnimals.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			list.Add(new AnimalFullInfoData(currentAnimals[i]));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		currentIndex = 0;
		currentAnimal = null;
		currentAnimals = host.DM_animal.AllAnimals.ToList();
	}

	protected override void Register()
	{
		base.Register();
		base.panel.onDataSelect.AddListener(OnDataSelect);
		base.panel.onDataClick.AddListener(OnDataClick);
		base.panel.CallButton.onClick.AddListener(OnCallButtonClick);
		base.panel.RenameButton.onClick.AddListener(OnRenameButtonClick);
	}

	protected override void Unregister()
	{
		currentIndex = 0;
		base.panel.onDataSelect.RemoveListener(OnDataSelect);
		base.panel.onDataClick.RemoveListener(OnDataClick);
		base.panel.CallButton.onClick.RemoveListener(OnCallButtonClick);
		base.panel.RenameButton.onClick.RemoveListener(OnRenameButtonClick);
		base.Unregister();
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < currentAnimals.Count)
		{
			currentAnimal = currentAnimals[index];
			currentIndex = index;
			DolocAPI.UIRaiseRoll();
		}
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			base.panel.CallButton.FireClick();
		}
	}

	private void OnCallButtonClick(int _)
	{
		if (DolocAPI.archiveHandle.IsResonatorUnlocked())
		{
			currentAnimal.ToggleAnimalIndoorsStatus();
			base.panel.Select(currentIndex);
			base.panel.RefreshView();
		}
	}

	private void OnRenameButtonClick()
	{
		Animal animal = currentAnimal;
		if (animal != null && animal.IsInfoVisible)
		{
			currentAnimal.InvokeChangeNameInputBox();
		}
	}

	protected override void Show()
	{
		base.Show();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_ANIMAL_BELL);
		if (totalCapacity > 0)
		{
			currentAnimal = currentAnimals[0];
		}
		base.panel.SetTitle(((Room)host).SceneConfig.Title);
		base.panel.SetCapacityInfo(DolocUtils.Format(base.staticTexts.UiAnimalCapacity, $"{host.TotalAnimalSpace}/{host.MaxAnimalSpace}"));
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		base.OnUiUpdate(deltaTime);
		if (userInput.BaseSubmitItem)
		{
			base.panel.RenameButton.FireClick();
		}
	}

	public override void OnPause()
	{
		base.OnPause();
		prevIndex = currentIndex;
	}

	public override void OnResume()
	{
		base.OnResume();
		base.panel.Select(prevIndex);
	}
}
