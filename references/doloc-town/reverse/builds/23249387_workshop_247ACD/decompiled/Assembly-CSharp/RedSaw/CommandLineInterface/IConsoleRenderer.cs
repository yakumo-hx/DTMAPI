using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

public interface IConsoleRenderer
{
	int OutputPanelCapacity { get; }

	bool IsVisible { get; set; }

	bool IsInputFieldFocus { get; }

	string InputText { get; set; }

	string InputTextToCursor { get; set; }

	bool IsAlternativeOptionsActive { get; set; }

	List<string> AlternativeOptions { set; }

	int AlternativeOptionsIndex { set; }

	void Focus();

	void ActivateInput();

	void QuitFocus();

	void BindOnTextChanged(Action<string> callback);

	void BindOnSubmit(Action<string> callback);

	void SetInputCursorPosition(int pos);

	void MoveScrollBarToEnd();

	void MoveCursorToEnd()
	{
		if (InputText != null)
		{
			SetInputCursorPosition(InputText.Length);
		}
	}

	void Output(string msg);

	void Output(string[] msg);

	void Output(string msg, string color = "#ffffff");

	void Output(string[] msg, string color = "#ffffff");

	void Clear();
}
