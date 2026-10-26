using System.Collections.Generic;
using UnityEngine;

// Stand-in artwork for cards that have no picture yet: a soft gradient in the card type's colours
// with a faint lattice and a glowing emblem, so missing art reads as a style rather than a hole.
public static class CardArtFallback
{
    private const int Size = 256;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite For(CardData data)
    {
        string kind =
            data is ArtifactData ? "Artifact"
            : data is SpellData ? "Spell"
            : data is ApostleData ? "Apostle"
            : "Monster";
        if (cache.TryGetValue(kind, out Sprite sprite) && sprite != null)
            return sprite;
        sprite = Build(kind);
        cache[kind] = sprite;
        return sprite;
    }

    private static Sprite Build(string kind)
    {
        Color top,
            bottom,
            accent;
        switch (kind)
        {
            case "Artifact":
                top = new Color(.98f, .82f, .45f);
                bottom = new Color(.72f, .43f, .16f);
                accent = new Color(1f, .96f, .82f);
                break;
            case "Spell":
                top = new Color(.62f, .62f, .98f);
                bottom = new Color(.33f, .22f, .62f);
                accent = new Color(.9f, .88f, 1f);
                break;
            case "Apostle":
                top = new Color(.99f, .72f, .78f);
                bottom = new Color(.71f, .32f, .47f);
                accent = new Color(1f, .92f, .94f);
                break;
            default:
                top = new Color(.64f, .88f, .6f);
                bottom = new Color(.24f, .5f, .38f);
                accent = new Color(.92f, 1f, .9f);
                break;
        }

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "Fallback Art " + kind,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[Size * Size];
        Vector2 centre = new Vector2(Size / 2f, Size * .54f);
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float v = y / (Size - 1f);
            Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, v));

            // Faint diagonal lattice.
            float a = Mathf.Abs(Mathf.Repeat((x + y) / 24f, 1f) - .5f);
            float b = Mathf.Abs(Mathf.Repeat((x - y) / 24f, 1f) - .5f);
            float line = Mathf.Max(
                Mathf.InverseLerp(.06f, .0f, a),
                Mathf.InverseLerp(.06f, .0f, b)
            );
            c = Color.Lerp(c, accent, line * .12f);

            // Glowing emblem: a soft halo with a crisp ring and a four-point star inside.
            Vector2 d = new Vector2(x, y) - centre;
            float r = d.magnitude / (Size * .5f);
            c = Color.Lerp(c, accent, Mathf.Clamp01(1f - r / .55f) * .35f);
            c = Color.Lerp(c, accent, Mathf.InverseLerp(.025f, 0f, Mathf.Abs(r - .3f)) * .8f);
            float star = Mathf.Abs(d.x) * Mathf.Abs(d.y) / (Size * Size * .5f);
            if (r < .26f)
                c = Color.Lerp(c, accent, Mathf.InverseLerp(.004f, 0f, star) * (1f - r / .26f));

            // Darken the corners slightly.
            float edge = new Vector2(x / (Size - 1f) - .5f, v - .5f).magnitude;
            c *= Mathf.Lerp(1f, .78f, Mathf.InverseLerp(.4f, .72f, edge));
            c.a = 1f;
            pixels[y * Size + x] = c;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, Size, Size),
            new Vector2(.5f, .5f),
            100f
        );
        sprite.name = texture.name;
        return sprite;
    }
}
