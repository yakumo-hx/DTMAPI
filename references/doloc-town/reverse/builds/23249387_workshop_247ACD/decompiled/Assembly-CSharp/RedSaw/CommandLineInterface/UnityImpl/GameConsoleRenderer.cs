using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl;

public class GameConsoleRenderer : MonoBehaviour, IConsoleRenderer
{
	[SerializeField]
	private Text outputPanel;

	[SerializeField]
	private InputField inputField;

	[SerializeField]
	private GameConsoleAlternativeOptionsPanel optionsPanel;

	[SerializeField]
	private ScrollRect scrollRect;

	private int outputPanelCapacity = 400;

	private readonly Queue<string> messageQueue = new Queue<string>();

	private readonly List<string> buffer = new List<string>();

	public string CurrentMessage
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string item in buffer)
			{
				stringBuilder.Append(item + "\n");
			}
			return stringBuilder.ToString();
		}
	}

	public bool IsVisible
	{
		get
		{
			return base.gameObject.activeSelf;
		}
		set
		{
			base.gameObject.SetActive(value);
		}
	}

	public bool IsInputFieldFocus => inputField.isFocused;

	public int OutputPanelCapacity => outputPanelCapacity;

	public string InputText
	{
		get
		{
			return inputField.text;
		}
		set
		{
			inputField.text = value;
		}
	}

	public string InputTextToCursor
	{
		get
		{
			return inputField.text[..inputField.caretPosition];
		}
		set
		{
			inputField.text = value + inputField.text[inputField.caretPosition..];
		}
	}

	public bool IsAlternativeOptionsActive
	{
		get
		{
			return optionsPanel.gameObject.activeSelf;
		}
		set
		{
			optionsPanel.gameObject.SetActive(value);
		}
	}

	public List<string> AlternativeOptions
	{
		set
		{
			optionsPanel.SetOptions(value);
		}
	}

	public int AlternativeOptionsIndex
	{
		set
		{
			optionsPanel.SelectionIndex = value;
		}
	}

	private void UpdateContent()
	{
		string currentMessage = CurrentMessage;
		if (currentMessage.Length > 16250)
		{
			buffer.Clear();
			buffer.Add("文本过多..");
			return;
		}
		outputPanel.text = currentMessage;
		TextGenerator textGenerator = new TextGenerator();
		TextGenerationSettings generationSettings = outputPanel.GetGenerationSettings(outputPanel.rectTransform.sizeDelta);
		outputPanel.rectTransform.sizeDelta = new Vector2(outputPanel.rectTransform.sizeDelta.x, textGenerator.GetPreferredHeight(outputPanel.text, generationSettings));
		scrollRect.verticalNormalizedPosition = 0f;
	}

	private void Update()
	{
		if (messageQueue.Count > 0)
		{
			if (buffer.Count > outputPanelCapacity)
			{
				buffer.RemoveAt(0);
			}
			buffer.Add(messageQueue.Dequeue());
			UpdateContent();
		}
	}

	public void ActivateInput()
	{
		inputField.Select();
		inputField.ActivateInputField();
		StartCoroutine(DisableHighlight());
	}

	private IEnumerator DisableHighlight()
	{
		Color originalTextColor = inputField.selectionColor;
		originalTextColor.a = 0f;
		inputField.selectionColor = originalTextColor;
		yield return null;
		inputField.caretPosition = inputField.text.Length;
		originalTextColor.a = 1f;
		inputField.selectionColor = originalTextColor;
	}

	public void SetInputCursorPosition(int value)
	{
		inputField.caretPosition = Mathf.Clamp(value, 0, inputField.text.Length);
	}

	public void MoveScrollBarToEnd()
	{
		StartCoroutine(MoveToLast());
	}

	private IEnumerator MoveToLast()
	{
		yield return null;
		scrollRect.verticalNormalizedPosition = 0f;
	}

	public void BindOnSubmit(Action<string> callback)
	{
		inputField.onSubmit.AddListener(delegate(string input)
		{
			callback?.Invoke(input);
		});
	}

	public void BindOnTextChanged(Action<string> callback)
	{
		inputField.onValueChanged.AddListener(delegate(string input)
		{
			callback?.Invoke(input);
		});
	}

	public void Focus()
	{
		inputField.Select();
		EventSystem.current?.SetSelectedGameObject(inputField.gameObject);
		inputField.ActivateInputField();
	}

	public void QuitFocus()
	{
		EventSystem.current?.SetSelectedGameObject(null);
	}

	public void Clear()
	{
		buffer.Clear();
		outputPanel.text = string.Empty;
	}

	public void OnEnable()
	{
		UpdateContent();
		scrollRect.verticalNormalizedPosition = 0f;
	}

	public void Output(string msg)
	{
		if (!DolocAPI.IsGameInitialized)
		{
			return;
		}
		if (base.transform.parent.gameObject.activeSelf)
		{
			messageQueue.Enqueue(msg);
			return;
		}
		if (buffer.Count > outputPanelCapacity)
		{
			buffer.RemoveAt(0);
		}
		buffer.Add(msg);
	}

	public void Output(string[] msgs)
	{
		Output(string.Concat(msgs, '\n'));
	}

	public void Output(string msg, string color)
	{
		Output("<color=" + color + ">" + msg + "</color>");
	}

	public void Output(string[] msgs, string color)
	{
		string text = string.Empty;
		foreach (string text2 in msgs)
		{
			text = text + "<color=" + color + ">" + text2 + "</color>\n";
		}
		Output(text);
	}
}
