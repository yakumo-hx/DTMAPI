using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class ObjectPreset : MonoBehaviour
{
	private Shader _shader;

	private Material _material;

	public Shader Shader
	{
		get
		{
			if (_shader == null)
			{
				_shader = Shader.Find("Shader Graphs/PresetIndicator");
			}
			return _shader;
		}
	}

	public virtual Vector2Int LocalGridPosition
	{
		get
		{
			RoomHandle componentInParent = GetComponentInParent<RoomHandle>();
			if (componentInParent == null)
			{
				return GlobalGridPosition;
			}
			if (componentInParent.isIndoorRoom)
			{
				return GlobalGridPosition - componentInParent.roomPosition.LatticeToGrid(1.5f);
			}
			return GlobalGridPosition - componentInParent.scenePos.LatticeToGrid(1.5f);
		}
	}

	public Color Color { get; set; } = Color.white;


	public abstract Sprite Sprite { get; }

	public virtual Vector2Int GlobalGridPosition => BuilderUtils.LatticeToGrid(base.transform.position, 1.5f);

	public abstract Vector2Int GridSize { get; }

	public abstract string PresetObjectName { get; }

	public abstract bool CreatePreset(out RoomPresetObjectSO preset);

	public virtual void OnDrawGizmos()
	{
		Vector2 vector = (Vector2)GlobalGridPosition * 1.5f;
		Vector2 size = (Vector2)GridSize * 1.5f;
		GizmosHelper.DrawBoxLB(vector, size, Color);
		Gizmos.color = Color;
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!(component != null))
		{
			return;
		}
		Sprite sprite = (Sprite ? Sprite : component.sprite);
		if (sprite != null)
		{
			Vector2 vector2 = vector;
			vector2.x += size.x * 0.5f;
			Vector2 vector3 = vector2 - (Vector2)base.transform.position;
			if (_material == null && Shader != null)
			{
				_material = new Material(Shader);
				component.sharedMaterial = _material;
			}
			component.sprite = sprite;
			component.sharedMaterial.SetVector("_Offset", vector3);
			component.color = new Color(1f, 1f, 1f, 0.6f);
		}
	}
}
