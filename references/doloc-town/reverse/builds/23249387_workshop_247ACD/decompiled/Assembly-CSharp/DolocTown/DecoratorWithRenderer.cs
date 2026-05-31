using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DecoratorWithRenderer : Equipment
{
	public interface IDecoratorRenderer
	{
		EquipmentRenderer Renderer { get; set; }

		void OnUpdate();

		void OnDisTouch();

		void OnTouch();
	}

	private readonly IDecoratorRenderer IRenderer;

	public DecoratorWithRenderer(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected DecoratorWithRenderer(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
	}

	protected override void Update()
	{
		IRenderer.OnUpdate();
	}

	protected override void OnTouch()
	{
		IRenderer.OnTouch();
	}

	protected override void OnDisTouch()
	{
		IRenderer.OnDisTouch();
	}
}
