using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField]
    private string localizationKey;


    private TMP_Text textComponent;


    private void Awake()
    {
        textComponent =
            GetComponent<TMP_Text>();
    }


    private void Start()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning(
                $"No LocalizationManager found for " +
                $"{gameObject.name}."
            );

            return;
        }


        LocalizationManager.Instance
            .OnLanguageChanged +=
            Refresh;


        Refresh();
    }


    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance
                .OnLanguageChanged -=
                Refresh;
        }
    }


    public void Refresh()
    {
        if (textComponent == null)
            return;

        if (LocalizationManager.Instance == null)
            return;


        textComponent.text =
            LocalizationManager.Instance.Get(
                localizationKey
            );
    }


    public void SetKey(
        string key)
    {
        localizationKey =
            key;

        Refresh();
    }
}