using UnityEngine;

namespace DolocTown;

public class DolocObject : MonoBehaviour
{
	protected bool isInitialized;

	protected bool IsNotInitialized => !isInitialized;

	public bool isVisible => base.gameObject.activeSelf;

	public virtual Vector3 position
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			base.transform.position = value;
		}
	}

	public virtual Vector2 position2d
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			Transform obj = base.transform;
			obj.position = new Vector3(z: obj.position.z, x: value.x, y: value.y);
		}
	}

	public virtual Vector3 positionLocal
	{
		get
		{
			return base.transform.localPosition;
		}
		set
		{
			base.transform.localPosition = value;
		}
	}

	public virtual Vector2 positionLocal2d
	{
		get
		{
			return base.transform.localPosition;
		}
		set
		{
			Transform obj = base.transform;
			obj.localPosition = new Vector3(z: obj.localPosition.z, x: value.x, y: value.y);
		}
	}

	public virtual void SetVisible(bool value)
	{
		if (!(this == null) && base.gameObject.activeSelf != value)
		{
			base.gameObject.SetActive(value);
		}
	}

	protected virtual void __Init()
	{
	}

	protected virtual bool __check()
	{
		return true;
	}

	public bool Init()
	{
		if (isInitialized)
		{
			return true;
		}
		__Init();
		isInitialized = __check();
		return isInitialized;
	}
}
