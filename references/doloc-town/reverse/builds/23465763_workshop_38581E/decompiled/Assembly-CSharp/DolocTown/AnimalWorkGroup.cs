using System.Collections.Generic;
using System.Text;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[DoNotList]
public class AnimalWorkGroup : AnimalWork
{
	[SerializeField]
	private readonly List<AnimalWork> works = new List<AnimalWork>();

	private int selectedIdx;

	public override bool isGroup => true;

	public List<AnimalWork> Works => works;

	public override string Title
	{
		get
		{
			if (works.IsNullOrEmpty())
			{
				return "未设置任何任务";
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (AnimalWork work in works)
			{
				stringBuilder.AppendLine(work.Title);
			}
			return stringBuilder.ToString().Trim('\n');
		}
	}

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = null;
		if (works.IsNullOrEmpty())
		{
			return false;
		}
		if (works.Count == 1)
		{
			return works[0].GenTask(animal, out task);
		}
		task = LinearTask.StartWith;
		foreach (AnimalWork work in works)
		{
			if (!work.GenTask(animal, out var task2))
			{
				return false;
			}
			task = task.Then(task2);
		}
		return true;
	}

	public void AppendWork(AnimalWork work)
	{
		if (!works.Contains(work))
		{
			works.Add(work);
		}
	}

	public AnimalWork RemoveWork(AnimalWork work)
	{
		if (!works.Contains(work))
		{
			return this;
		}
		works.Remove(work);
		if (works.Count == 1)
		{
			return works[0];
		}
		return this;
	}
}
