using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using DynamicMaps.Data;
using UnityEngine;

namespace DynamicMaps.Utils
{
    // NOTE: Most of this is adapted from work done for Prop's GTFO mod (https://github.com/dvize/GTFO)
    // this likely does not count as a "substantial portion" of the software
    // under MIT license (https://github.com/dvize/GTFO/blob/master/LICENSE.txt)
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
            if (TriggersWithIds != null)
            {
                TriggersWithIds.Clear();
                TriggersWithIds = null;
            }

            if (QuestItems != null)
            {
                QuestItems.Clear();
                QuestItems = null;
            }
        }

        public static IEnumerable<MapMarkerDef> GetMarkerDefsForCondition(Condition condition,
            string questName, string conditionDescription)
        {
            if (TriggersWithIds == null || QuestItems == null)
            {
                Plugin.Log.LogWarning($"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null}");
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
                    foreach (var marker in GetMarkerDefsForZoneId<PlaceItemTrigger>(location.zoneID, questName, conditionDescription))
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
            string questName, string conditionDescription)
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
                        foreach (var marker in GetMarkerDefsForZoneId<ExperienceTrigger>(place.target, questName, conditionDescription))
                        {
                            yield return marker;
                        }
                        break;
                    }
                    case ConditionInZone zone:
                    {
                        foreach (var zoneId in zone.zoneIds)
                        {
                            foreach (var marker in GetMarkerDefsForZoneId<ExperienceTrigger>(zoneId, questName, conditionDescription))
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

        private static IEnumerable<MapMarkerDef> GetMarkerDefsForZoneId<T>(string zoneId, string questName, string conditionDescription)
            where T : TriggerWithId
        {
            if (TriggersWithIds == null)
            {
                Plugin.Log.LogWarning($"TriggersWithIds is null, cannot search for quest zone for {questName} and {conditionDescription}");
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
            string questName, string conditionDescription)
        {
            if (QuestItems == null)
            {
                Plugin.Log.LogWarning($"QuestItems is null, cannot search for quest items for {questName} and {conditionDescription}");
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
                Plugin.Log.LogWarning($"GetIncompleteQuestConditions: Template.Conditions has null, skipping quest");
                yield break;
            }

            // TODO: conditions is a GClass reference
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                Plugin.Log.LogWarning($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} doesn't have conditions marked AvailableForFinish, skipping it");
                yield break;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    Plugin.Log.LogWarning($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} has null condition, skipping it");
                    continue;
                }

                if (quest.CompletedConditions.Contains(condition.id))
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
                if (quest?.Template?.Conditions == null)
                {
                    Plugin.Log.LogWarning($"quest?.Template?.Conditions == null, skipping quest with id: {quest.Id}");
                    continue;
                }

                if (quest.Status != EQuestStatus.Started)
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
