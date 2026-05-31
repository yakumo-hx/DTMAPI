using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class Workbench : Equipment
{
	private RecipeGroup recipeGroup;

	[JsonProperty]
	public string latestRecipeName;

	private EquipmentFuncWorkbench func => (EquipmentFuncWorkbench)proto.Function;

	public Workbench(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	protected Workbench(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, string latestRecipeName)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.latestRecipeName = latestRecipeName;
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationInteract);
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTipToHide();
		DolocAPI.EnterUI((RecipePanelUiState state) => state.HandleStartUpArgs(func.RecipeGroupName, DolocAPI.GetInventoriesAroundEquipment(this), latestRecipeName, closeAfterCraft: false, null, null, delegate(IRecipe recipe, IRecipeGroup _)
		{
			latestRecipeName = recipe?.RecipeId;
		}));
		SendUseEquipmentMessage();
	}
}
