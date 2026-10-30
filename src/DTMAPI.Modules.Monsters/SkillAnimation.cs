using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class AnimationResources
        {
            internal MonsterActionAnimation Definition = null!;
            internal Texture2D Texture = null!;
            internal readonly Dictionary<int, Sprite> Frames = new Dictionary<int, Sprite>();
        }
        private sealed class AnimationExecution
        {
            internal AnimationResources Resources = null!;
            internal Animator Animator = null!;
            internal SpriteRenderer Renderer = null!;
            internal Sprite? OriginalSprite;
            internal bool OriginalEnabled;
            internal int Frame = -1;
        }
        private MonsterDiagnostic? PrepareAnimations(Binding binding)
        {
            foreach (var skill in binding.Definition.Definition.Skills)
            {
                var a = skill.Animation;
                if (a == null) continue;
                var resources = new AnimationResources { Definition = a, Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) };
                binding.Animations.Add(skill.Id, resources); // Register before decode for partial rollback.
                resources.Texture.name = binding.Definition.Id + ":" + skill.Id + ":atlas";
                if (!ImageConversion.LoadImage(resources.Texture, a.Png, false)) return Error(binding.Definition, "PngDecodeFailed", "behavior.skills." + skill.Id + ".animation.png", "Action atlas could not be decoded.");
                resources.Texture.filterMode = FilterMode.Point; resources.Texture.wrapMode = TextureWrapMode.Clamp;
                int columns = resources.Texture.width / a.FrameWidth;
                foreach (int frame in a.WindupFrames.Concat(a.RecoveryFrames).Distinct())
                {
                    var sprite = Sprite.Create(resources.Texture, new Rect(frame % columns * a.FrameWidth, resources.Texture.height - (frame / columns + 1) * a.FrameHeight, a.FrameWidth, a.FrameHeight),
                        new Vector2(a.PivotX, a.PivotY), a.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = binding.Definition.Id + ":" + skill.Id + ":frame-" + frame;
                    resources.Frames.Add(frame, sprite);
                }
                resources.Texture.Apply(false, true);
            }
            return null;
        }
        private static void ReleaseAnimations(Binding binding)
        {
            foreach (var a in binding.Animations.Values)
            {
                foreach (var sprite in a.Frames.Values) Object.Destroy(sprite);
                Object.Destroy(a.Texture);
            }
            binding.Animations.Clear();
        }
        private static void BeginAnimation(InstanceScope scope, ControlledAction action, string skillId)
        {
            if (!scope.Binding.Animations.TryGetValue(skillId, out var resources)) return;
            var renderer = scope.Controller!.Renderer;
            action.Animation = new AnimationExecution { Resources = resources, Animator = renderer.Animator, Renderer = renderer.SpriteRenderer,
                OriginalEnabled = renderer.Animator.enabled, OriginalSprite = renderer.SpriteRenderer.sprite };
            // No native completion/damage callback can compete with the controlled action.
            NativeAccess.Set(scope.Controller, "animationEvtCallback", null);
            NativeAccess.Set(renderer, "onAnimationDone", null);
            NativeAccess.Set(renderer, "shouldCheckAnimationDone", false);
            renderer.Animator.enabled = false;
            AdvanceAnimation(action);
        }
        private static void AdvanceAnimation(ControlledAction action)
        {
            var a = action.Animation;
            if (a == null || action.Stopping || action.State != MonsterActionState.Running) return;
            var scatter = action.Scatter!;
            int frame = a.Resources.Definition.FrameAt(action.Elapsed, scatter.Parameters, scatter.Fired, scatter.FiredAt);
            a.Renderer.sprite = a.Resources.Frames[frame]; a.Frame = frame;
        }
        private static void EndAnimation(ControlledAction action)
        {
            var a = action.Animation;
            if (a == null) return;
            if (a.Renderer != null) a.Renderer.sprite = a.OriginalSprite;
            if (a.Animator != null) a.Animator.enabled = a.OriginalEnabled;
            action.Animation = null;
        }
    }
}
