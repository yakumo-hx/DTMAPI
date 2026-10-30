using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.MonsterContent;
using UnityEngine;

namespace Samples.MonsterWilds
{
    public sealed partial class ModEntry
    {
        sealed class KingArt
        {
            public string fingerprint = "";
            public int columns = 0;
            public KingArtFrame[] frames = Array.Empty<KingArtFrame>();
        }
        sealed class KingArtFrame
        {
            public string id = "";
            public int width = 0, height = 0, x = 0, top = 0, bodyWidth = 0;
            public float pivotX = 0, pivotY = 0, ppu = 0;
        }
        readonly Dictionary<string, Vector2> nativeCrownAnchors = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        readonly Dictionary<Rect, Vector2> skinCrownAnchors = new Dictionary<Rect, Vector2>();
        int kingAtlasWidth, kingAtlasHeight;

        MonsterSkin LoadKingSkin()
        {
            var assembly = typeof(ModEntry).Assembly;
            var art = CreateKingArt();
            if (art == null || art.columns <= 0 || art.frames.Length == 0)
                throw new InvalidDataException("King frame layout is empty: columns=" + art?.columns + "; frames=" + art?.frames?.Length);
            byte[] png;
            using (var stream = assembly.GetManifestResourceStream(Owner + ".amoeba.png")
                ?? throw new InvalidDataException("King skin resource missing."))
            using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); png = buffer.ToArray(); }
            kingAtlasWidth = art.columns * art.frames[0].width;
            kingAtlasHeight = (int)Math.Ceiling(art.frames.Length / (double)art.columns) * art.frames[0].height;
            var skin = new MonsterSkin { TemplateFingerprint = art.fingerprint, Png = png };
            for (int i = 0; i < art.frames.Length; i++) {
                var frame = art.frames[i]; int x = i % art.columns * frame.width, y = i / art.columns * frame.height;
                skin.Frames.Add(new MonsterFrameMapping { FrameId = frame.id, X = x, Y = y, Width = frame.width, Height = frame.height });
                // The atlas preserves the native transparent padding. Anchor above visible pixels, not the full cell.
                var anchor = new Vector2((frame.x + frame.bodyWidth * .5f - frame.width * frame.pivotX) / frame.ppu,
                    (frame.height - frame.top - frame.height * frame.pivotY) / frame.ppu + .1f);
                nativeCrownAnchors.Add(frame.id, anchor);
                skinCrownAnchors.Add(new Rect(x, kingAtlasHeight - y - frame.height, frame.width, frame.height), anchor);
            }
            return skin;
        }

        void PositionCrown(Transform crown, SpriteRenderer renderer)
        {
            var sprite = renderer.sprite;
            if (sprite == null) return;
            Vector2 anchor;
            bool ownAtlas = sprite.texture.width == kingAtlasWidth && sprite.texture.height == kingAtlasHeight;
            if (ownAtlas ? skinCrownAnchors.TryGetValue(sprite.rect, out anchor) : nativeCrownAnchors.TryGetValue(sprite.name, out anchor))
                crown.localPosition = new Vector3(renderer.flipX ? -anchor.x : anchor.x, anchor.y, 0);
        }

        public sealed class CrownFollower : MonoBehaviour
        {
            internal ModEntry? Owner;
            internal SpriteRenderer? Renderer;
            void LateUpdate()
            {
                if (Owner != null && !Owner.stopped && Renderer != null) Owner.PositionCrown(transform, Renderer);
            }
        }
    }
}
