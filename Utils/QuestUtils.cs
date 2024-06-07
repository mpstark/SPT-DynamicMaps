using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Data;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
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

        private static MethodInfo _questsGetConditionalMethod = AccessTools.Method(_questControllerQuestsProperty.PropertyType, "GetConditional", new Type[] { typeof(string) });

        private static Type _questType = _questControllerQuestsProperty.PropertyType.BaseType.GetGenericArguments()[0];
        private static MethodInfo _questIsConditionDone = AccessTools.Method(_questType, "IsConditionDone");

        private static FieldInfo _conditionCounterTemplateField = AccessTools.Field(typeof(ConditionCounterCreator), "_templateConditions");
        private static FieldInfo _templateConditionsConditionsField = AccessTools.Field(_conditionCounterTemplateField.FieldType, "Conditions");
        private static FieldInfo _conditionListField = AccessTools.Field(_templateConditionsConditionsField.FieldType, "list_0");
        //

        // TODO: move to config
        private const string _questCategory = "Quest";
        private const string _questImagePath = "Markers/quest.png";
        private static Vector2 _questPivot = new Vector2(0.5f, 0f);
        private static Color _questColor = Color.green;
        //

        public static List<TriggerWithId> TriggersWithIds;
        public static List<LootItem> QuestItems;

        internal static void TryCaptureQuestData()
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (TriggersWithIds == null)
            {
                TriggersWithIds = GameObject.FindObjectsOfType<TriggerWithId>().ToList();
            }

            if (QuestItems == null)
            {
                QuestItems = Traverse.Create(gameWorld)
                    .Field("LootItems")
                    .Field("list_0")
                    .GetValue<List<LootItem>>()
                    .Where(i => i.Item.QuestItem).ToList();
            }
        }

        internal static void DiscardQuestData()
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

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForPlayer(Player player)
        {
            if (TriggersWithIds == null || QuestItems == null)
            {
                Plugin.Log.LogWarning($"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null}");
                return null;
            }

            var markers = new List<MapMarkerDef>();

            var quests = GetIncompleteQuests(player);
            foreach (var quest in quests)
            {
                markers.AddRange(GetMarkerDefsForQuest(player, quest));
            }

            return markers;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForQuest(Player player, QuestDataClass quest)
        {
            var markers = new List<MapMarkerDef>();

            var conditions = GetIncompleteQuestConditions(player, quest);
            foreach (var condition in conditions)
            {
                var questName = quest.Template.NameLocaleKey.BSGLocalized();
                var conditionDescription = condition.id.BSGLocalized();

                var positions = GetPositionsForCondition(condition, questName, conditionDescription);
                foreach (var position in positions)
                {
                    var isDuplicate = false;

                    // check against previously created markers for duplicate position
                    foreach (var marker in markers)
                    {
                        if (MathUtils.ApproxEquals(marker.Position.x, position.x)
                         && MathUtils.ApproxEquals(marker.Position.y, position.y)
                         && MathUtils.ApproxEquals(marker.Position.z, position.z))
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (isDuplicate)
                    {
                        continue;
                    }

                    markers.Add(CreateQuestMapMarkerDef(position, questName, conditionDescription));
                }
            }

            return markers;
        }

        private static IEnumerable<Vector3> GetPositionsForCondition(Condition condition, string questName,
                                                                    string conditionDescription)
        {
            switch (condition)
            {
                case ConditionZone zoneCondition:
                {
                    foreach (var position in GetPositionsForZoneId(zoneCondition.zoneId, questName, conditionDescription))
                    {
                        yield return position;
                    }
                    break;
                }
                case ConditionLaunchFlare flareCondition:
                {
                    foreach (var position in GetPositionsForZoneId(flareCondition.zoneID, questName, conditionDescription))
                    {
                        yield return position;
                    }
                    break;
                }
                case ConditionVisitPlace place:
                {
                    foreach (var position in GetPositionsForZoneId(place.target, questName, conditionDescription))
                    {
                        yield return position;
                    }
                    break;
                }
                case ConditionInZone zone:
                {
                    foreach (var zoneId in zone.zoneIds)
                    {
                        foreach (var position in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                        {
                            yield return position;
                        }
                    }
                    break;
                }
                case ConditionFindItem findItemCondition:
                {
                    foreach (var position in GetPositionsForQuestItems(findItemCondition.target, questName, conditionDescription))
                    {
                        yield return position;
                    }
                    break;
                }
                case ConditionExitName exitCondition:
                {
                    var exfils = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints;
                    var specifiedExit = exfils.FirstOrDefault(e => e.Settings.Name == exitCondition.exitName);

                    if (specifiedExit != null)
                    {
                        yield return MathUtils.ConvertToMapPosition(specifiedExit.transform);
                    }

                    break;
                }
                case ConditionCounterCreator conditionCreator:
                {
                    // this will recurse back into this method
                    foreach (var position in GetPositionsForConditionCreator(conditionCreator, questName, conditionDescription))
                    {
                        yield return position;
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForConditionCreator(ConditionCounterCreator conditionCreator,
                                                                            string questName, string conditionDescription)
        {
            var counter = _conditionCounterTemplateField.GetValue(conditionCreator);
            var conditions = _templateConditionsConditionsField.GetValue(counter);
            var conditionsList = _conditionListField.GetValue(conditions) as IList<Condition>;

            foreach (var condition in conditionsList)
            {
                foreach (var position in GetPositionsForCondition(condition, questName, conditionDescription))
                {
                    yield return position;
                }
            }
        }

        private static IEnumerable<Vector3> GetPositionsForZoneId(string zoneId, string questName,
                                                                  string conditionDescription)
        {
            var zones = TriggersWithIds?.GetZoneTriggers(zoneId);
            foreach (var zone in zones)
            {
                yield return MathUtils.ConvertToMapPosition(zone.transform.position);
            }
        }

        private static IEnumerable<Vector3> GetPositionsForQuestItems(IEnumerable<string> questItemIds, string questName,
                                                                      string conditionDescription)
        {
            foreach (var questItemId in questItemIds)
            {
                var questItems = QuestItems?.Where(i => i.TemplateId == questItemId);
                foreach (var item in questItems)
                {
                    yield return MathUtils.ConvertToMapPosition(item.transform.position);
                }
            }
        }

        private static IEnumerable<Condition> GetIncompleteQuestConditions(Player player, QuestDataClass quest)
        {
            // TODO: Template.Conditions is a GClass reference
            if (quest?.Template?.Conditions == null)
            {
                Plugin.Log.LogError($"GetIncompleteQuestConditions: quest.Template.Conditions is null, skipping quest");
                yield break;
            }

            // TODO: conditions is a GClass reference
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out var conditions) || conditions == null)
            {
                Plugin.Log.LogError($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} doesn't have conditions marked AvailableForFinish, skipping it");
                yield break;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    Plugin.Log.LogWarning($"Quest {quest.Template.NameLocaleKey.BSGLocalized()} has null condition, skipping it");
                    continue;
                }

                // filter out completed conditions
                if (IsConditionCompleted(player, quest, condition))
                {
                    continue;
                }

                yield return condition;
            }
        }

        private static IEnumerable<QuestDataClass> GetIncompleteQuests(Player player)
        {
            var questController = _playerQuestControllerField.GetValue(player);
            if (questController == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, questController is null");
                yield break;
            }

            var quests = _questControllerQuestsProperty.GetValue(questController);
            if (quests == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, quests is null");
                yield break;
            }

            var questsList = _questsListField.GetValue(quests) as List<QuestDataClass>;
            if (questsList == null)
            {
                Plugin.Log.LogError($"Not able to get quests for player: {player.Id}, questsList is null");
                yield break;
            }

            foreach (var quest in questsList)
            {
                if (quest?.Template?.Conditions == null)
                {
                    continue;
                }

                if (quest.Status != EQuestStatus.Started)
                {
                    continue;
                }

                yield return quest;
            }
        }

        private static bool IsConditionCompleted(Player player, QuestDataClass questData, Condition condition)
        {
            // CompletedConditions is inaccurate (it doesn't reset when some quests do on death)
            // and also does not contain optional objectives, need to recheck if something is in there
            if (condition.IsNecessary && !questData.CompletedConditions.Contains(condition.id))
            {
                return false;
            }

            var questController = _playerQuestControllerField.GetValue(player);
            if (questController == null)
            {
                return false;
            }

            var quests = _questControllerQuestsProperty.GetValue(questController);
            if (quests == null)
            {
                return false;
            }

            var quest = _questsGetConditionalMethod.Invoke(quests, new object[] { questData.Id });
            if (quest == null)
            {
                return false;
            }

            return (bool)_questIsConditionDone.Invoke(quest, new object[] { condition });
        }

        private static IEnumerable<TriggerWithId> GetZoneTriggers(this IEnumerable<TriggerWithId> triggerWithIds, string zoneId)
        {
            return triggerWithIds.Where(t => t.Id == zoneId);
        }

        private static MapMarkerDef CreateQuestMapMarkerDef(Vector3 position, string questName, string conditionDescription)
        {
            return new MapMarkerDef
            {
                Category = _questCategory,
                Color = _questColor,
                ImagePath = _questImagePath,
                Position = position,
                Pivot = _questPivot,
                Text = questName
            };
        }
    }
}
