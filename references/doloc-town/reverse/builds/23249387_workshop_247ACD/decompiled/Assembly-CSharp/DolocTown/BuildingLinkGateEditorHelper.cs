using System.Text;
using UnityEngine;

namespace DolocTown;

public class BuildingLinkGateEditorHelper : MonoBehaviour
{
	private void ResetTouchIndicators()
	{
		BuildingLinkGate[] componentsInChildren = GetComponentsInChildren<BuildingLinkGate>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetTouchIndicatorShowRemoteBuildingTitle();
		}
	}

	private void CopyAllGateConfigs()
	{
		BuildingLinkGate[] array = LoadSortedGates();
		StringBuilder stringBuilder = new StringBuilder();
		BuildingLinkGate[] array2 = array;
		foreach (BuildingLinkGate buildingLinkGate in array2)
		{
			if (buildingLinkGate == null)
			{
				stringBuilder.Append('\t');
			}
			else
			{
				Vector3 position = buildingLinkGate.agentReference.position;
				stringBuilder.Append($"{position.x}\t{position.y}");
			}
			stringBuilder.Append('\t');
		}
		GUIUtility.systemCopyBuffer = stringBuilder.ToString();
		Debug.Log("已复制传送门配置数据到剪贴板");
	}

	private void RepairRotation()
	{
		BuildingLinkGate[] componentsInChildren = GetComponentsInChildren<BuildingLinkGate>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].RepairRotation();
		}
	}

	private BuildingLinkGate[] LoadSortedGates()
	{
		BuildingLinkGate[] componentsInChildren = GetComponentsInChildren<BuildingLinkGate>(includeInactive: true);
		BuildingLinkGate[] array = new BuildingLinkGate[12];
		BuildingLinkGate[] array2 = componentsInChildren;
		foreach (BuildingLinkGate buildingLinkGate in array2)
		{
			int index = GetIndex(buildingLinkGate);
			if (index < 0 || index >= array.Length)
			{
				Debug.LogError("传送门ID错误: " + buildingLinkGate.GateId.ToString());
			}
			else if (array[index] != null)
			{
				Debug.LogError("传送门ID重复: " + buildingLinkGate.GateId.ToString());
			}
			else
			{
				array[index] = buildingLinkGate;
			}
		}
		return array;
	}

	private int GetIndex(BuildingLinkGate linkGate)
	{
		BuildingLinkGateId gateId = linkGate.GateId;
		return GetTypeIndex(gateId.type) + gateId.index;
	}

	private int GetTypeIndex(BuildingLinkType type)
	{
		return type switch
		{
			BuildingLinkType.Left => 0, 
			BuildingLinkType.Right => 3, 
			BuildingLinkType.Bottom => 6, 
			BuildingLinkType.Top => 9, 
			_ => 0, 
		};
	}
}
