using System.Collections.Generic;
using System.Linq;

namespace DolocTown;

public class InteractableManager
{
	private readonly List<IInteractable> buffer = new List<IInteractable>();

	public IInteractable Current { get; private set; }

	public void MoveNext()
	{
		if (buffer.Count != 0)
		{
			IInteractable current = Current;
			current.OnDisTouch();
			Current = buffer.First();
			buffer.Remove(Current);
			buffer.Add(current);
			Current.OnTouch();
		}
	}

	public void Clear()
	{
		Current?.OnDisTouch();
		Current = null;
		buffer.Clear();
	}

	public bool TryInteract(bool continues)
	{
		if (Current == null)
		{
			return false;
		}
		if (continues && !Current.CanInteractContinues)
		{
			return false;
		}
		Current.OnInteract();
		return true;
	}

	public void Touch(IInteractable target)
	{
		if (target.OnlyTouch)
		{
			target.OnTouch();
		}
		else
		{
			if (target == Current)
			{
				return;
			}
			if (buffer.Contains(target))
			{
				buffer.Remove(target);
			}
			if (Current != null)
			{
				if (target.Priority >= Current.Priority)
				{
					buffer.Add(Current);
					Current.OnDisTouch();
					target.OnTouch();
					Current = target;
				}
				else
				{
					buffer.Add(target);
				}
			}
			else
			{
				Current = target;
				Current.OnTouch();
			}
		}
	}

	public void DisTouch(IInteractable target)
	{
		if (target.OnlyTouch)
		{
			target.OnDisTouch();
			return;
		}
		target.OnDisTouch();
		if (target == Current)
		{
			if (buffer.Count > 0)
			{
				buffer.Sort((IInteractable L, IInteractable R) => R.Priority - L.Priority);
				Current = buffer.First();
				Current.OnTouch();
				buffer.Remove(Current);
			}
			else
			{
				Current = null;
			}
		}
		else
		{
			buffer.Remove(target);
		}
	}
}
