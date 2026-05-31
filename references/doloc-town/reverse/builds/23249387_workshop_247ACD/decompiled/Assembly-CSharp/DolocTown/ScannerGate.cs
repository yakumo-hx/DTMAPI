using UnityEngine;

namespace DolocTown;

public class ScannerGate : DolocObject
{
	[SerializeField]
	[Tooltip("如果该选项为true,则遇到可交互对象时在内置控制台输出可交互对象的信息")]
	private bool checkIfTouched;

	public ManagerGate manager { get; private set; }

	public IGate lastGate { get; private set; }

	public IGate lastTouchedGate { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		manager = new ManagerGate();
	}

	public void ClearBuffer()
	{
		manager.Clear();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		IGate component = other.GetComponent<IGate>();
		if (component != null)
		{
			lastGate = component;
			if (checkIfTouched)
			{
				Debug.Log("检测到传送门对象: " + other.name);
			}
			manager.Touch(component);
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		IGate component = other.GetComponent<IGate>();
		if (component != null)
		{
			lastGate = null;
			if (checkIfTouched)
			{
				Debug.Log("检测到传送门对象退出: " + other.name);
			}
			lastTouchedGate = component;
			manager.Distouch(component);
		}
	}
}
