using System;
using UnityEngine;

namespace DolocTown;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public abstract class GameEntityManagerBaseAttribute : Attribute
{
	public readonly string containerPath;

	public abstract string PrefabPath { get; }

	public bool CustomManagement { get; set; }

	public string Alias { get; set; } = string.Empty;


	public virtual int Threshold { get; set; } = 5;


	public virtual int Frequency { get; set; } = 3;


	public GameEntityManagerBaseAttribute(string containerPath)
	{
		this.containerPath = containerPath;
	}

	public abstract GameObject LoadPrefab();

	public virtual void OnCreated(GameEntityManager manager)
	{
	}
}
