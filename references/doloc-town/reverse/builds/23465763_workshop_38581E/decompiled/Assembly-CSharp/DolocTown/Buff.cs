using DolocTown.Config;
using DolocTown.Config.Buff;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Buff
{
	private BuffInfo _proto;

	private BuffComponent[] _components;

	[JsonProperty]
	private float scale = 1f;

	[JsonProperty]
	private BuffTimer timer;

	public BuffInfo Proto => _proto;

	[JsonProperty("id")]
	public string Id => _proto.Id;

	public int Duration => _proto.Duration;

	public Sprite Icon => _proto.IconNormal.Asset;

	public string Description => _proto.Description;

	public float Scale
	{
		get
		{
			if (!_proto.SupportScale)
			{
				return 1f;
			}
			return scale;
		}
	}

	public BuffTimer Timer => timer;

	public bool Valid => _proto != null;

	public Buff(string id, float scale)
	{
		_proto = DolocConfig.Tables.TbBuff.GetOrDefault(id);
		if (_proto != null)
		{
			this.scale = scale;
			timer = new BuffTimer(_proto.Duration);
			InitComponents();
		}
	}

	[JsonConstructor]
	private Buff(string id, float scale, BuffTimer timer)
	{
		_proto = DolocConfig.Tables.TbBuff.GetOrDefault(id);
		if (_proto != null)
		{
			this.scale = scale;
			this.timer = timer;
			timer.Validate(_proto.Duration);
			InitComponents();
		}
	}

	private void InitComponents()
	{
		int num = _proto.Components.Length;
		_components = new BuffComponent[num];
		for (int i = 0; i < num; i++)
		{
			BuffComponentProto buffComponentProto = _proto.Components[i];
			_components[i] = new BuffComponentBasic(buffComponentProto.BuffType, buffComponentProto.Value, Scale);
		}
	}

	public void Apply()
	{
		BuffComponent[] components = _components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Apply();
		}
	}

	public void Remove()
	{
		BuffComponent[] components = _components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Remove();
		}
	}
}
