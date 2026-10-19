using System;
using System.Collections.Generic;
using UnityEngine;

// Player preferences shown in the Settings menu. Volumes are saved in PlayerPrefs;
// Unity itself remembers the window mode and resolution between launches.
public static class GameSettings
{
    private const string MusicVolumeKey = "settings.musicVolume";
    private const string SfxVolumeKey = "settings.sfxVolume";
    private const float DefaultVolume = .8f;

    public static event Action Changed;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        set => SaveVolume(MusicVolumeKey, value);
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
        set => SaveVolume(SfxVolumeKey, value);
    }

    public static bool Fullscreen
    {
        get => Screen.fullScreenMode != FullScreenMode.Windowed;
        set
        {
            Screen.fullScreenMode = value
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Changed?.Invoke();
        }
    }

    public static Vector2Int CurrentResolution => new Vector2Int(Screen.width, Screen.height);

    // Distinct sizes the display supports (refresh rates collapsed), smallest first, 1280x720 and up.
    public static List<Vector2Int> AvailableResolutions()
    {
        var sizes = new List<Vector2Int>();
        foreach (Resolution resolution in Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= 1280 && size.y >= 720 && !sizes.Contains(size))
                sizes.Add(size);
        }
        if (!sizes.Contains(CurrentResolution))
            sizes.Add(CurrentResolution);
        sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return sizes;
    }

    public static void SetResolution(Vector2Int size)
    {
        Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
        Changed?.Invoke();
    }

    private static void SaveVolume(string key, float value)
    {
        // Round to the 10% steps the menu uses, so repeated +/- never drifts.
        PlayerPrefs.SetFloat(key, Mathf.Round(Mathf.Clamp01(value) * 10f) / 10f);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
