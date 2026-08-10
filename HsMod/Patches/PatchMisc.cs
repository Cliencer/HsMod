using Blizzard.T5.Core.Time;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchMisc
        {
            //移除分辨率限制
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GraphicsResolution), "IsAspectRatioWithinLimit")]
            public static bool PatchAspectRatioWithinLimit(ref bool __result, int width, int height, bool isWindowedMode)
            {
                __result = true;
                return false;
            }
            //移除分辨率限制V2 
            /*
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ResizeManagerV2), "Update")]
            public static bool PatchResizeManagerV2Update(ref float ___m_minResolutionResizeDelay)
            {
                ___m_minResolutionResizeDelay = UnityEngine.Time.time + 114514;
                return true;
            }
            */
            // 上面的方法会导致偶尔出现炉石窗口消失，这里用transpiler修改分辨率值
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ResizeManagerV2), "Update")]
            public static IEnumerable<CodeInstruction> Patch_ResizeManagerV2_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                int counter = 0;
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Ldc_I4 && code.operand is int v && v == 400)
                    {
                        // 偶数位当成 width -> 144；奇数位当成 height -> 108
                        code.operand = (counter % 2 == 0) ? 144 : 108;
                        counter++;
                    }
                    yield return code;
                }
            }

            //命令行修改分辨率，阻止炉石自修改
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Screen), "SetResolution", new Type[] { typeof(int), typeof(int), typeof(bool) })]
            public static bool PatchSetResolution(ref int width, ref int height, ref bool fullscreen)
            {
                if (CommandConfig.width > 0 && CommandConfig.height > 0)
                {
                    if (width == CommandConfig.width && height == CommandConfig.height && fullscreen == false)
                    {
                        return true;
                    }
                    else if (Options.Get().GetInt(Option.GFX_WIDTH) == width && Options.Get().GetInt(Option.GFX_HEIGHT) == height)
                    {
                        return false;
                    }
                }
                return true;
            }

            // 帧率修改
            [HarmonyPrefix, HarmonyPatch(typeof(Options), nameof(Options.GetInt), new Type[] { typeof(Option) })]
            public static bool PatchOptionsGetInt(ref Option option, ref int __result)
            {
                if (option == Option.GFX_TARGET_FRAME_RATE && targetFrameRate.Value > 0)
                {
                    __result = targetFrameRate.Value;
                    return false;
                }
                else return true;
            }
            //[HarmonyPrefix, HarmonyPatch(typeof(GraphicsManager), "UpdateFramerateSettings")]
            //public static void PatchGraphicsManagerUpdateFramerateSettings()
            //{
            //    if (targetFrameRate.Value > 0)
            //    {
            //        Options.Get()?.SetInt(Option.GFX_TARGET_FRAME_RATE, targetFrameRate.Value); ;
            //    }
            //}

            //使用WebToken登录
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Hearthstone.Login.DesktopLoginTokenFetcher), "GetTokenFromTokenFetcher")]
            public static IEnumerable<CodeInstruction> PatchGetTokenFromTokenFetcher(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                list.RemoveAt(0);
                list.InsertRange(0, new CodeInstruction[4]
                {
                new CodeInstruction(OpCodes.Ldstr, "Aurora.VerifyWebCredentials"),
                new CodeInstruction(OpCodes.Call, new Func<string, Blizzard.T5.Configuration.VarKey>(Blizzard.T5.Configuration.Vars.Key).Method),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Callvirt, typeof(Blizzard.T5.Configuration.VarKey).GetMethod("GetStr", BindingFlags.Instance | BindingFlags.Public))
                });
                return list;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(Blizzard.T5.Configuration.ConfigFile), "Load", new Type[] { typeof(string), typeof(bool) })]
            public static void PatchPreConfigFileLoad(ref string path,
                                                      ref bool ignoreUselessLines,
                                                      Blizzard.T5.Configuration.ConfigFile __instance,
                                                      out string __state)
            {
                __state = null;
                string pattern = @"(CN|KR|TW|EU|US)\-[a-f0-9]{32}\-\d+";    // Country-Token-AccountID.Low
                string argv = String.Join(" ", Environment.GetCommandLineArgs());
                var res = System.Text.RegularExpressions.Regex.Match(argv, pattern);
                if (res.Success)
                {
                    __state = res.Value;
                    if (System.IO.File.Exists(path))
                    {
                        string configText = System.IO.File.ReadAllText(path);
                        if (!String.IsNullOrEmpty(configText) && configText.Contains("Aurora") && configText.Contains("VerifyWebCredentials") && configText.Contains("Env"))
                            return;
                    }

                    string configPath = "";
                    if (System.IO.Directory.Exists(Hearthstone.Util.PlatformFilePaths.CachePath))
                    {
                        configPath = Hearthstone.Util.PlatformFilePaths.CachePath;
                    }
                    //else if (System.IO.Directory.Exists(Hearthstone.Util.PlatformFilePaths.PersistentDataPath + "/Cache/"))
                    //{
                    //    configPath = Hearthstone.Util.PlatformFilePaths.PersistentDataPath + "/Cache/";
                    //}
                    else if (System.IO.Directory.Exists(BepInEx.Paths.ConfigPath))
                    {
                        configPath = BepInEx.Paths.ConfigPath;
                    }
                    else
                    {
                        configPath = "./";
                    }
                    path = System.IO.Path.Combine(configPath, "HsClient.config");
                    System.IO.File.WriteAllText(path, "[Config]\r\nVersion = 3\r\n[Aurora]\r\nVerifyWebCredentials = \"token\"\r\nClientCheck = 0\r\nEnv.Override = 1\r\nEnv = cn.actual.battlenet.com.cn\r\n");
                }
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, $"client.config => {path}");
            }
            [HarmonyPostfix, HarmonyPatch(typeof(Blizzard.T5.Configuration.ConfigFile), "Load", new Type[] { typeof(string), typeof(bool) })]
            public static void PatchPostConfigFileLoad(ref string path,
                                                       ref bool ignoreUselessLines,
                                                       Blizzard.T5.Configuration.ConfigFile __instance,
                                                       string __state)
            {
                if (String.IsNullOrEmpty(__state))
                {
                    return;
                }

                __instance.Set("Aurora.VerifyWebCredentials", __state);
                if (__state.Substring(0, 2).ToLower() == "cn")
                    __instance.Set("Aurora.Env", "cn.actual.battlenet.com.cn");
                else
                    __instance.Set("Aurora.Env", __state.Substring(0, 2).ToLower() + ".actual.battle.net");
            }

            //禁止发送错误报告
            [HarmonyPrefix]
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.ExceptionReporterControl), "ExceptionReportInitialize")]
            public static void PatchExceptionReporterControl()
            {
                Blizzard.BlizzardErrorMobile.ExceptionReporter.Get().SendExceptions = false;
                //Vars.Key("Application.SendExceptions")
            }

            //屏蔽错误报告
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Blizzard.BlizzardErrorMobile.ExceptionReporter), "ReportCaughtException", new Type[] { typeof(Exception) })]
            public static bool PatchReportCaughtException(ref Exception exception)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "message:" + exception.Message + "\nInnerException:\n" + exception.InnerException + "\nStackTrace:\n" + exception.StackTrace);
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Blizzard.BlizzardErrorMobile.ExceptionReporter), "ReportExceptions")]
            public static bool PatchExceptionReporterReportExceptions()
            {
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Blizzard.BlizzardErrorMobile.ExceptionReporter), "SetSettings")]
            public static bool PatchExceptionReporterSetSettings(ref Blizzard.BlizzardErrorMobile.ExceptionSettings settings, ref bool __result)
            {
                settings = null;
                __result = false;
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Blizzard.BlizzardErrorMobile.ExceptionReporter), "ExceptionSubmitURL", MethodType.Getter)]
            public static bool PatchExceptionReporterSubmitURLGetter(ref System.Uri __result)
            {
                __result = new Uri(string.Format("http://127.0.0.1/submit/{0}", Blizzard.BlizzardErrorMobile.ReportBuilder.Settings.m_projectID));
                return false;
            }

            // login success 
            [HarmonyPostfix]
            [HarmonyPatch(typeof(LoginManager), "OnLoginComplete")]
            public static void PatchOnLoginComplete()
            {
                Utils.OnHsLoginCompleted();
                //Vars.Key("Application.SendExceptions")
            }


            //禁用掉线
            [HarmonyPrefix]
            [HarmonyPatch(typeof(InactivePlayerKicker), "SetShouldCheckForInactivity")]
            public static void PatchSetShouldCheckForInactivity(ref bool check)
            {
                if (!isIdleKickEnable.Value)
                {
                    check = false;
                }
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(InactivePlayerKicker), "Update")]
            public static void PatchInactivePlayerKickerUpdate()
            {
                if (!isIdleKickEnable.Value)
                {
                    InactivePlayerKicker.Get().SetShouldCheckForInactivity(isIdleKickEnable.Value);
                }
            }

            ////自动重启 没有效果
            //[HarmonyPrefix]
            //[HarmonyPatch(typeof(Application), "Quit", new Type[] {  })]
            //public static void PatchApplicationQuit()
            //{
            //    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //    //System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //    System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //}

            //报错退出
            [HarmonyPrefix]
            [HarmonyPatch(typeof(DialogManager), "ShowReconnectHelperDialog")]
            [HarmonyPatch(typeof(ReconnectHelperDialog), "Show")]
            [HarmonyPatch(typeof(Network), "OnFatalBnetError")]
            public static bool PatchError()
            {
                if (isAutoExit.Value)
                {
                    Utils.Quit();
                    return false;
                }
                else return true;
            }

            //是否拦截弹窗
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AlertPopup), "Show")]
            public static bool PatchAlertPopupShow(ref UIBButton ___m_okayButton, ref UIBButton ___m_confirmButton, ref UIBButton ___m_cancelButton, ref AlertPopup.PopupInfo ___m_popupInfo)
            {
                if (isAlertPopupShow.Value) return true;
                else
                {
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTING_HEADER"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTING_LOGIN"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTING"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTED_HEADER"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTED_LOGIN"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTED"));
                    //Logger.LogWarning(GameStrings.Get("GLOBAL_RECONNECT_RECONNECTED_LOGIN"));

                    if (___m_popupInfo.m_text == GameStrings.Get("GLOBAL_RECONNECT_RECONNECTING_LOGIN"))
                    {
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, ___m_popupInfo.m_text);
                        return true;
                    }

                    switch (responseAlertPopup.Value)
                    {
                        case Utils.AlertPopupResponse.OK:
                            ___m_okayButton.TriggerPress();
                            ___m_okayButton.TriggerRelease();
                            break;
                        case Utils.AlertPopupResponse.CONFIRM:
                            ___m_confirmButton.TriggerPress();
                            ___m_confirmButton.TriggerRelease();
                            break;
                        case Utils.AlertPopupResponse.CANCEL:
                            ___m_cancelButton.TriggerPress();
                            ___m_cancelButton.TriggerRelease();
                            break;
                        case Utils.AlertPopupResponse.YES:
                            if (___m_confirmButton.gameObject.activeSelf)
                            {
                                ___m_confirmButton.TriggerPress();
                                ___m_confirmButton.TriggerRelease();
                                break;
                            }
                            else
                            {
                                ___m_okayButton.TriggerPress();
                                ___m_okayButton.TriggerRelease();
                                break;
                            }
                    }
                    return false;
                }
            }
            //处理断线重连
            [HarmonyPostfix]
            [HarmonyPatch(typeof(AlertPopup), "UpdateInfo")]
            public static void PatchAlertPopupUpdateInfo(ref AlertPopup.PopupInfo info, ref UIBButton ___m_okayButton, ref UIBButton ___m_confirmButton, ref UIBButton ___m_cancelButton, ref AlertPopup.PopupInfo ___m_popupInfo)
            {
                if (isAlertPopupShow.Value) return;
                else if (info.m_text == GameStrings.Get("GLOBAL_RECONNECT_TIMEOUT") || ___m_popupInfo.m_text == GameStrings.Get("GLOBAL_RECONNECT_TIMEOUT"))
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, info.m_text);
                    if (___m_confirmButton.gameObject.activeSelf)
                    {
                        ___m_confirmButton.TriggerPress();
                        ___m_confirmButton.TriggerRelease();
                    }
                    else
                    {
                        ___m_okayButton.TriggerPress();
                        ___m_okayButton.TriggerRelease();
                    }
                }
            }

            //处理置换
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RedundantNDEPopup), "Show")]
            public static void AutoClickRedundantNDE(UIBButton ___m_rerollButton, RedundantNDEPopup __instance)
            {
                if (!isAutoRedundantNDE.Value)
                    return;

                __instance.StartCoroutine(Utils.UIBButtonDelayClick(___m_rerollButton, 1f));
            }

            //处理未领取的奖励
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardTrackSeasonRoll), "Show")]
            public static void Patch_RewardTrackSeasonRoll_Show(RewardTrackSeasonRoll __instance)
            {
                if (isAlertPopupShow.Value)
                    return;

                __instance.ShowChooseOneRewardPickerPopup();
            }



            //屏蔽开屏防沉迷提示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SplashScreen), "GetRatingsScreenRegion")]
            public static bool PatchGetRatingsScreenRegion()
            {
                return false;
            }

            //结算任务、升级提示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(QuestPopups), "ShowNextQuestNotification")]
            [HarmonyPatch(typeof(EndGameScreen), "ShowMercenariesExperienceRewards")]
            public static bool PatchEndGameScreen()
            {
                return isRewardToastShow.Value;
            }

            //战令、成就等奖励领取提示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.Progression.RewardTrack), "HandleRewardGranted")]
            public static bool PatchHandleRewardGranted(int rewardTrackId, int level, PegasusShared.RewardTrackPaidType paidType, List<PegasusUtil.RewardItemOutput> rewardItemOutput)      //隐藏通行证奖励
            {
                if (!isRewardToastShow.Value)
                {
                    RewardTrackDbfRecord record = GameDbf.RewardTrack.GetRecord(rewardTrackId);
                    if (record == null)
                    {
                        return false;
                    }

                    Hearthstone.Progression.RewardTrackManager.Get()?.GetRewardTrack(record.RewardTrackType)?.AckReward(rewardTrackId, level, paidType);
                    return false;
                }
                else return true;
            }

            //测试补丁，屏蔽奖励显示
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.Progression.RewardPresenter), "ShowNextReward")]
            public static bool PatchRewardPresenterShowNextReward(Hearthstone.Progression.RewardPresenter __instance, ref Action onHiddenCallback)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "ShowNextReward");
                if (!isRewardToastShow.Value)
                {
                    __instance.Clear();
                    //onHiddenCallback?.Invoke();

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
                return true;
            }

            //清空自动分解的Handler
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManager), "RegisterCollectionNetHandlers")]
            public static void PatchRegisterCollectionNetHandlers()
            {
                try
                {
                    Network.Get().RemoveNetHandler(PegasusUtil.BoughtSoldCard.PacketID.ID, new Network.NetHandler(Utils.TryRefundCardDisenchantCallback));
                }
                catch
                {
                    ;
                }
            }

            //快速开包
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PackOpening), "AutomaticallyOpenPack")]
            public static IEnumerable<CodeInstruction> PatchAutomaticallyOpenPack(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindLastIndex((CodeInstruction x) => x.opcode == OpCodes.Ret);
                if (num > 0)
                {
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsQuickPackOpeningEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldfld, typeof(PackOpening).GetField("m_director", BindingFlags.Instance | BindingFlags.NonPublic)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, new Func<PackOpeningDirector, IEnumerator>(PackOpeningDirectorPatch.ForceRevealAllCards).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, typeof(MonoBehaviour).GetMethod("StartCoroutine", BindingFlags.Instance | BindingFlags.Public, null, new Type[1]
                    {
                    typeof(IEnumerator)
                    }, null)));
                    list.Insert(num, new CodeInstruction(OpCodes.Pop));
                }
                return list;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PackOpening), "SpaceToOpenPack")]
            public static bool PatchSpaceToOpenPack(ref bool __result)
            {
                if (PackOpeningDirectorPatch.m_WaitingForAllCardsRevealed)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PackOpening), "HandleKeyboardInput")]
            public static bool PatchPackOpeningHandleKeyboardInput(ref bool __result)
            {
                if (PackOpeningDirectorPatch.m_WaitingForAllCardsRevealed && isAutoPackOpeningEnable.Value && isQuickPackOpeningEnable.Value)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            //尝试分解
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PackOpeningDirector), "OnSpellFinished")]
            public static void PatchPackOpeningDirectorOnSpellFinished()
            {
                PackOpeningDirectorPatch.m_WaitingForCards = false;
                //Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "PackOpeningDirector.OnSpellFinished");
                if (isAutoRefundCardDisenchantEnable.Value)
                {
                    Utils.TryRefundCardDisenchant();
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PackOpeningDirector), "Awake")]
            public static void PatchPackOpeningDirectorAwake()
            {
                PackOpeningDirectorPatch.m_WaitingForCards = true;
            }

            //展示FPS信息
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ShowFPS), "Awake")]
            public static IEnumerable<CodeInstruction> PatchShowFPSAwake(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                //remove check HearthstoneApplication.IsPublic()
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].opcode == OpCodes.Brfalse_S || list[i].opcode == OpCodes.Brfalse)
                    {
                        num = i;
                        break;
                    }
                }
                num--;
                if (num > 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        list.RemoveAt(num);
                    }

                }
                return list;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ShowFPS), "OnGUI")]
            public static bool PatchOnGUI(ref Camera[] ___m_cameras, ref string ___m_fpsText)
            {
                int num = 0;
                if (___m_cameras.Length < Camera.allCamerasCount)
                {
                    ___m_cameras = new Camera[Camera.allCamerasCount];
                }
                int allCameras = Camera.GetAllCameras(___m_cameras);
                for (int i = 0; i < allCameras; i++)
                {
                    if (___m_cameras[i].TryGetComponent<FullScreenEffects>(out FullScreenEffects fullScreenEffects) && fullScreenEffects.IsActive)
                    {
                        num++;
                    }
                }
                string text = ___m_fpsText;
                try { text = text.Split('.')[0]; }
                catch { text = "-1"; }
                if (isShowFPSEnable.Value)
                {
                    GUI.Box(new Rect(0f, 0f, 0f, 0f), text, new GUIStyle("box")
                    {
                        clipping = TextClipping.Overflow,
                        stretchHeight = true,
                        stretchWidth = true,
                        wordWrap = true,
                        fontSize = 54,
                        alignment = TextAnchor.MiddleCenter,
                        contentOffset = new UnityEngine.Vector2(64f, 48f)
                    });
                }
                return false;
            }

            //展示CardID
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionCardVisual), "EnterCraftingMode")]
            public static bool PatchEnterCraftingMode(CollectionCardVisual __instance)
            {
                if (isShowCollectionCardIdEnable.Value)
                {
                    int dId = -1;
                    CollectionUtils.ViewMode viewMode = CollectionManager.Get().GetCollectibleDisplay().GetViewMode();
                    var thisActor = __instance.GetActor();
                    if (thisActor == null)
                    {
                        return true;
                    }
                    switch (viewMode)
                    {

                        case CollectionUtils.ViewMode.BATTLEGROUNDS_GUIDE_SKINS:
                        case CollectionUtils.ViewMode.BATTLEGROUNDS_HERO_SKINS:
                        case CollectionUtils.ViewMode.COINS:
                        case CollectionUtils.ViewMode.CARDS:
                        case CollectionUtils.ViewMode.HERO_SKINS:
                            if (__instance.CardId != "") dId = GameUtils.TranslateCardIdToDbId(__instance.CardId);
                            break;
                        case CollectionUtils.ViewMode.CARD_BACKS:
                            dId = __instance.GetActor().GetComponent<CollectionCardBack>().GetCardBackId();
                            break;
                        default:
                            return true;
                    }
                    UIStatus.Get().AddInfo($"ID: {dId}");
                    ClipboardUtils.CopyToClipboard(dId.ToString());
                }
                return true;
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaconFinisherCollectionDetails), "Show")]
            public static void PatchShowFinisherDetailsDisplay(ref Hearthstone.DataModels.BattlegroundsFinisherDataModel ___m_dataModel)
            {
                if (isShowCollectionCardIdEnable.Value)
                {
                    UIStatus.Get().AddInfo($"ID: {___m_dataModel?.FinisherDbiId}");
                }
            }
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaconBoardCollectionDetails), "Show")]
            public static void PatchShowBoardDetailsDisplay(ref Hearthstone.DataModels.BattlegroundsBoardSkinDataModel ___m_dataModel)
            {
                if (isShowCollectionCardIdEnable.Value)
                {
                    UIStatus.Get().AddInfo($"ID: {___m_dataModel?.BoardDbiId}");
                }
            }

            //允许放弃
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AdventureDungeonCrawlDisplay), "Update")]
            public static void PatchAdventureDungeonCrawlDisplayUpdate(ref GameObject ___m_retireButton)
            {
                if (isShowRetireForever.Value)
                {
                    ___m_retireButton?.SetActive(true);
                }
            }

            // fixme: no ShowClickedStandardDeckInTwistPopup
            //[HarmonyPrefix]
            //[HarmonyPatch(typeof(DeckPickerTrayDisplay), "OnCustomDeckPressed")]
            //private static bool PatchOnCustomDeckPressed(DeckPickerTrayDisplay __instance,
            //    ref CollectionDeckBoxVisual deckbox)
            //{
            //    if (!checkCollDeckValidForMode.Value)
            //    {
            //        return true;
            //    }

            //    if (SceneMgr.Get().GetMode() == SceneMgr.Mode.TOURNAMENT && Options.GetInRankedPlayMode())
            //    {
            //        CollectionDeck collectionDeck = deckbox.GetCollectionDeck();
            //        if (collectionDeck == null)
            //        {
            //            return true;
            //        }

            //        // 如果选择的是标准卡组，且当前模式是狂野模式，则给出提示，防止误触
            //        if (collectionDeck.FormatType == PegasusShared.FormatType.FT_STANDARD &&
            //            (int)Options.GetFormatType() == 1)
            //        {
            //            __instance.ShowClickedStandardDeckInTwistPopup();
            //            return false;
            //        }
            //    }

            //    return true;
            //}

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionDeckBoxVisual), "CanSelectDeck")]
            private static bool PatchCanSelectDeck(CollectionDeckBoxVisual __instance, ref bool __result)
            {
                if (!checkCollDeckValidForMode.Value)
                {
                    return true;
                }

                CollectionDeck collectionDeck = __instance.GetCollectionDeck();
                if (SceneMgr.Get().GetMode() == SceneMgr.Mode.TOURNAMENT && Options.GetInRankedPlayMode() &&
                    collectionDeck.FormatType == PegasusShared.FormatType.FT_STANDARD &&
                    (int)Options.GetFormatType() == 1
                    && collectionDeck.GetTotalInvalidCardCount((PegasusShared.FormatType)1) > 0)
                {
                    __result = false;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionDeck), "IsValidForModeAndFormat")]
            private static bool PatchIsValidForModeAndFormat(CollectionDeck __instance, SceneMgr.Mode mode,
                bool isRanked, PegasusShared.FormatType formatType, ref bool __result)
            {
                if (!checkCollDeckValidForMode.Value)
                {
                    return true;
                }

                if (SceneMgr.Get().GetMode() == SceneMgr.Mode.TOURNAMENT && Options.GetInRankedPlayMode() &&
                    __instance.FormatType == PegasusShared.FormatType.FT_STANDARD && (int)Options.GetFormatType() == 1)
                {
                    __result = false;
                    return false;
                }

                return true;
            }


            //OnApplicationFocus
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Hearthstone.HearthstoneApplication), "OnApplicationFocus")]
            public static bool PatchOnApplicationFocus(bool focus)
            {
                return isOnApplicationFocus.Value;
            }

            //齿轮 需要进行delegate委托
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeScaleMgr), "Update")]
            public static bool PatchTimeScaleMgr(ref float ___m_timeScaleMultiplier, ref float ___m_gameTimeScale)
            {
                if (isTimeGearEnable.Value)
                {
                    float timeScale = 1f;
                    if (timeGear.Value > 1) timeScale = (float)timeGear.Value;
                    else if (timeGear.Value < -1) timeScale = -1f / (float)timeGear.Value;
                    if (timeScale >= 8) timeScale = 8f;
                    else if (timeScale <= -8) timeScale = 0.125f;    // will not exec
                    Time.timeScale = ((timeScale > ___m_timeScaleMultiplier) ? ((timeScale + (___m_timeScaleMultiplier - 1f) * 0.5f) * ___m_gameTimeScale) : ((___m_timeScaleMultiplier + (timeScale - 1f) * 0.5f) * ___m_gameTimeScale));
                    return false;
                }
                else return true;
            }

            //toast变速修改
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SocialToastMgr), "AddToast", new Type[]
            {
                    typeof(UserAttentionBlocker),
                    typeof(string),
                    typeof(SocialToastMgr.TOAST_TYPE),
                    typeof(float),
                    typeof(bool)
            })]
            public static void PatchSocialToastMgrAddToast(ref float __3)
            {
                __3 *= Time.timeScale;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Log), "ConfigureLogSystem")]
            private static void PatchConfigureLogSystem()
            {
                Log.SetStandardLogInfo("LoadingScreen", Blizzard.T5.Logging.LogLevel.Debug);
            }
        }
    }
}
