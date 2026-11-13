using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static bool IsParameterPart(Template template) =>
            (template.Proto.Name == "nian_head" || template.Proto.Name == "nian_tail") &&
            template.Errors.Count > 0 && template.Errors.All(e => e.Code == "EncounterPart") &&
            template.Prefab?.GetComponent<MonsterDecoratorNian>() is MonsterDecoratorNian decorator &&
            !NativeAccess.Get<bool>(decorator, "firstStage");

        private static MonsterNativePartInfo[] DescribeNativeParts(Template root)
        {
            if (current == null || root.Errors.Count != 0) return Array.Empty<MonsterNativePartInfo>();
            string[] ids;
            if (root.Prefab?.GetComponent<MonsterDecoratorNian>() is MonsterDecoratorNian decorator && NativeAccess.Get<bool>(decorator, "firstStage"))
                ids = new[] { "nian_head", "nian_tail" };
            else if (root.Prefab?.GetComponent<MonsterDecoratorSpaceShip>() is MonsterDecoratorSpaceShip ship)
                ids = new[] { NativeAccess.Get<string>(ship, "bastionId") };
            else return Array.Empty<MonsterNativePartInfo>();
            return ids.Where(id => current.templates.TryGetValue(id, out var part) && (part.Errors.Count == 0 || IsParameterPart(part)))
                .Select(id => { var part = current.templates[id]; return new MonsterNativePartInfo(id, DescribeNativeAttacks(part, encounterPart: true), PartSoundEvents(part)) {
                    Health = part.Row?.Health, Defense = part.Row?.Defense, HurtDuration = part.Row?.HurtDuration, NativeParameters = DescribeTemplateParameters(part) }; }).ToArray();
        }

        private void PreparePrivateParts(Binding root)
        {
            foreach (var value in root.Definition.Definition.NativeParts)
            {
                if (!value.Health.HasValue && !value.Defense.HasValue && !value.HurtDuration.HasValue && !value.Scale.HasValue && value.NativeParameters == null) continue;
                var template = templates[value.TemplateId]; var original = template.Row!;
                string id = root.Proto.Name + "__part_" + value.TemplateId;
                var row = new MonsterInfo(id, original.GenType, original.Size, original.Offset, original.Prefab,
                    value.Health ?? original.Health, value.Defense ?? original.Defense, value.HurtDuration ?? original.HurtDuration,
                    original.DropSpawnEntry, original.ExpValue, original.HummingSound, original.HurtSound, original.DeadSound, original.DeadEffects);
                var part = new Binding(root.Definition, template, row, DolocConfig.Tables.TbMonster, root.Database, root.Manager,
                    nativeChild: true, nativePartAttacks: value.NativeAttacks, privatePartId: id);
                if (part.Table.DataMap.ContainsKey(id) || NativeAccess.Dictionary(part.Database, "protos").Contains(id) || NativeAccess.Dictionary(part.Manager, "nestedPools").Contains(id))
                    throw new InvalidOperationException("Private encounter part identity is already occupied: " + id);
                root.PrivateParts.Add(value.TemplateId, part);
                part.Container = new GameObject(id); part.Container.SetActive(false); part.Container.transform.SetParent(DolocAPI.EntitySystem.root, false);
                part.Prefab = UnityEngine.Object.Instantiate(template.Prefab!, part.Container.transform); part.Prefab.name = id; part.Prefab.SetActive(false);
                if (value.NativeParameters != null) PrepareTemplateParameters(part, value.NativeParameters);
                part.Container.SetActive(true);
                part.PoolManager = new GameEntityManager<MonsterController>(part.Container.transform, part.Prefab, 5, 3, true, id);
                part.Table.DataMap.Add(id, row); part.Table.DataList.Add(row);
                if (!part.Database.AddMonster(part.Proto) || !part.Manager.ComposeManager(part.PoolManager, id))
                    throw new InvalidOperationException("Private encounter part publication failed: " + id);
                bindings.Add(id, part);
            }
        }

        private static void NativePartAttackCreating(Transform host, ref IMonsterAttackBehaviour proto)
        {
            if (current == null || host == null) return;
            var controller = host.GetComponent<MonsterController>();
            if (controller == null || !current.controllers.TryGetValue(controller, out var scope) ||
                scope.Binding.NativePartAttacks == null) return;
            foreach (var mapped in scope.Binding.NativePartAttacks) if (ReferenceEquals(mapped, proto)) return;
            var source = scope.Binding.Template.Proto.AttackBehaviourProtos;
            for (int i = 0; i < source.Length; i++)
                if (ReferenceEquals(source[i], proto)) { proto = scope.Binding.NativePartAttacks[i]; return; }
            throw new InvalidOperationException("NativePartAttackSourceMismatch: " + scope.Binding.Template.Proto.Name);
        }
    }
}
