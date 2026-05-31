using DolocTown.Config.Resource;
using UnityEngine;

namespace DolocTown;

public abstract class EnvObject : WorldContent
{
	protected bool HasTouched;

	public IEnvObjectHost Host { get; set; }

	public EnvObjectRenderer Renderer => (EnvObjectRenderer)base.BaseRenderer;

	public EnvObjectInfo Proto { get; private set; }

	public string Name => Proto.Id;

	public override Sprite SceneSprite => Proto.SpriteAsset.Asset;

	public EnvObject(EnvObjectInfo proto, Vector2 position)
		: base(position)
	{
		HasTouched = false;
		Proto = proto;
	}

	public virtual void OnRender()
	{
		Renderer.AnimatorController = Proto.AnimatorAsset.Asset;
		Renderer.SetColRadius(Proto.Radius);
	}

	public virtual void OnUnRender()
	{
	}
}
