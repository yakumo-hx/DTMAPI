using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class CustomSynthesizer : Synthesizer
{
	protected override bool useConversionMode => true;

	protected CustomSynthesizer(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected CustomSynthesizer(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, IRecipe latestRecipe, IRecipeGroup latestRecipeGroup, LinearInventory itemBuffer, Counter taskCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter, latestRecipe, latestRecipeGroup, itemBuffer, taskCounter)
	{
	}

	protected override void OnInteract()
	{
		PushTipToHide();
		if (!TryQuickStart())
		{
			GetTaskInfo(out var _, out var taskTitle);
			latestItem = new CountItem(base.inventory.FirstItem);
			DolocAPI.EnterUI((ConversionRecipeUiState state) => state.HandleStartUpArgs(recipeGroup, this, base.CanEditBuffer, taskTitle, base.GetTimeInfo, base.OnCraft, GetExtraInfo, HandlePreviewItem));
		}
	}

	protected virtual string GetExtraInfo(Item item)
	{
		return string.Empty;
	}

	protected virtual Item HandlePreviewItem(Item item)
	{
		return item;
	}
}
