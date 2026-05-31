using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class DolocInfoPanelBox : DolocUiObject
{
	private Text[] texts;

	private TextGenerationSettings[] settings;

	private TextGenerator generator;

	private float titleHeight;

	private float contentWidth;

	private float space;

	protected override void __Init()
	{
		base.__Init();
		texts = GetComponentsInChildren<Text>();
		space = GetComponent<VerticalLayoutGroup>().spacing;
		titleHeight = texts[0].rectTransform.sizeDelta.y;
		contentWidth = texts[1].rectTransform.sizeDelta.x;
		generator = new TextGenerator();
		settings = new TextGenerationSettings[texts.Length];
		for (int i = 0; i < settings.Length; i++)
		{
			settings[i] = texts[i].GetGenerationSettings(texts[i].rectTransform.rect.size);
		}
	}

	public void renderOnly(string[] contents)
	{
		if (contents != null)
		{
			contents = paddingOrClip(contents, texts.Length);
			for (int i = 0; i < contents.Length; i++)
			{
				texts[i].text = contents[i];
			}
		}
	}

	public Vector2 render(string[] contents)
	{
		if (contents == null)
		{
			return Vector2.zero;
		}
		contents = paddingOrClip(contents, texts.Length);
		for (int i = 0; i < texts.Length; i++)
		{
			texts[i].text = contents[i];
		}
		return repos();
	}

	public Vector2 render(string[] contents, float width)
	{
		if (contents == null)
		{
			return Vector2.zero;
		}
		contents = paddingOrClip(contents, texts.Length);
		for (int i = 0; i < texts.Length; i++)
		{
			texts[i].text = contents[i];
		}
		return repos(width);
	}

	private string[] paddingOrClip(string[] src, int length)
	{
		if (src.Length >= length)
		{
			return src;
		}
		string[] array = new string[length];
		for (int i = 0; i < src.Length; i++)
		{
			array[i] = src[i];
		}
		for (int j = src.Length; j < length; j++)
		{
			array[j] = string.Empty;
		}
		return array;
	}

	public Vector2 repos()
	{
		float preferredWidth = generator.GetPreferredWidth(texts[0].text, settings[0]);
		float x = Mathf.Max(contentWidth, preferredWidth);
		texts[0].rectTransform.sizeDelta = new Vector2(x, titleHeight);
		float num = titleHeight;
		for (int i = 1; i < texts.Length; i++)
		{
			texts[i].rectTransform.sizeDelta = new Vector2(x, generator.GetPreferredHeight(texts[i].text, settings[i]));
			num += texts[i].rectTransform.sizeDelta.y;
		}
		base.size = new Vector2(x, num + space * (float)(texts.Length - 1));
		return base.size;
	}

	public Vector2 repos(float lockWidth)
	{
		float preferredWidth = generator.GetPreferredWidth(texts[0].text, settings[0]);
		float x = Mathf.Max(lockWidth, preferredWidth);
		texts[0].rectTransform.sizeDelta = new Vector2(x, titleHeight);
		float num = titleHeight;
		for (int i = 1; i < texts.Length; i++)
		{
			texts[i].rectTransform.sizeDelta = new Vector2(x, generator.GetPreferredHeight(texts[i].text, settings[i]));
			num += texts[i].rectTransform.sizeDelta.y;
		}
		base.size = new Vector2(x, num + space * (float)(texts.Length - 1));
		return base.size;
	}

	public void 武器信息渲染()
	{
		Init();
		render(new string[5] { "罐头罐头罐头罐", "罐头枪是虹视游戏工作室的主美扑克个人设计的一款武器，主打的是可爱和萌系的风格。", "伤害：10\n射速：0.7\n弹道速度：20\n射程：30\n弹夹容量：3", "特性：强力射击\n罐头枪在射击时有10%的概率发射一枚双倍伤害的子弹。", "废铁x3\n木料x1\n火药x1" }, 300f);
	}
}
