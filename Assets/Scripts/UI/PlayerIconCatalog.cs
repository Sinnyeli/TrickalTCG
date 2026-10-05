using System;
using UnityEngine;

public class PlayerIconCatalog : ScriptableObject
{
    public Texture2D[] icons = Array.Empty<Texture2D>();
    private Sprite[] sprites;
    public Sprite GetSprite(int index)
    {
        if (index < 0 || index >= icons.Length || icons[index] == null) return null;
        if (sprites == null || sprites.Length != icons.Length) sprites = new Sprite[icons.Length];
        if (sprites[index] == null)
        {
            Texture2D texture = icons[index];
            sprites[index] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        }
        return sprites[index];
    }
    public Sprite SelectedSprite()
    {
        string selected = PlayerPrefs.GetString(PreferenceKey, "");
        for (int i = 0; i < icons.Length; i++)
            if (icons[i] != null && icons[i].name == selected) return GetSprite(i);
        return null;
    }
    public void Select(int index)
    {
        if (index < 0 || index >= icons.Length || icons[index] == null) return;
        PlayerPrefs.SetString(PreferenceKey, icons[index].name);
        PlayerPrefs.Save();
        LocalStorageSync.Flush();
    }
    private static string PreferenceKey => "PlayerIcon." +
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            (LocalAccountSession.LoginID ?? "guest").Trim().ToUpperInvariant()));
}
