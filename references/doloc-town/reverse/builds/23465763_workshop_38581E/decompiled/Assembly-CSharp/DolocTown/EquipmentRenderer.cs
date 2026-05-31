using System;
using System.Collections.Generic;
using DG.Tweening;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
[GameEntityManager("/farm/equipment", DolocGameAssets.GAME_ENTITY_EQUIPMENT, Frequency = 10)]
public class EquipmentRenderer : GameRendererEntity
{
	private readonly Dictionary<Type, GameEntity> components = new Dictionary<Type, GameEntity>();

	private readonly Dictionary<Type, Dictionary<string, GameEntity>> aliasComponents = new Dictionary<Type, Dictionary<string, GameEntity>>();

	private readonly List<DolocUiRecyclableObject> uiComponents = new List<DolocUiRecyclableObject>();

	private Tween anim;

	private Shiner shiner;

	public Equipment Equipment { get; set; }

	public Animator Animator { get; protected set; }

	public SpriteRenderer Sr { get; protected set; }

	public override SpriteRenderer outlineTarget => Sr;

	protected override ObjectOutlineType outlineType
	{
		get
		{
			if (Equipment == null || !Equipment.proto.isDecal)
			{
				return ObjectOutlineType.ExcludeBottom;
			}
			return ObjectOutlineType.All;
		}
	}

	public Sprite Sprite
	{
		get
		{
			return Sr.sprite;
		}
		set
		{
			Sr.sprite = value;
		}
	}

	public float Alpha
	{
		get
		{
			return Sr.color.a;
		}
		set
		{
			Color color = Sr.color;
			color.a = value;
			Sr.color = color;
		}
	}

	public void PlayAnimation(string name)
	{
		Animator.enabled = true;
		if (!Animator.GetCurrentAnimatorStateInfo(0).IsName(name))
		{
			Animator.Play(name, 0, UnityEngine.Random.value);
		}
	}

	public T GetRenderComponent<T>(string alias) where T : GameEntity
	{
		if (Equipment == null)
		{
			return null;
		}
		Type typeFromHandle = typeof(T);
		if (!aliasComponents.TryGetValue(typeFromHandle, out var value))
		{
			value = new Dictionary<string, GameEntity>();
			aliasComponents.Add(typeFromHandle, value);
		}
		if (value.TryGetValue(alias, out var value2))
		{
			if (value2 != null || value2.gameObject != null)
			{
				return (T)value2;
			}
			value.Remove(alias);
		}
		T val = DolocAPI.EntitySystem.Next<T>(alias);
		value.Add(alias, val);
		return val;
	}

	public T GetRenderComponent<T>() where T : GameEntity
	{
		if (Equipment == null)
		{
			return null;
		}
		Type typeFromHandle = typeof(T);
		if (components.TryGetValue(typeFromHandle, out var value))
		{
			if (value != null || value.gameObject != null)
			{
				return (T)value;
			}
			components.Remove(typeFromHandle);
		}
		T val = DolocAPI.EntitySystem.Next<T>();
		components.Add(typeFromHandle, val);
		return val;
	}

	public void HandleComponentIfExist<T>(Action<T> handleFunc) where T : GameEntity
	{
		T val = FetchRenderComponent<T>();
		if (!(val == null))
		{
			handleFunc(val);
		}
	}

	public T GetUiComponent<T>() where T : DolocUiRecyclableObject
	{
		if (Equipment == null)
		{
			return null;
		}
		T fromPoolInScene = DolocAPI.uiSystem.GetFromPoolInScene<T>();
		uiComponents.Add(fromPoolInScene);
		return fromPoolInScene;
	}

	public bool TryGetFirstUiComponent<T>(out T component) where T : DolocUiRecyclableObject
	{
		component = null;
		if (Equipment == null)
		{
			return false;
		}
		for (int i = 0; i < uiComponents.Count; i++)
		{
			if (uiComponents[i] is T val)
			{
				component = val;
				return true;
			}
		}
		return false;
	}

	public T FetchRenderComponent<T>() where T : GameEntity
	{
		if (Equipment == null)
		{
			return null;
		}
		Type typeFromHandle = typeof(T);
		if (components.TryGetValue(typeFromHandle, out var value))
		{
			return (T)value;
		}
		return null;
	}

	public bool ContainsRenderComponent<T>() where T : GameEntity
	{
		if (Equipment == null)
		{
			return false;
		}
		Type typeFromHandle = typeof(T);
		return components.ContainsKey(typeFromHandle);
	}

	public void HideAllComponents()
	{
		if (Equipment == null)
		{
			return;
		}
		foreach (GameEntity value in components.Values)
		{
			value.SetVisible(value: false);
		}
		foreach (DolocUiRecyclableObject uiComponent in uiComponents)
		{
			uiComponent.SetVisible(value: false);
		}
	}

	public void RemoveRenderComponent<T>() where T : DolocRecyclableObject
	{
		if (Equipment != null && components.TryGetValue(typeof(T), out var value))
		{
			DolocAPI.EntitySystem.Recycle(value);
			components.Remove(typeof(T));
		}
	}

	public void RemoveUiComponent<T>() where T : DolocUiRecyclableObject
	{
		if (Equipment == null)
		{
			return;
		}
		for (int num = uiComponents.Count - 1; num >= 0; num--)
		{
			if (uiComponents[num] is T val)
			{
				uiComponents.Remove(val);
				DolocAPI.uiSystem.RecycleToPoolInScene(val);
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		Animator = GetComponent<Animator>();
		Animator.enabled = false;
		shiner = new Shiner(Sr);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (Equipment != null)
		{
			Equipment.OccupiedGridRenderer = null;
			Equipment.DecoratedUnRender();
			RecycleAllComponents();
			Equipment.Renderer = null;
			Equipment = null;
		}
		anim?.Kill();
		Animator.runtimeAnimatorController = null;
		Animator.enabled = false;
	}

	public override void OnReuse()
	{
		base.OnReuse();
		components.Clear();
		Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		Sr.color = Color.white;
	}

	private void RecycleAllComponents()
	{
		foreach (GameEntity value in components.Values)
		{
			DolocAPI.EntitySystem.Recycle(value);
		}
		components.Clear();
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		anim?.Kill();
		anim = base.transform.DOShakePosition(duration, new Vector3(shakeStrength, shakeStrength, 0f));
	}

	public void Shiner(float time)
	{
		shiner.Raise(LocMaterials.GAME_MAT_HOLOGRAM, time);
	}

	public void ToggleShine(bool value)
	{
		if (value)
		{
			Sr.ToggleItemShine();
		}
		else
		{
			Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
	}
}
