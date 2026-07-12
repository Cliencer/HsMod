using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchBgsUnlockCollection
        {
            //动态收集需要打补丁的"是否拥有"方法；找不到的自动跳过，避免暴雪改名后游戏崩溃
            public static IEnumerable<MethodBase> TargetMethods()
            {
                List<MethodBase> result = new List<MethodBase>();
                Type cm = typeof(CollectionManager);
                string[] ownNames =
                {
                    "OwnsBattlegroundsHeroSkin",
                    "OwnsBattlegroundsGuideSkin",
                    "OwnsBattlegroundsBoardSkin",
                    "OwnsBattlegroundsFinisher",
                    "OwnsBattlegroundsEmote"
                };
                foreach (MethodInfo m in cm.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.ReturnType == typeof(bool) && Array.IndexOf(ownNames, m.Name) >= 0)
                        result.Add(m);
                }
                //数据模型的 IsOwned（终结技/表情/棋盘）
                string[] dataModels =
                {
                    "Hearthstone.DataModels.BattlegroundsFinisherDataModel",
                    "Hearthstone.DataModels.BattlegroundsEmoteDataModel",
                    "Hearthstone.DataModels.BattlegroundsBoardSkinDataModel"
                };
                foreach (string typeName in dataModels)
                {
                    Type t = AccessTools.TypeByName(typeName);
                    MethodInfo getter = t?.GetProperty("IsOwned")?.GetGetMethod(true);
                    if (getter != null)
                        result.Add(getter);
                }
                //宠物皮肤用的是 Owned（而非 IsOwned）
                MethodInfo petGetter = AccessTools.TypeByName("Hearthstone.DataModels.PetSkinDataModel")?.GetProperty("Owned")?.GetGetMethod(true);
                if (petGetter != null)
                    result.Add(petGetter);
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"PatchBgsUnlockCollection => {result.Count} methods collected");
                return result;
            }

            public static void Postfix(ref bool __result)
            {
                if (isBgsUnlockCollectionEnable.Value)
                    __result = true;
            }

        }
    }
}
