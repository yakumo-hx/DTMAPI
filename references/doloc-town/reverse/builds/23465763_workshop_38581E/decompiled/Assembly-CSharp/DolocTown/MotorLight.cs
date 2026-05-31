using RedSaw;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class MotorLight : DolocObject
{
	[SerializeField]
	private AgentLight agentLight;

	[SerializeField]
	private Vector2 agentLightIntensity;

	[SerializeField]
	private float duration;

	[SerializeField]
	private Light2D[] light;

	[SerializeField]
	private SpriteRenderer[] volumes;

	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color emissionColor = Color.white;

	private readonly RSTimer timer = new RSTimer(10f);

	private bool isTurnOn;

	private bool isRiding;

	protected override void __Init()
	{
		base.__Init();
		agentLight.Init();
		CheckRiding(useTrait: false);
		TurnOff();
	}

	public void OnFixedUpdate(float dt)
	{
		if (timer.Tick(dt))
		{
			CheckStatus();
		}
	}

	public void CheckStatus()
	{
		if (isTurnOn != DolocAPI.archiveHandle.ShouldLightUp)
		{
			isTurnOn = !isTurnOn;
			if (isTurnOn)
			{
				TurnOn();
			}
			else
			{
				TurnOff();
			}
		}
		CheckRiding();
	}

	public void CheckRiding(bool useTrait = true)
	{
		if (isRiding != DolocAPI.IsAgentRiding)
		{
			isRiding = DolocAPI.IsAgentRiding;
			agentLight.Show();
			agentLight.Fade((isRiding && isTurnOn) ? agentLightIntensity.y : agentLightIntensity.x, useTrait ? duration : 0f);
		}
	}

	public void TurnOn()
	{
		spriteRenderer.enabled = true;
		spriteRenderer.ToggleLightOn(spriteRenderer.sprite, emissionColor);
		Light2D[] array = light;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		SpriteRenderer[] array2 = volumes;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].gameObject.SetActive(value: true);
		}
	}

	public void TurnOff()
	{
		spriteRenderer.enabled = false;
		spriteRenderer.ToggleLightOff();
		Light2D[] array = light;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		SpriteRenderer[] array2 = volumes;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].gameObject.SetActive(value: false);
		}
	}
}
