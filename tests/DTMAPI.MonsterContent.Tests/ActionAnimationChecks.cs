using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static MonsterDefinition AnimatedDefinition()
    {
        var d = ScatterDefinition();
        // Header-only grid dimensions are intentional for pure structural checks; native
        // PNG decode is verified separately with the original six-frame sample in game.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg==");
        png[19] = 6;
        d.Skills[0].Animation = new MonsterActionAnimation { Png = png, FrameWidth = 1, FrameHeight = 1, WindupFrames = new List<int>{0,1,2}, RecoveryFrames = new List<int>{3,4,5} };
        return d;
    }
    static void ActionAnimationChecks()
    {
        var d = AnimatedDefinition(); var a = d.Skills[0].Animation!; var s = d.Skills[0].Scatter!;
        Check(MonsterValidation.Validate("A",d)==null,"Scatter action atlas admitted structurally.");
        var publisher=new Publisher{Ready=true};var registry=new DefinitionRegistry(publisher,()=>{});
        Check(registry.Register("A",d).Success,"Ordinary API admits action animation.");
        a.Png[0]=0;a.WindupFrames[0]=5;a.PivotX=0;
        var frozen=publisher.Last!.Definition.Skills[0].Animation!;
        Check(frozen.Png[0]==137&&frozen.WindupFrames[0]==0&&frozen.PivotX==.5f,"Animation bytes, phase indices and pivot are frozen.");
        d=AnimatedDefinition();a=d.Skills[0].Animation!;
        foreach(var change in new Action<MonsterActionAnimation>[] { x=>x.FrameWidth=0,x=>x.FrameWidth=4,x=>x.FrameHeight=2,x=>x.PivotX=float.NaN,x=>x.PivotY=2,x=>x.PixelsPerUnit=0,x=>x.WindupFrames.Clear(),x=>x.RecoveryFrames=null!,x=>x.RecoveryFrames[0]=6,x=>x.WindupFrames[0]=-1,x=>x.Png[0]=0 })
        {var bad=AnimatedDefinition();change(bad.Skills[0].Animation!);Check(MonsterValidation.Validate("A",bad)!.Code=="InvalidActionAnimation","Unsafe animation rejected before publishing.");}
        var badTiming=AnimatedDefinition();badTiming.Skills[0].Scatter!.WindupGameSeconds=0;
        Check(MonsterValidation.Validate("A",badTiming)!.Code=="InvalidActionAnimation","Animation needs a positive authoritative windup.");
        var dash=DashDefinition();dash.Skills[0].Animation=a;
        Check(MonsterValidation.Validate("A",dash)!.Code=="InvalidActionAnimation","This first slice cannot silently animate dash.");
        Check(a.FrameAt(0,s,false,0)==0&&a.FrameAt(s.WindupGameSeconds/2,s,false,0)==1&&a.FrameAt(s.WindupGameSeconds,s,false,0)==2,"Pre-fire sampling never displays the firing frame.");
        float firedAt=s.WindupGameSeconds+.02f;
        Check(a.FrameAt(firedAt,s,true,firedAt)==3&&a.FrameAt(firedAt+s.RecoveryGameSeconds/2,s,true,firedAt)==4&&a.FrameAt(20,s,true,firedAt)==5,"Actual fire step starts recovery, with last frame held for the volley.");
        var catalog=AuthorCatalog();catalog.Templates[0].Actions.ScatterAttackIds=new[]{"AtkNormal"};
        Check(MonsterAdmission.Definition("A",d,catalog)!.Code=="ActionAnimationUnsupported","Older catalogs cannot grant animation support.");
        catalog.Templates[0].Actions.ActionAnimation=true;
        Check(MonsterAdmission.Definition("A",d,catalog)==null&&CatalogCodec.Read(CatalogCodec.Write(catalog)).Templates[0].Actions.ActionAnimation,"Capability survives the actual catalog codec.");
        var input=new ContentInput{OwnerId="A"};input.Definitions.Add(d);var text=ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(text, "A",_=>a.Png))==text,"All animation fields and image identity roundtrip.");
        foreach(var invalid in new[]{text.Replace("\"pivotX\":0.5","\"pivotX\":0.5,\"hitEvent\":true"),text.Replace("\"frameWidth\":1","\"frameWidth\":1,\"colliderScale\":2")})
        {try{ContentCodec.ReadContent(invalid, "A",_=>a.Png);throw new Exception("Expected rejection");}catch(AuthoringException){Check(true,"Unknown physical or damage-event fields cannot silently disappear.");}}
        try{ContentCodec.ReadContent(text, "A",_=>new byte[33]);throw new Exception("Expected rejection");}catch(AuthoringException){Check(true,"Payload image corruption is rejected.");}
    }
}
