
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.Mine
{
    internal sealed partial class MineNativeRuntime
    {
        private MineDefinition mineDefinition = new MineDefinition();

        private void ConfigureNativeMineContent(
            MineDefinition definition)
        {
            mineDefinition = NormalizeMineDefinition(definition);
            state.NativeTechTreeSummary =
                EnsureNativeMachineTechRoute(mineDefinition);
            string recipeSummary =
                EnsureNativeMineRecipeInputs(mineDefinition);
            state.LastMessage =
                "Configured the fixed electric-only Mine with " +
                mineDefinition.OutputRules.Count +
                " output rules." +
                (string.IsNullOrWhiteSpace(recipeSummary)
                    ? string.Empty
                    : " recipe={" + recipeSummary + "}");
            runtime.RuntimeMonitor.Log(
                "Mine ProductNative content configured outputRules=" +
                mineDefinition.OutputRules.Count +
                " fixedPower=" +
                MineProductContract.FixedPowerCost + ".");
        }

        private string EnsureNativeMineRecipeInputs(
            MineDefinition definition)
        {
            if (definition.RecipeInputs == null || definition.RecipeInputs.Count == 0)
                return string.Empty;

            string recipeId = MineProductContract.MineItemId;

            try
            {
                object? recipe = GetDolocConfigTableEntry("TbRecipe", recipeId);
                if (recipe == null)
                    return "pending=missing-native-recipe:" + recipeId;

                object? countItems = CreateCountItemsArray(definition.RecipeInputs);
                if (countItems == null)
                    return "failed=count-item-array";

                CaptureMemberRestore(
                    "recipe-inputs:" + recipeId,
                    recipe,
                    "InputItems",
                    "input_items");
                if (!SetMemberValue(recipe, "InputItems", countItems) && !SetMemberValue(recipe, "input_items", countItems))
                    return "failed=set-input-items";

                string summary = "recipe=" + recipeId + ", inputs=" + FormatMachineRecipeInputs(definition.RecipeInputs);
                runtime.RuntimeMonitor.Log("Machine recipe inputs updated " + summary + ".");
                runtime.SetHookStatus("Machine.MineRecipeInputs", "experimental", "DolocConfig.Tables.TbRecipe recipe input override", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.Mine", "Machine recipe input override failed for " + recipeId + ".", ex.ToString());
                runtime.SetHookStatus("Machine.MineRecipeInputs", "failed", "DolocConfig.Tables.TbRecipe recipe input override", ex.GetType().Name + ": " + ex.Message);
                return "failed=" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static object? GetDolocConfigTableEntry(string tableName, string id)
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = ReadStaticMember(dolocConfig, "Tables");
            object? table = tables == null ? null : ReadMember(tables, tableName);
            object? dataMap = table == null ? null : ReadMember(table, "DataMap");
            if (dataMap is IDictionary dictionary && dictionary.Contains(id))
                return dictionary[id];

            MethodInfo? getOrDefault = table == null ? null : FindMethodInHierarchy(table.GetType(), "GetOrDefault", 1);
            return getOrDefault?.Invoke(table, new object[] { id });
        }

        private static object? CreateCountItemsArray(
            IReadOnlyList<MineRecipeInput> inputs)
        {
            Type? countItemType = ResolveType("DolocTown.CountItem, Assembly-CSharp");
            if (countItemType == null)
                return null;

            Array array = Array.CreateInstance(countItemType, inputs.Count);
            ConstructorInfo? ctor = countItemType.GetConstructor(new[] { typeof(string), typeof(int) });
            for (int i = 0; i < inputs.Count; i++)
            {
                MineRecipeInput input =
                    inputs[i] ?? new MineRecipeInput();
                object? item = ctor != null
                    ? ctor.Invoke(new object[] { input.ItemId ?? string.Empty, Math.Max(1, input.Count) })
                    : Activator.CreateInstance(countItemType);
                if (item == null)
                    return null;
                if (ctor == null)
                {
                    SetMemberValue(item, "itemName", input.ItemId ?? string.Empty);
                    SetMemberValue(item, "itemCount", Math.Max(1, input.Count));
                }
                array.SetValue(item, i);
            }

            return array;
        }

        private static string FormatMachineRecipeInputs(
            IReadOnlyList<MineRecipeInput> inputs)
        {
            return string.Join("|", (inputs ?? Array.Empty<MineRecipeInput>())
                .Where(input => input != null && !string.IsNullOrWhiteSpace(input.ItemId))
                .Select(input => input.ItemId + "x" + Math.Max(1, input.Count).ToString(CultureInfo.InvariantCulture)));
        }

        private string EnsureNativeMachineTechRoute(
            MineDefinition definition)
        {
            string nodeId = FirstText(
                definition.NativeTechNodeId,
                MineProductContract.MineItemId);
            if (string.IsNullOrWhiteSpace(nodeId))
                return string.Empty;

            string parentId = FirstText(definition.NativeTechNodeParentId, "alloy_material");
            string aboveTitle = FirstText(definition.NativeTechNodeAboveTitleContains, "指挥官", "Commander");
            string equipmentId = MineProductContract.MineItemId;
            string recipeId = MineProductContract.MineItemId;
            try
            {
                string infoSummary = EnsureNativeTechNodeInfo(
                    nodeId,
                    FirstText(
                        definition.NativeTechNodeTitle,
                        nodeId),
                    "解锁矿井制作配方。矿井固定耗电 10，并把矿物产出到自己的储物格。");
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? assets = ReadStaticMember(dolocApi, "assets");
                object? techTrees = assets == null ? null : ReadMember(assets, "techTrees");
                object? allTreeGraphs = techTrees == null ? null : ReadMember(techTrees, "AllTreeGraphs");
                if (!(allTreeGraphs is IEnumerable graphs))
                    return "pending:missing-native-techtrees, node=" + nodeId + ", " + infoSummary;

                object? targetGraph = null;
                object? parentNode = null;
                object? aboveNode = null;
                foreach (object graph in graphs)
                {
                    string treeId = ReadStringMember(graph, "id");
                    if (!string.IsNullOrWhiteSpace(definition.NativeTechTreeId) &&
                        !treeId.Equals(definition.NativeTechTreeId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryFindTechGraphNode(graph, nodeId, out object? existingNode))
                    {
                        string payloadSummary = EnsureNativeMachineTechGraphNodePayload(graph, existingNode!, parentId, definition, equipmentId, recipeId);
                        string existingSummary = BuildNativeTechRouteSummary(graph, existingNode!, parentId, aboveTitle, "verified-existing", infoSummary, equipmentId, recipeId) + ", " + payloadSummary;
                        PublishNativeTechRouteStatus(existingSummary);
                        return existingSummary;
                    }

                    if (TryFindTechGraphNode(graph, parentId, out object? candidateParent))
                    {
                        targetGraph = graph;
                        parentNode = candidateParent;
                        aboveNode = FindTechGraphNodeByTitle(graph, aboveTitle) ?? FindTechGraphNodeByTitle(graph, "Commander");
                        break;
                    }
                }

                if (targetGraph == null || parentNode == null)
                    return "pending:parent-not-found, node=" + nodeId + ", parent=" + parentId + ", " + infoSummary;

                string injectSummary = InjectNativeTechGraphNode(targetGraph, parentNode, aboveNode, definition, nodeId, parentId, aboveTitle, equipmentId, recipeId, infoSummary);
                PublishNativeTechRouteStatus(injectSummary);
                return injectSummary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.Mine", "Native machine tech route injection failed for " + nodeId + ".", ex.ToString());
                return "failed:" + ex.GetType().Name + ", node=" + nodeId + ", message=" + ex.Message;
            }
        }

        private string InjectNativeTechGraphNode(
            object graph,
            object parentNode,
            object? aboveNode,
            MineDefinition definition,
            string nodeId,
            string parentId,
            string aboveTitle,
            string equipmentId,
            string recipeId,
            string infoSummary)
        {
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is Array nodes) || nodes.Length == 0)
                return "failed:graph-nodes-missing, node=" + nodeId + ", parent=" + parentId + ", " + infoSummary;

            object? parentPos = ReadMember(parentNode, "pos");
            object? abovePos = aboveNode == null ? null : ReadMember(aboveNode, "pos");
            int parentX = ReadIntMember(parentPos ?? new object(), "x", 0);
            int parentY = ReadIntMember(parentPos ?? new object(), "y", 0);
            int desiredX = parentX + 1;
            int desiredY = abovePos == null ? parentY - 1 : ReadIntMember(abovePos, "y", parentY) - 1;
            if (!TryFindFreeTechNodePosition(graph, desiredX, desiredY, parentX, abovePos, out int finalX, out int finalY, out string positionSummary))
                return "failed:no-free-position, node=" + nodeId + ", parent=" + parentId + ", desired=" + desiredX + "," + desiredY + ", " + positionSummary + ", " + infoSummary;

            Type? techNodeProtoType = ResolveType("DolocTown.GameData.TechNodeProto, Assembly-CSharp");
            Type? techNodeCostType = ResolveType("DolocTown.GameData.TechNodeCost, Assembly-CSharp");
            Type? spriteType = ResolveType("UnityEngine.Sprite, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Sprite, UnityEngine");
            if (techNodeProtoType == null || techNodeCostType == null)
                return "failed:missing-tech-types, node=" + nodeId + ", protoType=" + (techNodeProtoType != null) + ", costType=" + (techNodeCostType != null) + ", " + infoSummary;

            object costs = CreateNativeTechNodeCosts(techNodeCostType, graph, parentNode, ResolveNativeMachineTechCost(definition));
            ConstructorInfo? protoCtor = techNodeProtoType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(ctor => ctor.GetParameters().Length >= 6);
            if (protoCtor == null)
                return "failed:missing-TechNodeProto-ctor, node=" + nodeId + ", " + infoSummary;

            object? icon = TryResolveNativeMachineIcon(equipmentId, spriteType);
            object proto = protoCtor.Invoke(new object?[]
            {
                nodeId,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { recipeId },
                costs,
                icon,
                true
            });

            Type? nodeType = nodes.GetType().GetElementType();
            Type? vector2IntType = ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2Int, UnityEngine");
            if (nodeType == null || vector2IntType == null)
                return "failed:missing-node-vector-types, node=" + nodeId + ", nodeType=" + (nodeType != null) + ", vector2IntType=" + (vector2IntType != null) + ", " + infoSummary;

            object? position = CreateUnityVector2Int(finalX, finalY);
            if (position == null)
                return "failed:create-position, node=" + nodeId + ", " + infoSummary;

            object newNode = Activator.CreateInstance(nodeType, new object[] { nodeId, position, new[] { parentId }, proto })!;
            Array newNodes = Array.CreateInstance(nodeType, nodes.Length + 1);
            Array.Copy(nodes, newNodes, nodes.Length);
            newNodes.SetValue(newNode, nodes.Length);

            object? size = ReadMember(graph, "size");
            int sizeX = size == null ? 0 : ReadIntMember(size, "x", 0);
            int sizeY = size == null ? 0 : ReadIntMember(size, "y", 0);
            object? newSize = CreateUnityVector2Int(Math.Max(sizeX, finalX + 1), Math.Max(sizeY, finalY + 1));
            if (newSize == null)
                return "failed:create-size, node=" + nodeId + ", " + infoSummary;

            string treeId = ReadStringMember(graph, "id");
            object? defaultNode = ReadMember(graph, "defaultNode");
            string defaultNodeName = FirstText(defaultNode == null ? string.Empty : ReadStringMember(defaultNode, "id"), ReadStringMember(nodes.GetValue(0)!, "id"));
            ConstructorInfo? graphCtor = graph.GetType().GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(ctor => ctor.GetParameters().Length == 4);
            if (graphCtor == null)
                return "failed:missing-TreeGraph-ctor, node=" + nodeId + ", tree=" + treeId + ", " + infoSummary;

            object newGraph = graphCtor.Invoke(new object[] { treeId, newNodes, newSize, defaultNodeName });
            object? assets = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "assets");
            object? techTrees = assets == null ? null : ReadMember(assets, "techTrees");
            object? treeMap = techTrees == null ? null : ReadMember(techTrees, "trees");
            if (treeMap is IDictionary dictionary)
            {
                CaptureDictionaryEntryRestore(
                    "tech-tree-graph:" + treeId,
                    dictionary,
                    treeId);
                dictionary[treeId] = newGraph;
            }
            else
            {
                return "failed:tech-tree-map-unavailable, node=" + nodeId + ", tree=" + treeId + ", " + infoSummary;
            }

            bool rightOfParent = finalX > parentX;
            bool aboveCommander = abovePos != null && finalY < ReadIntMember(abovePos, "y", finalY + 1);
            int costCount = CountArrayItems(costs);
            string summary = "injected, node=" + nodeId +
                ", tree=" + treeId +
                ", parent=" + parentId +
                ", pos=" + finalX + "," + finalY +
                ", parentPos=" + parentX + "," + parentY +
                ", aboveTitle=" + aboveTitle +
                ", rightOfParent=" + rightOfParent +
                ", aboveCommander=" + aboveCommander +
                ", equipment=" + equipmentId +
                ", recipe=" + recipeId +
                ", unlockEntries=recipe-only" +
                ", equipmentEntries=0" +
                ", recipeEntries=1" +
                ", costs=" + costCount +
                ", costValues=" + DescribeTechNodeCosts(costs) +
                ", icon=" + (icon == null ? "null" : icon.GetType().Name) +
                ", " + positionSummary +
                ", " + infoSummary;
            runtime.RuntimeMonitor.Log("Native machine tech route injected " + summary + ".");
            return summary;
        }

        private string EnsureNativeTechNodeInfo(string nodeId, string title, string description)
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = ReadStaticMember(dolocConfig, "Tables");

            object? techNodeTable = tables == null ? null : ReadMember(tables, "TbTechNode");
            object? dataMap = techNodeTable == null ? null : ReadMember(techNodeTable, "DataMap");
            object? dataList = techNodeTable == null ? null : ReadMember(techNodeTable, "DataList");
            if (!(dataMap is IDictionary map))
                return "techInfo=pending:missing-TbTechNode";

            if (map.Contains(nodeId))
                return "techInfo=existing";

            Type? infoType = ResolveType("DolocTown.Config.TechTree.TechNodeInfo, Assembly-CSharp");
            ConstructorInfo? ctor = infoType?.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
            if (ctor == null)
                return "techInfo=failed:missing-TechNodeInfo-ctor";

            object info = ctor.Invoke(new object[] { nodeId, title, description, string.Empty });
            CaptureDictionaryEntryRestore(
                "tech-info-map:" + nodeId,
                map,
                nodeId);
            map[nodeId] = info;
            if (dataList is IList list)
            {
                CaptureListAdditionRestore(
                    "tech-info-list:" + nodeId,
                    list,
                    info);
                list.Add(info);
            }
            runtime.RuntimeMonitor.Log("Native TechNodeInfo injected node=" + nodeId + " title=" + title + ".");
            return "techInfo=injected";
        }

        private string EnsureNativeMachineTechGraphNodePayload(
            object graph,
            object node,
            string parentId,
            MineDefinition definition,
            string equipmentId,
            string recipeId)
        {
            object? data = ReadMember(node, "data");
            if (data == null)
                return "payload=failed:missing-data";

            Type? techNodeCostType = ResolveType("DolocTown.GameData.TechNodeCost, Assembly-CSharp");
            if (techNodeCostType == null)
                return "payload=failed:missing-TechNodeCost";

            object? parentNode = null;
            TryFindTechGraphNode(graph, parentId, out parentNode);
            object costs = CreateNativeTechNodeCosts(techNodeCostType, graph, parentNode ?? node, ResolveNativeMachineTechCost(definition));
            CaptureMemberRestore(
                "tech-payload-equipments:" + definition.NativeTechNodeId,
                data,
                "equipments");
            CaptureMemberRestore(
                "tech-payload-buildings:" + definition.NativeTechNodeId,
                data,
                "buildings");
            CaptureMemberRestore(
                "tech-payload-recipes:" + definition.NativeTechNodeId,
                data,
                "recipes");
            CaptureMemberRestore(
                "tech-payload-costs:" + definition.NativeTechNodeId,
                data,
                "costs");
            bool equipmentSet = SetMemberValue(data, "equipments", Array.Empty<string>());
            bool buildingsSet = SetMemberValue(data, "buildings", Array.Empty<string>());
            bool recipesSet = SetMemberValue(data, "recipes", new[] { recipeId });
            bool costsSet = SetMemberValue(data, "costs", costs);
            return "payload=recipe-only" +
                ", payloadUpdated=" + (equipmentSet && buildingsSet && recipesSet && costsSet) +
                ", equipmentEntries=0" +
                ", recipeEntries=1" +
                ", costValues=" + DescribeTechNodeCosts(costs);
        }

        private static bool TryFindTechGraphNode(object graph, string nodeId, out object? node)
        {
            node = null;
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return false;

            foreach (object candidate in nodes)
            {
                if (candidate != null && ReadStringMember(candidate, "id").Equals(nodeId, StringComparison.OrdinalIgnoreCase))
                {
                    node = candidate;
                    return true;
                }
            }
            return false;
        }

        private static object? FindTechGraphNodeByTitle(object graph, string titlePart)
        {
            if (string.IsNullOrWhiteSpace(titlePart))
                return null;

            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return null;

            foreach (object node in nodes)
            {
                object? data = ReadMember(node, "data");
                string title = data == null ? string.Empty : ReadStringMember(data, "Title");
                string id = ReadStringMember(node, "id");
                if (title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    id.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return node;
                }
            }
            return null;
        }

        private static bool TryFindFreeTechNodePosition(object graph, int desiredX, int desiredY, int parentX, object? abovePos, out int finalX, out int finalY, out string summary)
        {
            finalX = desiredX;
            finalY = desiredY;
            if (!IsTechGraphPositionOccupied(graph, finalX, finalY))
            {
                summary = "position=desired";
                return true;
            }

            int commanderY = abovePos == null ? desiredY + 1 : ReadIntMember(abovePos, "y", desiredY + 1);
            for (int yOffset = 0; yOffset <= 8; yOffset++)
            {
                for (int xOffset = 1; xOffset <= 8; xOffset++)
                {
                    int candidateX = parentX + xOffset;
                    int candidateY = abovePos == null ? desiredY - yOffset : commanderY - 1 - yOffset;
                    if (candidateX <= parentX)
                        continue;
                    if (abovePos != null && candidateY >= commanderY)
                        continue;
                    if (IsTechGraphPositionOccupied(graph, candidateX, candidateY))
                        continue;
                    finalX = candidateX;
                    finalY = candidateY;
                    summary = "position=adjusted, desired=" + desiredX + "," + desiredY;
                    return true;
                }
            }

            summary = "position=occupied, desired=" + desiredX + "," + desiredY;
            return false;
        }

        private static bool IsTechGraphPositionOccupied(object graph, int x, int y)
        {
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return false;

            foreach (object node in nodes)
            {
                object? pos = ReadMember(node, "pos");
                if (pos == null)
                    continue;
                if (ReadIntMember(pos, "x", int.MinValue) == x && ReadIntMember(pos, "y", int.MinValue) == y)
                    return true;
            }
            return false;
        }

        private static object CreateNativeTechNodeCosts(Type techNodeCostType, object graph, object parentNode, int count)
        {
            Array empty = Array.CreateInstance(techNodeCostType, 0);
            if (count <= 0)
                return empty;

            object? referenceCost = FindReferenceTechNodeCost(parentNode) ?? FindReferenceTechNodeCost(graph);
            object? costType = referenceCost == null ? null : ReadMember(referenceCost, "type");
            if (costType == null)
                return empty;

            ConstructorInfo? ctor = techNodeCostType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == 2 && candidate.GetParameters()[1].ParameterType == typeof(int));
            if (ctor == null)
                return empty;

            Array costs = Array.CreateInstance(techNodeCostType, 1);
            costs.SetValue(ctor.Invoke(new object[] { costType, count }), 0);
            return costs;
        }

        private static object? FindReferenceTechNodeCost(object source)
        {
            object? nodesObject = ReadMember(source, "nodes");
            if (nodesObject is IEnumerable nodes)
            {
                foreach (object node in nodes)
                {
                    object? cost = FindReferenceTechNodeCost(node);
                    if (cost != null)
                        return cost;
                }
                return null;
            }

            object? data = ReadMember(source, "data") ?? source;
            object? costsObject = ReadMember(data, "costs");
            if (!(costsObject is IEnumerable costs))
                return null;

            foreach (object cost in costs)
            {
                if (cost != null)
                    return cost;
            }
            return null;
        }

        private static string DescribeTechNodeCosts(object? costsObject)
        {
            if (!(costsObject is IEnumerable costs))
                return "none";

            var values = new List<string>();
            foreach (object? cost in costs)
            {
                if (cost == null)
                    continue;

                object? type = ReadMember(cost, "type");
                int count = ReadIntMember(cost, "count", -1);
                string id = type == null
                    ? "unknown"
                    : FirstText(ReadStringMember(type, "id"), ReadStringMember(type, "Id"), type.ToString() ?? "unknown");
                values.Add(id + ":" + count.ToString(CultureInfo.InvariantCulture));
            }

            return values.Count == 0 ? "none" : string.Join("|", values);
        }

        private static int ResolveNativeMachineTechCost(
            MineDefinition definition) => 1;

        private static object? TryResolveNativeMachineIcon(string itemId, Type? spriteType)
        {
            if (spriteType == null)
                return null;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? getItemSprite = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method => method.Name == "GetItemSprite" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string));
                object? sprite = getItemSprite?.Invoke(null, new object[] { itemId });
                return spriteType.IsInstanceOfType(sprite) ? sprite : null;
            }
            catch
            {
                return null;
            }
        }

        private string BuildNativeTechRouteSummary(object graph, object node, string parentId, string aboveTitle, string status, string infoSummary, string equipmentId, string recipeId)
        {
            object? pos = ReadMember(node, "pos");
            int x = pos == null ? 0 : ReadIntMember(pos, "x", 0);
            int y = pos == null ? 0 : ReadIntMember(pos, "y", 0);
            object? parentNode = null;
            TryFindTechGraphNode(graph, parentId, out parentNode);
            object? parentPos = parentNode == null ? null : ReadMember(parentNode, "pos");
            object? aboveNode = FindTechGraphNodeByTitle(graph, aboveTitle) ?? FindTechGraphNodeByTitle(graph, "Commander");
            object? abovePos = aboveNode == null ? null : ReadMember(aboveNode, "pos");
            bool rightOfParent = parentPos != null && x > ReadIntMember(parentPos, "x", x);
            bool aboveCommander = abovePos != null && y < ReadIntMember(abovePos, "y", y + 1);
            object? data = ReadMember(node, "data");
            object? equipmentEntriesObject = data == null ? null : ReadMember(data, "equipments");
            object? recipeEntriesObject = data == null ? null : ReadMember(data, "recipes");
            object? costsObject = data == null ? null : ReadMember(data, "costs");
            int equipmentEntryCount = CountArrayItems(equipmentEntriesObject);
            int recipeEntryCount = CountArrayItems(recipeEntriesObject);
            int costCount = CountArrayItems(costsObject);
            return status +
                ", node=" + ReadStringMember(node, "id") +
                ", tree=" + ReadStringMember(graph, "id") +
                ", parent=" + parentId +
                ", pos=" + x + "," + y +
                ", rightOfParent=" + rightOfParent +
                ", aboveCommander=" + aboveCommander +
                ", equipment=" + equipmentId +
                ", recipe=" + recipeId +
                ", unlockEntries=" + (equipmentEntryCount == 0 && recipeEntryCount == 1 ? "recipe-only" : "mixed") +
                ", equipmentEntries=" + equipmentEntryCount +
                ", recipeEntries=" + recipeEntryCount +
                ", costs=" + costCount +
                ", costValues=" + DescribeTechNodeCosts(costsObject) +
                ", " + infoSummary;
        }

        private void PublishNativeTechRouteStatus(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return;

            string status = summary.StartsWith("failed", StringComparison.OrdinalIgnoreCase) || summary.StartsWith("pending", StringComparison.OrdinalIgnoreCase)
                ? "pending"
                : "verified";
            runtime.SetHookStatus("Machine.MineTechTreeRoute", status, "DolocAPI.assets.techTrees + DolocConfig.Tables.TbTechNode", summary);
        }

        private static int CountArrayItems(object? value)
        {
            if (value is Array array)
                return array.Length;
            if (value is ICollection collection)
                return collection.Count;
            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object _ in enumerable)
                    count++;
                return count;
            }
            return 0;
        }
    }
}
