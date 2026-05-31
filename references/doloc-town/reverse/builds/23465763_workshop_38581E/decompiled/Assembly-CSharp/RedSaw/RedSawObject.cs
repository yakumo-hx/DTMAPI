using UnityEngine;

namespace RedSaw;

public abstract class RedSawObject : MonoBehaviour
{
	public Vector3 positionWS
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

	public Vector3 positionLocal
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

	public void setVisible(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public virtual void OnCreated()
	{
	}

	public virtual void OnRecycle()
	{
		setVisible(value: false);
	}

	public virtual void OnReuse()
	{
		setVisible(value: true);
	}
}
