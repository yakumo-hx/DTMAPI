using System;
using UnityEngine;

namespace RedSaw;

public interface IRecyclable
{
	void OnRecycle();

	void OnCreated();

	void OnReuse();

	void OnCreatedProtected()
	{
		try
		{
			OnCreated();
		}
		catch (Exception ex)
		{
			Debug.LogError("Created Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	void OnRecycleProtected()
	{
		try
		{
			OnRecycle();
		}
		catch (Exception ex)
		{
			Debug.LogError("Recycle Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}

	void OnReuseProtected()
	{
		try
		{
			OnReuse();
		}
		catch (Exception ex)
		{
			Debug.LogError("Reuse Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}
}
