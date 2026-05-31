using System.Collections.Generic;
using DolocTown.Config.Room;

namespace DolocTown;

public class ManagerGate
{
	private readonly List<IGate> buffer = new List<IGate>();

	private IGate current;

	public IGate CurrentGate => current;

	public void Clear()
	{
		current?.OnDisTouch();
		current = null;
		buffer.Clear();
	}

	public bool TryInteract()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Interact)
		{
			return false;
		}
		current.OnInteract();
		return true;
	}

	public bool TryInteractOnMotor()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Interact)
		{
			return false;
		}
		TryInteractOnMotor(current);
		return true;
	}

	public bool TryEnter()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Enter)
		{
			return false;
		}
		current.OnInteract();
		return true;
	}

	public bool TryEnterOnMotor()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Enter)
		{
			return false;
		}
		TryInteractOnMotor(current);
		return true;
	}

	public bool TryQuit()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Exit)
		{
			return false;
		}
		current.OnInteract();
		return true;
	}

	public bool TryQuitOnMotor()
	{
		IGate gate = current;
		if (gate == null || gate.InteractKey != PortalInteractKey.Exit)
		{
			return false;
		}
		TryInteractOnMotor(current);
		return true;
	}

	private void TryInteractOnMotor(IGate gate)
	{
		if (gate != null)
		{
			if (!gate.AvailableToMotor)
			{
				DolocAPI.gameStateManager.agentController.GetOffMotor();
			}
			gate.OnInteract();
		}
	}

	public void Touch(IGate tmp)
	{
		if (!tmp.NeedInteract)
		{
			tmp.OnTouch();
			return;
		}
		if (current != null)
		{
			buffer.Add(tmp);
			return;
		}
		current = tmp;
		current.OnTouch();
	}

	public void Distouch(IGate tmp)
	{
		tmp.OnDisTouch();
		if (!tmp.NeedInteract)
		{
			return;
		}
		if (current == tmp)
		{
			current = null;
			if (buffer.Count > 0)
			{
				current = buffer[0];
				buffer.Remove(current);
				current.OnTouch();
			}
		}
		else
		{
			buffer.Remove(tmp);
		}
	}
}
