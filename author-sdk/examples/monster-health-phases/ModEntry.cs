using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterPhasesCode
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterPhasesCode";
        readonly Dictionary<MonsterHandle, Behavior> behaviors = new Dictionary<MonsterHandle, Behavior>();
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("boss", () => new Behavior(behaviors))));
            owned.Register(Require(api.RegisterDefinition(Define())));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("phases", c => {
                var a = c.Arguments;
                if (a.Count == 3 && a[0] == "spawn") {
                    var s = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":boss", X = float.Parse(a[1], CultureInfo.InvariantCulture), Y = float.Parse(a[2], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + s.Handle);
                } else if (a.Count == 1 && a[0] == "clear") {
                    foreach(var s in Require(api.GetInstances())) Require(api.Despawn(s.Handle)); c.WriteLine("cleared");
                } else if (a.Count >= 1 && (a[0] == "cast" || a[0] == "cancel")) {
                    foreach(var s in Require(api.GetInstances())) c.WriteLine(behaviors[s.Handle].Invoke(a[0] == "cancel", a.Count > 1 ? a[1] : "fan", a.Count > 2 ? float.Parse(a[2], CultureInfo.InvariantCulture) : 10));
                } else if(a.Count==1 && a[0]=="status") {
                    foreach(var s in Require(api.GetInstances())) c.WriteLine(s.Health+"/"+s.MaxHealth+" phase="+s.HealthPhase!.Index+" pending="+s.HealthPhase.PendingId+" revision="+s.HealthPhase.Revision);
                } else c.WriteLine("phases spawn <x> <y> | cast <fan|dash> [budget] | cancel | status | clear");
            }, description:"Two-stage Boss through ordinary Strict DTO and action APIs.");
        }
        public static MonsterDefinition Define() => new MonsterDefinition {
            LocalId="boss",TemplateId="ball_drone",Health=100,Defense=0,Behavior="code-provider",BehaviorId=Owner+":boss",
            HealthPhases=new MonsterHealthPhases {Transition=MonsterPhaseTransition.FinishCurrent,Stages=new List<MonsterHealthPhase>{new MonsterHealthPhase{Id="enraged",AtOrBelow=.5f,SpeedMultiplier=1.5f,DamageMultiplier=2}}},
            Skills=new List<MonsterSkill>{
                new MonsterSkill{Id="fan",Scatter=new MonsterScatter{ProjectileAttackId="AtkNormal",BulletCount=1,SpreadDegrees=0,BulletSpeed=4,BulletLifetimeGameSeconds=2,Damage=11,Range=12,WindupGameSeconds=1.2f,RecoveryGameSeconds=.4f,CooldownGameSeconds=.2f}},
                new MonsterSkill{Id="dash",Kind="dash",Dash=new MonsterDash{Distance=2,Speed=4,Damage=11,Range=12,WindupGameSeconds=.5f,RecoveryGameSeconds=.4f,CooldownGameSeconds=.2f}}
            },Display=new MonsterDisplay{Name="阶段 Boss C#",Nameplate=true,HealthBar=MonsterHealthBar.Always,BossBar=true}
        };
        sealed class Behavior : IMonsterBehavior
        {
            readonly Dictionary<MonsterHandle,Behavior> registry; IMonsterBehaviorContext? context; MonsterHandle instance; MonsterActionHandle? action;
            public Behavior(Dictionary<MonsterHandle,Behavior> registry){this.registry=registry;}
            public void Start(IMonsterBehaviorContext value){context=value;instance=Require(value.GetSelf()).Handle;registry.Add(instance,this);}
            public void Tick(float gameSeconds) { }
            public string Invoke(bool cancel,string skill,float budget){
                if(context==null)return "StaleHandle";
                if(cancel){if(!action.HasValue)return "NoAction";var r=context.Cancel(action.Value);return r.Success?"cancelled="+r.Value:r.Diagnostic!.Code;}
                var cast=context.UseSkill(skill,budget);if(cast.Success){action=cast.Value;return "started="+action;}return cast.Diagnostic!.Code;
            }
            public void Stop(){context=null;registry.Remove(instance);}
        }
        static T Require<T>(MonsterResult<T> r)=>r.Success?r.Value!:throw new InvalidOperationException(r.Diagnostic!.Code+": "+r.Diagnostic.Message);
    }
}
