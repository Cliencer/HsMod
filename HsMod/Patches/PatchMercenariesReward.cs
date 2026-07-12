using HarmonyLib;
using System;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchMercenariesReward
        {
            // 屏蔽+1+5提示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RewardPopups), "ShowMercenariesFullyUpgraded")]
            public static bool PatchShowMercenariesFullyUpgraded(ref Action doneCallback, ref bool __result, ref RewardPopups __instance, ref Action ___OnPopupClosed)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    var notices = NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices;
                    for (int i = 0; i < notices.Count; i++)
                    {
                        var notice = notices[i];
                        if (notice != null)
                        {
                            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"NetCacheProfileNotices {notice.Origin} {notice.Type} {notice.NoticeID}");
                            Network.Get().AckNotice(notice.NoticeID);
                        }
                    }
                    NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices.Clear();

                    NetCache.ProfileNoticeMercenariesMercenaryFullyUpgraded upgradeNotice = (NetCache.ProfileNoticeMercenariesMercenaryFullyUpgraded)Traverse.Create(__instance).Field("GetNextMercenaryFullUpgradedToShow")?.GetValue();
                    if (upgradeNotice != null)
                    {
                        Network.Get().AckNotice(upgradeNotice.NoticeID);
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"ShowMercenariesFullyUpgraded {upgradeNotice.NoticeID}");
                        doneCallback?.Invoke();
                        ___OnPopupClosed?.Invoke();
                    }
                    __result = false;
                    return false;
                }
                else return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardPopups), "GetNextMercenaryFullUpgradedToShow")]
            public static void PatchGetNextMercenaryFullUpgradedToShow(ref NetCache.ProfileNoticeMercenariesMercenaryFullyUpgraded __result)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    if (__result != null)
                    {
                        Network.Get().AckNotice(__result.NoticeID);
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"GetNextMercenaryFullUpgradedToShow {__result.NoticeID}");
                    }
                    __result = null;

                    var notices = NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices;
                    for (int i = 0; i < notices.Count; i++)
                    {
                        var notice = notices[i];
                        if (notice != null)
                        {
                            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"NetCacheProfileNotices {notice.Origin} {notice.Type} {notice.NoticeID}");
                            Network.Get().AckNotice(notice.NoticeID);
                        }
                    }
                    NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices.Clear();
                }
            }

            // 屏蔽佣兵冒险解锁提示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RewardPopups), "ShowMercenariesZoneUnlockPopup")]
            public static bool PatchShowMercenariesZoneUnlockPopup(ref Action onPopupCompleteCallback, ref bool __result, ref RewardPopups __instance, ref Action ___OnPopupClosed)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    var notices = NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices;
                    for (int i = 0; i < notices.Count; i++)
                    {
                        var notice = notices[i];
                        if (notice != null)
                        {
                            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"NetCacheProfileNotices {notice.Origin} {notice.Type} {notice.NoticeID}");
                            Network.Get().AckNotice(notice.NoticeID);
                        }
                    }
                    NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices.Clear();

                    NetCache.ProfileNoticeMercenariesZoneUnlock zoneNotice = (NetCache.ProfileNoticeMercenariesZoneUnlock)Traverse.Create(__instance).Field("GetNextMercenariesZoneUnlockToShow")?.GetValue();
                    if (zoneNotice != null)
                    {
                        Network.Get().AckNotice(zoneNotice.NoticeID);
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"ShowMercenariesZoneUnlockPopup {zoneNotice.NoticeID}");
                        onPopupCompleteCallback?.Invoke();
                        ___OnPopupClosed?.Invoke();
                    }
                    __result = false;
                    return false;
                }
                else return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardPopups), "GetNextMercenariesZoneUnlockToShow")]
            public static void PatchGetNextMercenariesZoneUnlockToShow(ref NetCache.ProfileNoticeMercenariesZoneUnlock __result)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    if (__result != null)
                    {
                        Network.Get().AckNotice(__result.NoticeID);
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"GetNextMercenariesZoneUnlockToShow {__result.NoticeID}");
                    }
                    __result = null;

                    var notices = NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices;
                    for (int i = 0; i < notices.Count; i++)
                    {
                        var notice = notices[i];
                        if (notice != null)
                        {
                            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"NetCacheProfileNotices {notice.Origin} {notice.Type} {notice.NoticeID}");
                            Network.Get().AckNotice(notice.NoticeID);
                        }
                    }
                    NetCache.Get().GetNetObject<NetCache.NetCacheProfileNotices>().Notices.Clear();
                }
            }

            //天梯奖励
            [HarmonyPrefix]
            [HarmonyPatch(typeof(MercenariesSeasonRewardsDialog), "Show")]
            [HarmonyPatch(typeof(MercenariesSeasonRewardsDialog), "ShowWhenReady")]
            public static bool PatchPatchSeasonEndDialog(MercenariesSeasonRewardsDialog __instance)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    Traverse.Create(__instance).Field("AckAndHide").GetValue();
                    return false;
                }
                else return true;
            }

            //天梯奖励
            [HarmonyPrefix]
            [HarmonyPatch(typeof(DialogManager), "ProcessMercenariesSeasonRewardsDialog")]
            public static bool PatchProcessMercenariesSeasonRewardsDialog(ref DialogManager.DialogRequest request,
                                                                          ref MercenariesSeasonRewardsDialog dialog)
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    MercenariesSeasonRewardsDialog.Info info = (MercenariesSeasonRewardsDialog.Info)request.m_info;
                    Network.Get().AckNotice(info.m_noticeId);
                    return false;
                }
                else return true;
            }

            //不显示佣兵宝箱奖励
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RewardPopups), "ShowMercenariesRewards")]
            public static bool PatchShowMercenariesRewards(ref bool autoOpenChest,
                                                           ref NetCache.ProfileNoticeMercenariesRewards rewardNotice,
                                                           ref NetCache.ProfileNoticeMercenariesRewards bonusRewardNotice,
                                                           ref Action doneCallback,
                                                           ref RewardPopups __instance,
                                                           ref bool __result
                                                           )
            {
                if (isAutoRecvMercenaryRewardEnable.Value)
                {
                    autoOpenChest = true;
                    if (rewardNotice != null)
                    {
                        Network.Get().AckNotice(rewardNotice.NoticeID);
                        if (bonusRewardNotice != null)
                        {
                            Network.Get().AckNotice(bonusRewardNotice.NoticeID);
                        }
                        doneCallback?.Invoke();
                        Traverse.Create(__instance).Field("OnPopupClosed").GetValue();
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
