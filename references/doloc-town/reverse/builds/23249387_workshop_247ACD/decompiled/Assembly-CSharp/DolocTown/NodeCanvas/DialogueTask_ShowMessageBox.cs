using DolocTown.Config;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("弹出消息框", 0)]
[Description("弹出各类不同的消息框,可以选择不同的类别")]
public class DialogueTask_ShowMessageBox : DialogueTask
{
	public enum MessageBoxType
	{
		Common,
		Small,
		Mission,
		Large,
		Attention
	}

	public enum CommonMessageIconType : byte
	{
		Info,
		Error
	}

	public enum MissionMessageIconType : byte
	{
		Error,
		Complete
	}

	[SerializeField]
	[ExposeField]
	private string textKey = string.Empty;

	[SerializeField]
	[ExposeField]
	private MessageBoxType messageBoxType = MessageBoxType.Small;

	[SerializeField]
	[ExposeField]
	private bool customMessageIcon;

	[SerializeField]
	[ExposeField]
	private byte defaultType;

	[SerializeField]
	[ExposeField]
	private Sprite messageIcon;

	[SerializeField]
	[ExposeField]
	private Color color = Color.white;

	[SerializeField]
	[ExposeField]
	private float holdTime = 2f;

	public override string taskTitle => $"{messageBoxType}:\"{textKey.CapLength(20)}\"";

	public override void DoAction(Graph graph)
	{
		string l10nText = DolocConfig.GetL10nText(textKey);
		switch (messageBoxType)
		{
		case MessageBoxType.Small:
			DolocAPI.ShowMessageBoxSmall(l10nText, color, holdTime);
			break;
		case MessageBoxType.Common:
		{
			string msg3 = "<color=" + color.ToHex() + ">" + l10nText + "</color>";
			if (customMessageIcon)
			{
				DolocAPI.ShowMessageBox(messageIcon, msg3, holdTime);
				break;
			}
			switch ((CommonMessageIconType)defaultType)
			{
			case CommonMessageIconType.Info:
				DolocAPI.ShowMessageBoxInfo(msg3, holdTime);
				break;
			case CommonMessageIconType.Error:
				DolocAPI.ShowMessageBoxErr(msg3, holdTime);
				break;
			}
			break;
		}
		case MessageBoxType.Mission:
		{
			string msg2 = "<color=" + color.ToHex() + ">" + l10nText + "</color>";
			if (customMessageIcon)
			{
				DolocAPI.ShowMessageBoxNode(messageIcon, msg2);
				break;
			}
			switch ((MissionMessageIconType)defaultType)
			{
			case MissionMessageIconType.Error:
				DolocAPI.ShowMessageBoxNodeError(msg2);
				break;
			case MissionMessageIconType.Complete:
				DolocAPI.ShowMessageBoxNodeComplete(msg2);
				break;
			}
			break;
		}
		case MessageBoxType.Large:
		{
			string msg = "<color=" + color.ToHex() + ">" + l10nText + "</color>";
			DolocAPI.ShowMessageBoxLarge(messageIcon, msg, holdTime);
			break;
		}
		case MessageBoxType.Attention:
			DolocAPI.ShowMessageBoxAttention(l10nText, holdTime);
			break;
		}
	}
}
