using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class SceneLightTelevision : SceneLight
{
	[SerializeField]
	private Sprite[] _sprites;

	private LightController _controller;

	public override void Init()
	{
		base.Init();
		_controller = LightController.FromGameObject(base.gameObject);
		_controller.SetIntensity(0f);
	}

	public override void OnUpdatePerTu()
	{
		if (base.IsTurnOn && !_sprites.IsNullOrEmpty())
		{
			Sprite televisionSprite = _sprites[Random.Range(0, _sprites.Length)];
			SetTelevisionSprite(televisionSprite);
		}
	}

	private void SetTelevisionSprite(Sprite sprite)
	{
		if (!(sprite == null))
		{
			SpriteRenderer component = GetComponent<SpriteRenderer>();
			if (!(component == null))
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				component.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetTexture("_TelevisionTexture", sprite.texture);
				component.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	protected override void TurnOff(bool isInitial)
	{
		if (isInitial)
		{
			_controller.SetIntensity(0f);
		}
		else
		{
			_controller.TurnOffLinear(1f);
		}
	}

	protected override void TurnOn(bool isInitial)
	{
		if (isInitial)
		{
			_controller.SetIntensity(1f);
		}
		else
		{
			_controller.TurnOnFlicker(1f);
		}
	}
}
