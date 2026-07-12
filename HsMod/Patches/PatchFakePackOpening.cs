using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchFakePackOpening
        {
            //激活开包模拟
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GameUtils), "IsFakePackOpeningEnabled")]
            public static bool PatchIsFakePackOpeningEnabled(ref bool __result)
            {
                if (isFakeOpenEnable.Value == false)
                {
                    return true;
                }
                __result = true;
                return false;
            }
            //设置模拟卡包数量
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GameUtils), "GetFakePackCount")]
            public static bool PatchGetFakePackCount(ref int __result)
            {
                if (isFakeOpenEnable.Value == false)
                {
                    return true;
                }
                __result = fakePackCount.Value >= 0 ? fakePackCount.Value : 0;
                return false;
            }
            //设置模拟卡包类型
            [HarmonyPrefix]
            [HarmonyPatch(typeof(NetCache), "GetTestData")]
            public static bool PatchGetTestData(ref Type type, ref object __result)
            {
                if (isFakeOpenEnable.Value == false)
                {
                    return true;
                }
                if (type == typeof(NetCache.NetCacheBoosters) && GameUtils.IsFakePackOpeningEnabled())
                {
                    NetCache.NetCacheBoosters netCacheBoosters = new NetCache.NetCacheBoosters();
                    int fakePackCount = GameUtils.GetFakePackCount();
                    NetCache.BoosterStack boosterStack = new NetCache.BoosterStack
                    {
                        Id = (int)fakeBoosterDbId.Value,
                        Count = fakePackCount
                    };
                    netCacheBoosters.BoosterStacks.Add(boosterStack);
                    __result = netCacheBoosters;
                    return false;
                }
                __result = null;
                return false;
            }

            //模拟开包
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(MonoBehaviour), "StopCoroutine", new Type[] { typeof(Coroutine) })]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void PackOpeningBaseStopCoroutine(PackOpening instance, Coroutine routine) {; }
            private static readonly MethodInfo onDirectorFinished = typeof(PackOpening).GetMethod("OnDirectorFinished",
                BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo onBoosterOpened = typeof(PackOpening).GetMethod("OnBoosterOpened",
                BindingFlags.Instance | BindingFlags.NonPublic);
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PackOpening), "OpenBooster")]
            public static bool PatchOpenBooster(ref UnopenedPack pack,
                                                ref int numPacks,
                                                ref PackOpening __instance,
                                                ref float ___m_packOpeningStartTime,
                                                ref int ___m_packOpeningId,
                                                ref GameObject ___m_InputBlocker,
                                                ref Coroutine ___m_autoOpenPackCoroutine,
                                                ref PackOpeningDirector ___m_director,
                                                ref int ___m_lastOpenedBoosterId,
                                                ref UIBScrollable ___m_UnopenedPackScroller
                )
            {
                if (isFakeOpenEnable.Value == false)
                {
                    return true;
                }

                Hearthstone.Progression.AchievementManager.Get().PauseToastNotifications();
                int num = (int)fakeBoosterDbId.Value;
                if (!GameUtils.IsFakePackOpeningEnabled())
                {
                    num = pack.GetBoosterId();
                    ___m_packOpeningStartTime = Time.realtimeSinceStartup;
                    ___m_packOpeningId = num;
                    BoosterPackUtils.OpenBooster(num, numPacks);
                }
                ___m_InputBlocker.SetActive(true);
                if (___m_autoOpenPackCoroutine != null)
                {
                    PackOpeningBaseStopCoroutine(__instance, ___m_autoOpenPackCoroutine);
                    ___m_autoOpenPackCoroutine = null;
                }

                object target = __instance;
                Delegate myDelegate = Delegate.CreateDelegate(typeof(EventHandler), target, onDirectorFinished);
                EventHandler myMethod = myDelegate as EventHandler;
                ___m_director.OnFinishedEvent += myMethod;
                ___m_lastOpenedBoosterId = num;
                BnetBar.Get().HideCurrencyFrames(false);
                if (GameUtils.IsFakePackOpeningEnabled())
                {
                    onBoosterOpened?.Invoke(__instance, null);
                }
                ___m_UnopenedPackScroller.Pause(pause: true);
                return false;
            }
            // 开包结果替换
            private static readonly MethodInfo triggerHooverDeath = typeof(PackOpening).GetMethod("TriggerHooverDeath", BindingFlags.Instance | BindingFlags.NonPublic);
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PackOpening), "OnBoosterOpened")]
            public static bool PatchOnBoosterOpened(ref float ___m_packOpeningStartTime,
                                               ref PackOpeningDirector ___m_director,
                                               ref int ___m_lastOpenedBoosterId,
                                               ref int ___m_packOpeningId,
                                               ref bool ___m_autoOpenPending,
                                               ref UnopenedPack ___m_draggedPack,
                                               ref UnopenedPack ___m_selectedPack,
                                               ref GameObject ___m_centerPack,
                                               ref PackOpening __instance)
            {
                if (isFakeOpenEnable.Value == false)
                {
                    return true;
                }

                triggerHooverDeath?.Invoke(__instance, null);

                float timeToRegisterPackOpening = Time.realtimeSinceStartup - ___m_packOpeningStartTime;

                if (___m_centerPack != null)
                {
                    UnityEngine.Object.Destroy(___m_centerPack);
                    ___m_centerPack = null;
                }

                ___m_director.Play(___m_lastOpenedBoosterId, timeToRegisterPackOpening, ___m_packOpeningId);
                ___m_director.SetNumPacksOpened(1);
                ___m_autoOpenPending = false;
                bool isCatchup = (bool)(GameDbf.Booster?.GetRecord(___m_lastOpenedBoosterId)?.IsCatchupPack);
                //List<NetCache.BoosterCard> list = Network.Get().OpenedBooster();
                if (isFakeRandomResult.Value)
                {
                    Utils.GenerateRandomCard(isFakeRandomRarity.Value, isFakeRandomPremium.Value);
                }

                List<NetCache.BoosterCard> cards = new List<NetCache.BoosterCard>
            {
                new NetCache.BoosterCard
                {
                    Def = {
                            Name = GameUtils.TranslateDbIdToCardId(fakeCardID1.Value),
                            Premium = fakeCardPremium1.Value
                        }
                },
                new NetCache.BoosterCard
                {
                    Def = {
                            Name = GameUtils.TranslateDbIdToCardId(fakeCardID2.Value),
                            Premium = fakeCardPremium2.Value
                        }
                },
                new NetCache.BoosterCard
                {
                    Def = {
                            Name = GameUtils.TranslateDbIdToCardId(fakeCardID3.Value),
                            Premium = fakeCardPremium3.Value
                        }
                },
                new NetCache.BoosterCard
                {
                    Def = {
                            Name = GameUtils.TranslateDbIdToCardId(fakeCardID4.Value),
                            Premium = fakeCardPremium4.Value
                        }
                },
                new NetCache.BoosterCard
                {
                    Def = {
                            Name = GameUtils.TranslateDbIdToCardId(fakeCardID5.Value),
                            Premium = fakeCardPremium5.Value
                        }
                }
            };

                if (isCatchup)
                {
                    int catchupCount = UnityEngine.Random.Range(0, 999);
                    catchupCount = fakeCatchupCount.Value < 5 ? catchupCount : fakeCatchupCount.Value - 5;
                    for (int i = 0; i < catchupCount; i++)
                    {
                        cards.Add(Utils.GenerateRandomACard(isFakeRandomRarity.Value, isFakeRandomPremium.Value));
                    }

                }
                ___m_director.OnBoosterOpened(cards, isCatchup);
                return false;
            }
        }
    }
}
