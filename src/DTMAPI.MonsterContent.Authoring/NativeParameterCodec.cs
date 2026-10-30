using System;
using System.Collections.Generic;

namespace DTMAPI.MonsterContent.Authoring
{
    internal static class NativeParameterCodec
    {
        internal static MonsterNativeParameters? Read(object? value)
        {
            if (value == null) return null;
            var row = Json.Object(value, "nativeParameters", "", "amoeba bastion trafficLight nian");
            var result = new MonsterNativeParameters();
            if (row.TryGetValue("amoeba", out var amoeba) && amoeba != null)
            {
                var p = Json.Object(amoeba, "nativeParameters.amoeba", "", "hurtDuration backForce addDebuff debuffId");
                result.Amoeba = new MonsterAmoebaParameters {
                    HurtDuration = p.TryGetValue("hurtDuration", out var vHurtDuration) && vHurtDuration != null ? (float?)(float)Json.Number(vHurtDuration, "nativeParameters.amoeba.hurtDuration") : null,
                    BackForce = p.TryGetValue("backForce", out var vBackForce) && vBackForce != null ? (float?)(float)Json.Number(vBackForce, "nativeParameters.amoeba.backForce") : null,
                    AddDebuff = p.TryGetValue("addDebuff", out var vAddDebuff) && vAddDebuff != null ? (bool?)Json.Bool(vAddDebuff, "nativeParameters.amoeba.addDebuff") : null,
                    DebuffId = p.TryGetValue("debuffId", out var vDebuffId) && vDebuffId != null ? (string?)Json.Text(vDebuffId, "nativeParameters.amoeba.debuffId") : null,
                };
            }
            if (row.TryGetValue("bastion", out var bastion) && bastion != null)
            {
                var p = Json.Object(bastion, "nativeParameters.bastion", "", "animationDelay");
                result.Bastion = new MonsterBastionParameters {
                    AnimationDelay = p.TryGetValue("animationDelay", out var vAnimationDelay) && vAnimationDelay != null ? (float?)(float)Json.Number(vAnimationDelay, "nativeParameters.bastion.animationDelay") : null,
                };
            }
            if (row.TryGetValue("trafficLight", out var trafficLight) && trafficLight != null)
            {
                var p = Json.Object(trafficLight, "nativeParameters.trafficLight", "", "hoverHeight hoverSpeedMin hoverSpeedMax readyShakeFrequency");
                result.TrafficLight = new MonsterTrafficLightParameters {
                    HoverHeight = p.TryGetValue("hoverHeight", out var vHoverHeight) && vHoverHeight != null ? (float?)(float)Json.Number(vHoverHeight, "nativeParameters.trafficLight.hoverHeight") : null,
                    HoverSpeedMin = p.TryGetValue("hoverSpeedMin", out var vHoverSpeedMin) && vHoverSpeedMin != null ? (float?)(float)Json.Number(vHoverSpeedMin, "nativeParameters.trafficLight.hoverSpeedMin") : null,
                    HoverSpeedMax = p.TryGetValue("hoverSpeedMax", out var vHoverSpeedMax) && vHoverSpeedMax != null ? (float?)(float)Json.Number(vHoverSpeedMax, "nativeParameters.trafficLight.hoverSpeedMax") : null,
                    ReadyShakeFrequency = p.TryGetValue("readyShakeFrequency", out var vReadyShakeFrequency) && vReadyShakeFrequency != null ? (float?)(float)Json.Number(vReadyShakeFrequency, "nativeParameters.trafficLight.readyShakeFrequency") : null,
                };
            }
            if (row.TryGetValue("nian", out var nian) && nian != null)
            {
                var p = Json.Object(nian, "nativeParameters.nian", "", "downTime maxChopCount toolAttack downDefense boomForceX boomForceY firecrackersImmunityTime maxDropDuration");
                result.Nian = new MonsterNianParameters {
                    DownTime = p.TryGetValue("downTime", out var vDownTime) && vDownTime != null ? (float?)(float)Json.Number(vDownTime, "nativeParameters.nian.downTime") : null,
                    MaxChopCount = p.TryGetValue("maxChopCount", out var vMaxChopCount) && vMaxChopCount != null ? (int?)Json.Int(vMaxChopCount, "nativeParameters.nian.maxChopCount") : null,
                    ToolAttack = p.TryGetValue("toolAttack", out var vToolAttack) && vToolAttack != null ? (float?)(float)Json.Number(vToolAttack, "nativeParameters.nian.toolAttack") : null,
                    DownDefense = p.TryGetValue("downDefense", out var vDownDefense) && vDownDefense != null ? (float?)(float)Json.Number(vDownDefense, "nativeParameters.nian.downDefense") : null,
                    BoomForceX = p.TryGetValue("boomForceX", out var vBoomForceX) && vBoomForceX != null ? (float?)(float)Json.Number(vBoomForceX, "nativeParameters.nian.boomForceX") : null,
                    BoomForceY = p.TryGetValue("boomForceY", out var vBoomForceY) && vBoomForceY != null ? (float?)(float)Json.Number(vBoomForceY, "nativeParameters.nian.boomForceY") : null,
                    FirecrackersImmunityTime = p.TryGetValue("firecrackersImmunityTime", out var vFirecrackersImmunityTime) && vFirecrackersImmunityTime != null ? (float?)(float)Json.Number(vFirecrackersImmunityTime, "nativeParameters.nian.firecrackersImmunityTime") : null,
                    MaxDropDuration = p.TryGetValue("maxDropDuration", out var vMaxDropDuration) && vMaxDropDuration != null ? (float?)(float)Json.Number(vMaxDropDuration, "nativeParameters.nian.maxDropDuration") : null,
                };
            }
            return result;
        }
        internal static object? Write(MonsterNativeParameters? p)
        {
            if (p == null) return null;
            return Json.Map(
                "amoeba", p.Amoeba == null ? null : Json.Map("hurtDuration", p.Amoeba.HurtDuration, "backForce", p.Amoeba.BackForce, "addDebuff", p.Amoeba.AddDebuff, "debuffId", p.Amoeba.DebuffId),
                "bastion", p.Bastion == null ? null : Json.Map("animationDelay", p.Bastion.AnimationDelay),
                "trafficLight", p.TrafficLight == null ? null : Json.Map("hoverHeight", p.TrafficLight.HoverHeight, "hoverSpeedMin", p.TrafficLight.HoverSpeedMin, "hoverSpeedMax", p.TrafficLight.HoverSpeedMax, "readyShakeFrequency", p.TrafficLight.ReadyShakeFrequency),
                "nian", p.Nian == null ? null : Json.Map("downTime", p.Nian.DownTime, "maxChopCount", p.Nian.MaxChopCount, "toolAttack", p.Nian.ToolAttack, "downDefense", p.Nian.DownDefense, "boomForceX", p.Nian.BoomForceX, "boomForceY", p.Nian.BoomForceY, "firecrackersImmunityTime", p.Nian.FirecrackersImmunityTime, "maxDropDuration", p.Nian.MaxDropDuration));
        }
    }
}
