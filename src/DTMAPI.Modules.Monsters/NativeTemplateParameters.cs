using System;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static MonsterNativeParameters? DescribeTemplateParameters(Template template)
        {
            var prefab = template.Prefab;
            if (prefab == null) return null;
            var result = new MonsterNativeParameters(); bool found = false;
            var amoeba = prefab.GetComponent<MonsterDecoratorAmoeba>();
            if (amoeba != null)
            {
                found = true; result.Amoeba = new MonsterAmoebaParameters {
                    HurtDuration = NativeAccess.Get<float>(amoeba, "hurtDuration"), BackForce = NativeAccess.Get<float>(amoeba, "backForce"),
                    AddDebuff = NativeAccess.Get<bool>(amoeba, "isAddDebuff"), DebuffId = NativeAccess.Get<string>(amoeba, "debuffId") };
            }
            var bastion = prefab.GetComponent<MonsterDecoratorBastion>();
            if (bastion != null) { found = true; result.Bastion = new MonsterBastionParameters { AnimationDelay = NativeAccess.Get<float>(bastion, "animationDelay") }; }
            var light = prefab.GetComponent<MonsterDecoratorTrafficLight>();
            if (light != null)
            {
                found = true; var speed = NativeAccess.Get<Vector2>(light, "_hoverSpeedRange");
                result.TrafficLight = new MonsterTrafficLightParameters { HoverHeight = NativeAccess.Get<float>(light, "_hoverHeight"),
                    HoverSpeedMin = speed.x, HoverSpeedMax = speed.y, ReadyShakeFrequency = NativeAccess.Get<float>(light, "_readyShakeFrequency") };
            }
            var nian = prefab.GetComponent<MonsterDecoratorNian>();
            if (nian != null)
            {
                found = true; var config = NativeAccess.Get<NianConfig>(nian, "nianConfig");
                result.Nian = new MonsterNianParameters { DownTime = config.downTime, MaxChopCount = config.maxChopCount,
                    ToolAttack = config.toolAttack, DownDefense = config.downDefense, BoomForceX = config.boomForce.x, BoomForceY = config.boomForce.y,
                    FirecrackersImmunityTime = config.firecrackersImmunityTime, MaxDropDuration = NativeAccess.Get<float>(nian, "maxDropDuration") };
            }
            return found ? result : null;
        }

        private static void PrepareTemplateParameters(Binding binding, MonsterNativeParameters? parameters = null)
        {
            var p = parameters ?? binding.Definition.Definition.NativeParameters;
            if (p == null) return;
            void Set<T>(object component, string field, T? value) where T : struct
            { if (value.HasValue) NativeAccess.Set(component, field, value.Value); }
            if (p.Amoeba != null)
            {
                var component = binding.Prefab!.GetComponent<MonsterDecoratorAmoeba>();
                if (component == null) throw new InvalidOperationException("NativeParameterUnsupported: amoeba");
                Set(component, "hurtDuration", p.Amoeba.HurtDuration); Set(component, "backForce", p.Amoeba.BackForce);
                Set(component, "isAddDebuff", p.Amoeba.AddDebuff);
                if (p.Amoeba.DebuffId != null) NativeAccess.Set(component, "debuffId", p.Amoeba.DebuffId);
            }
            if (p.Bastion != null)
            {
                var component = binding.Prefab!.GetComponent<MonsterDecoratorBastion>();
                if (component == null) throw new InvalidOperationException("NativeParameterUnsupported: bastion");
                Set(component, "animationDelay", p.Bastion.AnimationDelay);
            }
            if (p.TrafficLight != null)
            {
                var component = binding.Prefab!.GetComponent<MonsterDecoratorTrafficLight>();
                if (component == null) throw new InvalidOperationException("NativeParameterUnsupported: trafficLight");
                Set(component, "_hoverHeight", p.TrafficLight.HoverHeight); Set(component, "_readyShakeFrequency", p.TrafficLight.ReadyShakeFrequency);
                var speed = NativeAccess.Get<Vector2>(component, "_hoverSpeedRange");
                NativeAccess.Set(component, "_hoverSpeedRange", new Vector2(p.TrafficLight.HoverSpeedMin ?? speed.x, p.TrafficLight.HoverSpeedMax ?? speed.y));
            }
            if (p.Nian != null)
            {
                var component = binding.Prefab!.GetComponent<MonsterDecoratorNian>();
                if (component == null) throw new InvalidOperationException("NativeParameterUnsupported: nian");
                // ScriptableObject configuration is shared by native prefabs. The definition
                // owns this copy, which its private pool instances may safely share.
                var config = Object.Instantiate(NativeAccess.Get<NianConfig>(component, "nianConfig"));
                binding.ParameterResources.Add(config); NativeAccess.Set(component, "nianConfig", config);
                config.downTime = p.Nian.DownTime ?? config.downTime; config.maxChopCount = p.Nian.MaxChopCount ?? config.maxChopCount;
                config.toolAttack = p.Nian.ToolAttack ?? config.toolAttack; config.downDefense = p.Nian.DownDefense ?? config.downDefense;
                config.boomForce = new Vector2(p.Nian.BoomForceX ?? config.boomForce.x, p.Nian.BoomForceY ?? config.boomForce.y);
                config.firecrackersImmunityTime = p.Nian.FirecrackersImmunityTime ?? config.firecrackersImmunityTime;
                Set(component, "maxDropDuration", p.Nian.MaxDropDuration);
            }
        }
    }
}
