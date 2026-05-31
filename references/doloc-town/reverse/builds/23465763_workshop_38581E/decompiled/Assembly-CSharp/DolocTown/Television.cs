using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class Television : Equipment
{
	private static Texture _screenTexture;

	private static Texture _screenTextureWhite;

	[JsonProperty]
	private bool isTurnOn;

	private readonly EquipmentFuncTelevision func;

	private Material televisionMaterial;

	private static Texture ScreenTexture
	{
		get
		{
			if (_screenTexture != null)
			{
				return _screenTexture;
			}
			Color[] array = new Color[7]
			{
				Color.white,
				Color.yellow,
				Color.cyan,
				Color.green,
				Color.magenta,
				Color.red,
				Color.blue
			};
			Texture2D texture2D = new Texture2D(array.Length * 5, 24);
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					for (int k = 0; k < 24; k++)
					{
						texture2D.SetPixel(i * 5 + j, k, array[i]);
					}
				}
			}
			texture2D.Apply();
			_screenTexture = texture2D;
			return _screenTexture;
		}
	}

	private static Texture ScreenTextureWhite
	{
		get
		{
			if (_screenTextureWhite != null)
			{
				return _screenTextureWhite;
			}
			Texture2D texture2D = new Texture2D(42, 24);
			for (int i = 0; i < texture2D.width; i++)
			{
				for (int j = 0; j < texture2D.height; j++)
				{
					texture2D.SetPixel(i, j, Color.white);
				}
			}
			texture2D.Apply();
			_screenTextureWhite = texture2D;
			return _screenTextureWhite;
		}
	}

	private string Prompt
	{
		get
		{
			if (!isTurnOn)
			{
				return DolocConfig.StaticTexts.UiOperationOpen;
			}
			return DolocConfig.StaticTexts.UiOperationClose;
		}
	}

	public Television(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncTelevision)proto.Function;
		televisionMaterial = new Material(LocMaterials.GAME_MAT_2D_TELEVISION);
	}

	[JsonConstructor]
	protected Television(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isTurnOn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncTelevision)proto.Function;
			this.isTurnOn = isTurnOn;
			televisionMaterial = new Material(LocMaterials.GAME_MAT_2D_TELEVISION);
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		OnSwitchTo(isTurnOn);
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		if (base.Renderer != null)
		{
			base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, Prompt, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		this.PushSceneOperationTip();
		DolocAPI.agent._Interact(delegate
		{
			isTurnOn = !isTurnOn;
			OnSwitchTo(isTurnOn);
			ChangeTipPrompt(Prompt);
		});
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		if (!(televisionMaterial == null))
		{
			Object.Destroy(televisionMaterial);
			televisionMaterial = null;
		}
	}

	private void OnSwitchTo(bool value)
	{
		if (base.IsRender)
		{
			if (value)
			{
				base.Renderer.Sr.sharedMaterial = televisionMaterial;
				SetupMaterial(base.Renderer.Sr);
			}
			else
			{
				base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
			}
		}
	}

	private void SetupMaterial(SpriteRenderer spriteRenderer)
	{
		if (!(spriteRenderer == null))
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			spriteRenderer.GetPropertyBlock(materialPropertyBlock);
			Vector4 outerUV = DataUtility.GetOuterUV(spriteRenderer.sprite);
			Vector4 outerUV2 = DataUtility.GetOuterUV(func.ScreenMask.Asset);
			Vector4 value = new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y);
			materialPropertyBlock.SetTexture("_TelevisionMask", func.ScreenMask.Asset.texture);
			materialPropertyBlock.SetVector("_TelevisionTextureUV", value);
			materialPropertyBlock.SetTexture("_TelevisionTexture", ScreenTextureWhite);
			spriteRenderer.SetPropertyBlock(materialPropertyBlock);
		}
	}
}
