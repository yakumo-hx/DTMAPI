using DolocTown.Config;
using DolocTown.Config.TechTree;
using UnityEngine;

namespace DolocTown.GameData;

public class TechNodeProto
{
	public readonly string id;

	public readonly string[] equipments;

	public readonly string[] buildings;

	public readonly string[] recipes;

	public readonly TechNodeCost[] costs;

	public readonly Sprite icon;

	public readonly bool isUseful;

	private TechNodeInfo Info => DolocConfig.Tables.TbTechNode.GetOrDefault(id);

	public string Title
	{
		get
		{
			if (Info == null)
			{
				return "<Empty Title>";
			}
			return Info.Title;
		}
	}

	public string Description
	{
		get
		{
			if (Info == null)
			{
				return "<Empty Description>";
			}
			return Info.Description;
		}
	}

	public TechNodeProto(string id, string[] equipments, string[] buildings, string[] recipes, TechNodeCost[] costs, Sprite icon, bool isUseful = true)
	{
		this.id = id;
		this.equipments = equipments;
		this.buildings = buildings;
		this.recipes = recipes;
		this.costs = costs;
		this.icon = icon;
		this.isUseful = isUseful;
	}
}
