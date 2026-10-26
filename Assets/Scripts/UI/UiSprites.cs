using System;
using System.Collections.Generic;
using UnityEngine;

// Smooth UI shapes generated at runtime (circles, rings, rounded panels, diamonds, gradients),
// so the battle HUD needs no art assets. Rounded shapes are 9-sliced and scale to any size.
public static class UiSprites
{
    private const int Size = 128;
    private const int Radius = 28;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    public static Sprite Circle => Shape("circle", 0, p => p.magnitude - Size / 2f + 1);

    public static Sprite Ring => Shape("ring", 0, p => Mathf.Abs(p.magnitude - Size / 2f + 7) - 5);

    public static Sprite Diamond =>
        Shape("diamond", 0, p => (Mathf.Abs(p.x) + Mathf.Abs(p.y) - Size / 2f + 2) * .7071f);

    /// <summary>Filled rounded rectangle; use with Image.Type.Sliced.</summary>
    public static Sprite Panel =>
        Shape("panel", Radius + 2, p => RoundedBox(p, Size / 2f - 1, Radius));

    /// <summary>Rounded rectangle outline (about 5px); use with Image.Type.Sliced.</summary>
    public static Sprite PanelOutline =>
        Shape(
            "panel-outline",
            Radius + 2,
            p => Mathf.Abs(RoundedBox(p, Size / 2f - 4, Radius - 3)) - 2.5f
        );

    public static Sprite VerticalGradient(Color top, Color bottom)
    {
        string key =
            $"gradient-{ColorUtility.ToHtmlStringRGBA(top)}-{ColorUtility.ToHtmlStringRGBA(bottom)}";
        if (cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;
        var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = key,
        };
        for (int y = 0; y < 64; y++)
            texture.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
        texture.Apply();
        return cache[key] = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 64),
            new Vector2(.5f, .5f),
            100
        );
    }

    // Signed distance (pixels, negative inside) for a square of half-size h with corner radius r.
    private static float RoundedBox(Vector2 p, float h, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(h - r, h - r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
    }

    private static Sprite Shape(string key, float border, Func<Vector2, float> distance)
    {
        if (cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = key,
        };
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            Vector2 p = new Vector2(x + .5f - Size / 2f, y + .5f - Size / 2f);
            byte alpha = (byte)(Mathf.Clamp01(.5f - distance(p)) * 255);
            pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return cache[key] = Sprite.Create(
            texture,
            new Rect(0, 0, Size, Size),
            new Vector2(.5f, .5f),
            100,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border)
        );
    }
}
