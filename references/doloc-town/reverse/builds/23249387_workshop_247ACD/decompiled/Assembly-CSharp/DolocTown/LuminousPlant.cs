using DolocTown.Config;
using DolocTown.Config.Equipment;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class LuminousPlant : SceneLight
{
	[SerializeField]
	private string lampConfig;

	private SpriteRenderer renderer;

	private SimpleLampController lampController;

	public override void Init()
	{
		base.Init();
		renderer = GetComponent<SpriteRenderer>();
		LampInfo orDefault = DolocConfig.Tables.TbLamp.GetOrDefault(lampConfig);
		lampController = new SimpleLampController(renderer, orDefault);
	}

	protected override void TurnOn(bool isInitial)
	{
		Debug.Log("<color=#ffff00>打开灯光LuminousPlant</color>");
		base.gameObject.SetActive(value: true);
		if (isInitial)
		{
			lampController.ToggleLight(value: true);
		}
		else
		{
			lampController.ToggleLight(value: true, 10f);
		}
	}

	protected override void TurnOff(bool isInitial)
	{
		lampController.ToggleLight(value: false);
		lampController.StopTween();
	}

	protected override void OnDispose()
	{
		lampController.Dispose();
	}
}
