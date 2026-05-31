using DolocTown.Config.Buff;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BuffIconRenderer : DolocNavigationButton
{
	[SerializeField]
	private Text textCount;

	private int _duration;

	private bool isHovered;

	public BuffInfo buffProto { get; set; }

	public int Duration
	{
		get
		{
			return _duration;
		}
		set
		{
			_duration = value;
			textCount.text = ((_duration > 0) ? (value * DolocAPI.GlobalParameter.TU2Min).ToString() : "");
		}
	}

	public void Render(BuffInfo proto, int currentDuration)
	{
		buffProto = proto;
		if (proto == null)
		{
			buttonCanvasGroup.alpha = 0f;
			return;
		}
		buttonCanvasGroup.alpha = 1f;
		base.iconSprite = proto.IconNormal.Asset;
		Duration = currentDuration;
	}

	protected override void OnPointerEnter()
	{
		isHovered = true;
		base.OnPointerEnter();
		TryRefreshHoverBox();
	}

	public void TryRefreshHoverBox()
	{
		if (isHovered && buffProto != null)
		{
			Color color = (buffProto.IsDebuff ? DolocUiColor.SLIENTCOLOR_RED : DolocUiColor.SLIENTCOLOR_GREEN);
			string str = (buffProto.IsDebuff ? base.staticTexts.UiTipDebuffDuration : base.staticTexts.UiTipBuffDuration);
			this.HoverText(new TextGroup(buffProto.Title.Colored(color), (buffProto.Duration <= 0) ? "" : DolocUtils.Format(str, DolocAPI.GetFormatTimeLengthByTU(_duration)), buffProto.Description), UIAlignmentType.LeftBottom, UIAlignmentType.LeftTop);
		}
	}

	protected override void OnPointerExit()
	{
		isHovered = false;
		base.OnPointerExit();
		this.HideHoverBox();
	}
}
