namespace DolocTown;

public class EquipmentInteractableBridge : IInteractable
{
	private readonly Equipment equipment;

	public bool OnlyTouch => false;

	public bool CanInteractContinues => equipment.CanInteractContinues;

	public EquipmentInteractableBridge(Equipment equipment)
	{
		this.equipment = equipment;
	}

	public void OnTouch()
	{
		equipment.DecoratedTouch();
	}

	public void OnDisTouch()
	{
		equipment.DecoratedDisTouch();
	}

	public void OnInteract()
	{
		equipment.DecoratedInteract();
	}
}
