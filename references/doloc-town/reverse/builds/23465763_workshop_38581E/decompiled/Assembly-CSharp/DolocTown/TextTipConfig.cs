using System;
using DolocTown.Config;
using DolocTown.Config.Localization;
using DolocTown.Config.UI;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct TextTipConfig
{
	[SerializeField]
	private bool usePureText;

	[SerializeField]
	private string textId;

	[SerializeField]
	private string tipId;

	private Action recycleCallback;

	public string Id
	{
		get
		{
			if (!usePureText)
			{
				return tipId;
			}
			return textId;
		}
	}

	public string Content
	{
		get
		{
			object obj;
			if (!usePureText)
			{
				obj = proto?.TipArgs.Content.Text;
				if (obj == null)
				{
					return tipId;
				}
			}
			else
			{
				obj = l10nText;
			}
			return (string)obj;
		}
	}

	public bool isSceneTip => proto?.TipArgs is SceneTextBoxArgs;

	private string l10nText => DolocConfig.GetL10nText(textId ?? "");

	private TextTipInfo proto => DolocConfig.Tables.TbTextTip.GetOrDefault(tipId ?? "");

	public bool isEmpty => Content.IsNullOrEmpty();

	public override string ToString()
	{
		return Content;
	}

	public void ShowText(Vector2 pos)
	{
		if (proto == null)
		{
			return;
		}
		AlignmentText content = proto.TipArgs.Content;
		TipTextArgsBase tipArgs = proto.TipArgs;
		if (!(tipArgs is UISmallMessageBoxArgs uISmallMessageBoxArgs))
		{
			if (!(tipArgs is UIBigMessageBoxArgs uIBigMessageBoxArgs))
			{
				if (!(tipArgs is UITextConfirmBoxArgs uITextConfirmBoxArgs))
				{
					if (!(tipArgs is UINodeMessageBoxArgs uINodeMessageBoxArgs))
					{
						if (tipArgs is SceneTextBoxArgs sceneTextArgs)
						{
							DolocAPI.ShowSceneTextBox(pos, sceneTextArgs, null, DolocAPI.GlobalParameter.UiSceneTextTipDuration);
							recycleCallback = DolocAPI.HideSceneBox;
						}
					}
					else
					{
						DolocAPI.ShowMessageBoxNode(uINodeMessageBoxArgs.Icon.Asset, content.Text);
					}
				}
				else
				{
					DolocAPI.ShowConfirmBox(uITextConfirmBoxArgs.Title, uITextConfirmBoxArgs.Content, uITextConfirmBoxArgs.Signature, null);
				}
			}
			else
			{
				DolocAPI.ShowMessageBoxLarge(uIBigMessageBoxArgs.Icon.Asset, content.Text);
			}
		}
		else if (uISmallMessageBoxArgs.ErrorStyle)
		{
			DolocAPI.ShowMessageBoxSmallErr(content.Text);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmall(content.Text);
		}
	}

	public void ShowText(Action<string> showTextAction, Func<Vector2> positionGetter)
	{
		if (usePureText)
		{
			showTextAction?.Invoke(l10nText);
			return;
		}
		Vector2 pos = positionGetter?.Invoke() ?? Vector2.zero;
		ShowText(pos);
	}

	public void HideText()
	{
		recycleCallback?.Invoke();
	}
}
