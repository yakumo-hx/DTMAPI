using System;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class DialogueCommandRunner
{
	public DialogueCommandRunner(Action<string, Delegate> register)
	{
		foreach (var allCommandFunction in DolocAPI.GetAllCommandFunctions())
		{
			if ((object)allCommandFunction.Item2 != null)
			{
				register(allCommandFunction.Item1, allCommandFunction.Item2);
			}
		}
	}

	public CommandExecuteResult Execute(string args, out UniTask task)
	{
		task = default(UniTask);
		if (!DolocAPI.IsGameInitialized)
		{
			Debug.LogWarning("游戏未初始化结束！");
			return CommandExecuteResult.Failed;
		}
		string text = Regex.Replace(args, "^wait ", string.Empty);
		float result;
		string input = (float.TryParse(text, out result) ? args : text);
		try
		{
			DolocAPI.ExecuteCommand(input, out var result2);
			if (!(result2 is UniTask uniTask))
			{
				return CommandExecuteResult.SucceededSync;
			}
			task = uniTask;
			return (!args.StartsWith("wait ")) ? CommandExecuteResult.SucceededSync : CommandExecuteResult.SucceededAsync;
		}
		catch (Exception ex)
		{
			Debug.LogError(ex?.ToString() + ex.Message);
			Debug.LogError(ex.StackTrace);
			return CommandExecuteResult.Failed;
		}
	}

	[Command("async_test")]
	private static async UniTask AsyncTest1()
	{
		for (int i = 0; i < 5; i++)
		{
			await UniTask.Delay(1000);
			Debug.Log(i);
		}
	}
}
