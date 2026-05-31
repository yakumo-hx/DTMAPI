using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class MotorWorldContentHandle : DolocObject
{
	protected override void __Init()
	{
		base.__Init();
		Collider2D component = GetComponent<Collider2D>();
		if (component != null)
		{
			component.isTrigger = true;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (DolocAPI.IsAgentRiding)
		{
			WorldContentRenderer component = other.GetComponent<WorldContentRenderer>();
			if (component != null && component.OnlyTouch)
			{
				component.OnTouch();
			}
		}
	}
}
