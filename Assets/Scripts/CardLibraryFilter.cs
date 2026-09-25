using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardLibraryFilter : MonoBehaviour
{
    [Header("Database")]
    [SerializeField]
    private CardDatabase cardDatabase;

    [Header("Card Display")]
    [SerializeField]
    private Transform cardContent;

    [SerializeField]
    private DeckEditorCardView cardViewPrefab;

    [Header("Filters")]
    [SerializeField]
    private TMP_InputField searchInput;

    [SerializeField]
    private TMP_Dropdown typeDropdown;

    [SerializeField]
    private TMP_Dropdown raceDropdown;

    [SerializeField]
    private TMP_Dropdown costDropdown;


    private readonly Dictionary<CardData, DeckEditorCardView>
        cardViews =
            new Dictionary<CardData, DeckEditorCardView>();


    private void Start()
    {
        SetupDropdowns();
        CreateCardLibrary();
        ApplyFilters();
    }


    // =========================================================
    // SETUP FILTERS
    // =========================================================

    private void SetupDropdowns()
    {
        SetupTypeDropdown();
        SetupRaceDropdown();
        SetupCostDropdown();

        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveListener(
                OnSearchChanged
            );

            searchInput.onValueChanged.AddListener(
                OnSearchChanged
            );
        }

        if (typeDropdown != null)
        {
            typeDropdown.onValueChanged.RemoveListener(
                OnTypeChanged
            );

            typeDropdown.onValueChanged.AddListener(
                OnTypeChanged
            );
        }

        if (raceDropdown != null)
        {
            raceDropdown.onValueChanged.RemoveListener(
                OnRaceChanged
            );

            raceDropdown.onValueChanged.AddListener(
                OnRaceChanged
            );
        }

        if (costDropdown != null)
        {
            costDropdown.onValueChanged.RemoveListener(
                OnCostChanged
            );

            costDropdown.onValueChanged.AddListener(
                OnCostChanged
            );
        }
    }


    // =========================================================
    // TYPE DROPDOWN
    // =========================================================

    private void SetupTypeDropdown()
    {
        if (typeDropdown == null)
            return;

        typeDropdown.ClearOptions();

        typeDropdown.AddOptions(
            new List<string>
            {
                "All Types",
                "Apostle",
                "Monster",
                "Spell",
                "Artifact"
            }
        );

        typeDropdown.value = 0;
        typeDropdown.RefreshShownValue();
    }


    // =========================================================
    // RACE DROPDOWN
    // =========================================================

    private void SetupRaceDropdown()
    {
        if (raceDropdown == null)
            return;

        raceDropdown.ClearOptions();

        List<string> options =
            new List<string>();

        options.Add("All Races");

        string[] raceNames =
            Enum.GetNames(typeof(CardRace));

        foreach (string raceName in raceNames)
        {
            options.Add(raceName);
        }

        raceDropdown.AddOptions(options);

        raceDropdown.value = 0;
        raceDropdown.RefreshShownValue();
    }


    // =========================================================
    // COST DROPDOWN
    // =========================================================

    private void SetupCostDropdown()
    {
        if (costDropdown == null)
            return;

        costDropdown.ClearOptions();

        costDropdown.AddOptions(
            new List<string>
            {
                "All Costs",
                "0",
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10+"
            }
        );

        costDropdown.value = 0;
        costDropdown.RefreshShownValue();
    }


    // =========================================================
    // CREATE LIBRARY
    // =========================================================

    private void CreateCardLibrary()
    {
        if (cardDatabase == null)
        {
            Debug.LogError(
                "CardLibraryFilter has no CardDatabase."
            );

            return;
        }

        if (cardContent == null)
        {
            Debug.LogError(
                "CardLibraryFilter has no Card Content."
            );

            return;
        }

        if (cardViewPrefab == null)
        {
            Debug.LogError(
                "CardLibraryFilter has no Card View Prefab."
            );

            return;
        }


        foreach (CardData card
                 in cardDatabase.AllCards)
        {
            if (card == null)
                continue;

            // Generated/token cards do not appear
            // in the deck-building library.
            if (!card.Collectible)
                continue;


            DeckEditorCardView view =
                Instantiate(
                    cardViewPrefab,
                    cardContent
                );

            view.SetCard(card);

            cardViews.Add(
                card,
                view
            );
        }
    }


    // =========================================================
    // FILTER EVENTS
    // =========================================================

    private void OnSearchChanged(
        string value)
    {
        ApplyFilters();
    }


    private void OnTypeChanged(
        int value)
    {
        ApplyFilters();
    }


    private void OnRaceChanged(
        int value)
    {
        ApplyFilters();
    }


    private void OnCostChanged(
        int value)
    {
        ApplyFilters();
    }


    // =========================================================
    // APPLY FILTERS
    // =========================================================

    public void ApplyFilters()
    {
        foreach (
            KeyValuePair<CardData, DeckEditorCardView>
            pair in cardViews)
        {
            CardData card =
                pair.Key;

            DeckEditorCardView view =
                pair.Value;

            if (card == null ||
                view == null)
                continue;


            bool visible =
                MatchesSearch(card) &&
                MatchesType(card) &&
                MatchesRace(card) &&
                MatchesCost(card);


            view.gameObject.SetActive(
                visible
            );
        }
    }


    // =========================================================
    // SEARCH
    // =========================================================

    private bool MatchesSearch(
        CardData card)
    {
        if (searchInput == null)
            return true;


        string search =
            searchInput.text.Trim();


        if (string.IsNullOrWhiteSpace(search))
            return true;


        return card.cardName.IndexOf(
            search,
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }


    // =========================================================
    // TYPE
    // =========================================================

    private bool MatchesType(
        CardData card)
    {
        if (typeDropdown == null)
            return true;


        switch (typeDropdown.value)
        {
            // All
            case 0:
                return true;

            // Apostle
            case 1:
                return card is ApostleData;

            // Monster
            case 2:
                return card is MonsterData &&
                       !(card is ApostleData);

            // Spell
            case 3:
                return card is SpellData;

            // Artifact
            case 4:
                return card is ArtifactData;
        }


        return true;
    }


    // =========================================================
    // RACE
    // =========================================================

    private bool MatchesRace(
        CardData card)
    {
        if (raceDropdown == null)
            return true;


        // All Races
        if (raceDropdown.value == 0)
            return true;


        if (!(card is MinionData minion))
            return false;


        int raceIndex =
            raceDropdown.value - 1;


        CardRace selectedRace =
            (CardRace)raceIndex;


        return minion.cardRace ==
               selectedRace;
    }


    // =========================================================
    // COST
    // =========================================================

    private bool MatchesCost(
        CardData card)
    {
        if (costDropdown == null)
            return true;


        // All Costs
        if (costDropdown.value == 0)
            return true;


        // Last option = 10+
        if (costDropdown.value == 11)
        {
            return card.manaCost >= 10;
        }


        // Dropdown:
        //
        // 1 = cost 0
        // 2 = cost 1
        // 3 = cost 2
        //
        // Therefore subtract 1.

        int selectedCost =
            costDropdown.value - 1;


        return card.manaCost ==
               selectedCost;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetFilters()
    {
        if (searchInput != null)
            searchInput.text = "";

        if (typeDropdown != null)
            typeDropdown.value = 0;

        if (raceDropdown != null)
            raceDropdown.value = 0;

        if (costDropdown != null)
            costDropdown.value = 0;

        ApplyFilters();
    }
}