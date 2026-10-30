using System;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.AutoFishingShortProbe
{
    // Bounded, opt-in local acceptance observer. Never distributed with the player product.
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private static ModEntry? self;
        private readonly Harmony harmony = new Harmony("DTMAPI.Internal.AutoFishingShortProbe.Native");
        private IDtmHelper? helper;
        private StreamWriter? output;
        private string commandPath = "";
        private float nextPoll;
        private float deadline;
        private string lastPhase = "";
        private bool loaded, frozen;
        private bool oldGlobalPause;
        private bool finishing;
        private float quitAt;
        private int stableHeld, stableMissed, bonusClicks, bonusRepeated, redLeaks, lastBonus;
        private bool miniEnded;
        private static readonly FieldInfo Note = AccessTools.Field(typeof(FishingGameScrollBar), "currentNote");
        private static readonly FieldInfo Started = AccessTools.Field(typeof(FishingGameScrollBar), "startTime");
        private static readonly FieldInfo Score = AccessTools.Field(typeof(FishingGameScrollBar), "currentScore");
        private static readonly FieldInfo Stamina = AccessTools.Field(typeof(FishingGameScrollBar), "fishStamina");
        private object? product;
        private FieldInfo? productConfig, productLoop;

        public override void Entry(IDtmHelper value)
        {
            self = this; helper = value;
            string stateRoot = Path.GetDirectoryName(Path.GetDirectoryName(value.Diagnostics.GetLatestLogPath()))!;
            string evidence = Path.Combine(stateRoot, "evidence", "AUTOFISHING-REBUILD", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(evidence);
            output = new StreamWriter(Path.Combine(evidence,"observations.log")) { AutoFlush = true };
            commandPath = Path.Combine(stateRoot, "autofishing-short-probe.txt");
            if (File.Exists(commandPath)) File.Delete(commandPath);
            deadline = Time.realtimeSinceStartup + 900f;
            Patch(typeof(FishRodRenderer), "SetPower", null, nameof(Power));
            Patch(typeof(FishingGameScrollBar), "StartGame", null, nameof(MiniStart));
            Patch(typeof(FishingGameScrollBar), "UpdateGame", null, nameof(MiniUpdate));
            Patch(typeof(DolocAPI), "SaveGame", nameof(NoSave), null);
            helper.Events.GameLoop.UpdateTicked += Tick;
            Write("probe-ready; slot=5/index=4; deadline=900s; evidence="+evidence);
        }
        private void Patch(Type type, string method, string? prefix, string? postfix)
        {
            harmony.Patch(AccessTools.DeclaredMethod(type,method), prefix == null ? null : new HarmonyMethod(typeof(ModEntry),prefix),
                postfix == null ? null : new HarmonyMethod(typeof(ModEntry),postfix){priority=Priority.Last});
        }
        private void Write(string value) { string line=DateTime.Now.ToString("O")+" t="+Time.time.ToString("F3")+" "+value; output?.WriteLine(line); Monitor.Log("AF short probe: "+value); }
        private void Tick(object? sender, UpdateTickedEventArgs e)
        {
            try
            {
                if (finishing) { if(Time.realtimeSinceStartup >= quitAt) Application.Quit(); return; }
                if(Time.realtimeSinceStartup >= deadline) { Write("deadline-exit"); Finish(); return; }
                if(Time.realtimeSinceStartup < nextPoll) return;
                nextPoll=Time.realtimeSinceStartup+.25f;
                if(product==null)
                {
                    Type? type=Type.GetType("Yuuka.DTMAPI.AutoFishing.NativeFishing, Yuuka.DTMAPI.AutoFishing");
                    product=type?.GetField("instance",BindingFlags.Static|BindingFlags.NonPublic)?.GetValue(null);
                    productConfig=type?.GetField("config",BindingFlags.Instance|BindingFlags.NonPublic);
                    productLoop=type?.GetField("loop",BindingFlags.Instance|BindingFlags.NonPublic);
                }
                if(DolocAPI.IsDataLoaded && DolocAPI.agent != null && !DolocAPI.agent.Pause)
                {
                    if(!loaded)
                    {
                        if(DolocAPI.archiveHandle.archiveIndex != 4) throw new InvalidOperationException("Unexpected archive; only native index 4 is authorized.");
                        loaded=true; oldGlobalPause=DolocAPI.gameLoop.IsGlobalPaused; DolocAPI.gameLoop.IsGlobalPaused=true; frozen=true;
                        Write("world-ready; slot="+DolocAPI.archiveHandle.archiveIndex+"; position="+DolocAPI.agent.transform.position+"; selected="+DolocAPI.SelectedItem?.name+"; native-time-paused");
                    }
                    string phase=DolocAPI.agent.StateManager.current.GetType().Name;
                    if(phase!=lastPhase){lastPhase=phase;Write("phase="+phase+"; enabled="+Enabled()+"; hook="+DolocAPI.agent.fishRodRenderer.FishHookPosition);}
                }
                if(!File.Exists(commandPath)) return;
                string command=File.ReadAllText(commandPath).Trim(); File.Delete(commandPath);
                string[] args=command.Split(' ');
                Write("command="+command);
                if(args[0]=="settings")
                {
                    object config=productConfig!.GetValue(product)!;
                    string[] names={"InstantBite","SkipMiniGame","FastAnimations","CastChargeRatio","AnimationMultiplier"};
                    for(int i=0;i<names.Length;i++){var p=config.GetType().GetProperty(names[i])!;p.SetValue(config,Convert.ChangeType(args[i+1],p.PropertyType,System.Globalization.CultureInfo.InvariantCulture));}
                    Write("settings-applied-to-current-product");
                }
                else if(args[0]=="toggle") { product!.GetType().GetMethod("Toggle")!.Invoke(product,null); Write("direct-product-toggle; enabled="+Enabled()); }
                else if(args[0]=="status") Write("enabled="+Enabled()+"; phase="+lastPhase+"; native-normal="+DolocAPI.IsNormalState);
                else if(args[0]=="config") helper!.UI.OpenConfigPage("Yuuka.DTMAPI.AutoFishing");
                else if(args[0]=="quit") Finish();
            }
            catch(Exception ex){Write("PROBE-ERROR "+ex);Finish();}
        }
        private bool Enabled(){object? loop=productLoop?.GetValue(product);return loop!=null && (bool)loop.GetType().GetProperty("Enabled")!.GetValue(loop)!;}
        private static void Power(float value) => self?.Write("native-cast-power="+value.ToString("F4"));
        private static bool NoSave(ref bool __result){self?.Write("FAIL: native SaveGame attempted");__result=false;return false;}
        private static void MiniStart(FishingGameScrollBar __instance)
        {
            if(self==null)return;
            self.stableHeld=self.stableMissed=self.bonusClicks=self.bonusRepeated=self.redLeaks=0;self.lastBonus=-1;self.miniEnded=false;
            self.Write("native-minigame-start; valid="+__instance.IsValid+"; score="+Score.GetValue(__instance)+"; stamina="+Stamina.GetValue(__instance));
        }
        private static void MiniUpdate(FishingGameScrollBar __instance)
        {
            if(self==null || !__instance.IsValid)return;
            if(__instance.CurrentGameStatus!=FishingGameController.GameStatus.Running)
            {
                if(!self.miniEnded){self.miniEnded=true; self.Write("native-minigame-result="+__instance.CurrentGameStatus+"; duration="+(Time.time-(float)Started.GetValue(__instance)!).ToString("F3")+"; greenHeld="+self.stableHeld+"; greenMissed="+self.stableMissed+"; bonusClicks="+self.bonusClicks+"; bonusRepeated="+self.bonusRepeated+"; redLeaks="+self.redLeaks);}
                return;
            }
            var note=(FishingNoteData?)Note.GetValue(__instance);
            if(note==null || note.noteType==FishingNoteType.Delay)return;
            float t=Time.time-(float)Started.GetValue(__instance)!;
            bool press=DolocAPI.UserInput.NormalUseTool;
            bool held=DolocAPI.UserInput.NormalUseToolInProgress;
            if(t<note.startTime){if(press||held)self.redLeaks++;return;}
            if(note.noteType==FishingNoteType.Stable){if(held)self.stableHeld++;else self.stableMissed++;}
            if(note.noteType==FishingNoteType.Bonus && press){self.bonusClicks++;if(self.lastBonus==note.index)self.bonusRepeated++;self.lastBonus=note.index;}
        }
        private void Finish()
        {
            if(finishing)return;
            finishing=true;
            Write("finish; enabled="+Enabled());
            if(frozen){DolocAPI.gameLoop.IsGlobalPaused=oldGlobalPause;frozen=false;}
            if(DolocAPI.IsDataLoaded)DolocAPI.ReturnHome();
            quitAt=Time.realtimeSinceStartup+4f;
        }
        public void Dispose()
        {
            // Core owns event removal, including when this probe's owner is already inactive.
            Exception? failure = null;
            try { if(frozen){DolocAPI.gameLoop.IsGlobalPaused=oldGlobalPause;frozen=false;} }
            catch (Exception ex) { failure = ex; }
            try { harmony.UnpatchSelf(); }
            catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
            try { output?.Dispose(); output = null; }
            catch (Exception ex) { if (failure != null) throw new AggregateException(failure, ex); throw; }
            finally { self = null; helper = null; }
            if (failure != null) throw failure;
        }
    }
}
