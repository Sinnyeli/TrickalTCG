[System.Serializable]
public class LocalizationEntry
{
    public string Key;
    public string English;
    public string Korean;

    public string GetText(
        GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Korean:
                return Korean;

            case GameLanguage.English:
            default:
                return English;
        }
    }
}