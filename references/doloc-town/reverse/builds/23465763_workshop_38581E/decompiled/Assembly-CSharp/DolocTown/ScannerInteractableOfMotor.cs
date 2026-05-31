using UnityEngine;

namespace DolocTown;

public class ScannerInteractableOfMotor : DolocObject
{
	[SerializeField]
	[Tooltip("如果该选项为true,则遇到可交互对象时在内置控制台输出可交互对象的信息")]
	private bool checkIfTouched;

	public InteractableManagerEx Manager { get; set; }

	private void OnTriggerEnter2D(Collider2D other)
	{
		IInteractable component = other.GetComponent<IInteractable>();
		if (component is InteractableObject && !(component is IInteractableExclude) && !(component is IGate))
		{
			if (checkIfTouched)
			{
				Debug.Log("检测到可交互对象: " + other.name);
			}
			Manager.Touch(component);
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		IInteractable component = other.GetComponent<IInteractable>();
		if (component is InteractableObject && !(component is IInteractableExclude) && !(component is IGate))
		{
			if (checkIfTouched)
			{
				Debug.Log("检测到可交互对象退出: " + other.name);
			}
			Manager.DisTouch(component);
		}
	}

	public void ResetCollider()
	{
		Collider2D component = GetComponent<Collider2D>();
		component.enabled = false;
		component.enabled = true;
	}
}
