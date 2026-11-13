using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent.Hosting;

namespace DTMAPI.MonsterContent
{
    // Pure candidate metadata. It describes prerequisites, not current action success.
    public sealed class MonsterActionCapabilities
    {
        public MonsterActionCapabilities(IEnumerable<string> attacks, bool movement, string movementReason = "", string targetRequirement = "Current-room active native locked target")
        { AttackIds = Array.AsReadOnly(attacks.Distinct(StringComparer.Ordinal).ToArray()); ControlledMovement = movement; MovementReason = movementReason; TargetRequirement = targetRequirement; }
        public IReadOnlyList<string> AttackIds { get; }
        public IReadOnlyList<string> ScatterAttackIds { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<MonsterNativeAttackInfo> NativeAttacks { get; internal set; } = Array.Empty<MonsterNativeAttackInfo>();
        public IReadOnlyList<MonsterNativePartInfo> NativeParts { get; internal set; } = Array.Empty<MonsterNativePartInfo>();
        public bool Dash { get; internal set; }
        public bool ActionAnimation { get; internal set; }
        /// <summary>Native Attack/MoveTo may join health-phase handovers. Skill multipliers do not change native parameters.</summary>
        public bool PhasedNativeActions { get; internal set; }
        public string DashReason { get; internal set; } = "Dash capability has not been established by this catalog.";
        public bool ControlledMovement { get; }
        public string MovementReason { get; }
        public string TargetRequirement { get; }
        public string RuntimeConditions => "Target, range, cooldown, hurt lock, pause and path are checked at execution.";
    }
    public sealed class MonsterBehaviorRequirements
    {
        public bool Movement { get; set; }
        public List<string> AttackIds { get; set; } = new List<string>();
        internal MonsterBehaviorRequirements Copy() => new MonsterBehaviorRequirements { Movement = Movement, AttackIds = AttackIds.ToList() };
    }
    public sealed class MonsterBehaviorInfo
    {
        private readonly MonsterBehaviorRequirements? requirements;
        public MonsterBehaviorInfo(string id, string version, bool available, MonsterBehaviorRequirements? requirements)
        { Id = id; Version = version; Available = available; this.requirements = requirements?.Copy(); }
        public string Id { get; }
        public string Version { get; }
        public bool Available { get; }
        public MonsterBehaviorRequirements? Requirements => requirements?.Copy();
    }
    public sealed class MonsterCatalogObservation
    {
        public MonsterCatalogObservation(long? saveSessionEpoch, bool worldReady) { SaveSessionEpoch = saveSessionEpoch; WorldReady = worldReady; }
        public long? SaveSessionEpoch { get; }
        public bool WorldReady { get; }
    }
    /// <summary>Shared structural/reference checks; no native objects, publication or JSON.</summary>
    public static class MonsterAdmission
    {
        private static MonsterDiagnostic Error(string owner, string code, string field, string message, string id = "", string template = "") =>
            new MonsterDiagnostic(code, message, owner, id, template, "ValidateReferences", field, "Correct the input or refresh the catalog and active dependencies.");
        public static bool ValidLocalId(string id) => MonsterValidation.ValidId(id);
        public static MonsterDiagnostic? Definition(string owner, MonsterDefinition definition, MonsterContentCatalog catalog)
        {
            var structural = MonsterValidation.Validate(owner, definition);
            if (structural != null) return structural;
            if (definition.Compendium != null && (!Version.TryParse(catalog.ModuleVersion, out var compendiumVersion) || compendiumVersion < new Version(0, 1, 8)))
                return Error(owner, "CompendiumUnsupported", "compendium", "Refresh the catalog from Host 0.1.8 or later.", owner + ":" + definition.LocalId, definition.TemplateId);
            if ((definition.HealthPhases != null || definition.Display.BossBar) && (!Version.TryParse(catalog.ModuleVersion, out var version) || version < new Version(0, 1, 7)))
                return Error(owner, "HealthPhasesUnsupported", definition.HealthPhases != null ? "behavior.healthPhases" : "display.bossBar", "Refresh the catalog from Host 0.1.7 or later.", owner + ":" + definition.LocalId, definition.TemplateId);
            var composition = CompositionAdmission.Validate(owner, definition, catalog.Templates); if (composition != null) return composition;
            var template = catalog.Templates.FirstOrDefault(t => t.Id == definition.TemplateId);
            return DefinitionReferences(owner, definition,
                template == null ? null : CompositionAdmission.Project(template, definition, catalog.Templates),
                definition.Behavior == "code-provider" ? catalog.Behaviors.FirstOrDefault(b => b.Id == definition.BehaviorId && b.Available) : null,
                definition.Drops.ReferencesPool ? catalog.DropPools.FirstOrDefault(p => p.Source == definition.Drops.Source && p.Reference.Id == definition.Drops.PoolId) : null);
        }
        // Native publication can supply the three selected references without
        // materializing a public catalog. Both entrypoints use this validation core.
        internal static MonsterDiagnostic? Definition(string owner, MonsterDefinition definition, MonsterTemplate? template, MonsterBehaviorInfo? behavior, MonsterDropPoolInfo? pool)
        {
            var structural = MonsterValidation.Validate(owner, definition);
            return structural ?? DefinitionReferences(owner, definition, template, behavior, pool);
        }
        private static MonsterDiagnostic? DefinitionReferences(string owner, MonsterDefinition definition, MonsterTemplate? template, MonsterBehaviorInfo? behavior, MonsterDropPoolInfo? pool)
        {
            string id = owner + ":" + definition.LocalId;
            MonsterDiagnostic Fail(string code, string field, string message) => Error(owner, code, field, message, id, definition.TemplateId);
            if (template == null || template.Id != definition.TemplateId) return Fail("TemplateMissing", "templateId", "Template is absent from the current directory.");
            if (!template.CanInherit) { var d = template.Diagnostics.FirstOrDefault(); return Fail(d?.Code ?? "TemplateUnavailable", d?.Field ?? "templateId", d?.Message ?? "Template cannot be inherited."); }
            var physical = PhysicalValidation.Validate(owner, definition, template.Physical, true);
            if (physical != null) return physical;
            var attacks = NativeAttackValidation.Validate(owner, definition.LocalId, definition.TemplateId, definition.NativeAttacks, template.Actions.NativeAttacks);
            if (attacks != null) return attacks;
            var parts = NativePartValidation.Validate(owner, definition, template.Actions.NativeParts);
            if (parts != null) return parts;
            if (definition.Sounds?.Hurt != null && !template.SoundEvents.Contains("hurt"))
                return Fail("SoundUnsupported", "sounds.hurt", "This catalog has no supported native Hurt event/output backend for the template.");
            if (definition.Sounds?.Dead != null && !template.SoundEvents.Contains("dead"))
                return Fail("SoundUnsupported", "sounds.dead", "This catalog has no supported native Dead event/output backend for the template.");
            if (definition.Sounds?.Humming != null && !template.SoundEvents.Contains("humming"))
                return Fail("SoundUnsupported", "sounds.humming", "This catalog has no supported native Humming event/output backend for the template.");
            if (definition.Sounds?.RecoveryWarning != null && !template.SoundEvents.Contains("recoveryWarning"))
                return Fail("SoundUnsupported", "sounds.recoveryWarning", "This catalog has no supported native recovery warning event/output backend for the template.");
            bool needsPhasedNative = definition.HealthPhases != null &&
                (definition.Sequence?.Steps.Any(s => s.Action == "attack" || s.Action == "move-to") == true ||
                 behavior?.Requirements?.Movement == true || behavior?.Requirements?.AttackIds.Count > 0);
            if (needsPhasedNative && !template.Actions.PhasedNativeActions)
                return Fail("PhasedNativeActionsUnsupported", "behavior.healthPhases", "Refresh the catalog from Host 0.1.27 or later with phasedNativeActions capability. Native parameters are not affected by skill multipliers.");
            if (definition.HurtDuration.HasValue && !template.EditableFields.Contains("hurtDuration"))
                return Fail("DefinitionFieldUnsupported", "hurtDuration", "This catalog does not advertise hurt-duration editing; export the current Host catalog.");
            var parameters = NativeParameterValidation.Validate(owner, definition, template.NativeParameters, references: true);
            if (parameters != null) return parameters;
            var skin = SkinMappingAdmission.Validate(owner, definition, template.Presentation);
            if (skin != null) return skin;
            for (int i = 0; i < definition.Skills.Count; i++)
            {
                var skill = definition.Skills[i];
                if (skill.Animation != null && !template.Actions.ActionAnimation)
                    return Fail("ActionAnimationUnsupported", "behavior.skills[" + i + "].animation", "Catalog has not established the root Animator/SpriteRenderer override boundary.");
                if (skill.Kind == "dash" && !template.Actions.Dash)
                    return Fail("DashUnsupported", "behavior.skills[" + i + "].dash", template.Actions.DashReason);
                if (skill.Kind == "scatter" && !template.Actions.ScatterAttackIds.Contains(skill.Scatter!.ProjectileAttackId))
                    return Fail("ScatterUnsupported", "behavior.skills[" + i + "].scatter.projectileAttackId", "Template has no admitted linear projectile source with this attack ID.");
            }
            if (definition.Behavior == "configured-sequence" && definition.Sequence != null && definition.Combat?.Mode != "passive")
            {
                for (int i = 0; i < definition.Sequence!.Steps.Count; i++)
                {
                    var step = definition.Sequence.Steps[i]; string field = "behavior.sequence.steps[" + i + "]";
                    if (step.Action == "move-to" && !template.Actions.ControlledMovement) return Fail("MovementUnsupported", field + ".action", template.Actions.MovementReason);
                    if (step.Action == "attack" && !template.Actions.AttackIds.Contains(step.AttackId)) return Fail("AttackMissing", field + ".attackId", "Native attack is absent: " + step.AttackId);
                }
            }
            if (definition.Behavior == "code-provider")
            {
                if (behavior == null || behavior.Id != definition.BehaviorId || !behavior.Available) return Fail("BehaviorUnavailable", "behaviorId", "Register the behavior provider before its definitions.");
                var requirements = behavior.Requirements;
                if (requirements != null)
                {
                    if (requirements.Movement && !template.Actions.ControlledMovement) return Fail("MovementUnsupported", "behaviorId", template.Actions.MovementReason);
                    var missing = requirements.AttackIds.FirstOrDefault(a => !template.Actions.AttackIds.Contains(a));
                    if (missing != null) return Fail("AttackMissing", "behaviorId", "Required native attack is absent: " + missing);
                }
            }
            if (definition.Drops.ReferencesPool)
            {
                if (pool == null || pool.Reference.Id != definition.Drops.PoolId || pool.Source != definition.Drops.Source || (pool.Source == MonsterPoolSource.Managed && !pool.Reference.CanUse))
                    return Fail(definition.Drops.Source == MonsterPoolSource.Managed ? "DropPoolUnavailable" : "DropPoolMissing", "drops.poolId", "Referenced pool is absent or unavailable.");
            }
            return null;
        }
        public static MonsterDiagnostic? Pool(string owner, MonsterDropPool pool, MonsterContentCatalog catalog)
        {
            var structural = ContentValidation.Pool(owner, pool);
            if (structural != null) return structural;
            for (int i = 0; i < pool.Entries.Count; i++)
                if (!catalog.Items.Any(item => item.Id == pool.Entries[i].ItemId && item.CanUse))
                    return Error(owner, "ItemMissing", "entries[" + i + "].itemId", "Item is absent from the effective directory: " + pool.Entries[i].ItemId, owner + ":" + pool.LocalId);
            return null;
        }
        public static MonsterDiagnostic? Rule(string owner, MonsterSpawnRule rule, MonsterTemplate template, MonsterContentCatalog catalog, MonsterBodyParameters? body = null)
        {
            var structural = ContentValidation.Rule(owner, rule);
            if (structural != null) return structural;
            var room = catalog.Rooms.FirstOrDefault(r => r.Reference.Id == rule.RoomId);
            if (room == null) return Error(owner, "RoomMissing", "roomId", "Room prototype is absent from the directory.", owner + ":" + rule.LocalId);
            var regions = room.Regions.Where(r => r.GenerationType == template.GenerationType && (rule.RegionId.Length == 0 || rule.RegionId == r.Id)).ToArray();
            if (regions.Length == 0) return Error(owner, "SpawnRegionMissing", "regionId", "No region matches this template's generation type.", owner + ":" + rule.LocalId);
            int volume = PhysicalValidation.Volume(body, template.Volume);
            if (template.Volume <= 0 || volume <= 0 || regions.Sum(r => (long)r.Capacity / volume) < rule.Count)
                return Error(owner, "SpawnCapacityExceeded", "count", "Count and template volume exceed total native slots; remaining slots are a runtime condition.", owner + ":" + rule.LocalId);
            return null;
        }
    }
}
