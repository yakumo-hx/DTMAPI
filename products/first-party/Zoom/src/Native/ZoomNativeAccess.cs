using System;
using System.Collections.Generic;
using System.Reflection;
using DolocTown;
using UnityEngine;

namespace DTMAPI.Zoom
{
    // Metadata is resolved once. Discovery visits only these exact native holders.
    internal static class ZoomNativeAccess
    {
        private static readonly FieldInfo Sky = Field(typeof(BackgroundRenderer), "skyRenderer");
        private static readonly FieldInfo Layers = Field(typeof(BackgroundRenderer), "_layers");
        private static readonly FieldInfo Outdoor = Field(typeof(EnvCovariantController), "_depthFogController");
        private static readonly FieldInfo Indoor = Field(typeof(EnvCovariantController), "_depthFogControllerBuilding");
        private static readonly FieldInfo FogSprites = Field(typeof(DepthFogController), "fogRenderers");
        private static readonly FieldInfo IndoorSky = Field(typeof(DepthFogControllerBuilding), "skyRenderer");
        private static readonly FieldInfo IndoorRemote = Field(typeof(DepthFogControllerBuilding), "remoteRenderer");

        internal static void Discover(List<ZoomSpriteTarget> targets)
        {
            targets.Clear();
            BackgroundRenderer background = DolocAPI.envBackgroundEx;
            if (background)
            {
                var sky = (BackgroundLayerRenderer?)Sky.GetValue(background);
                if (sky) Add(targets, sky!.SpriteRenderer, background);
                var layers = (BackgroundLayerRenderer[]?)Layers.GetValue(background);
                if (layers != null)
                    foreach (var layer in layers)
                        if (layer) Add(targets, layer.SpriteRenderer, background);
            }
            var environment = DolocAPI.EnvCovariantController;
            if (environment == null) return;
            var outdoor = (DepthFogController?)Outdoor.GetValue(environment);
            if (outdoor)
            {
                var sprites = (SpriteRenderer[]?)FogSprites.GetValue(outdoor);
                if (sprites != null)
                    foreach (var sprite in sprites) Add(targets, sprite, outdoor!);
            }
            var indoor = (DepthFogControllerBuilding?)Indoor.GetValue(environment);
            if (indoor)
            {
                Add(targets, (SpriteRenderer?)IndoorSky.GetValue(indoor), indoor!);
                Add(targets, (SpriteRenderer?)IndoorRemote.GetValue(indoor), indoor!);
            }
        }

        private static void Add(List<ZoomSpriteTarget> targets, SpriteRenderer? sprite, object holder)
        {
            if (sprite && sprite!.sprite) targets.Add(new ZoomSpriteTarget(sprite, holder));
        }
        private static FieldInfo Field(Type type, string name) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new MissingFieldException(type.FullName, name);
    }

    internal readonly struct ZoomSpriteTarget
    {
        internal ZoomSpriteTarget(SpriteRenderer renderer, object holder)
        { Renderer = renderer; Holder = holder; }
        internal SpriteRenderer Renderer { get; }
        internal object Holder { get; }
    }
}
