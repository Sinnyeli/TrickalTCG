using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Find All Cards"))
        {
            FindAllCards();
        }
    }

    private void FindAllCards()
    {
        CardDatabase database = (CardDatabase)target;

        string[] guids =
            AssetDatabase.FindAssets("t:CardData");

        List<CardData> cards = new List<CardData>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            CardData card =
                AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (card != null)
                cards.Add(card);
        }

        SerializedObject serializedDatabase =
            new SerializedObject(database);

        SerializedProperty allCardsProperty =
            serializedDatabase.FindProperty("allCards");

        allCardsProperty.ClearArray();

        for (int i = 0; i < cards.Count; i++)
        {
            allCardsProperty.InsertArrayElementAtIndex(i);

            allCardsProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue = cards[i];
        }

        serializedDatabase.ApplyModifiedProperties();

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Card Database updated: {cards.Count} cards found."
        );
    }
}