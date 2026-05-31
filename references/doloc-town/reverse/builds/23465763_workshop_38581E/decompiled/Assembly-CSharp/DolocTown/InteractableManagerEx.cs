using System.Collections.Generic;
using System.Linq;

namespace DolocTown;

public class InteractableManagerEx
{
	private readonly InteractableManager baseManager = new InteractableManager();

	private readonly Dictionary<int, InteractableManager> subManagers = new Dictionary<int, InteractableManager>();

	public void Touch(IInteractable interactable)
	{
		if (interactable.InteractableLayer == 0)
		{
			baseManager.Touch(interactable);
			return;
		}
		if (!subManagers.ContainsKey(interactable.InteractableLayer))
		{
			subManagers[interactable.InteractableLayer] = new InteractableManager();
		}
		subManagers[interactable.InteractableLayer].Touch(interactable);
	}

	public void DisTouch(IInteractable interactable)
	{
		InteractableManager value;
		if (interactable.InteractableLayer == 0)
		{
			baseManager.DisTouch(interactable);
		}
		else if (subManagers.TryGetValue(interactable.InteractableLayer, out value))
		{
			value.DisTouch(interactable);
		}
	}

	public void Clear()
	{
		baseManager.Clear();
		foreach (InteractableManager value in subManagers.Values)
		{
			value.Clear();
		}
	}

	public bool TryInteract(bool continues)
	{
		if (baseManager.TryInteract(continues))
		{
			return true;
		}
		return subManagers.Values.Any((InteractableManager manager) => manager.TryInteract(continues));
	}
}
