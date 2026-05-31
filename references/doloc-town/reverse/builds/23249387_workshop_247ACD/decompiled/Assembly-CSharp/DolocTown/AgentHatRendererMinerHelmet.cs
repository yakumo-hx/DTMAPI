using DolocTown.Config.Time;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class AgentHatRendererMinerHelmet : AgentHatRenderer
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
	private GameObject lightObject;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color emissionColor = Color.white;

	private bool _isReverted;

	private SpriteRenderer[] _lightRenderers;

	private Material _material;

	private DayPeriodType _lastDayPeriodType;

	public override int SortingOrder
	{
		set
		{
			base.SortingOrder = value;
			SpriteRenderer[] lightRenderers = _lightRenderers;
			for (int i = 0; i < lightRenderers.Length; i++)
			{
				lightRenderers[i].sortingOrder = value;
			}
		}
	}

	public override string SortingLayerName
	{
		set
		{
			base.SortingLayerName = value;
			SpriteRenderer[] lightRenderers = _lightRenderers;
			for (int i = 0; i < lightRenderers.Length; i++)
			{
				lightRenderers[i].sortingLayerName = value;
			}
		}
	}

	public Sprite MainSprite
	{
		get
		{
			if (!_isReverted)
			{
				return mainSprite;
			}
			return revertMainSprite;
		}
	}

	public Sprite MaskSprite
	{
		get
		{
			if (!_isReverted)
			{
				return maskSprite;
			}
			return revertMaskSprite;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (lightObject != null)
		{
			_lightRenderers = lightObject.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
		}
	}

	protected override bool CheckUseFrameAnim(string animName)
	{
		return false;
	}

	public override void OnRender()
	{
		RefreshRenderState();
	}

	private void RenderAsDaytime()
	{
		SetAnimator(MainSprite, LocMaterials.GAME_MAT_2D);
		if (lightObject != null)
		{
			lightObject.SetActive(value: false);
		}
	}

	private void RenderAsNighttime()
	{
		if (_material == null)
		{
			_material = new Material(emissionShader)
			{
				hideFlags = HideFlags.DontSave
			};
		}
		_material.SetTexture("_EmissionTex", MaskSprite.texture);
		Vector4 outerUV = DataUtility.GetOuterUV(MainSprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(MaskSprite);
		Vector4 value = new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y);
		_material.SetVector("_UvCoordinates", value);
		_material.SetColor("_EmissionColor", emissionColor);
		SetAnimator(MainSprite, _material);
		if (lightObject != null)
		{
			lightObject.SetActive(value: true);
			lightObject.transform.localScale = new Vector3(_isReverted ? 1 : (-1), 1f, 1f);
		}
	}

	protected override void HandleRevert(string animName)
	{
		base.HandleRevert(animName);
		if (ShouldRevert(animName))
		{
			if (!_isReverted)
			{
				_isReverted = true;
				RefreshRenderState(ignoreDayPeriod: true);
			}
		}
		else if (_isReverted)
		{
			_isReverted = false;
			RefreshRenderState(ignoreDayPeriod: true);
		}
	}

	private void RefreshRenderState(bool ignoreDayPeriod = false)
	{
		DayPeriodType currentDayPeriodType = DolocAPI.archiveHandle.CurrentDayPeriodType;
		if (_lastDayPeriodType != currentDayPeriodType || ignoreDayPeriod)
		{
			_lastDayPeriodType = currentDayPeriodType;
			if (currentDayPeriodType == DayPeriodType.Daytime)
			{
				RenderAsDaytime();
			}
			else
			{
				RenderAsNighttime();
			}
		}
	}

	public override void OnFaint()
	{
		RenderAsDaytime();
		_lastDayPeriodType = DayPeriodType.None;
	}

	public override void UpdatePerTU()
	{
		RefreshRenderState();
	}
}
