using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        public MachineRegisterResult RegisterMachine(IManifest owner, MachineDefinition definition)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            MachineDefinition normalized = NormalizeMachineDefinition(owner, definition);
            var result = new MachineRegisterResult
            {
                OwnerId = owner.UniqueID,
                MachineId = normalized.MachineId,
                Definition = normalized
            };

            if (string.IsNullOrWhiteSpace(normalized.MachineId) || string.IsNullOrWhiteSpace(normalized.EquipmentId))
            {
                result.FailureReason = "missing-id";
                result.Message = "MachineId and EquipmentId are required.";
                return result;
            }

            if (!machineDefinitions.TryGetValue(owner.UniqueID, out List<MachineDefinition>? definitions))
            {
                definitions = new List<MachineDefinition>();
                machineDefinitions[owner.UniqueID] = definitions;
            }

            definitions.RemoveAll(candidate => candidate.MachineId.Equals(normalized.MachineId, StringComparison.OrdinalIgnoreCase));
            definitions.Add(normalized);

            string nativeTechTreeSummary = EnsureNativeMachineTechRoute(normalized);
            string recipeSummary = EnsureNativeMachineRecipeInputs(normalized);
            machineStates[owner.UniqueID] = new MachineProductionState
            {
                OwnerId = owner.UniqueID,
                IsConfigured = true,
                RegisteredMachineCount = definitions.Count,
                RuntimeHookInstalled = machineRuntimeLoopInstalled,
                Status = machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook",
                NativeTechTreeSummary = nativeTechTreeSummary,
                LastMessage = "Registered machine definition " + normalized.MachineId + " with " + normalized.OutputRules.Count + " output rules." +
                    (string.IsNullOrWhiteSpace(nativeTechTreeSummary) ? string.Empty : " nativeTech={" + nativeTechTreeSummary + "}") +
                    (string.IsNullOrWhiteSpace(recipeSummary) ? string.Empty : " recipe={" + recipeSummary + "}")
            };
            ApplyMachineDefinitionState(machineStates[owner.UniqueID], normalized, 0, null);
            runtime.RuntimeMonitor.Log("Machine production definition registered owner=" + owner.UniqueID + " machine=" + normalized.MachineId + " equipment=" + normalized.EquipmentId + " outputs=" + normalized.OutputRules.Count + ".");
            runtime.SetHookStatus("Machine.ProductionApi", machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook", "DTMAPI.GameBridge.DolocTown API", "Registered " + normalized.MachineId + " for " + owner.UniqueID + "; DTMAPI runtime loop handles cycle/output state while native recipe overrides and electric-only/fuel-capable runtime definitions remain experimental.");

            result.Success = true;
            result.Message = machineStates[owner.UniqueID].LastMessage;
            return result;
        }

        private string EnsureNativeMachineRecipeInputs(MachineDefinition definition)
        {
            if (definition.RecipeInputs == null || definition.RecipeInputs.Count == 0)
                return string.Empty;

            string recipeId = FirstText(definition.RecipeId, definition.ItemId, definition.EquipmentId, definition.MachineId);
            if (string.IsNullOrWhiteSpace(recipeId))
                return "skipped=missing-recipe-id";

            try
            {
                object? recipe = GetDolocConfigTableEntry("TbRecipe", recipeId);
                if (recipe == null)
                    return "pending=missing-native-recipe:" + recipeId;

                object? countItems = CreateCountItemsArray(definition.RecipeInputs);
                if (countItems == null)
                    return "failed=count-item-array";

                if (!SetMemberValue(recipe, "InputItems", countItems) && !SetMemberValue(recipe, "input_items", countItems))
                    return "failed=set-input-items";

                string summary = "recipe=" + recipeId + ", inputs=" + FormatMachineRecipeInputs(definition.RecipeInputs);
                runtime.RuntimeMonitor.Log("Machine recipe inputs updated " + summary + ".");
                runtime.SetHookStatus("Machine.MineRecipeInputs", "experimental", "DolocConfig.Tables.TbRecipe recipe input override", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Machine recipe input override failed for " + recipeId + ".", ex.ToString());
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

        private static object? CreateCountItemsArray(IReadOnlyList<MachineRecipeInput> inputs)
        {
            Type? countItemType = ResolveType("DolocTown.CountItem, Assembly-CSharp");
            if (countItemType == null)
                return null;

            Array array = Array.CreateInstance(countItemType, inputs.Count);
            ConstructorInfo? ctor = countItemType.GetConstructor(new[] { typeof(string), typeof(int) });
            for (int i = 0; i < inputs.Count; i++)
            {
                MachineRecipeInput input = inputs[i] ?? new MachineRecipeInput();
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

        private static string FormatMachineRecipeInputs(IReadOnlyList<MachineRecipeInput> inputs)
        {
            return string.Join("|", (inputs ?? Array.Empty<MachineRecipeInput>())
                .Where(input => input != null && !string.IsNullOrWhiteSpace(input.ItemId))
                .Select(input => input.ItemId + "x" + Math.Max(1, input.Count).ToString(CultureInfo.InvariantCulture)));
        }

        private string EnsureNativeMachineTechRoute(MachineDefinition definition)
        {
            string nodeId = FirstText(definition.NativeTechNodeId, definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ? definition.EquipmentId : string.Empty);
            if (string.IsNullOrWhiteSpace(nodeId))
                return string.Empty;

            string parentId = FirstText(definition.NativeTechNodeParentId, "alloy_material");
            string aboveTitle = FirstText(definition.NativeTechNodeAboveTitleContains, "指挥官", "Commander");
            string equipmentId = FirstText(definition.EquipmentId, definition.ItemId, nodeId);
            string recipeId = FirstText(definition.RecipeId, equipmentId);
            try
            {
                string infoSummary = EnsureNativeTechNodeInfo(nodeId, FirstText(definition.NativeTechNodeTitle, definition.DisplayName, nodeId), FirstText(definition.NativeTechNodeDescription, "Unlocks " + definition.DisplayName + "."));
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
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Native machine tech route injection failed for " + nodeId + ".", ex.ToString());
                return "failed:" + ex.GetType().Name + ", node=" + nodeId + ", message=" + ex.Message;
            }
        }

        private string InjectNativeTechGraphNode(object graph, object parentNode, object? aboveNode, MachineDefinition definition, string nodeId, string parentId, string aboveTitle, string equipmentId, string recipeId, string infoSummary)
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
            map[nodeId] = info;
            if (dataList is IList list)
                list.Add(info);
            runtime.RuntimeMonitor.Log("Native TechNodeInfo injected node=" + nodeId + " title=" + title + ".");
            return "techInfo=injected";
        }

        private string EnsureNativeMachineTechGraphNodePayload(object graph, object node, string parentId, MachineDefinition definition, string equipmentId, string recipeId)
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

        private static int ResolveNativeMachineTechCost(MachineDefinition definition)
        {
            if (definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ||
                definition.MachineId.Equals("dtmapi.mine", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            return 0;
        }

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

        public IReadOnlyList<MachineDefinition> GetMachines(string uniqueId)
        {
            return machineDefinitions.TryGetValue(uniqueId ?? string.Empty, out List<MachineDefinition>? definitions)
                ? definitions.ToArray()
                : Array.Empty<MachineDefinition>();
        }

        MachineProductionState IMachineProductionApi.GetState(string uniqueId)
        {
            if (machineStates.TryGetValue(uniqueId ?? string.Empty, out MachineProductionState state))
                return state;

            return new MachineProductionState
            {
                OwnerId = uniqueId ?? string.Empty,
                Status = "not-configured",
                LastMessage = "No machine definition registered."
            };
        }

        BridgeFeatureStatus IMachineProductionApi.GetStatus(string uniqueId)
        {
            return machineDefinitions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook", machineRuntimeLoopInstalled ? "Machine definitions are accepted and remain isolated from raw Doloc Town types; DTMAPI observes placed equipment and can deliver weighted outputs, while native electric/runtime UI remains experimental." : "Machine definitions are accepted and remain isolated from raw Doloc Town types; production, electric, optional fuel, placement-scale, and output-delivery hooks still need third-save evidence.")
                : new BridgeFeatureStatus("not-configured", "No machine definitions were registered for this mod.");
        }

        internal int GetMachineProductionCycleCountForSmoke(string ownerId)
        {
            return machineStates.TryGetValue(ownerId ?? string.Empty, out MachineProductionState state)
                ? state.ProductionCycleCount
                : 0;
        }

        internal string GetMachineProductionStateSummaryForSmoke(string ownerId)
        {
            if (!machineStates.TryGetValue(ownerId ?? string.Empty, out MachineProductionState state))
                return "not-configured";

            return "status=" + state.Status +
                ", machine=" + state.MachineId +
                ", equipment=" + state.EquipmentId +
                ", recipe=" + state.RecipeId +
                ", recipeGroup=" + state.RecipeGroupId +
                ", visualScale=" + state.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                ", electricOnly=" + (!state.AllowFuelMode && state.AllowElectricMode) +
                ", fuelMode=" + state.AllowFuelMode +
                ", defaultMode=" + state.DefaultMode +
                ", cycleMinutes=" + state.CycleMinutes +
                ", cycleTUs=" + state.CycleTUs +
                ", nextDueTUs=" + state.NextDueTotalTUs +
                ", fuel=" + (state.AllowFuelMode ? state.RemainingFuel + "/" + state.FuelCapacity : "disabled") +
                ", fuelOnlyCost=" + (state.AllowFuelMode ? state.FuelOnlyFuelCostPerCycle.ToString(CultureInfo.InvariantCulture) : "disabled") +
                ", electricFuelCost=" + (state.AllowFuelMode ? state.ElectricModeFuelCostPerCycle.ToString(CultureInfo.InvariantCulture) : "disabled") +
                ", electricPowerPerCycle=" + state.ElectricModePowerCostPerCycle +
                ", placed=" + state.PlacedMachineCount +
                ", cycles=" + state.ProductionCycleCount +
                ", output=" + state.LastOutputItemId +
                ", count=" + state.LastOutputCount +
                ", outputTarget=" + state.LastOutputTarget +
                ", storage=" + state.LastStorageFilledSlots + "/" + state.LastStorageCapacity +
                ", storageLineCapacity=" + state.LastStorageLineCapacity +
                ", lastMode=" + state.LastMode +
                ", lastFuelCost=" + state.LastFuelCost +
                ", lastPowerCost=" + state.LastElectricPowerCost +
                ", techTree={" + state.NativeTechTreeSummary + "}" +
                ", message=" + state.LastMessage;
        }

        private void UpdateMachineProduction(bool forcePoll = false)
        {
            if (machineDefinitions.Count == 0)
                return;
            if (!forcePoll && (DateTimeOffset.Now - lastMachineProductionPollAt).TotalSeconds < 0.5)
                return;

            lastMachineProductionPollAt = DateTimeOffset.Now;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            if (dolocApi == null || archive == null || currentRoom == null)
                return;

            int totalTus = GetCurrentTotalTus(archive);
            if (totalTus < 0)
                return;

            machineRuntimeLoopInstalled = true;
            int tuMinutes = Math.Max(1, GetCurrentTuMinutes(archive));
            bool forceDue = ForceMachineProductionDueForSmoke;
            bool newTu = forceDue || totalTus != lastMachineProductionTotalTus;
            if (newTu)
                lastMachineProductionTotalTus = totalTus;

            var allEquipments = EnumerateMachineCandidateEquipments(dolocApi, archive, currentRoom).ToArray();
            foreach (KeyValuePair<string, List<MachineDefinition>> ownerEntry in machineDefinitions.ToArray())
            {
                string ownerId = ownerEntry.Key;
                int placedCount = 0;
                int productionCount = machineStates.TryGetValue(ownerId, out MachineProductionState existingState) ? existingState.ProductionCycleCount : 0;
                string nativeTechTreeSummary = machineStates.TryGetValue(ownerId, out MachineProductionState existingTechState) ? existingTechState.NativeTechTreeSummary : string.Empty;

                foreach (MachineDefinition definition in ownerEntry.Value.ToArray())
                {
                    nativeTechTreeSummary = FirstText(EnsureNativeMachineTechRoute(definition), nativeTechTreeSummary);
                    object[] placed = allEquipments
                        .Where(equipment => string.Equals(ReadStringMember(equipment, "Name"), definition.EquipmentId, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    placedCount += placed.Length;

                    foreach (object equipment in placed)
                    {
                        string machineKey = BuildMachineRuntimeKey(ownerId, definition, equipment);
                        if (!machineRuntimeEntries.TryGetValue(machineKey, out MachineRuntimeEntry? entry))
                        {
                            int cycleTus = GetMachineCycleTus(definition, tuMinutes);
                            entry = new MachineRuntimeEntry
                            {
                                OwnerId = ownerId,
                                MachineId = definition.MachineId,
                                EquipmentId = definition.EquipmentId,
                                MachineKey = machineKey,
                                RemainingFuel = Math.Max(0, definition.FuelCapacity),
                                NextDueTotalTus = forceDue ? totalTus : totalTus + cycleTus,
                                LastObservedTotalTus = totalTus
                            };
                            machineRuntimeEntries[machineKey] = entry;
                        }

                        entry.LastObservedTotalTus = totalTus;
                        if (forceDue && totalTus < entry.NextDueTotalTus)
                            entry.NextDueTotalTus = totalTus;
                        string visualScaleSummary = TryApplyMachineVisualScale(definition, equipment);
                        if (!string.IsNullOrWhiteSpace(visualScaleSummary))
                            entry.LastVisualScaleSummary = visualScaleSummary;
                        if (!newTu || totalTus < entry.NextDueTotalTus)
                            continue;

                        int catchUpGuard = 0;
                        int catchUpCycleTus = GetMachineCycleTus(definition, tuMinutes);
                        while (totalTus >= entry.NextDueTotalTus && catchUpGuard++ < 96)
                        {
                            int dueAt = entry.NextDueTotalTus;
                            if (TryRunMachineProductionCycle(dolocApi, definition, equipment, entry, totalTus, tuMinutes, out string message))
                            {
                                entry.NextDueTotalTus = dueAt + catchUpCycleTus;
                                productionCount++;
                                MachineOutputRule output = entry.LastOutputRule ?? new MachineOutputRule();
                                MachineProductionState state = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                                ApplyMachineDefinitionState(state, definition, tuMinutes, entry);
                                state.RuntimeHookInstalled = true;
                                state.PlacedMachineCount = placedCount;
                                state.ProductionCycleCount = productionCount;
                                state.LastOutputItemId = entry.LastOutputItemId;
                                state.LastOutputDisplayName = output.DisplayName ?? string.Empty;
                                state.LastOutputCount = entry.LastOutputCount;
                                state.LastMachineKey = machineKey;
                                state.LastMode = entry.LastMode;
                                state.LastObservedTotalTUs = totalTus;
                                state.LastOutputTarget = entry.LastOutputTarget;
                                state.LastStorageFilledSlots = entry.LastStorageFilledSlots;
                                state.LastStorageCapacity = entry.LastStorageCapacity;
                                state.LastStorageLineCapacity = entry.LastStorageLineCapacity;
                                state.NativeTechTreeSummary = nativeTechTreeSummary;
                                state.Status = "configured-experimental-runtime-loop";
                                state.LastMessage = string.IsNullOrWhiteSpace(entry.LastVisualScaleSummary) ? message : message + " visual={" + entry.LastVisualScaleSummary + "}";
                                machineStates[ownerId] = state;
                                string fuelSummary = definition.AllowFuelMode ? " fuelCost=" + entry.LastFuelCost + " fuelRemaining=" + entry.RemainingFuel + "/" + definition.FuelCapacity : " fuel=disabled";
                                runtime.RuntimeMonitor.Log("MachineProduction cycle OK owner=" + ownerId + " machine=" + definition.MachineId + " equipment=" + definition.EquipmentId + " output=" + entry.LastOutputItemId + " count=" + entry.LastOutputCount + " mode=" + entry.LastMode + fuelSummary + " electricPowerCost=" + entry.LastElectricPowerCost + " dueAt=" + dueAt + " nextDue=" + entry.NextDueTotalTus + " totalTUs=" + totalTus + ".");
                                runtime.SetHookStatus("Machine.ProductionApi", "configured-experimental-runtime-loop", "DTMAPI runtime update catch-up -> equipment IContainer/LinearInventory", message + " dueAt=" + dueAt + ", nextDue=" + entry.NextDueTotalTus + ".");
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    MachineProductionState state = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                                    ApplyMachineDefinitionState(state, definition, tuMinutes, entry);
                                    state.RuntimeHookInstalled = true;
                                    state.PlacedMachineCount = placedCount;
                                    state.LastMachineKey = machineKey;
                                    state.LastMode = entry.LastMode;
                                    state.LastObservedTotalTUs = totalTus;
                                    state.NativeTechTreeSummary = nativeTechTreeSummary;
                                    state.Status = "configured-experimental-runtime-loop";
                                    state.LastMessage = (string.IsNullOrWhiteSpace(entry.LastVisualScaleSummary) ? message : message + " visual={" + entry.LastVisualScaleSummary + "}") + " Due retained at " + entry.NextDueTotalTus + ".";
                                    machineStates[ownerId] = state;
                                    runtime.SetHookStatus("Machine.ProductionApi", "configured-experimental-runtime-loop", "DTMAPI runtime update catch-up -> equipment IContainer/LinearInventory", state.LastMessage);
                                }
                                break;
                            }
                        }

                        if (catchUpGuard >= 96 && totalTus >= entry.NextDueTotalTus)
                        {
                            MachineProductionState state = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                            ApplyMachineDefinitionState(state, definition, tuMinutes, entry);
                            state.RuntimeHookInstalled = true;
                            state.PlacedMachineCount = placedCount;
                            state.LastMachineKey = machineKey;
                            state.LastMode = entry.LastMode;
                            state.LastObservedTotalTUs = totalTus;
                            state.NativeTechTreeSummary = nativeTechTreeSummary;
                            state.Status = "configured-experimental-runtime-loop";
                            state.LastMessage = "Machine catch-up reached the 96-cycle safety cap; due retained at " + entry.NextDueTotalTus + ".";
                            machineStates[ownerId] = state;
                            runtime.SetHookStatus("Machine.ProductionApi", "configured-experimental-runtime-loop", "DTMAPI runtime update catch-up safety cap", state.LastMessage);
                        }
                    }
                }

                MachineProductionState finalState = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                MachineDefinition? finalDefinition = ownerEntry.Value.FirstOrDefault();
                MachineRuntimeEntry? finalEntry = finalDefinition == null
                    ? null
                    : machineRuntimeEntries.Values.FirstOrDefault(entry => entry.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) && entry.MachineId.Equals(finalDefinition.MachineId, StringComparison.OrdinalIgnoreCase));
                if (finalDefinition != null)
                    ApplyMachineDefinitionState(finalState, finalDefinition, tuMinutes, finalEntry);
                finalState.RuntimeHookInstalled = true;
                finalState.PlacedMachineCount = placedCount;
                finalState.ProductionCycleCount = productionCount;
                finalState.LastObservedTotalTUs = totalTus;
                finalState.NativeTechTreeSummary = nativeTechTreeSummary;
                if (placedCount == 0 && string.IsNullOrWhiteSpace(finalState.LastMessage))
                    finalState.LastMessage = "Runtime loop active; no placed registered machine found in current/root/farm room candidates.";
                finalState.Status = "configured-experimental-runtime-loop";
                machineStates[ownerId] = finalState;
            }
        }

        private MachineProductionState GetMachineProductionStateForUpdate(string ownerId, int registeredMachineCount)
        {
            if (!machineStates.TryGetValue(ownerId, out MachineProductionState state))
            {
                state = new MachineProductionState
                {
                    OwnerId = ownerId,
                    IsConfigured = true
                };
            }

            state.RegisteredMachineCount = registeredMachineCount;
            return state;
        }

        private string TryApplyMachineVisualScale(MachineDefinition definition, object equipment)
        {
            if (definition.VisualScale <= 0 || Math.Abs(definition.VisualScale - 1d) < 0.01)
                return string.Empty;

            try
            {
                string actualEquipmentId = ReadStringMember(equipment, "Name", string.Empty);
                if (!actualEquipmentId.Equals(definition.EquipmentId, StringComparison.OrdinalIgnoreCase))
                {
                    return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                        ", skipped=equipment-id-mismatch actual=" + FirstText(actualEquipmentId, equipment.GetType().Name);
                }

                object? renderer = ReadMember(equipment, "Renderer");
                object? transform = renderer == null ? null : ReadMember(renderer, "transform");
                if (transform == null)
                    return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", renderer=pending";

                object? currentScale = ReadMember(transform, "localScale");
                double currentX = currentScale == null ? 1 : ReadVectorComponent(currentScale, "x");
                double currentY = currentScale == null ? 1 : ReadVectorComponent(currentScale, "y");
                double currentZ = currentScale == null ? 1 : ReadVectorComponent(currentScale, "z");
                double signedX = currentX < 0 ? -definition.VisualScale : definition.VisualScale;
                double z = Math.Abs(currentZ) < 0.001 || double.IsNaN(currentZ) ? 1 : currentZ;
                bool alreadyApplied = Math.Abs(Math.Abs(currentX) - definition.VisualScale) < 0.01 && Math.Abs(Math.Abs(currentY) - definition.VisualScale) < 0.01;
                if (!alreadyApplied)
                {
                    if (!TrySetTransformLocalScale(transform, signedX, definition.VisualScale, z))
                        return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", rendererScale=failed";
                }

                string summary = "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                    ", rendererScale=" + signedX.ToString("0.##", CultureInfo.InvariantCulture) + "x" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                    ", applied=" + (!alreadyApplied);
                runtime.SetHookStatus("Machine.VisualScale", "verified", "Equipment.Renderer.transform.localScale", definition.EquipmentId + " " + summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Machine visual scale failed for " + definition.EquipmentId + ".", ex.ToString());
                return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", failed=" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        internal void ResetEquipmentRendererScaleOnReuse(object renderer)
        {
            try
            {
                object? transform = renderer == null ? null : ReadMember(renderer, "transform");
                if (transform == null)
                    return;

                object? currentScale = ReadMember(transform, "localScale");
                double currentX = currentScale == null ? 1 : ReadVectorComponent(currentScale, "x");
                double currentY = currentScale == null ? 1 : ReadVectorComponent(currentScale, "y");
                double currentZ = currentScale == null ? 1 : ReadVectorComponent(currentScale, "z");
                if (IsNearScale(currentX, 1) && IsNearScale(currentY, 1) && IsNearScale(currentZ, 1))
                    return;

                if (TrySetTransformLocalScale(transform, 1, 1, 1))
                {
                    runtime.RuntimeMonitor.LogOnce("mine-renderer-onreuse-scale-reset", "EquipmentRenderer.OnReuse resets localScale to 1 so Mine visual scale cannot leak through renderer pooling.");
                    runtime.SetHookStatus("Machine.MineVisualContainment", "experimental", "EquipmentRenderer.OnReuse postfix", "rendererPoolScaleReset=True previous=" + FormatScale(currentX, currentY, currentZ));
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Equipment renderer scale reset failed.", ex.ToString());
            }
        }

        internal void ApplyMineBuilderPreviewScale(object builder, string reason)
        {
            try
            {
                string equipmentId = ResolveBuilderEquipmentId(builder);

                object? indicatorRenderer = builder == null ? null : ReadMember(builder, "indicatorRenderer");
                object? indicator = indicatorRenderer == null ? null : ReadMember(indicatorRenderer, "indicator");
                object? transform = ResolveBuilderIndicatorTransform(indicator);
                if (transform == null || string.IsNullOrWhiteSpace(equipmentId))
                    return;

                object? currentScale = ReadMember(transform, "localScale");
                double currentX = currentScale == null ? 1 : ReadVectorComponent(currentScale, "x");
                double currentY = currentScale == null ? 1 : ReadVectorComponent(currentScale, "y");
                double currentZ = currentScale == null ? 1 : ReadVectorComponent(currentScale, "z");
                double target = equipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ? 2d : 1d;
                double signedX = currentX < 0 ? -target : target;
                double z = Math.Abs(currentZ) < 0.001 || double.IsNaN(currentZ) ? 1 : currentZ;
                bool alreadyApplied = IsNearScale(Math.Abs(currentX), target) && IsNearScale(Math.Abs(currentY), target);
                if (!alreadyApplied && !TrySetTransformLocalScale(transform, signedX, target, z))
                    return;

                if (equipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                {
                    string summary = "previewScale=2, reason=" + reason + ", previous=" + FormatScale(currentX, currentY, currentZ) + ", applied=" + (!alreadyApplied);
                    runtime.RuntimeMonitor.LogOnce("mine-builder-preview-scale", "Mine placement preview scale applied. " + summary);
                    runtime.SetHookStatus("Machine.MinePreviewScale", "experimental", "EquipmentBuilder.CreateIndicator/TurnIndicator", summary);
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine builder preview scale failed.", ex.ToString());
            }
        }

        private static string ResolveBuilderEquipmentId(object? builder)
        {
            if (builder == null)
                return string.Empty;

            object? equipmentProto = ReadMember(builder, "equipmentProto");
            string id = ReadEquipmentProtoId(equipmentProto);
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            object? selectedItem = ReadMember(builder, "SelectedItem") ?? ReadMember(builder, "CurrentItem");
            id = selectedItem == null ? string.Empty : ReadEquipmentProtoId(ReadMember(selectedItem, "EquipmentProto"));
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            object? selectedContent = ReadMember(builder, "SelectedContent") ?? ReadMember(builder, "CurrentContent") ?? ReadMember(builder, "CheckedContent");
            return FirstText(
                selectedContent == null ? string.Empty : ReadStringMember(selectedContent, "Name"),
                selectedContent == null ? string.Empty : ReadEquipmentProtoId(ReadMember(selectedContent, "proto")),
                selectedContent == null ? string.Empty : ReadEquipmentProtoId(ReadMember(selectedContent, "Proto")));
        }

        private static string ReadEquipmentProtoId(object? proto)
        {
            if (proto == null)
                return string.Empty;
            return FirstText(
                ReadStringMember(proto, "Id"),
                ReadStringMember(proto, "id"),
                ReadStringMember(proto, "Name"),
                ReadStringMember(proto, "name"));
        }

        private static object? ResolveBuilderIndicatorTransform(object? indicator)
        {
            if (indicator == null)
                return null;

            object? transform = ReadMember(indicator, "transform");
            if (transform != null)
                return transform;

            object? gameObject = ReadMember(indicator, "gameObject");
            return gameObject == null ? null : ReadMember(gameObject, "transform");
        }

        internal string ProbeMachineVisualScaleContainmentForSmoke(string scaledEquipmentId)
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (currentRoom == null)
                    return "containment=pending, reason=missing-current-room";

                int total = 0;
                int mineScaled = 0;
                int nonMineScaled = 0;
                var samples = new List<string>();
                foreach (object equipment in EnumerateEquipments(currentRoom))
                {
                    string equipmentId = ReadStringMember(equipment, "Name", ReadStringMember(equipment, "Title", equipment.GetType().Name));
                    object? renderer = ReadMember(equipment, "Renderer");
                    object? transform = renderer == null ? null : ReadMember(renderer, "transform");
                    object? localScale = transform == null ? null : ReadMember(transform, "localScale");
                    if (localScale == null)
                        continue;

                    total++;
                    double x = ReadVectorComponent(localScale, "x");
                    double y = ReadVectorComponent(localScale, "y");
                    bool isMine = equipmentId.Equals(scaledEquipmentId, StringComparison.OrdinalIgnoreCase);
                    bool scaledToMine = IsNearScale(Math.Abs(x), 2) && IsNearScale(Math.Abs(y), 2);
                    bool scaledNonMine = !isMine && (!IsNearScale(Math.Abs(x), 1) || !IsNearScale(Math.Abs(y), 1));
                    if (isMine && scaledToMine)
                        mineScaled++;
                    if (scaledNonMine)
                        nonMineScaled++;
                    if (samples.Count < 6)
                        samples.Add(equipmentId + "=" + FormatScale(x, y, ReadVectorComponent(localScale, "z")));
                }

                string summary = "containment=True" +
                    ", contamination=" + (nonMineScaled > 0) +
                    ", total=" + total +
                    ", mineScaled=" + mineScaled +
                    ", nonMineScaled=" + nonMineScaled +
                    ", samples=" + string.Join("|", samples);
                runtime.SetHookStatus("Machine.MineVisualContainment", nonMineScaled == 0 ? "verified" : "failed", "current-room equipment renderer scale scan", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine visual containment probe failed.", ex.ToString());
                return "containment=failed, error=" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static bool TrySetTransformLocalScale(object transform, double x, double y, double z)
        {
            object? scale = CreateUnityVector3(x, y, z);
            return scale != null && SetMemberValue(transform, "localScale", scale);
        }

        private static bool IsNearScale(double value, double expected)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return false;
            return Math.Abs(value - expected) <= 0.05;
        }

        private static string FormatScale(double x, double y, double z)
        {
            return x.ToString("0.##", CultureInfo.InvariantCulture) + "x" +
                y.ToString("0.##", CultureInfo.InvariantCulture) + "x" +
                z.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void ApplyMachineDefinitionState(MachineProductionState state, MachineDefinition definition, int tuMinutes, MachineRuntimeEntry? entry)
        {
            state.MachineId = definition.MachineId;
            state.DisplayName = definition.DisplayName;
            state.ItemId = definition.ItemId;
            state.EquipmentId = definition.EquipmentId;
            state.RecipeId = definition.RecipeId;
            state.RecipeGroupId = definition.RecipeGroupId;
            state.VisualScale = definition.VisualScale;
            state.AllowFuelMode = definition.AllowFuelMode;
            state.AllowElectricMode = definition.AllowElectricMode;
            state.DefaultMode = definition.DefaultMode;
            state.FuelCapacity = definition.FuelCapacity;
            state.RemainingFuel = entry == null ? 0 : entry.RemainingFuel;
            state.FuelOnlyFuelCostPerCycle = definition.FuelOnlyFuelCostPerCycle;
            state.ElectricModeFuelCostPerCycle = definition.ElectricModeFuelCostPerCycle;
            state.ElectricModePowerCostPerCycle = definition.ElectricModePowerCostPerCycle;
            state.CycleMinutes = definition.CycleMinutes;
            state.CycleTUs = tuMinutes <= 0 ? 0 : GetMachineCycleTus(definition, tuMinutes);
            state.NextDueTotalTUs = entry == null ? -1 : entry.NextDueTotalTus;
            if (entry != null)
            {
                state.LastFuelCost = entry.LastFuelCost;
                state.LastElectricPowerCost = entry.LastElectricPowerCost;
                state.LastOutputTarget = entry.LastOutputTarget;
                state.LastStorageFilledSlots = entry.LastStorageFilledSlots;
                state.LastStorageCapacity = entry.LastStorageCapacity;
                state.LastStorageLineCapacity = entry.LastStorageLineCapacity;
            }
        }

        private bool TryRunMachineProductionCycle(Type dolocApi, MachineDefinition definition, object equipment, MachineRuntimeEntry entry, int totalTus, int tuMinutes, out string message)
        {
            message = string.Empty;
            entry.LastMode = definition.AllowElectricMode && definition.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ? "electric" : "fuel";
            int fuelCost = entry.LastMode.Equals("electric", StringComparison.OrdinalIgnoreCase)
                ? definition.ElectricModeFuelCostPerCycle
                : definition.FuelOnlyFuelCostPerCycle;
            entry.LastFuelCost = fuelCost;
            entry.LastElectricPowerCost = entry.LastMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ? definition.ElectricModePowerCostPerCycle : 0;

            if (fuelCost > 0 && entry.RemainingFuel < fuelCost)
            {
                message = "Machine " + definition.MachineId + " skipped production because DTMAPI fuel state is empty. mode=" + entry.LastMode + ", fuelRemaining=" + entry.RemainingFuel + ", fuelCost=" + fuelCost + ".";
                return false;
            }

            MachineOutputRule? selected = PickMachineOutput(definition);
            if (selected == null)
            {
                message = "Machine " + definition.MachineId + " skipped production because it has no positive output weights.";
                return false;
            }

            int count = machineRandom.Next(Math.Min(selected.MinCount, selected.MaxCount), Math.Max(selected.MinCount, selected.MaxCount) + 1);
            if (!TryConsumeMachineElectricPower(definition, equipment, entry, out string electricMessage))
            {
                message = electricMessage;
                return false;
            }

            if (!TryPlaceMachineOutput(dolocApi, definition, equipment, selected.ItemId, count, out string placementMessage, out string outputTarget, out int filledSlots, out int storageCapacity, out int storageLineCapacity))
            {
                message = "Machine " + definition.MachineId + " produced " + selected.ItemId + " x" + count + " but output placement failed: " + placementMessage;
                return false;
            }

            entry.RemainingFuel = Math.Max(0, entry.RemainingFuel - fuelCost);
            entry.LastOutputRule = selected;
            entry.LastOutputItemId = selected.ItemId;
            entry.LastOutputCount = count;
            entry.LastOutputTarget = outputTarget;
            entry.LastStorageFilledSlots = filledSlots;
            entry.LastStorageCapacity = storageCapacity;
            entry.LastStorageLineCapacity = storageLineCapacity;
            entry.ProductionCycleCount++;
            string costSummary = definition.AllowFuelMode
                ? "fuelCost=" + fuelCost + ", electricPowerCost=" + entry.LastElectricPowerCost
                : "fuel=disabled, electricPowerCost=" + entry.LastElectricPowerCost;
            message = "Machine " + definition.MachineId + " produced " + selected.ItemId + " x" + count + " via " + entry.LastMode + " mode; " + costSummary + ", " + electricMessage + ", " + placementMessage;
            return true;
        }

        private bool TryConsumeMachineElectricPower(MachineDefinition definition, object equipment, MachineRuntimeEntry entry, out string message)
        {
            message = string.Empty;
            if (!entry.LastMode.Equals("electric", StringComparison.OrdinalIgnoreCase) || definition.ElectricModePowerCostPerCycle <= 0)
                return true;

            object? component = ReadMember(equipment, "IElectronicComponent");
            string componentType = component == null ? "none" : component.GetType().FullName ?? component.GetType().Name;
            if (component == null)
            {
                message = "Machine " + definition.MachineId + " skipped production because the equipment has no official IElectronicComponent; expected EComProtoAppliance threshold=" + definition.ElectricModePowerCostPerCycle + ".";
                return false;
            }

            MethodInfo? launch = FindMethodInHierarchy(component.GetType(), "Launch", 0);
            if (launch == null)
            {
                message = "Machine " + definition.MachineId + " skipped production because official electric component " + componentType + " has no Launch() path.";
                return false;
            }

            object? launched = launch.Invoke(component, null);
            if (launched is bool ok && ok)
            {
                string powerInfo = ReadStringMember(component, "PowerInfo");
                message = "Official electric component Launch OK type=" + componentType + ", powerInfo=" + powerInfo + ".";
                return true;
            }

            message = "Machine " + definition.MachineId + " skipped production because official electric component Launch returned false (low power). type=" + componentType + ", powerInfo=" + ReadStringMember(component, "PowerInfo") + ", required=" + definition.ElectricModePowerCostPerCycle + ".";
            return false;
        }

        private bool TryPlaceMachineOutput(Type dolocApi, MachineDefinition definition, object equipment, string itemId, int count, out string message, out string outputTarget, out int filledSlots, out int storageCapacity, out int storageLineCapacity)
        {
            message = string.Empty;
            outputTarget = "unknown";
            filledSlots = 0;
            storageCapacity = 0;
            storageLineCapacity = 0;

            object? inventory = ReadMember(equipment, "inventory");
            if (inventory != null)
            {
                storageCapacity = ReadIntMember(inventory, "capacity", 0);
                filledSlots = ReadIntMember(inventory, "filledCount", 0);
                int emptySlots = ReadIntMember(inventory, "emptyCount", 0);
                storageLineCapacity = ReadIntMember(equipment, "lineCapacity", 0);
                if (storageCapacity <= 0)
                {
                    message = "Equipment inventory exists but reports no capacity.";
                    return false;
                }
                if (emptySlots < count)
                {
                    message = "Machine-owned storage is full. emptySlots=" + emptySlots + ", requestedSlots=" + count + ", filled=" + filledSlots + "/" + storageCapacity + ".";
                    outputTarget = "equipment-storage-full";
                    return false;
                }

                MethodInfo? contentFilter = FindMethodInHierarchy(equipment.GetType(), "ContentFilter", 1);
                MethodInfo? placeItemAt = inventory.GetType().GetMethod("PlaceItemAt", BindingFlags.Public | BindingFlags.Instance);
                if (placeItemAt == null)
                {
                    message = "LinearInventory.PlaceItemAt was not available on machine-owned storage.";
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                    {
                        message = "Could not generate output item for storage: " + itemReason + " " + itemMessage;
                        return false;
                    }
                    object? filterResult = contentFilter?.Invoke(equipment, new[] { item });
                    if (filterResult is bool allowed && !allowed)
                    {
                        message = "Machine-owned storage ContentFilter rejected " + itemId + ".";
                        return false;
                    }

                    int slot = ReadIntMember(inventory, "FirstEmptyIndex", -1);
                    if (slot < 0)
                    {
                        message = "Machine-owned storage had no empty slot during placement.";
                        return false;
                    }

                    object? leftover = placeItemAt.Invoke(inventory, new[] { slot, item });
                    if (leftover != null && ReadIntMember(leftover, "count", 1) > 0)
                    {
                        message = "LinearInventory.PlaceItemAt returned leftover item for " + itemId + " at slot " + slot + ".";
                        return false;
                    }
                }

                filledSlots = ReadIntMember(inventory, "filledCount", filledSlots);
                storageCapacity = ReadIntMember(inventory, "capacity", storageCapacity);
                storageLineCapacity = ReadIntMember(equipment, "lineCapacity", storageLineCapacity);
                outputTarget = "equipment-storage";
                message = "Stored " + itemId + " x" + count + " in machine-owned storage. filledSlots=" + filledSlots + "/" + storageCapacity + ", lineCapacity=" + storageLineCapacity + ".";
                return true;
            }

            if (definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
            {
                message = "dtmapi_mine is expected to be an IContainer/Case with machine-owned storage; backpack fallback is disabled for Mine.";
                outputTarget = "missing-equipment-storage";
                return false;
            }

            if (TryPlaceNativeItemInBackpack(dolocApi, itemId, count, out message))
            {
                outputTarget = "backpack";
                return true;
            }

            outputTarget = "backpack-failed";
            return false;
        }

        private MachineOutputRule? PickMachineOutput(MachineDefinition definition)
        {
            var weighted = new List<(MachineOutputRule Rule, double Weight)>();
            foreach (MachineOutputRule rule in definition.OutputRules ?? Array.Empty<MachineOutputRule>())
            {
                double weight = rule.Weight;
                if (rule.AllowProbabilityOverride &&
                    definition.ProbabilityOverrides != null &&
                    definition.ProbabilityOverrides.TryGetValue(rule.ItemId, out double overrideWeight))
                {
                    weight = overrideWeight;
                }

                if (weight > 0 && !string.IsNullOrWhiteSpace(rule.ItemId))
                    weighted.Add((rule, weight));
            }

            double total = weighted.Sum(entry => entry.Weight);
            if (total <= 0)
                return null;

            double roll = machineRandom.NextDouble() * total;
            double cursor = 0;
            foreach ((MachineOutputRule rule, double weight) in weighted)
            {
                cursor += weight;
                if (roll <= cursor)
                    return rule;
            }

            return weighted[weighted.Count - 1].Rule;
        }

        private static int GetCurrentTotalTus(object archive)
        {
            object? dateNow = ReadMember(archive, "DateNow");
            object? timeData = ReadMember(archive, "timeData");
            dateNow ??= timeData == null ? null : ReadMember(timeData, "dateNow");
            return dateNow == null ? -1 : ReadIntMember(dateNow, "TotalTUs", -1);
        }

        private static int GetCurrentTuMinutes(object archive)
        {
            object? timeData = ReadMember(archive, "timeData");
            object? dateConfig = timeData == null ? null : ReadMember(timeData, "dateConfig");
            if (dateConfig != null)
                return Math.Max(1, ReadIntMember(dateConfig, "TU2Min", 10));

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            object? globalDateConfig = globalParameter == null ? null : ReadMember(globalParameter, "DateConfig");
            return globalDateConfig == null ? 10 : Math.Max(1, ReadIntMember(globalDateConfig, "TU2Min", 10));
        }

        private static int GetMachineCycleTus(MachineDefinition definition, int tuMinutes)
        {
            tuMinutes = Math.Max(1, tuMinutes);
            return Math.Max(1, (int)Math.Ceiling(Math.Max(1, definition.CycleMinutes) / (double)tuMinutes));
        }

        private static string BuildMachineRuntimeKey(string ownerId, MachineDefinition definition, object equipment)
        {
            int index = ReadIntMember(equipment, "index", -1);
            if (index < 0)
                index = equipment.GetHashCode();
            object? room = ReadMember(equipment, "CurrentRoom") ?? ReadMember(equipment, "Host");
            string roomId = room == null ? "unknown-room" : FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "SceneRawName"), room.GetType().Name);
            return ownerId + "|" + definition.MachineId + "|" + definition.EquipmentId + "|" + roomId + "|" + index.ToString(CultureInfo.InvariantCulture);
        }

        private static MachineDefinition NormalizeMachineDefinition(IManifest owner, MachineDefinition? definition)
        {
            definition ??= new MachineDefinition();
            var normalized = new MachineDefinition
            {
                MachineId = FirstText(definition.MachineId, definition.EquipmentId, owner.UniqueID + ".machine"),
                DisplayName = FirstText(definition.DisplayName, definition.MachineId, definition.EquipmentId, "DTMAPI Machine"),
                ItemId = FirstText(definition.ItemId, definition.EquipmentId, definition.MachineId),
                EquipmentId = FirstText(definition.EquipmentId, definition.ItemId, definition.MachineId),
                RecipeId = FirstText(definition.RecipeId, definition.ItemId, definition.EquipmentId, definition.MachineId),
                RecipeGroupId = FirstText(definition.RecipeGroupId, "equipment_workbench"),
                VisualScale = ClampDouble(definition.VisualScale <= 0 ? 1 : definition.VisualScale, 0.25, 4),
                AllowFuelMode = definition.AllowFuelMode,
                AllowElectricMode = definition.AllowElectricMode,
                DefaultMode = FirstText(definition.DefaultMode, "fuel"),
                NativeTechTreeId = definition.NativeTechTreeId ?? string.Empty,
                NativeTechNodeId = definition.NativeTechNodeId ?? string.Empty,
                NativeTechNodeTitle = definition.NativeTechNodeTitle ?? string.Empty,
                NativeTechNodeDescription = definition.NativeTechNodeDescription ?? string.Empty,
                NativeTechNodeParentId = definition.NativeTechNodeParentId ?? string.Empty,
                NativeTechNodeAboveTitleContains = definition.NativeTechNodeAboveTitleContains ?? string.Empty,
                FuelCapacity = ClampInt(definition.FuelCapacity, 0, 999999),
                FuelOnlyFuelCostPerCycle = ClampInt(definition.FuelOnlyFuelCostPerCycle, 0, 999999),
                ElectricModeFuelCostPerCycle = ClampInt(definition.ElectricModeFuelCostPerCycle, 0, 999999),
                ElectricModePowerCostPerCycle = ClampInt(definition.ElectricModePowerCostPerCycle, 0, 999999),
                CycleMinutes = ClampInt(definition.CycleMinutes, 5, 1440),
                RecipeInputs = definition.RecipeInputs == null
                    ? Array.Empty<MachineRecipeInput>()
                    : definition.RecipeInputs
                        .Where(input => input != null && !string.IsNullOrWhiteSpace(input.ItemId) && input.Count > 0)
                        .Select(input => new MachineRecipeInput
                        {
                            ItemId = input.ItemId.Trim(),
                            Count = ClampInt(input.Count, 1, 9999)
                        })
                        .ToArray(),
                IncludeRuntimeModMinerals = definition.IncludeRuntimeModMinerals,
                VerboseLogging = definition.VerboseLogging
            };
            if (!normalized.AllowFuelMode && !normalized.AllowElectricMode)
                normalized.AllowElectricMode = true;
            if (!normalized.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase))
                normalized.DefaultMode = normalized.AllowFuelMode ? "fuel" : "electric";
            if (normalized.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) && !normalized.AllowElectricMode)
                normalized.DefaultMode = "fuel";
            if (normalized.DefaultMode.Equals("fuel", StringComparison.OrdinalIgnoreCase) && !normalized.AllowFuelMode)
                normalized.DefaultMode = "electric";
            if (!normalized.AllowFuelMode)
            {
                normalized.DefaultMode = "electric";
                normalized.FuelCapacity = 0;
                normalized.FuelOnlyFuelCostPerCycle = 0;
                normalized.ElectricModeFuelCostPerCycle = 0;
            }
            else if (normalized.FuelCapacity <= 0)
            {
                normalized.FuelCapacity = 1;
            }
            if (definition.OutputRules != null)
            {
                normalized.OutputRules = definition.OutputRules
                    .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.ItemId))
                    .Select(rule => new MachineOutputRule
                    {
                        ItemId = rule.ItemId,
                        DisplayName = rule.DisplayName ?? string.Empty,
                        Weight = ClampDouble(rule.Weight <= 0 ? 1 : rule.Weight, 0.0001, 100000),
                        MinCount = ClampInt(rule.MinCount, 1, 9999),
                        MaxCount = ClampInt(Math.Max(rule.MaxCount, rule.MinCount), 1, 9999),
                        Source = FirstText(rule.Source, "default"),
                        AllowProbabilityOverride = rule.AllowProbabilityOverride
                    })
                    .ToArray();
            }
            normalized.ProbabilityOverrides = definition.ProbabilityOverrides ?? new Dictionary<string, double>();
            return normalized;
        }

        private sealed class MachineRuntimeEntry
        {
            public string OwnerId { get; set; } = string.Empty;
            public string MachineId { get; set; } = string.Empty;
            public string EquipmentId { get; set; } = string.Empty;
            public string MachineKey { get; set; } = string.Empty;
            public int RemainingFuel { get; set; }
            public int NextDueTotalTus { get; set; }
            public int LastObservedTotalTus { get; set; } = -1;
            public int ProductionCycleCount { get; set; }
            public string LastMode { get; set; } = string.Empty;
            public int LastFuelCost { get; set; }
            public int LastElectricPowerCost { get; set; }
            public string LastOutputItemId { get; set; } = string.Empty;
            public int LastOutputCount { get; set; }
            public string LastOutputTarget { get; set; } = string.Empty;
            public int LastStorageFilledSlots { get; set; }
            public int LastStorageCapacity { get; set; }
            public int LastStorageLineCapacity { get; set; }
            public string LastVisualScaleSummary { get; set; } = string.Empty;
            public MachineOutputRule? LastOutputRule { get; set; }
        }
    }
}
