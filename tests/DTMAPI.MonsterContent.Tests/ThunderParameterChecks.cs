using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ThunderParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill03", CooldownGameSeconds = 4,
            StartOnCooldown = false, Damage = 19, CriticalRate = 1, WindupGameSeconds = .6f,
            ThunderPile = new MonsterThunderPileParameters { WindupEnabled = true, Range = 100, MinCount = 3, MaxCount = 4,
                SubWindupGameSeconds = .2f, DropSpeed = 12, MaxDropGameSeconds = 2, WaitGameSeconds = .4f, FadeGameSeconds = .3f } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.ThunderPile!.MinCount = 60; input.ThunderPile.DropSpeed = 100;
        Check(frozen.NativeAttacks[0].ThunderPile!.MinCount == 3 && frozen.NativeAttacks[0].ThunderPile!.DropSpeed == 12, "Thunder registration freezes nested count and per-pile configuration.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key,f.Value,"native-unit","native-read",true,true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill03","thunder-pile",fields) };
        Check(fields.Length == 14 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Fourteen consumed Thunder fields admitted.");
        foreach (var change in new Action<MonsterThunderPileParameters>[] { p=>p.MinCount=0,p=>p.DropSpeed=0,p=>p.MaxDropGameSeconds=0,p=>p.WaitGameSeconds=0,p=>p.FadeGameSeconds=-1,p=>p.SubWindupGameSeconds=-1,p=>p.Range=-1,p=>p.DropSpeed=float.NaN,p=>p.WaitGameSeconds=float.PositiveInfinity })
        { var p=Parameters();change(p.ThunderPile!);Check(MonsterValidation.Validate("A",Make(p))?.Code=="InvalidNativeAttack","Out-of-range Thunder field rejects before native allocation."); }
        var empty=Parameters();empty.ThunderPile=new MonsterThunderPileParameters();Check(MonsterValidation.Validate("A",Make(empty))?.Code=="InvalidNativeAttack","Empty Thunder group rejects despite common fields.");
        var edge=Parameters();edge.ThunderPile=new MonsterThunderPileParameters { WindupEnabled=false,Range=0,MinCount=64,MaxCount=1,SubWindupGameSeconds=0,DropSpeed=1,MaxDropGameSeconds=.1f,WaitGameSeconds=.1f,FadeGameSeconds=0 };
        Check(MonsterAdmission.Definition("A",Make(edge),catalog)==null,"Native count fallback and effective lower bounds preserve explicit zero/false where valid.");
        var native=new MonsterNativeOverride { LocalId="thunder",TargetId=template.Id };native.NativeAttacks.Add(edge);
        var content=new ContentInput { OwnerId="A" };content.Definitions.Add(frozen);content.NativeOverrides.Add(native);string payload=ContentCodec.WriteContent(content);
        var reopened=ContentCodec.ReadContent(payload, "A",_=>Array.Empty<byte>());Check(ContentCodec.WriteContent(reopened)==payload&&ContentCompiler.Validate(reopened,CatalogCodec.Read(CatalogCodec.Write(catalog))).Count==0,"Thunder payload/catalog compiler roundtrip preserves both domains.");
        foreach(string bad in new[]{payload.Replace("\"minCount\":3","\"minCount\":3.5"),payload.Replace("\"dropSpeed\":12","\"dropSpeed\":\"12\""),payload.Replace("\"range\":100","\"strikeRadius\":100"),payload.Replace("\"windupEnabled\":true","\"windupEnabled\":1")})
        {try{ContentCodec.ReadContent(bad, "A",_=>Array.Empty<byte>());Check(false,"Malformed Thunder field must reject");}catch(AuthoringException){Check(true,"Strict integer/number/boolean and exact fields reject.");}}
        template.Actions.NativeAttacks=new[]{new MonsterNativeAttackInfo("AtkSkill03","thunder-pile",fields.Select(f=>new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false)))};
        Check(MonsterAdmission.Definition("A",frozen,catalog)==null&&MonsterOverrideAdmission.Validate("A",native,catalog)?.Code=="NativeAttackFieldUnsupported","Thunder author domains remain independently admitted.");
        var registry=new NativeOverrideRegistry(()=>{},(_,_)=>8,(_,_,_)=>null);registry.Refresh(true,1);var lease=registry.Register("A",1,native).Value!;edge.ThunderPile!.DropSpeed=80;
        Check(registry.Resolve(template.Id,"nativeAttacks.AtkSkill03.thunderPile.dropSpeed",8)==1,"Native parameter registration freezes effective speed minimum.");
        var higher=new MonsterNativeOverride {LocalId="speed",TargetId=template.Id};higher.NativeAttacks.Add(new MonsterNativeAttackParameters{AttackId="AtkSkill03",ThunderPile=new MonsterThunderPileParameters{DropSpeed=20}});var top=registry.Register("B",2,higher).Value!;
        Check(registry.Resolve(template.Id,"nativeAttacks.AtkSkill03.thunderPile.dropSpeed",8)==20&&registry.Resolve(template.Id,"nativeAttacks.AtkSkill03.thunderPile.fadeGameSeconds",8)==0,"Leaf arbitration changes speed and retains independent zero fade.");
        top.Dispose();Check(registry.Resolve(template.Id,"nativeAttacks.AtkSkill03.thunderPile.dropSpeed",8)==1,"Leaf withdrawal reveals lower declaration.");lease.Dispose();Check(registry.Resolve(template.Id,"nativeAttacks.AtkSkill03.thunderPile.dropSpeed",8)==8,"Final withdrawal reveals raw baseline.");
    }
}
