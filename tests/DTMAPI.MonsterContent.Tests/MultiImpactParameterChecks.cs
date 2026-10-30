using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void MultiImpactParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            MultiImpact = new MonsterMultiImpactParameters { WindupEnabled = false, Distance = 6, TravelSpeed = 4, MinExtraDashes = 2, MaxExtraDashes = 4 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var original = Parameters(); var frozen = new DefinitionRecord("A", Make(original), 1).Definition;
        original.MultiImpact!.Distance = 80; original.MultiImpact.MinExtraDashes = 63;
        Check(frozen.NativeAttacks[0].MultiImpact!.Distance == 6 && frozen.NativeAttacks[0].MultiImpact!.MinExtraDashes == 2,
            "MultiImpact freezes geometry and extra-dash count before registration.");
        var catalog = AuthorCatalog(); var root = catalog.Templates[0]; root.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key,f.Value,"native-unit","native-read",true,true)).ToArray();
        var slot = new MonsterNativeAttackInfo("AtkNormal", "multi-impact", fields);
        root.Actions.NativeAttacks = new[] { slot }; root.Actions.NativeParts = new[] { new MonsterNativePartInfo("nian_head", new[] { slot }) };
        Check(fields.Length == 10 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Ten MultiImpact fields use their own capability, not Impact arc fields.");
        foreach (var edit in new Action<MonsterMultiImpactParameters>[] { p=>p.Distance=-1, p=>p.Distance=float.NaN,
            p=>p.TravelSpeed=0, p=>p.TravelSpeed=float.PositiveInfinity, p=>p.MinExtraDashes=0 })
        { var row=Parameters(); edit(row.MultiImpact!); Check(MonsterValidation.Validate("A",Make(row))?.Code=="InvalidNativeAttack", "Invalid dash geometry, duration or extra-count bound rejects before allocation."); }
        var zero = Parameters(); zero.WindupGameSeconds=0; zero.MultiImpact!.Distance=0; zero.MultiImpact.TravelSpeed=.1f;
        zero.MultiImpact.MinExtraDashes=63; zero.MultiImpact.MaxExtraDashes=1;
        Check(MonsterAdmission.Definition("A",Make(zero),catalog)==null, "Zero wait/displacement and reversed extra endpoints are valid; positive speed prevents undefined duration.");
        var empty=Parameters(); empty.MultiImpact=new MonsterMultiImpactParameters();
        Check(MonsterValidation.Validate("A",Make(empty))?.Code=="InvalidNativeAttack", "Empty MultiImpact intent rejects beside common fields.");
        var wrong=Parameters(); wrong.Impact=new MonsterImpactParameters { ArcPath=true };
        Check(MonsterAdmission.Definition("A",Make(wrong),catalog)?.Field=="nativeAttacks[0].impact.arcPath", "IImpact interface inheritance cannot grant unconsumed Arc/Path capability.");
        var nested=Definition(); nested.NativeParts.Add(new MonsterNativePartParameters { TemplateId="nian_head", NativeAttacks={Parameters()} });
        Check(MonsterAdmission.Definition("A",nested,catalog)==null,"Existing part contract admits head MultiImpact through the complete root.");
        var native=new MonsterNativeOverride { LocalId="multi",TargetId=root.Id,NativeAttacks={Parameters()} };
        var content=new ContentInput {OwnerId="A"}; content.Definitions.Add(frozen);content.Definitions.Add(nested);nested.LocalId="nested";content.NativeOverrides.Add(native);
        string payload=ContentCodec.WriteContent(content);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A",_=>Array.Empty<byte>()))==payload && ContentCompiler.Validate(content,CatalogCodec.Read(CatalogCodec.Write(catalog))).Count==0,
            "Compiler preserves direct/nested definitions and original overrides with all ten fields.");
        foreach (string bad in new[] { payload.Replace("\"minExtraDashes\":2","\"minExtraDashes\":2.5"), payload.Replace("\"windupEnabled\":false","\"windupEnabled\":0"),
            payload.Replace("\"maxExtraDashes\":4","\"arcPath\":true"),payload.Replace("\"maxExtraDashes\":4","\"shakeScreen\":true") })
        { try { ContentCodec.ReadContent(bad, "A",_=>Array.Empty<byte>()); Check(false,"Malformed MultiImpact payload must reject."); } catch(AuthoringException){Check(true,"Fractional counts, untyped flags and unsupported shape/effects reject.");} }
        var registry=new NativeOverrideRegistry(()=>{},(_,_)=>3,(_,_,_)=>null);registry.Refresh(true,1);
        var lease=registry.Register("A",1,native).Value!;native.NativeAttacks[0].MultiImpact!.MinExtraDashes=63;
        Check(registry.Resolve(root.Id,"nativeAttacks.AtkNormal.multiImpact.minExtraDashes",1)==2,"Native override freezes extra-dash endpoints.");
        var upper=new MonsterNativeOverride{LocalId="upper",TargetId=root.Id,NativeAttacks={new MonsterNativeAttackParameters { AttackId="AtkNormal",MultiImpact=new MonsterMultiImpactParameters {MaxExtraDashes=1}}}};
        var top=registry.Register("B",2,upper).Value!;
        Check(registry.Resolve(root.Id,"nativeAttacks.AtkNormal.multiImpact.maxExtraDashes",3)==1 && registry.Resolve(root.Id,"nativeAttacks.AtkNormal.multiImpact.minExtraDashes",1)==2,
            "Independent authors can form reversed endpoints without erasing lower leaves.");
        top.Dispose();Check(registry.Resolve(root.Id,"nativeAttacks.AtkNormal.multiImpact.maxExtraDashes",3)==4,"Leaf withdrawal restores lower upper endpoint.");
        lease.Dispose();Check(registry.Resolve(root.Id,"nativeAttacks.AtkNormal.multiImpact.minExtraDashes",7)==7,"Final withdrawal reads the contemporary native baseline.");
        root.Actions.NativeAttacks=new[]{new MonsterNativeAttackInfo("AtkNormal","multi-impact",fields.Select(f=>new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false)))};
        Check(MonsterAdmission.Definition("A",frozen,catalog)==null && MonsterOverrideAdmission.Validate("A",native,catalog)?.Code=="NativeAttackFieldUnsupported","Direct and native override admission remain independent.");
    }
}
