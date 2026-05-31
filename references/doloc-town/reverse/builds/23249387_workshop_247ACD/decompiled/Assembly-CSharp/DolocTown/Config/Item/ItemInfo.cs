using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemInfo : BeanBase
{
	public const int __ID__ = 799826652;

	public string Id { get; private set; }

	public ItemOrderInfo Id_Ref { get; private set; }

	public string SubType { get; private set; }

	public ItemSubTypeInfo SubType_Ref { get; private set; }

	public bool Salable { get; private set; }

	public bool Disposable { get; private set; }

	public bool Consumable { get; private set; }

	public bool Cookable { get; private set; }

	public int ElectricEnergy { get; private set; }

	public bool Viewable { get; private set; }

	public string[] Source { get; private set; }

	public ItemSourceInfo[] Source_Ref { get; private set; }

	public int SellingPrice { get; private set; }

	public int BuyingPrice { get; private set; }

	public int Overlay { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string DescriptionBasic { get; private set; }

	public string DescriptionBasic_l10n_key { get; }

	public ItemFunctionBase Function { get; private set; }

	public string DescriptionPrefix
	{
		get
		{
			if (!Consumable)
			{
				return string.Empty;
			}
			return DolocConfig.StaticTexts.UiTipConsumable.Colored(DolocUiColor.SLIENTCOLOR_RED) + "\u00a0";
		}
	}

	public string Description
	{
		get
		{
			if (!DescriptionBasic.IsNullOrEmpty())
			{
				return DescriptionPrefix + DescriptionBasic;
			}
			return string.Empty;
		}
	}

	public ItemMainTypeInfo MainType => SubType_Ref?.MainType_Ref ?? DolocAPI.GlobalParameter.ItemDefaultType_Ref;

	public ItemInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sub_type"].IsString)
		{
			throw new SerializationException();
		}
		SubType = _json["sub_type"];
		if (!_json["salable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Salable = _json["salable"];
		if (!_json["disposable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Disposable = _json["disposable"];
		if (!_json["consumable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Consumable = _json["consumable"];
		if (!_json["cookable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Cookable = _json["cookable"];
		if (!_json["electric_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		ElectricEnergy = _json["electric_energy"];
		if (!_json["viewable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Viewable = _json["viewable"];
		JSONNode jSONNode = _json["source"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Source = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			Source[num++] = text;
		}
		if (!_json["selling_price"].IsNumber)
		{
			throw new SerializationException();
		}
		SellingPrice = _json["selling_price"];
		if (!_json["buying_price"].IsNumber)
		{
			throw new SerializationException();
		}
		BuyingPrice = _json["buying_price"];
		if (!_json["overlay"].IsNumber)
		{
			throw new SerializationException();
		}
		Overlay = _json["overlay"];
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["description_basic"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionBasic_l10n_key = _json["description_basic"]["key"];
		if (!_json["description_basic"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionBasic = _json["description_basic"]["text"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = ItemFunctionBase.DeserializeItemFunctionBase(_json["function"]);
	}

	public ItemInfo(string id, string sub_type, bool salable, bool disposable, bool consumable, bool cookable, int electric_energy, bool viewable, string[] source, int selling_price, int buying_price, int overlay, SpriteAsset ui_sprite_asset, string title, string description_basic, ItemFunctionBase function)
	{
		Id = id;
		SubType = sub_type;
		Salable = salable;
		Disposable = disposable;
		Consumable = consumable;
		Cookable = cookable;
		ElectricEnergy = electric_energy;
		Viewable = viewable;
		Source = source;
		SellingPrice = selling_price;
		BuyingPrice = buying_price;
		Overlay = overlay;
		UiSpriteAsset = ui_sprite_asset;
		Title = title;
		DescriptionBasic = description_basic;
		Function = function;
	}

	public static ItemInfo DeserializeItemInfo(JSONNode _json)
	{
		return new ItemInfo(_json);
	}

	public override int GetTypeId()
	{
		return 799826652;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItemOrder"] as TbItemOrder).GetOrDefault(Id);
		SubType_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(SubType);
		int num = Source.Length;
		TbItemSource tbItemSource = (TbItemSource)_tables["Item.TbItemSource"];
		Source_Ref = new ItemSourceInfo[num];
		for (int i = 0; i < num; i++)
		{
			Source_Ref[i] = tbItemSource.GetOrDefault(Source[i]);
		}
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		DescriptionBasic = translator(DescriptionBasic_l10n_key, DescriptionBasic);
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SubType:" + SubType + ",Salable:" + Salable + ",Disposable:" + Disposable + ",Consumable:" + Consumable + ",Cookable:" + Cookable + ",ElectricEnergy:" + ElectricEnergy + ",Viewable:" + Viewable + ",Source:" + StringUtil.CollectionToString(Source) + ",SellingPrice:" + SellingPrice + ",BuyingPrice:" + BuyingPrice + ",Overlay:" + Overlay + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",Title:" + Title + ",DescriptionBasic:" + DescriptionBasic + ",Function:" + Function?.ToString() + ",}";
	}
}
