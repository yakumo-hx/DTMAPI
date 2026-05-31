using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DroneItemSlot : DolocNavigationButton
{
	[SerializeField]
	protected Text txtTitle;

	[SerializeField]
	protected Text txtDesc;

	[SerializeField]
	protected Text txtType;

	[SerializeField]
	protected Text txtEmpty;

	[SerializeField]
	protected Image icon;

	[SerializeField]
	protected DynamicArrow arrow;

	[SerializeField]
	private CanvasGroup content;

	public UnityEvent onEmptyChange = new UnityEvent();

	private bool _isEmpty = true;

	public bool isEmpty
	{
		get
		{
			return _isEmpty;
		}
		set
		{
			if (_isEmpty != value)
			{
				OnEmptyChange(value);
				onEmptyChange.Invoke();
				_isEmpty = value;
			}
		}
	}

	public bool isHighLight { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		Clear();
		arrow.Init();
	}

	public void Render(Item item)
	{
		if (item == null)
		{
			Clear();
			return;
		}
		isEmpty = false;
		icon.sprite = item.uiSprite;
		txtTitle.text = item.title;
		txtDesc.text = item.description;
		txtType.text = item.subTypeText;
	}

	public void Clear()
	{
		OnEmptyChange(value: true);
		_isEmpty = true;
	}

	private void OnEmptyChange(bool value)
	{
		content.alpha = ((!value) ? 1 : 0);
		content.interactable = !value;
		txtEmpty.gameObject.SetActive(value);
	}

	public void SetNavigationOnUp(DroneItemSlot itemSlot)
	{
		base.button.SetNavigationOnUp(itemSlot.button);
	}

	public void SetNavigationOnDown(DroneItemSlot itemSlot)
	{
		base.button.SetNavigationOnDown(itemSlot.button);
	}

	public void HighLight(bool value)
	{
		isHighLight = value;
		arrow.SetVisible(value);
	}
}
