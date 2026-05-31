using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DolocText : DolocUiRecyclableObject
{
	public Text textbox { get; private set; }

	public float preferredWidth => textbox.preferredWidth;

	public float preferredHeight => textbox.preferredHeight;

	public Font font
	{
		get
		{
			return textbox.font;
		}
		set
		{
			textbox.font = value;
		}
	}

	public int fontSize
	{
		get
		{
			return textbox.fontSize;
		}
		set
		{
			textbox.fontSize = value;
		}
	}

	public string text
	{
		get
		{
			return textbox.text;
		}
		set
		{
			textbox.text = value;
		}
	}

	public Color color
	{
		get
		{
			return textbox.color;
		}
		set
		{
			textbox.color = value;
		}
	}

	public float alpha
	{
		get
		{
			return textbox.color.a;
		}
		set
		{
			Color color = textbox.color;
			color.a = value;
			textbox.color = color;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		textbox = GetComponent<Text>();
	}
}
