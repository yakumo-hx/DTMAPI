using System;
using System.Linq;
using DolocTown.Config;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DecorativeAnimalManager : InteractableObject
{
	[Serializable]
	private struct AnimalInfo
	{
		[SerializeField]
		public string animalName;

		[SerializeField]
		public bool isChild;
	}

	[SerializeField]
	private Transform transitionPoint;

	[SerializeField]
	private GameObject[] feederObjects;

	[SerializeField]
	private AnimalInfo[] animals;

	private NashObjectPool<DecorativeAnimal> pool;

	public Vector2 TransitionPoint
	{
		get
		{
			if (transitionPoint == null)
			{
				return base.transform.position;
			}
			return transitionPoint.position;
		}
	}

	private Vector2[] AllFeederPoints
	{
		get
		{
			if (feederObjects.IsNullOrEmpty())
			{
				return Array.Empty<Vector2>();
			}
			return feederObjects.Where((GameObject x) => x != null).Select((Func<GameObject, Vector2>)((GameObject x) => x.transform.position)).ToArray();
		}
	}

	protected override void OnRender(Room room)
	{
		base.OnRender(room);
		int num = 0;
		AnimalInfo[] array = animals;
		foreach (AnimalInfo animalInfo in array)
		{
			GenAnimal(animalInfo, num++);
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		pool.RecycleAll();
	}

	private void GenAnimal(AnimalInfo animalInfo, int index)
	{
		if (pool == null)
		{
			GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_DECORATIVE_ANIMAL);
			pool = new NashObjectPool<DecorativeAnimal>(asset, base.transform);
		}
		if (!DolocConfig.Tables.TbAnimal.DataMap.ContainsKey(animalInfo.animalName))
		{
			Debug.LogError("DecorativeAnimalManager.GenAnimal: 未知的小动物\"" + animalInfo.animalName + "\"");
			return;
		}
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		float x = component.size.x;
		float x2 = component.offset.x;
		DecorativeAnimal next = pool.Next;
		Vector2 vector = base.transform.position;
		next.Setup(startPoint: new Vector2(vector.x - x * 0.5f + x2, vector.y), animalName: animalInfo.animalName, activeRange: x, transitionPoint: TransitionPoint, isChild: animalInfo.isChild, feederPoints: AllFeederPoints, index: index);
	}
}
