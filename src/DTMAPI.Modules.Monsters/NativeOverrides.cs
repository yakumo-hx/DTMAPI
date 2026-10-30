using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DolocTown.Config;
using DolocTown.GameData;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule : IMonsterOverrideBackend, IMonsterOrderedDefinitionBackend
    {
        private readonly NativeOverrideRegistry nativeOverrides;
        private MonsterDiagnostic? overrideHookConflict;
        private static readonly IReadOnlyList<string> OverrideFields = Array.AsReadOnly(new[] { "defense", "hurtDuration" });
        private void InstallOverrideHooks()
        {
            Patch(AccessTools.PropertyGetter(typeof(MonsterProto), "Defense"), postfix: nameof(NativeDefenseRead));
            Patch(AccessTools.PropertyGetter(typeof(MonsterProto), "HurtDuration"), postfix: nameof(NativeHurtRead));
            Patch(AccessTools.PropertyGetter(typeof(MonsterProto), "DropSpawnEntry"), postfix: nameof(NativeDropEntryRead));
            Patch(AccessTools.Method(typeof(DolocTown.GuaranteedManager), "SpawnMonsterDropItems"), postfix: nameof(AppendNativeDrops));
            Patch(AccessTools.Method(typeof(DolocTown.DolocRecyclableObject), "OnRecycle"), prefix: nameof(RestoreProjectile));
            Patch(AccessTools.Method(typeof(DolocTown.DolocRecyclableObject), "OnReuse"), prefix: nameof(RestoreProjectile));
            InstallNativeAttackParameterHooks();
        }
        // Inspect at registration, native generation changes and explicit diagnostics. Unknown
        // Harmony writers are outside our field arbitration; never invent an ordering contract.
        private void CheckOverrideHooks()
        {
            overrideHookConflict = null;
            CheckNativeAttackParameterHooks();
            var dropOwners = Harmony.GetPatchInfo(AccessTools.PropertyGetter(typeof(MonsterProto), "DropSpawnEntry"))?.Owners.Where(o => o != HarmonyOwner).OrderBy(o => o).ToArray();
            dropHookConflict = dropOwners?.Length > 0 ? new MonsterDiagnostic("ExternalOverrideHookConflict", "DropSpawnEntry has unmanaged patches: " + string.Join(", ", dropOwners), field: "fields.drops") : null;
            foreach (string property in new[] { "Defense", "HurtDuration" })
            {
                var info = Harmony.GetPatchInfo(AccessTools.PropertyGetter(typeof(MonsterProto), property));
                var owners = info?.Owners.Where(o => o != HarmonyOwner).OrderBy(o => o, StringComparer.Ordinal).ToArray();
                if (owners != null && owners.Length != 0)
                {
                    overrideHookConflict = new MonsterDiagnostic("ExternalOverrideHookConflict", "Native getter " + property + " has unmanaged patches: " + string.Join(", ", owners), stage: "NativeOverride", field: property, recovery: "Coordinate the external patch and restart.");
                    break;
                }
            }
        }
        private MonsterDiagnostic? AdmitNativeOverride(string owner, string id, MonsterNativeOverride value)
        {
            if ((value.Defense.HasValue || value.HurtDuration.HasValue) && overrideHookConflict != null) return overrideHookConflict;
            Template? template;
            if (MonsterValidation.ValidQualifiedId(value.TargetId))
            {
                var target = bindings.Values.FirstOrDefault(b => !b.NativeChild && b.Definition.Id == value.TargetId);
                if (target == null) return new MonsterDiagnostic("WaitingNative", "The target definition is not currently published.", field: "targetId");
                template = target.Template;
            }
            else if (!templates.TryGetValue(value.TargetId, out template) || DolocConfig.Tables.TbMonster.GetOrDefault(value.TargetId) == null)
                return new MonsterDiagnostic("OverrideTargetMissing", "Target is absent from the current native directory.", field: "targetId");
            if (value.NativeAttacks.Count > 0)
            {
                if (value.NativeAttacks.Any(a => NativeAttackValidation.Fields(a).Any()) && nativeAttackHookConflict != null) return nativeAttackHookConflict;
                var body = new MonsterDefinition { NativeAttacks = value.NativeAttacks.Select(a => a.Copy()).ToList() };
                SoundValidation.ReleaseInputs(body);
                body.NativeAttacks = body.NativeAttacks.Where(a => NativeAttackValidation.Fields(a).Any()).ToList();
                var attackError = NativeAttackValidation.Validate("", value.LocalId, value.TargetId, body.NativeAttacks, DescribeNativeAttacks(template), nativeOverride: true);
                if (attackError != null) return attackError;
                if (value.NativeAttacks.Any(a => NativeAttackValidation.Fields(a).Any()) && !NativeAttackHoldersMatch(template)) return new MonsterDiagnostic("NativeAttackParameterHolderMismatch", "An existing native attack has different prototype holders; coordinate that writer first.", field: "fields.nativeAttacks");
            }
            if ((value.Defense.HasValue || value.HurtDuration.HasValue) && template.Errors.Count != 0)
                return new MonsterDiagnostic("NativeOverrideUnsupported", "Target does not satisfy the admitted common native boundary: " + template.Errors[0].Message, field: "targetId");
            if (value.Tint != null && !CanTintNative(template))
                return new MonsterDiagnostic("NativeOverrideUnsupported", "Target has no root SpriteRenderer for body tint.", field: "fields.tint");
            if (value.ProjectileTint != null && !CanTintProjectiles(template))
                return new MonsterDiagnostic("NativeOverrideUnsupported", "Target has no verified native bullet attack.", field: "fields.projectileTint");
            if (value.Drops != null && dropHookConflict != null) return dropHookConflict;
            if (value.Skin != null)
            {
                if (!CanSkinNative(template)) return new MonsterDiagnostic("NativeOverrideUnsupported", "Target has no verified root frame mapping.", field: "fields.skin");
                var mapping = SkinMappingAdmission.Validate("", value.LocalId, value.TargetId, value.Skin, PresentationInfo(template), "fields.skin.");
                if (mapping != null) return mapping;
            }
            try
            {
                if (value.Drops != null && value.Drops.Mode != MonsterDropMode.Inherit && !nativeDropEntries.ContainsKey(value.Drops))
                {
                    var error = ResolveDropEntry(value.Drops, template.Row!.DropSpawnEntry, owner, id, out var entry);
                    if (error != null) return error;
                    if (entry != null) nativeDropEntries.Add(value.Drops, entry);
                }
                if (value.Skin != null && !nativeSkinResources.ContainsKey(value.Skin))
                    nativeSkinResources.Add(value.Skin, CreateSkin(value.Skin, PresentationInfo(template), "native:" + value.TargetId + ":" + value.LocalId));
            }
            catch (Exception ex) { return new MonsterDiagnostic("NativeContentInvalid", ex.Message, field: "fields"); }
            return null;
        }
        private double? OverrideBaseline(string target, string field)
        {
            if (NativeAttackValidation.IsAttackField(field)) return NativeAttackBaseline(target, field);
            var row = DolocConfig.Tables?.TbMonster.GetOrDefault(target);
            return row == null ? (double?)null : field == "defense" ? row.Defense : row.HurtDuration;
        }
        private void RefreshNativeOverrides()
        { CheckOverrideHooks(); nativeOverrides.Refresh(Ready, generation); }
        MonsterResult<MonsterOverrideLease> IMonsterOverrideBackend.RegisterOverride(string owner, int loadIndex, MonsterNativeOverride value)
        {
            RequireThread();
            if (nativeStateFaulted) return MonsterResult<MonsterOverrideLease>.Fail(new MonsterDiagnostic("RestartRequired", "Native generation failed; restart the game.", owner));
            RefreshNativeOverrides();
            var result = nativeOverrides.Register(owner, loadIndex, value);
            if (!result.Success) RefreshNativeContent();
            Log("NativeOverride owner=" + owner + " id=" + value?.LocalId + " target=" + value?.TargetId + " loadIndex=" + loadIndex +
                " declarationOrder=" + result.Value?.Snapshot.DeclarationOrder + " accepted=" + result.Success + " diagnostic=" + result.Diagnostic?.Code);
            return result;
        }
        public IReadOnlyList<MonsterOverrideSnapshot> GetOverrides()
        { RequireThread(); RefreshNativeOverrides(); return nativeOverrides.Snapshots(); }
        private static void NativeDefenseRead(ref MonsterProto __instance, ref int __result)
        {
            var module = current;
            if (module != null && module.Ready && module.overrideHookConflict == null)
                __result = (int)module.nativeOverrides.Resolve(__instance.Name, "defense", __result);
        }
        private static void NativeHurtRead(ref MonsterProto __instance, ref float __result)
        {
            var module = current;
            if (module != null && module.Ready && module.overrideHookConflict == null)
                __result = (float)module.nativeOverrides.Resolve(__instance.Name, "hurtDuration", __result);
        }
    }
}
