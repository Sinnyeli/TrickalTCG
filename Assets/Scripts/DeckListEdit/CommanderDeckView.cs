using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommanderDeckView : MonoBehaviour
{
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text manaCostText;

    [SerializeField]
    private GameObject emptyCommanderDisplay;

    [SerializeField]
    private GameObject commanderDisplay;

    private ApostleData commander;

    public ApostleData Commander =>
        commander;

    public void SetCommander(
        ApostleData data)
    {
        commander = data;

        bool hasCommander =
            commander != null;

        emptyCommanderDisplay.SetActive(
            !hasCommander
        );

        commanderDisplay.SetActive(
            hasCommander
        );

        if (!hasCommander)
            return;

        nameText.text =
            commander.cardName;

        manaCostText.text =
            commander.manaCost.ToString();

        artworkImage.sprite =
            commander.artwork;
    }
}