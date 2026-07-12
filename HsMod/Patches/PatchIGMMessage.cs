using HarmonyLib;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchIGMMessage
        {
            //排队人数
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SplashScreen), "UpdateQueueInfo")]
            public static void PatchSplashScreenUpdateQueueInfo(ref Network.QueueInfo queueInfo)
            {
                if (isIGMMessageShow.Value)
                {
                    UIStatus.Get()?.AddInfo(string.Concat(new string[] {
                                "当前排队人数：",
                                queueInfo.position.ToString(),
                                ", 还剩",
                                (queueInfo.secondsTilEnd / 60L).ToString(),
                                "分钟"
                    }), ((float)(queueInfo.secondsTilEnd)) + 3f);
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"当前排队人数：{queueInfo.position}，还剩{queueInfo.secondsTilEnd / 60L}分钟（{queueInfo.secondsTilEnd}秒）。");
                }
            }


            //拦截削弱补丁信息
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CardListPopup), "Show")]
            public static void PatchCardListPopupShow(ref UIBButton ___m_okayButton)
            {
                if (!isIGMMessageShow.Value)
                {
                    ___m_okayButton.TriggerPress();
                    ___m_okayButton.TriggerRelease();
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.InGameMessage.ViewCountController), "GetViewCount")]
            public static bool PatchGetViewCount(ref int __result, string uid)
            {
                if (isIGMMessageShow.Value) return true;
                __result = 0;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.InGameMessage.UI.MessagePopupDisplay), "GetMessageCount")]
            public static bool PatchGetMessageCount(ref int __result, Hearthstone.InGameMessage.UI.PopupEvent eventID)
            {
                if (isIGMMessageShow.Value) return true;
                __result = 0;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(InnKeepersSpecial), "ShowAdAndIncrementViewCountWhenReady")]
            public static bool PatchInnKeepersSpecial()
            {
                if (isIGMMessageShow.Value) return true;
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(InnKeepersSpecial), "UpdateAdJson")]
            public static bool PatchUpdateAdJson(ref string jsonResponse, ref object param)
            {
                if (!isIGMMessageShow.Value)
                {
                    jsonResponse = "";
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.InGameMessage.ViewCountController), "Serialize")]
            [HarmonyPatch(typeof(Hearthstone.InGameMessage.ViewCountController), "Deserialize")]
            public static bool PatchViewCountController()
            {
                if (isIGMMessageShow.Value) return true;
                return false;
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.InGameMessage.UI.MessagePopupDisplay), "DisplayIGMMessage")]
            public static void PatchDisplayIGMMessage(Hearthstone.InGameMessage.UI.MessagePopupDisplay __instance)
            {
                if (isIGMMessageShow.Value) return;
                Traverse.Create(__instance).Method("OnMessageClosed").GetValue();
                Traverse.Create(__instance).Method("OnMessageClosed").GetValue();
            }

            //天梯结算奖励
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SeasonEndDialog), "ShowWhenReady")]
            public static bool PatchSeasonEndDialog(SeasonEndDialog __instance)
            {
                if (isIGMMessageShow.Value) return true;
                Traverse.Create(__instance).Method("Finish").GetValue();
                return false;
            }

            //屏蔽借用套牌时限已到期
            [HarmonyPrefix]
            [HarmonyPatch(typeof(LoanerDeckDisplay), "CanShowTimerExpiredState")]
            public static bool PatchCanShowTimerExpiredState(ref bool __result)
            {
                if (isIGMMessageShow.Value) return true;
                __result = false;
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(LoanerDeckDisplay), "ShouldLoanerDecksBeDisplayed")]
            public static bool PatchShouldLoanerDecksBeDisplayed(ref bool __result)
            {
                if (isIGMMessageShow.Value) return true;
                __result = false;
                return false;
            }

            // 屏蔽推广活动领取的奖励
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BoosterPackReward), "ShowReward")]
            public static void PatchBoosterPackReward_ShowReward(PegUIElement ___m_clickCatcher, BoosterPackReward __instance)
            {
                if (isIGMMessageShow.Value)
                    return;

                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "[Patch] BoosterPackReward");
                __instance.StartCoroutine(Utils.PegUIElementDelayClick(___m_clickCatcher, 1f));
            }
        }
    }
}
