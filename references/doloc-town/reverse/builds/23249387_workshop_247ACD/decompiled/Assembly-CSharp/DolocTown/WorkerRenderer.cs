using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DolocTown;

public abstract class WorkerRenderer : IEquipmentWorkerRenderer
{
	public static readonly IEquipmentWorkerRenderer DefaultRenderer = new IEquipmentWorkerRenderer.WorkerRendererDefault();

	private static readonly Dictionary<string, Type> WorkerRenderers = LoadWorkerRendererTypes();

	protected readonly Equipment equipment;

	protected EquipmentRenderer EquipmentRenderer => equipment.Renderer;

	public static IEquipmentWorkerRenderer CreateWorkerRenderer(string name, Equipment equipment)
	{
		if (!WorkerRenderers.TryGetValue(name, out var value))
		{
			return IEquipmentWorkerRenderer.Default;
		}
		try
		{
			return (IEquipmentWorkerRenderer)Activator.CreateInstance(value, equipment);
		}
		catch (Exception exception)
		{
			Debug.LogError("创建渲染器" + name + "失败");
			Debug.LogException(exception);
			return IEquipmentWorkerRenderer.Default;
		}
	}

	private static Dictionary<string, Type> LoadWorkerRendererTypes()
	{
		Type baseType = typeof(WorkerRenderer);
		Type[] array = (from t in Assembly.GetExecutingAssembly().GetTypes()
			where !t.IsAbstract && t.IsSubclassOf(baseType)
			select t).ToArray();
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] array2 = array;
		foreach (Type type in array2)
		{
			WorkerRendererAttribute customAttribute = type.GetCustomAttribute<WorkerRendererAttribute>();
			if (customAttribute != null)
			{
				if (dictionary.ContainsKey(customAttribute.workerRendererName))
				{
					Debug.LogWarning("工作器渲染器Id重复:" + customAttribute.workerRendererName);
				}
				else
				{
					dictionary.Add(customAttribute.workerRendererName.ToLower(), type);
				}
			}
		}
		return dictionary;
	}

	protected WorkerRenderer(Equipment equipment)
	{
		this.equipment = equipment;
	}

	public abstract void OnFailed();

	public abstract void OnIdle();

	public abstract void OnStop();

	public abstract void OnWork();

	public abstract void OnRemove();

	public abstract void OnWorkDone();
}
