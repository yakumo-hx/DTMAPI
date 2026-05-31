using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Player;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EquipmentBarWidget : DolocUIPanel, INavPanel
{
	[SerializeField]
	private Image body;

	[SerializeField]
	private Image hair;

	[SerializeField]
	private Image hat;

	[SerializeField]
	private new Text name;

	[SerializeField]
	private Text gold;

	[SerializeField]
	private Text time;

	[SerializeField]
	private DolocNavigationButton doubleJump;

	[SerializeField]
	private DolocNavigationButton sprint;

	[SerializeField]
	public AccessoriesBar accessoriesBar;

	[SerializeField]
	public DroneEquipmentBar droneEquipmentBar;

	public Selectable[] allSelectablesArray
	{
		get
		{
			List<Selectable> list = new List<Selectable>();
			list.AddRange(accessoriesBar.allSelectablesArray);
			list.AddRange(droneEquipmentBar.droneBar.allSelectablesArray);
			list.AddRange(new DolocButtonComponent[2] { doubleJump.button, sprint.button });
			return list.ToArray();
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		accessoriesBar.Init();
		droneEquipmentBar.Init();
		doubleJump.Init();
		sprint.Init();
		doubleJump.onPointerEnter.AddListener(delegate
		{
			doubleJump.HoverTextSmall(GetDoubleJumpText());
		});
		doubleJump.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		doubleJump.onSelect.AddListener(delegate
		{
			if (base.isRender)
			{
				doubleJump.GetItemBorder();
				doubleJump.HoverTextSmall(GetDoubleJumpText());
				DolocAPI.uiSystem.inventoryMouse.HoverTo(doubleJump.rectTransform);
			}
		});
		doubleJump.onDeselect.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		sprint.onPointerEnter.AddListener(delegate
		{
			sprint.HoverTextSmall(GetSprintText());
		});
		sprint.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		sprint.onSelect.AddListener(delegate
		{
			if (base.isRender)
			{
				sprint.GetItemBorder();
				sprint.HoverTextSmall(GetSprintText());
				DolocAPI.uiSystem.inventoryMouse.HoverTo(sprint.rectTransform);
			}
		});
		sprint.onDeselect.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
	}

	public void RenderPlayerInfo(Sprite bodySprite, Sprite hairSprite, string name, int gold, string time, bool doubleJump, bool sprint)
	{
		SetSprite(body, bodySprite);
		SetSprite(hair, hairSprite);
		this.name.text = name;
		this.gold.text = gold + DolocConfig.StaticTexts.ItemTipMoneyUnit;
		this.time.text = time;
		this.doubleJump.iconColor = (doubleJump ? Color.white : DolocColor.empty);
		this.sprint.iconColor = (sprint ? Color.white : DolocColor.empty);
	}

	public void RenderHat(HatInfo hatInfo)
	{
		hat.gameObject.SetActive(hatInfo != null);
		hair.gameObject.SetActive(hatInfo == null || !hatInfo.HideHair);
		if (hatInfo != null)
		{
			Sprite asset = hatInfo.IdleSprite.Asset;
			hat.sprite = asset;
			Rect rect = asset.rect;
			hat.rectTransform.sizeDelta = new Vector2(rect.width, rect.height) * 4f;
			Vector2 vector = new Vector2((asset.pivot.x / rect.width - 0.5f) * rect.width, 0f - asset.pivot.y) * 4f;
			hat.rectTransform.localPosition = vector;
		}
	}

	private string GetDoubleJumpText()
	{
		if (doubleJump.iconColor.a < 0.1f)
		{
			return base.staticTexts.EquipmentBarSkillLock;
		}
		return base.staticTexts.EquipmentBarDoubleJumpDesc;
	}

	private string GetSprintText()
	{
		if (sprint.iconColor.a < 0.1f)
		{
			return base.staticTexts.EquipmentBarSkillLock;
		}
		return base.staticTexts.EquipmentBarSprintDesc;
	}
}
