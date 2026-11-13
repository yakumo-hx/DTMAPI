namespace DTMAPI.MonsterContent
{
    internal static class ContentNativeParameterReader
    {
        internal static MonsterNativeParameters? Read(object? value, ContentJson json)
        {
            if (value == null) return null;
            var row = json.Object(value, "nativeParameters", "", "amoeba bastion trafficLight nian");
            var result = new MonsterNativeParameters();
            if (row.TryGetValue("amoeba", out var amoeba) && amoeba != null)
            {
                var p = json.Object(amoeba, "nativeParameters.amoeba", "", "hurtDuration backForce addDebuff debuffId");
                result.Amoeba = new MonsterAmoebaParameters {
                    HurtDuration = p.TryGetValue("hurtDuration", out var vHurtDuration) && vHurtDuration != null ? (float?)(float)json.Number(vHurtDuration, "nativeParameters.amoeba.hurtDuration") : null,
                    BackForce = p.TryGetValue("backForce", out var vBackForce) && vBackForce != null ? (float?)(float)json.Number(vBackForce, "nativeParameters.amoeba.backForce") : null,
                    AddDebuff = p.TryGetValue("addDebuff", out var vAddDebuff) && vAddDebuff != null ? (bool?)json.Bool(vAddDebuff, "nativeParameters.amoeba.addDebuff") : null,
                    DebuffId = p.TryGetValue("debuffId", out var vDebuffId) && vDebuffId != null ? (string?)json.Text(vDebuffId, "nativeParameters.amoeba.debuffId") : null,
                };
            }
            if (row.TryGetValue("bastion", out var bastion) && bastion != null)
            {
                var p = json.Object(bastion, "nativeParameters.bastion", "", "animationDelay");
                result.Bastion = new MonsterBastionParameters {
                    AnimationDelay = p.TryGetValue("animationDelay", out var vAnimationDelay) && vAnimationDelay != null ? (float?)(float)json.Number(vAnimationDelay, "nativeParameters.bastion.animationDelay") : null,
                };
            }
            if (row.TryGetValue("trafficLight", out var trafficLight) && trafficLight != null)
            {
                var p = json.Object(trafficLight, "nativeParameters.trafficLight", "", "hoverHeight hoverSpeedMin hoverSpeedMax readyShakeFrequency");
                result.TrafficLight = new MonsterTrafficLightParameters {
                    HoverHeight = p.TryGetValue("hoverHeight", out var vHoverHeight) && vHoverHeight != null ? (float?)(float)json.Number(vHoverHeight, "nativeParameters.trafficLight.hoverHeight") : null,
                    HoverSpeedMin = p.TryGetValue("hoverSpeedMin", out var vHoverSpeedMin) && vHoverSpeedMin != null ? (float?)(float)json.Number(vHoverSpeedMin, "nativeParameters.trafficLight.hoverSpeedMin") : null,
                    HoverSpeedMax = p.TryGetValue("hoverSpeedMax", out var vHoverSpeedMax) && vHoverSpeedMax != null ? (float?)(float)json.Number(vHoverSpeedMax, "nativeParameters.trafficLight.hoverSpeedMax") : null,
                    ReadyShakeFrequency = p.TryGetValue("readyShakeFrequency", out var vReadyShakeFrequency) && vReadyShakeFrequency != null ? (float?)(float)json.Number(vReadyShakeFrequency, "nativeParameters.trafficLight.readyShakeFrequency") : null,
                };
            }
            if (row.TryGetValue("nian", out var nian) && nian != null)
            {
                var p = json.Object(nian, "nativeParameters.nian", "", "downTime maxChopCount toolAttack downDefense boomForceX boomForceY firecrackersImmunityTime maxDropDuration");
                result.Nian = new MonsterNianParameters {
                    DownTime = p.TryGetValue("downTime", out var vDownTime) && vDownTime != null ? (float?)(float)json.Number(vDownTime, "nativeParameters.nian.downTime") : null,
                    MaxChopCount = p.TryGetValue("maxChopCount", out var vMaxChopCount) && vMaxChopCount != null ? (int?)json.Int(vMaxChopCount, "nativeParameters.nian.maxChopCount") : null,
                    ToolAttack = p.TryGetValue("toolAttack", out var vToolAttack) && vToolAttack != null ? (float?)(float)json.Number(vToolAttack, "nativeParameters.nian.toolAttack") : null,
                    DownDefense = p.TryGetValue("downDefense", out var vDownDefense) && vDownDefense != null ? (float?)(float)json.Number(vDownDefense, "nativeParameters.nian.downDefense") : null,
                    BoomForceX = p.TryGetValue("boomForceX", out var vBoomForceX) && vBoomForceX != null ? (float?)(float)json.Number(vBoomForceX, "nativeParameters.nian.boomForceX") : null,
                    BoomForceY = p.TryGetValue("boomForceY", out var vBoomForceY) && vBoomForceY != null ? (float?)(float)json.Number(vBoomForceY, "nativeParameters.nian.boomForceY") : null,
                    FirecrackersImmunityTime = p.TryGetValue("firecrackersImmunityTime", out var vFirecrackersImmunityTime) && vFirecrackersImmunityTime != null ? (float?)(float)json.Number(vFirecrackersImmunityTime, "nativeParameters.nian.firecrackersImmunityTime") : null,
                    MaxDropDuration = p.TryGetValue("maxDropDuration", out var vMaxDropDuration) && vMaxDropDuration != null ? (float?)(float)json.Number(vMaxDropDuration, "nativeParameters.nian.maxDropDuration") : null,
                };
            }
            return result;
        }
    }
}
