using System;

namespace DTMAPI.MonsterContent
{
    /// <summary>Configuration of existing template components. Null leaves native configuration unchanged.</summary>
    public sealed class MonsterNativeParameters
    {
        public MonsterAmoebaParameters? Amoeba { get; set; }
        public MonsterBastionParameters? Bastion { get; set; }
        public MonsterTrafficLightParameters? TrafficLight { get; set; }
        public MonsterNianParameters? Nian { get; set; }
        internal MonsterNativeParameters Copy() => new MonsterNativeParameters { Amoeba = Amoeba?.Copy(), Bastion = Bastion?.Copy(), TrafficLight = TrafficLight?.Copy(), Nian = Nian?.Copy() };
    }

    public sealed class MonsterAmoebaParameters
    {
        public float? HurtDuration { get; set; }
        public float? BackForce { get; set; }
        public bool? AddDebuff { get; set; }
        public string? DebuffId { get; set; }
        internal MonsterAmoebaParameters Copy() => (MonsterAmoebaParameters)MemberwiseClone();
    }

    public sealed class MonsterBastionParameters
    {
        public float? AnimationDelay { get; set; }
        internal MonsterBastionParameters Copy() => (MonsterBastionParameters)MemberwiseClone();
    }

    public sealed class MonsterTrafficLightParameters
    {
        public float? HoverHeight { get; set; }
        public float? HoverSpeedMin { get; set; }
        public float? HoverSpeedMax { get; set; }
        public float? ReadyShakeFrequency { get; set; }
        internal MonsterTrafficLightParameters Copy() => (MonsterTrafficLightParameters)MemberwiseClone();
    }

    public sealed class MonsterNianParameters
    {
        public float? DownTime { get; set; }
        public int? MaxChopCount { get; set; }
        public float? ToolAttack { get; set; }
        public float? DownDefense { get; set; }
        public float? BoomForceX { get; set; }
        public float? BoomForceY { get; set; }
        public float? FirecrackersImmunityTime { get; set; }
        public float? MaxDropDuration { get; set; }
        internal MonsterNianParameters Copy() => (MonsterNianParameters)MemberwiseClone();
    }

    internal static class NativeParameterValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition, MonsterNativeParameters? available = null, bool references = false)
        {
            var p = definition.NativeParameters; if (p == null) return null;
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("NativeParameterUnsupported", message, owner, owner + ":" + definition.LocalId, definition.TemplateId, references ? "ValidateReferences" : "Validate", "nativeParameters." + field);
            if (p.Amoeba != null)
            {
                if (references && available?.Amoeba == null) return Error("amoeba", "The selected template does not contain this configured component.");
                if (p.Amoeba.HurtDuration.HasValue && (!ContentValidation.Finite(p.Amoeba.HurtDuration.Value) || p.Amoeba.HurtDuration < 0)) return Error("amoeba.hurtDuration", "Expected a finite configuration value at least zero.");
                if (p.Amoeba.BackForce.HasValue && (!ContentValidation.Finite(p.Amoeba.BackForce.Value) || p.Amoeba.BackForce < 0)) return Error("amoeba.backForce", "Expected a finite configuration value at least zero.");
            }
            if (p.Bastion != null)
            {
                if (references && available?.Bastion == null) return Error("bastion", "The selected template does not contain this configured component.");
                if (p.Bastion.AnimationDelay.HasValue && (!ContentValidation.Finite(p.Bastion.AnimationDelay.Value) || p.Bastion.AnimationDelay < 0)) return Error("bastion.animationDelay", "Expected a finite configuration value at least zero.");
            }
            if (p.TrafficLight != null)
            {
                if (references && available?.TrafficLight == null) return Error("trafficLight", "The selected template does not contain this configured component.");
                if (p.TrafficLight.HoverHeight.HasValue && (!ContentValidation.Finite(p.TrafficLight.HoverHeight.Value) || p.TrafficLight.HoverHeight < 0)) return Error("trafficLight.hoverHeight", "Expected a finite configuration value at least zero.");
                if (p.TrafficLight.HoverSpeedMin.HasValue && (!ContentValidation.Finite(p.TrafficLight.HoverSpeedMin.Value) || p.TrafficLight.HoverSpeedMin < 0)) return Error("trafficLight.hoverSpeedMin", "Expected a finite configuration value at least zero.");
                if (p.TrafficLight.HoverSpeedMax.HasValue && (!ContentValidation.Finite(p.TrafficLight.HoverSpeedMax.Value) || p.TrafficLight.HoverSpeedMax < 0)) return Error("trafficLight.hoverSpeedMax", "Expected a finite configuration value at least zero.");
                if (p.TrafficLight.ReadyShakeFrequency.HasValue && (!ContentValidation.Finite(p.TrafficLight.ReadyShakeFrequency.Value) || p.TrafficLight.ReadyShakeFrequency < 0)) return Error("trafficLight.readyShakeFrequency", "Expected a finite configuration value at least zero.");
            }
            if (p.Nian != null)
            {
                if (references && available?.Nian == null) return Error("nian", "The selected template does not contain this configured component.");
                if (p.Nian.DownTime.HasValue && (!ContentValidation.Finite(p.Nian.DownTime.Value) || p.Nian.DownTime < 0)) return Error("nian.downTime", "Expected a finite configuration value at least zero.");
                if (p.Nian.MaxChopCount < 1) return Error("nian.maxChopCount", "Expected a positive native count.");
                if (p.Nian.ToolAttack.HasValue && (!ContentValidation.Finite(p.Nian.ToolAttack.Value) || p.Nian.ToolAttack < 0)) return Error("nian.toolAttack", "Expected a finite configuration value at least zero.");
                if (p.Nian.DownDefense.HasValue && (!ContentValidation.Finite(p.Nian.DownDefense.Value) || p.Nian.DownDefense < 0)) return Error("nian.downDefense", "Expected a finite configuration value at least zero.");
                if (p.Nian.BoomForceX.HasValue && (!ContentValidation.Finite(p.Nian.BoomForceX.Value))) return Error("nian.boomForceX", "Expected a finite configuration value.");
                if (p.Nian.BoomForceY.HasValue && (!ContentValidation.Finite(p.Nian.BoomForceY.Value))) return Error("nian.boomForceY", "Expected a finite configuration value.");
                if (p.Nian.FirecrackersImmunityTime.HasValue && (!ContentValidation.Finite(p.Nian.FirecrackersImmunityTime.Value) || p.Nian.FirecrackersImmunityTime < 0)) return Error("nian.firecrackersImmunityTime", "Expected a finite configuration value at least zero.");
                if (p.Nian.MaxDropDuration.HasValue && (!ContentValidation.Finite(p.Nian.MaxDropDuration.Value) || p.Nian.MaxDropDuration < 0)) return Error("nian.maxDropDuration", "Expected a finite configuration value at least zero.");
            }
            if (p.TrafficLight != null && (p.TrafficLight.HoverSpeedMin ?? available?.TrafficLight?.HoverSpeedMin) > (p.TrafficLight.HoverSpeedMax ?? available?.TrafficLight?.HoverSpeedMax)) return Error("trafficLight.hoverSpeedMax", "Maximum speed must not be below inherited minimum.");
            return null;
        }
    }
}
