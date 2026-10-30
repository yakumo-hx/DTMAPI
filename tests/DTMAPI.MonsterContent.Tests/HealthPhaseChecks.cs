using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static MonsterDefinition PhaseDefinition()
    {
        var d = ScatterDefinition(); d.Display.BossBar = true;
        d.HealthPhases = new MonsterHealthPhases { Stages = new List<MonsterHealthPhase> {
            new MonsterHealthPhase { Id = "alert", AtOrBelow = .7f, DamageMultiplier = 2, SpeedMultiplier = 2 },
            new MonsterHealthPhase { Id = "enraged", AtOrBelow = .3f, DamageMultiplier = 3, SpeedMultiplier = 1.5f } } };
        return d;
    }
    static void HealthPhaseChecks()
    {
        var d = PhaseDefinition();
        Check(MonsterValidation.Validate("A", d) == null, "Controlled finite phases admit through normal registration.");
        foreach (var edit in new Action<MonsterDefinition>[] {
            x => x.HealthPhases!.Stages.Clear(), x => x.HealthPhases!.Transition = (MonsterPhaseTransition)90,
            x => x.HealthPhases!.Stages[0].AtOrBelow = 1, x => x.HealthPhases!.Stages[0].AtOrBelow = float.NaN,
            x => x.HealthPhases!.Stages[1].AtOrBelow = .7f, x => x.HealthPhases!.Stages[1].AtOrBelow = 0,
            x => x.HealthPhases!.Stages[1].Id = "alert", x => x.HealthPhases!.Stages[0].SpeedMultiplier = float.PositiveInfinity,
            x => x.HealthPhases!.Stages[0].DamageMultiplier = 0 })
        { var bad = PhaseDefinition(); edit(bad); Check(MonsterValidation.Validate("A", bad)!.Code == "InvalidHealthPhases", "Invalid thresholds/scales reject before publication."); }
        var native = PhaseDefinition(); native.Sequence!.Steps[0] = new MonsterSequenceStep { Id = "attack", Action = "attack", AttackId = "AtkNormal", MaxGameSeconds = 5 };
        native.Skills.Clear();
        Check(MonsterValidation.Validate("A", native) == null, "Native steps may share phase handovers without skill parameter scaling.");
        var inherited = new MonsterDefinition { LocalId = "native", TemplateId = "test-template", HealthPhases = native.HealthPhases };
        Check(MonsterValidation.Validate("A", inherited)!.Code == "InvalidHealthPhases", "Native AI and controlled phase decisions still cannot run together.");
        var oldCatalog = AuthorCatalog();
        var phasedCatalog = new MonsterContentCatalog(1, "fixture", "0.1.27", "phased", oldCatalog.Templates, oldCatalog.Items, oldCatalog.DropPools, oldCatalog.Rooms) { Behaviors = oldCatalog.Behaviors };
        Check(MonsterAdmission.Definition("A", native, phasedCatalog)!.Code == "PhasedNativeActionsUnsupported", "A version number alone cannot grant the phase/native action boundary.");
        phasedCatalog.Templates[0].Actions.PhasedNativeActions = true;
        Check(MonsterAdmission.Definition("A", native, phasedCatalog) == null, "An explicit catalog capability admits native phase combinations.");
        var phaseInput = new ContentInput { OwnerId = "A" }; phaseInput.Definitions.Add(native);
        var nativePayload = ContentCodec.WriteContent(phaseInput);
        Check(ContentCompiler.Validate(ContentCodec.ReadContent(nativePayload, "A", _ => throw new Exception()), phasedCatalog).Count == 0, "Manual payload and direct definitions admit the same phased native attack.");
        string phasedJson = CatalogCodec.Write(phasedCatalog);
        Check(CatalogCodec.Read(phasedJson).Templates[0].Actions.PhasedNativeActions, "Catalog codec preserves the explicit phase/native capability.");
        Check(!CatalogCodec.Read(phasedJson.Replace("\"phasedNativeActions\":true,", "")).Templates[0].Actions.PhasedNativeActions, "Missing historical capability remains false on read.");
        var code = new MonsterDefinition { LocalId = "code-phases", TemplateId = "test-template", Behavior = "code-provider", BehaviorId = "Provider:patrol", HealthPhases = native.HealthPhases };
        Check(MonsterAdmission.Definition("A", code, phasedCatalog) == null, "Ordinary code requirements admit phased native actions.");
        phasedCatalog.Templates[0].Actions.PhasedNativeActions = false;
        Check(MonsterAdmission.Definition("A", code, phasedCatalog)!.Code == "PhasedNativeActionsUnsupported", "Code providers with native requirements also respect the capability gate.");
        var dash = PhaseDefinition(); dash.Skills[0] = new MonsterSkill { Id = "fan", Kind = "dash", Dash = new MonsterDash { Speed = 40 } };
        Check(MonsterValidation.Validate("A", dash) == null, "Scaled dash speed is an author choice without a platform ceiling.");
        var publisher = new Publisher(); var registry = new DefinitionRegistry(publisher, () => { }); registry.Register("Phase", d);
        d.HealthPhases!.Stages[0].DamageMultiplier = 4; d.Display.BossBar = false;
        var frozen = registry.Find("Phase", "Phase:scatter")!.Definition;
        Check(frozen.HealthPhases!.Stages[0].DamageMultiplier == 2 && frozen.Display.BossBar, "Registration freezes nested phase and Boss inputs.");
        var state = new HealthPhaseState(frozen.HealthPhases);
        state.Observe(71,100); Check(!state.Pending && state.Index == 0, "Above threshold stays base.");
        state.Observe(70,100); Check(state.Pending && state.ReachedIndex == 1, "Decimal equality at 70% crosses exactly.");
        Check(!state.Commit(70,true) && state.Index == 0, "An active action keeps old phase until retirement.");
        state.Observe(100,100); Check(state.Pending && state.Commit(100,false), "Healing never cancels an already reached pending phase.");
        var old = state.Scale(new MonsterDash { Speed = 8, Damage = 10 });
        for(int i=0;i<4;i++){state.Observe(50,100);state.Commit(50,false);}
        Check(state.Revision == 1 && state.Scale(new MonsterDash { Speed=8,Damage=10 }).Damage==20, "Repeated observations neither retrigger nor compound scales.");
        state.Observe(30,100); state.Commit(30,false);
        Check(state.Speed==1.5f && state.Damage==3 && old.Speed==16 && old.Damage==20, "Later stage replaces multipliers; running action snapshots stay unchanged.");
        var jump = new HealthPhaseState(frozen.HealthPhases); jump.Observe(10,100); jump.Commit(10,false);
        Check(jump.Index==2 && jump.ReachedIndex==2 && jump.Revision==1, "One hit across thresholds enters only the deepest phase once.");
        var dead = new HealthPhaseState(frozen.HealthPhases); dead.Observe(0,100);
        Check(dead.Index==0 && dead.ReachedIndex==0 && !dead.Commit(0,false), "Lethal health never triggers phases.");
        dead.Observe(60,100); Check(!dead.Commit(0,false), "Death also wins over a previously pending transition.");
        var other = new HealthPhaseState(frozen.HealthPhases); other.Observe(100,100);
        Check(other.Index==0 && other.Revision==0 && state.Index==2, "Another logical encounter has independent base state.");
        var input = new ContentInput { OwnerId="A" }; input.Definitions.Add(PhaseDefinition());
        string payload=ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A",_=>throw new Exception()))==payload, "Phases and Boss display roundtrip through strict runtime payload.");
        foreach(string bad in new[]{payload.Replace("CancelCurrent","unknown"),payload.Replace("\"atOrBelow\":0.7","\"atOrBelow\":0.7,\"resetOnHeal\":true")})
        {try {ContentCodec.ReadContent(bad, "A",_=>throw new Exception());throw new Exception("Expected rejection");}catch(AuthoringException){Check(true,"Unknown phase intent is never erased or defaulted.");}}
        Check(MonsterAdmission.Definition("A",PhaseDefinition(),AuthorCatalog())!.Code=="HealthPhasesUnsupported","Old catalogs cannot grant the new capability.");
        var context=new CombatContext{Target=1}; var sequence=new ConfiguredSequence(ScatterDefinition().Sequence!);
        sequence.Start(context);sequence.Tick(.1f);context.ActionState=MonsterActionState.PhaseChanged;sequence.Tick(.1f);
        Check(context.Starts==1 && sequence.Snapshot.State==MonsterSequenceState.Running,"Phase cancel retires the token without stopping the configured sequence.");
        sequence.Tick(.1f);Check(context.Starts==2,"Next tick retries current step through ordinary admission.");
        sequence.Cancel("Cancelled");context.ActionState=MonsterActionState.PhaseChanged;sequence.Tick(1);
        Check(context.Starts==2 && sequence.Snapshot.State==MonsterSequenceState.Stopped,"A phase change cannot revive explicit cancellation.");
        var combat=new CombatSequence(ScatterDefinition().Sequence!,new MonsterCombatPolicy());context=new CombatContext{Target=1};combat.Start(context);
        CombatTick(combat,.1f);CombatTick(combat,.1f);context.ActionState=MonsterActionState.PhaseChanged;CombatTick(combat,.1f);
        Check(context.Starts==1 && combat.CombatSnapshot.State==MonsterCombatState.Running,"Combat observes phase retirement once per tick.");
        CombatTick(combat,.1f);Check(context.Starts==2,"Combat resumes the interrupted skill without a second scheduler.");
    }
}
