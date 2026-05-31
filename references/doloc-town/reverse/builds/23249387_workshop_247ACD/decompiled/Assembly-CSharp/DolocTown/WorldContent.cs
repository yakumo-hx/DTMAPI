using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class WorldContent : IHasIndex
{
	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	private Vector2 position;

	[JsonProperty]
	public int index { get; set; }

	public bool isDeserializationValid => ValidateDeserialization();

	private WorldContentRenderer ContentRenderer { get; set; }

	public WorldContentRenderer BaseRenderer
	{
		get
		{
			return ContentRenderer;
		}
		set
		{
			ContentRenderer = value;
		}
	}

	public Vector2 PositionWS => position;

	public abstract Sprite SceneSprite { get; }

	public WorldContent(Vector2 position)
	{
		this.position = position;
	}

	[JsonConstructor]
	protected WorldContent(int index, Vector2 position)
	{
		this.index = index;
		this.position = position;
	}

	public void SetPosition(Vector3 position)
	{
		this.position = position;
		if (ContentRenderer != null)
		{
			ContentRenderer.position2d = position;
		}
	}

	protected abstract bool ValidateDeserialization();

	public virtual void OnInteract()
	{
	}

	public virtual void OnTouch()
	{
	}

	public virtual void OnDisTouch()
	{
	}
}
