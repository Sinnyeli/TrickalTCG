using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardEffect), true)]
public class CardEffectInspector : Editor
{
    private static readonly string[] BaseFields =
    {
        "targetType", "targetFilter", "cardRace", "cardType", "specificCard"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CardEffect effect = (CardEffect)target;
        SerializedProperty targetType = serializedObject.FindProperty("targetType");
        SerializedProperty filter = serializedObject.FindProperty("targetFilter");
        SerializedProperty specificCard = serializedObject.FindProperty("specificCard");

        bool drawsCards = effect is DrawCardEffect;
        bool modifiesHandCosts = effect is CostModifierEffect;
        bool usesTargeting = !drawsCards && !modifiesHandCosts &&
                             !(effect is CreateCardsEffect) &&
                             !(effect is SummonEffect) &&
                             !(effect is ConditionalEffect) &&
                             !(effect is TriggeredEffect) &&
                             !(effect is AddCardToDeckEffect) &&
                             !(effect is IncreaseMaxManaEffect);

        if (!(effect is TriggeredEffect))
        {
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetType);
            if (!usesTargeting &&
                (EffectTargetType)targetType.enumValueIndex != EffectTargetType.None)
            {
                EditorGUILayout.HelpBox(
                    "This effect handles its own targets. Set Target Type to None so EffectManager calls it directly.",
                    MessageType.Warning);
            }
        }

        bool usesFilter = modifiesHandCosts ||
                          (drawsCards && GetDrawType() == DrawType.RandomMatching) ||
                          (usesTargeting && IsUnitTarget((EffectTargetType)targetType.enumValueIndex));

        if (usesFilter)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Filter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(filter);
            switch ((EffectTargetFilter)filter.enumValueIndex)
            {
                case EffectTargetFilter.CardRace:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cardRace"));
                    break;
                case EffectTargetFilter.CardType:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cardType"));
                    break;
                case EffectTargetFilter.SpecificCard:
                    EditorGUILayout.PropertyField(specificCard);
                    break;
            }
        }

        if (drawsCards && GetDrawType() == DrawType.Specific)
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(specificCard, new GUIContent("Card To Draw"));
        }

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, BaseFields);
        serializedObject.ApplyModifiedProperties();
    }

    private DrawType GetDrawType()
    {
        SerializedProperty property = serializedObject.FindProperty("drawType");
        return property == null ? DrawType.Top : (DrawType)property.enumValueIndex;
    }

    private static bool IsUnitTarget(EffectTargetType type)
    {
        switch (type)
        {
            case EffectTargetType.EnemyUnit:
            case EffectTargetType.FriendlyUnit:
            case EffectTargetType.AnyUnit:
            case EffectTargetType.AnyTarget:
            case EffectTargetType.AllEnemyUnits:
            case EffectTargetType.AllFriendlyUnits:
            case EffectTargetType.AllUnits:
            case EffectTargetType.RandomEnemyUnit:
            case EffectTargetType.RandomFriendlyUnit:
            case EffectTargetType.RandomUnit:
            case EffectTargetType.TriggerCard:
                return true;
            default:
                return false;
        }
    }
}
