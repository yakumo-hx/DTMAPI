using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class TechTreeUiState : DolocUiState<TechTreePanel>
{
	private HashSet<string> unlockTechTree;

	private TechLevelData[] pointInfos;

	private int currentTreeIndex;

	private TreeGraph<TechNodeProto> currentTree;

	public override bool ShowPauseTip => false;

	protected override bool hideOnPause => true;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private TbTechTree TreeInfos => DolocConfig.Tables.TbTechTree;

	private int treeCount => unlockTechTree.Count;

	private string currentTreeName => TreeInfos.DataList[currentTreeIndex].Id;

	private TechTreeWidget techTreeWidget => base.panel.treeWidget;

	private Vector2Int selectedPosition => techTreeWidget.selectedPosition;

	public bool HandleStartUpArgs(string treeName, string nodeName)
	{
		if (DolocAPI.assets.techTrees.QueryTechTreeNode(treeName, nodeName, out var proto))
		{
			currentTreeIndex = TreeInfos.DataList.FindIndex((TechTreeInfo data) => data.Id == treeName);
			DolocAPI.DelayFrame(delegate
			{
				techTreeWidget.Select(proto.pos);
			});
			return true;
		}
		return false;
	}

	protected override void BeforeRegister()
	{
		SendMessage();
		unlockTechTree = new HashSet<string>();
		foreach (TechTreeInfo data in TreeInfos.DataList)
		{
			if (DolocAPI.archiveHandle.CheckTechTreeUnlocked(data.Id))
			{
				unlockTechTree.Add(data.Id);
			}
		}
		string[] title = (from info in TreeInfos.DataList
			where unlockTechTree.Contains(info.Id)
			select info into x
			select x.Title).ToArray();
		base.panel.treeMenu.Render(title);
		List<TechLevelData> list = new List<TechLevelData>();
		TechLevelData[] allLevelData = DolocAPI.archiveHandle.farmData.techLevelManager.GetAllLevelData();
		foreach (TechLevelData techLevelData in allLevelData)
		{
			TechTreeInfo byTechPoint = TreeInfos.GetByTechPoint(techLevelData.type);
			if (byTechPoint != null && unlockTechTree.Contains(byTechPoint.Id))
			{
				list.Add(techLevelData);
			}
		}
		pointInfos = list.ToArray();
	}

	protected override void Register()
	{
		base.panel.techPointViewer.SetSelectCallbacks(delegate(int index)
		{
			OnPointerEnterPoint(index);
			techTreeWidget.LoseFocus();
			base.panel.operationTip.SetTextKey(base.staticTexts.UiTipTechtreeFocus);
		});
		base.panel.techPointViewer.SetDeselectCallbacks(delegate
		{
			OnPointerExitPoint();
		});
		base.panel.techPointViewer.SetPointerEnterCallbacks(OnPointerEnterPoint);
		base.panel.techPointViewer.SetPointerExitCallbacks(delegate
		{
			OnPointerExitPoint();
		});
		base.panel.treeMenu.SetClickCallbacks(OnSwitchTree);
		base.panel.previewViewer.button.onClick.AddListener(delegate
		{
			OnConfirmButtonClick();
		});
	}

	protected override void Unregister()
	{
		base.panel.techPointViewer.RemoveCallbacks();
		base.panel.treeMenu.SetClickCallbacks(null);
		base.panel.previewViewer.button.onClick.RemoveAllListeners();
	}

	protected override void Show()
	{
		base.Show();
		base.panel.pointPreviewBox.setInvisible();
		base.panel.techPointViewer.Render(pointInfos.Select((TechLevelData data) => new TechPointSimpleData(data)).ToArray());
		base.panel.Show();
		base.panel.treeMenu.Select(currentTreeIndex);
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed || (userInput.GlobalToggleTechTree && userInput.BaseNotFixedOperation))
		{
			gameController.PopState();
		}
		else if (userInput.BaseIsLastPressed)
		{
			SelectPrevTree();
		}
		else if (userInput.BaseIsNextPressed)
		{
			SelectNextTree();
		}
		else if (userInput.BaseIsConfirmPressed)
		{
			OnConfirmButtonClick();
		}
		else if (userInput.BaseMiscellaneousPressed)
		{
			if (techTreeWidget.isFocused)
			{
				techTreeWidget.LoseFocus();
				base.panel.techPointViewer.GetFocus();
			}
			else
			{
				base.panel.techPointViewer.LoseFocus();
				techTreeWidget.GetFocus();
			}
		}
		else if (userInput.BaseScrollDir.magnitude > 0f)
		{
			base.panel.previewViewer.listViewer.SetVerticalScrollbar(userInput.BaseScrollDir.y);
		}
	}

	private void SendMessage()
	{
		DolocAPI.Broadcast(OperationEventType.OPEN_TECH_TREE_PANEL);
		DolocAPI.BroadcastString(GameEventType.OPEN_UI_PANEL, "tech_tree_panel");
	}

	private void OnSwitchTree(int index)
	{
		currentTreeIndex = index;
		base.panel.TreeDesc = TreeInfos.GetById(currentTreeName).Description;
		DolocAPI.assets.techTrees.QueryTechTree(currentTreeName, out currentTree);
		RenderTree(currentTree);
		techTreeWidget.Select(currentTree.defaultPos);
	}

	private void SelectPrevTree()
	{
		int index = (currentTreeIndex + treeCount - 1) % treeCount;
		base.panel.treeMenu.Select(index);
	}

	private void SelectNextTree()
	{
		int index = (currentTreeIndex + 1) % treeCount;
		base.panel.treeMenu.Select(index);
	}

	private void RenderTree(TreeGraph<TechNodeProto> tree)
	{
		techTreeWidget.RenderAllNode(tree.nodes.Select((TreeGraphNode<TechNodeProto> node) => new TechNodeData(node, IsNodeParentUnlocked, CheckUnlockCondition)).ToArray(), tree.size);
		TreeGraphLink[] links = tree.links;
		for (int i = 0; i < links.Length; i++)
		{
			TreeGraphLink treeGraphLink = links[i];
			TreeGraphNode<TechNodeProto> node2 = tree.GetNode(treeGraphLink.parent);
			TreeGraphNode<TechNodeProto> node3 = tree.GetNode(treeGraphLink.child);
			if (node2 != null && node3 != null)
			{
				techTreeWidget.RenderLine(node2.pos, node3.pos, DolocAPI.archiveHandle.GetTechNodeUnlockState(node2.id));
			}
		}
		techTreeWidget.ReBuildLinks();
		techTreeWidget.RefreshSelectCallback(delegate(Vector2Int pos)
		{
			OnNodeDataSelect(pos);
			base.panel.techPointViewer.LoseFocus();
			base.panel.operationTip.SetTextKey(base.staticTexts.UiTipTechtreePointFocus);
		});
	}

	private void OnPointerEnterPoint(int index)
	{
		if (index >= 0 && index < pointInfos.Length)
		{
			TechLevelData levelData = pointInfos[index];
			Vector2 position = base.panel.techPointViewer.GetSlot(index).position;
			base.panel.pointPreviewBox.Render(new TechPointData(levelData));
			base.panel.pointPreviewBox.position = position - new Vector2(0f, 16f);
			base.panel.pointPreviewBox.AdaptionBoundary();
		}
	}

	private void OnPointerExitPoint()
	{
		base.panel.pointPreviewBox.setInvisible();
	}

	private void OnConfirmButtonClick()
	{
		techTreeWidget.Select(selectedPosition);
		if (!currentTree.QueryNode(selectedPosition, out var node) || DolocAPI.archiveHandle.GetTechNodeUnlockState(node.id))
		{
			return;
		}
		if (DolocAPI.archiveHandle.GetFirstLockNode(out var nodeName) && nodeName != node.id)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.Tables.TbTechNode.GetOrDefault(nodeName)?.LockPrompt);
		}
		else if (!node.data.isUseful)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TechtreeNodeUnopen);
		}
		else if (!IsNodeParentUnlocked(node))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TechtreeNodeNotAvaiable);
		}
		else if (CheckUnlockCondition(node))
		{
			DolocAPI.archiveHandle.UnlockTechNode(currentTree.id, node.id);
			TechNodeCost[] costs = node.data.costs;
			for (int i = 0; i < costs.Length; i++)
			{
				TechNodeCost techNodeCost = costs[i];
				DolocAPI.archiveHandle._CostTechPoint(techNodeCost.type, techNodeCost.count);
			}
			RenderTree(currentTree);
			techTreeWidget.Select(selectedPosition);
			base.panel.previewViewer.Render(new TechNodePreviewData(node));
			base.panel.techPointViewer.Render(pointInfos.Select((TechLevelData data) => new TechPointSimpleData(data)).ToArray());
			DolocAPI.ShowMessageBoxLarge(LocSprites.UI_INFOICON_STAR, base.staticTexts.TechtreeNodeUnlocked + " " + node.data.Title);
			DolocAPI.BroadcastString(GameEventType.UNLOCK_TECH_NODE, node.id);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TechtreeNodeLackOfPoints);
		}
	}

	private bool IsNodeParentUnlocked(TreeGraphNode<TechNodeProto> node)
	{
		if (node == null)
		{
			return false;
		}
		if (node.parents.Length != 0)
		{
			return node.parents.Any((string parent) => DolocAPI.archiveHandle.GetTechNodeUnlockState(parent));
		}
		return true;
	}

	private bool CheckUnlockCondition(TreeGraphNode<TechNodeProto> node)
	{
		if (node == null)
		{
			return false;
		}
		TechNodeCost[] costs = node.data.costs;
		for (int i = 0; i < costs.Length; i++)
		{
			TechNodeCost techNodeCost = costs[i];
			if (DolocAPI.archiveHandle.GetTechPoint(techNodeCost.type) < techNodeCost.count)
			{
				return false;
			}
		}
		return true;
	}

	private void OnNodeDataSelect(Vector2Int pos)
	{
		if (currentTree.QueryNode(pos, out var node))
		{
			base.panel.previewViewer.Render(new TechNodePreviewData(node));
		}
		DolocAPI.UIRaiseRoll();
	}
}
