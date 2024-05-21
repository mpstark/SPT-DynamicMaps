using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using InGameMap.Data;
using UnityEngine;

namespace InGameMap.Utils
{
    public static class QuestUtils
    {
        // reflection
        private static FieldInfo _playerQuestControllerField = AccessTools.Field(typeof(Player), "_questController");
        private static PropertyInfo _questControllerQuestsProperty = AccessTools.Property(typeof(AbstractQuestControllerClass), "Quests");
        private static FieldInfo _questsListField = AccessTools.Field(_questControllerQuestsProperty.PropertyType, "list_1");
        private static FieldInfo _conditionCounterTemplateField = AccessTools.Field(typeof(ConditionCounterCreator), "_templateConditions");
        private static FieldInfo _templateConditionsConditionsField = AccessTools.Field(_conditionCounterTemplateField.FieldType, "Conditions");
        private static FieldInfo _conditionListField = AccessTools.Field(_templateConditionsConditionsField.FieldType, "list_0");
        //

        public static List<TriggerWithId> TriggersWithIds;
        public static List<LootItem> QuestItems;

        internal static void OnGameStarted(GameWorld gameWorld)
        {
            TriggersWithIds = GameObject.FindObjectsOfType<TriggerWithId>().ToList();
            QuestItems = Traverse.Create(gameWorld)
                .Field("LootItems")
                .Field("list_0")
                .GetValue<List<LootItem>>()
                .Where(i => i.Item.QuestItem).ToList();
        }

        internal static void OnGameEnded()
        {
            TriggersWithIds.Clear();
            QuestItems.Clear();
            TriggersWithIds = null;
            QuestItems = null;
        }

        public static IEnumerable<MapMarkerDef> GetMarkerDefsForCondition(Condition condition,
            string questName, string conditionDescription)
        {
            if (TriggersWithIds == null || QuestItems == null)
            {
                yield break;
            }

            switch (condition)
            {
                case ConditionLeaveItemAtLocation location:
                {
                    foreach (var marker in GetMarkerDefsForZoneId<PlaceItemTrigger>(location.zoneId, questName, conditionDescription))
                    {
                        yield return marker;
                    }
                    break;
                }
                case ConditionPlaceBeacon beacon:
                {
                    foreach (var marker in GetMarkerDefsForZoneId<PlaceItemTrigger>(beacon.zoneId, questName, conditionDescription))
                    {
                        yield return marker;
                    }
                    break;
                }
                case ConditionFindItem findItem:
                {
                    foreach (var marker in GetMarkerDefsForQuestItems(findItem.target, questName, conditionDescription))
                    {
                        yield return marker;
                    }
                    break;
                }
                case ConditionLaunchFlare location:
                {
                    foreach (var marker in GetMarkerDefsForZoneId<PlaceItemTrigger>(location.id, questName, conditionDescription))
                    {
                        yield return marker;
                    }
                    break;
                }
                case ConditionCounterCreator creator:
                {
                    foreach (var marker in GetMarkerDefsForConditionCounter(creator, questName, conditionDescription))
                    {
                        yield return marker;
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        private static IEnumerable<MapMarkerDef> GetMarkerDefsForConditionCounter(ConditionCounterCreator creator,
            string questName, string questDescription)
        {
            var counter = _conditionCounterTemplateField.GetValue(creator);
            var conditions = _templateConditionsConditionsField.GetValue(counter);
            var conditionsList = _conditionListField.GetValue(conditions) as IList<Condition>;

            foreach (var counterCondition in conditionsList)
            {
                switch (counterCondition)
                {
                    case ConditionVisitPlace place:
                    {
                        foreach (var marker in GetMarkerDefsForZoneId<ExperienceTrigger>(place.target, questName, questDescription))
                        {
                            yield return marker;
                        }
                        break;
                    }
                    case ConditionInZone zone:
                    {
                        foreach (var zoneId in zone.zoneIds)
                        {
                            foreach (var marker in GetMarkerDefsForZoneId<ExperienceTrigger>(zoneId, questName, questDescription))
                            {
                                yield return marker;
                            }
                        }
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
            }
        }

        private static IEnumerable<MapMarkerDef> GetMarkerDefsForZoneId<T>(string zoneId, string questName, string questDescription)
            where T : TriggerWithId
        {
            if (TriggersWithIds == null)
            {
                yield break;
            }

            var zones = TriggersWithIds.GetZoneTriggers<T>(zoneId);
            foreach (var zone in zones)
            {
                yield return new MapMarkerDef
                {
                    Category = "Quest",
                    Color = Color.green,
                    ImagePath = "Markers/quest.png",
                    Position = MathUtils.ConvertToMapPosition(zone.transform.position),
                    Pivot = new Vector2(0.5f, 0f),
                    Text = questName
                };
            }
        }

        private static IEnumerable<MapMarkerDef> GetMarkerDefsForQuestItems(IEnumerable<string> questItemIds,
            string questName, string questDescription)
        {
            if (QuestItems == null)
            {
                yield break;
            }

            foreach (var questItemId in questItemIds)
            {
                var questItems = QuestItems.Where(i => i.TemplateId == questItemId);
                foreach (var item in questItems)
                {
                    yield return new MapMarkerDef
                    {
                        Category = "Quest",
                        Color = Color.green,
                        ImagePath = "Markers/quest.png",
                        Position = MathUtils.ConvertToMapPosition(item.transform.position),
                        Pivot = new Vector2(0.5f, 0f),
                        Text = questName
                    };
                }
            }
        }

        public static IEnumerable<Condition> GetIncompleteQuestConditions(QuestDataClass quest)
        {
            // TODO: Template.Conditions is a GClass reference
            if (quest?.Template?.Conditions == null)
            {
                yield break;
            }

            // TODO: conditions is a GClass reference
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                yield break;
            }

            foreach (var condition in conditions)
            {
                if (condition == null || quest.CompletedConditions.Contains(condition.id))
                {
                    continue;
                }

                yield return condition;
            }
        }

        public static IEnumerable<QuestDataClass> GetIncompleteQuests(Player player)
        {
            var questController = _playerQuestControllerField.GetValue(player);
            var quests = _questControllerQuestsProperty.GetValue(questController);
            var questsList = _questsListField.GetValue(quests) as List<QuestDataClass>;

            foreach (var quest in questsList)
            {
                if (quest?.Template?.Conditions == null || quest.Status != EQuestStatus.Started)
                {
                    continue;
                }

                yield return quest;
            }
        }

        private static IEnumerable<T> GetZoneTriggers<T>(this IEnumerable<TriggerWithId> triggerWithIds, string zoneId)
            where T : TriggerWithId
        {
            return triggerWithIds.OfType<T>().Where(t => t.Id == zoneId);
        }

    }
}
