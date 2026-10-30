using System;
using System.Collections;
using System.Linq;
using DolocTown;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DTMAPI.Internal.HeartMap
{
    // Opt-in experiment UI. Disabled for the entire performance sampling window.
    // Input is queued into the real keyboard device; native actions and physics own movement.
    internal sealed class MapPanel : MonoBehaviour
    {
        internal MapModule Owner = null!;
        private bool sending;
        private Keyboard? keyboard;
        private GUIStyle? style;
        private static readonly string[] Actions = { "enter", "exit", "overview", "capture", "trace", "fault", "cycles", "measure" };

        private void OnGUI()
        {
            if (Owner == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            GUI.Box(new Rect(8, 370, 230, 610), "HEART MAP / NO SAVE");
            for (int i = 0; i < Actions.Length; i++)
                if (GUI.Button(new Rect(18, 400 + i * 36, 210, 31), Actions[i], style)) Owner.Execute(Actions[i]);
            if (GUI.Button(new Rect(250, 400, 165, 31), "control: farm", style)) Owner.Execute("farm");
            if (GUI.Button(new Rect(250, 440, 165, 31), "SELECT SLOT 8", style)) Send(.8f, Key.Digit8);
            if (GUI.Button(new Rect(18, 695, 100, 36), "LEFT .25s", style)) Send(.25f, Key.A);
            if (GUI.Button(new Rect(128, 695, 100, 36), "RIGHT .25s", style)) Send(.25f, Key.D);
            if (GUI.Button(new Rect(18, 736, 210, 36), "JUMP .35s", style)) Send(.35f, Key.Space);
            if (GUI.Button(new Rect(18, 777, 100, 36), "LEFT + JUMP", style)) Send(.4f, Key.A, Key.Space);
            if (GUI.Button(new Rect(128, 777, 100, 36), "RIGHT + JUMP", style)) Send(.4f, Key.D, Key.Space);
            if (GUI.Button(new Rect(18, 818, 210, 36), "DOWN .25s", style)) Send(.25f, Key.S);
            if (GUI.Button(new Rect(18, 859, 210, 36), "MENU (Escape)", style)) Send(.15f, Key.Escape);
            if (GUI.Button(new Rect(18, 900, 210, 36), "RELEASE INPUT", style)) Release();
        }

        private void Send(float seconds, params Key[] keys)
        {
            if (sending || !DolocAPI.IsDataLoaded || !(DolocAPI.userInput.CurrentState is NormalGameState)) return;
            keyboard = Keyboard.current;
            if (keyboard == null || keyboard.allKeys.Any(k => k.isPressed)) { Owner.Log("bounded input rejected: device absent or keys already held"); return; }
            StartCoroutine(Hold());
            IEnumerator Hold()
            {
                sending = true;
                // IMGUI clicks also reach native mouse input. Let that finite tool action finish
                // before sending the keyboard edge; do not patch native input or tool ownership.
                yield return new WaitForSecondsRealtime(1f);
                if (keyboard == null || keyboard.allKeys.Any(k => k.isPressed) || !DolocAPI.IsDataLoaded ||
                    !(DolocAPI.userInput.CurrentState is NormalGameState))
                {
                    keyboard = null; sending = false;
                    yield break;
                }
                Owner.Log("native input start=" + string.Join("+", keys.Select(k => k.ToString())) + "; seconds=" + seconds + "; position=" + DolocAPI.AgentPosition);
                Owner.Log("native input context; agentRootLayer=" + LayerMask.LayerToName(DolocAPI.agent.gameObject.layer) +
                    "; touchedPlatforms=" + DolocAPI.agent.GetComponentInChildren<GroundChecker>()?.PlatformCount +
                    "; platformExcludeIgnored=" + Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Platform"), LayerMask.NameToLayer("PlatformExclude")));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                yield return new WaitForSecondsRealtime(seconds);
                Release();
                yield return new WaitForSecondsRealtime(.6f);
                Owner.LogStatus("after-bounded-input");
            }
        }
        private void Release()
        {
            if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            keyboard = null; sending = false;
        }
        private void OnDisable() { StopAllCoroutines(); Release(); }
    }
}
