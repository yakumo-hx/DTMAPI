using System;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public abstract class MissionRequireTemplate
{
	[SerializeField]
	public Graph missionGraph;

	public abstract string SummaryInfo { get; }

	public virtual bool AllowCondition => true;

	public string Description
	{
		get
		{
			object[] customAttributes = GetType().GetCustomAttributes(typeof(DescriptionAttribute), inherit: true);
			if (customAttributes.Length == 0)
			{
				return "？？？";
			}
			return ((DescriptionAttribute)customAttributes[0]).description;
		}
	}

	public string Name
	{
		get
		{
			object[] customAttributes = GetType().GetCustomAttributes(typeof(NameAttribute), inherit: true);
			if (customAttributes.Length == 0)
			{
				return "？？？";
			}
			return ((NameAttribute)customAttributes[0]).name;
		}
	}

	protected MissionRequireTemplate(Graph graph)
	{
		missionGraph = graph;
		Reset();
	}

	public abstract MissionRequireTemplateHandle CreateHandle();

	public abstract string GetTemplateFingerPrint();

	public virtual void Reset()
	{
	}
}
