using System.Linq;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown.UI;

public struct EffectGroupData : IUIData
{
	public Sprite[] icons;

	public string[] descriptions;

	public bool notEmpty { get; }

	public EffectGroupData(Item item)
	{
		this = default(EffectGroupData);
		if (item is IEatable { isValid: not false } eatable)
		{
			notEmpty = true;
			FoodEffect[] allEffects = eatable.GetAllEffects();
			icons = allEffects.Select((FoodEffect x) => x.Buff_Ref.IconSmall.Asset).ToArray();
			descriptions = allEffects.Select(GetDescription).ToArray();
		}
	}

	private string GetDescription(FoodEffect effect)
	{
		float scale = effect.Scale;
		int valueDiffInGame = effect.Buff_Ref.GetValueDiffInGame(Mathf.RoundToInt(effect.Scale));
		string text = effect.Buff_Ref.Description.Format(scale);
		if (valueDiffInGame > 0)
		{
			text += $" [+{valueDiffInGame}]".Colored(DolocUiColor.SLIENTCOLOR_GREEN);
		}
		if (valueDiffInGame < 0)
		{
			text += $" [{valueDiffInGame}]".Colored(DolocUiColor.SLIENTCOLOR_RED);
		}
		return text;
	}
}
