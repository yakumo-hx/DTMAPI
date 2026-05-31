using DolocTown.Config;
using DolocTown.Config.Player;
using UnityEngine;

namespace DolocTown;

public class PlayerSplitRenderer : DolocObject
{
	private SpriteRenderer baseRenderer;

	[SerializeField]
	private SpriteRenderer hairRenderer;

	[SerializeField]
	private SpriteRenderer bodyRenderer;

	public SpriteRenderer HairRenderer => hairRenderer;

	public SpriteRenderer BodyRenderer => bodyRenderer;

	public Sprite HairSprite
	{
		get
		{
			if (!hairRenderer.enabled)
			{
				return null;
			}
			return hairRenderer.sprite;
		}
	}

	public Sprite BodySprite
	{
		get
		{
			if (!bodyRenderer.enabled)
			{
				return null;
			}
			return bodyRenderer.sprite;
		}
	}

	public Sprite BaseSprite
	{
		get
		{
			if (!baseRenderer.enabled)
			{
				return null;
			}
			return baseRenderer.sprite;
		}
	}

	public virtual int SortingOrder
	{
		set
		{
			hairRenderer.sortingOrder = value;
			bodyRenderer.sortingOrder = value;
		}
	}

	public virtual string SortingLayerName
	{
		set
		{
			hairRenderer.sortingLayerName = value;
			bodyRenderer.sortingLayerName = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		baseRenderer = base.transform.parent.GetComponent<SpriteRenderer>();
	}

	public void Play(string animName)
	{
		bool value = false;
		HatInfo hatInfo = (DolocAPI.archiveHandle?.farmData?.agentData?.agentEquipment?.hatItem as ItemHat)?.function?.HatId_Ref;
		PlayerAnimationFrameInfo orDefault = DolocConfig.Tables.TbPlayerAnimationFrame.GetOrDefault(animName);
		if (DolocAPI.modManager.HasPlayerOverride)
		{
			value = false;
		}
		else if (DolocAPI.modManager.HasPlayerBodyOverride || DolocAPI.modManager.HasPlayerHairOverride)
		{
			value = true;
		}
		else if (hatInfo != null)
		{
			value = hatInfo.HideHair;
		}
		bool flag = DolocAPI.modManager.HasPlayerOverride;
		if (!flag && hatInfo != null && orDefault != null)
		{
			bool flag2 = hatInfo.HasAnimationAssets(animName);
			flag = hatInfo.HideHair && (!orDefault.ForceShowHair || flag2);
		}
		UseSplitAnimation(value);
		hairRenderer.enabled = !flag;
	}

	private void UseSplitAnimation(bool value)
	{
		baseRenderer.enabled = !value;
		hairRenderer.enabled = value;
		bodyRenderer.enabled = value;
	}
}
