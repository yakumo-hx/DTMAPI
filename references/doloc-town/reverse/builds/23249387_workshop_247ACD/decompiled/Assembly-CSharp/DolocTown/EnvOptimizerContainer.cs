using DolocTown.Config;

namespace DolocTown;

public class EnvOptimizerContainer : IContainer
{
	private readonly int index;

	public string title => DolocConfig.StaticTexts.UiEnvOptimizerSlotTitle.Format(index + 1);

	public LinearInventory inventory { get; private set; }

	public int totalCapacity => inventory.capacity;

	public int lineCapacity => inventory.capacity;

	private EnvOptimizerSystem envOptimizerSystem => DolocAPI.archiveHandle.farmData.envOptimizerSystem;

	public bool Valid { get; private set; }

	public EnvOptimizerContainer(int index)
	{
		this.index = index;
		if (envOptimizerSystem.CheckSlotIndexValid(index))
		{
			Valid = true;
			inventory = new LinearInventory(new Item[1] { envOptimizerSystem.slots[index].CurrentItem });
		}
	}

	public void BindEnvOptimizerSystem()
	{
		inventory.AddReceiver(OnItemChange);
	}

	public void UnbindEnvOptimizerSystem()
	{
		inventory.RemoveReceiver(OnItemChange);
	}

	private void OnItemChange(int i, Item item, bool _)
	{
		if (envOptimizerSystem.CheckSlotIndexValid(index))
		{
			envOptimizerSystem.slots[index].PlaceItem(item);
		}
	}

	public bool ContentFilter(Item content)
	{
		return envOptimizerSystem.IfItemIsComponent(content);
	}
}
