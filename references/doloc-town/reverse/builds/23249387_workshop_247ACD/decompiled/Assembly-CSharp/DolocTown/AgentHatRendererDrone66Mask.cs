using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class AgentHatRendererDrone66Mask : AgentHatRenderer
{
	[SerializeField]
	private Sprite mainSprite;

	[SerializeField]
	private Sprite maskSprite;

	[SerializeField]
	private Sprite revertMainSprite;

	[SerializeField]
	private Sprite revertMaskSprite;

	[SerializeField]
	private Shader emissionShader;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color emissionColor = Color.white;

	private Material _material;

	private bool _isReverted;

	public override void OnRender()
	{
		if (emissionShader == null || mainSprite == null || maskSprite == null)
		{
			Debug.LogError("AgentHatRendererDrone66Mask: 缺少必要的资源，无法渲染帽子");
			return;
		}
		_isReverted = false;
		SetupMaterial(mainSprite, maskSprite);
		SetAnimator(mainSprite, _material);
	}

	protected override bool CheckUseFrameAnim(string animName)
	{
		return false;
	}

	private void SetupMaterial(Sprite mainSprite, Sprite maskSprite)
	{
		if (_material == null)
		{
			_material = new Material(emissionShader)
			{
				hideFlags = HideFlags.DontSave
			};
		}
		_material.SetTexture("_EmissionTex", maskSprite.texture);
		Vector4 outerUV = DataUtility.GetOuterUV(mainSprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(maskSprite);
		Vector4 value = new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y);
		_material.SetVector("_UvCoordinates", value);
		_material.SetColor("_EmissionColor", emissionColor);
	}

	protected override void HandleRevert(string animName)
	{
		base.HandleRevert(animName);
		if (ShouldRevert(animName))
		{
			if (!_isReverted)
			{
				_isReverted = true;
				SetupMaterial(revertMainSprite, revertMaskSprite);
			}
		}
		else if (_isReverted)
		{
			_isReverted = false;
			SetupMaterial(mainSprite, maskSprite);
		}
	}

	public void OnDestroy()
	{
		if (_material != null)
		{
			Object.Destroy(_material);
		}
	}
}
