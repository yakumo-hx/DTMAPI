using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown;
using RedSaw;
using RedSaw.CommandLineInterface.UnityImpl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DevHelper : MonoBehaviour
{
	private Vector2 funcListPos = new Vector2(10f, 60f);

	private Vector2 buttonSize = new Vector2(150f, 30f);

	private readonly Dictionary<string, Action> funcList = new Dictionary<string, Action>();

	private readonly Dictionary<string, Action> devMenuItems = new Dictionary<string, Action>();

	private bool shouldFuncListShowing;

	public GameConsole Console { get; private set; }

	public bool IsActive => base.gameObject.activeSelf;

	public string[] DevMenuItemTitles => devMenuItems.Keys.ToArray();

	public void Init()
	{
		Console = GetComponentInChildren<GameConsole>();
		Console.Init();
		Console.AddListenerOnFocus(delegate
		{
			DolocAPI.UserInput.DisableAllInput(includeGlobal: true);
		});
		Console.AddListenerOnFocusOut(delegate
		{
			DolocAPI.UserInput.ResumeCurrentInput();
			if (EventSystem.current != null)
			{
				EventSystem.current?.SetSelectedGameObject(null);
			}
		});
		InitFastButtons();
		InitDevMenuItems();
		base.gameObject.SetActive(value: true);
	}

	private void InitFastButtons()
	{
		MethodInfo[] array = ReflectionUtils.FindMethodsWithAttributeInExecutingAsm<FastButtonAttribute>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in array)
		{
			if (methodInfo.GetParameters().Length == 0)
			{
				string title = methodInfo.GetCustomAttribute<FastButtonAttribute>().title ?? methodInfo.Name;
				AppendFunction(title, delegate
				{
					methodInfo.Invoke(null, null);
				});
			}
		}
	}

	private void InitDevMenuItems()
	{
		MethodInfo[] array = ReflectionUtils.FindMethodsWithAttributeInExecutingAsm<DevMenuItemAttribute>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in array)
		{
			if (methodInfo.GetParameters().Length == 0)
			{
				string key = methodInfo.GetCustomAttribute<DevMenuItemAttribute>().title ?? methodInfo.Name;
				devMenuItems.Add(key, delegate
				{
					methodInfo.Invoke(null, null);
				});
			}
		}
	}

	public void DevMenuCallback(string menuItemName)
	{
		if (devMenuItems.TryGetValue(menuItemName, out var value))
		{
			value();
		}
	}

	public void Update()
	{
		if (Keyboard.current.f1Key.wasPressedThisFrame && DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
		{
			if (!Console.gameObject.activeSelf)
			{
				Console.gameObject.SetActive(value: true);
				return;
			}
			Console.QuitFocus();
			Console.SaveSettings();
			Console.gameObject.SetActive(value: false);
		}
	}

	public void HideConsole()
	{
		if (Console.gameObject.activeSelf)
		{
			Console.QuitFocus();
			Console.SaveSettings();
			Console.gameObject.SetActive(value: false);
		}
	}

	private void AppendFunction(string title, Action act)
	{
		funcList.TryAdd(title, act);
	}

	public void OnGUI()
	{
		if (!shouldFuncListShowing)
		{
			return;
		}
		Vector2 position = funcListPos;
		foreach (KeyValuePair<string, Action> func in funcList)
		{
			if (GUI.Button(new Rect(position, buttonSize), func.Key))
			{
				func.Value();
			}
			position.y += buttonSize.y + 10f;
		}
	}
}
