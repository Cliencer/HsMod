using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchAntiCheat
        {
            //禁用反作弊
            private static IEnumerator<Blizzard.T5.Jobs.IAsyncJobResult> EmptyCoroutine()
            {
                yield break;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "Initialize")]
            public static bool PatchAntiCheatManagerInitialize(ref IEnumerator<Blizzard.T5.Jobs.IAsyncJobResult> __result, Blizzard.T5.Services.ServiceLocator serviceLocator)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "AntiCheat Initialize feature is disabled.");
                __result = EmptyCoroutine();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "OnLoginComplete")]
            public static bool PatchAntiCheatManagerOnLoginComplete()
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "AntiCheat OnLoginComplete feature is disabled.");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "Shutdown")]
            public static bool PatchAntiCheatManagerShutdown()
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "AntiCheat Shutdown feature is disabled.");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "TryCallSDK")]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "CallInterfaceCallSDK")]
            public static bool PatchAntiCheatManagerTryCallSDK(ref string scriptId)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "AntiCheat TryCallSDK feature is disabled.");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(AntiCheatSDK.AntiCheatManager), "InnerSDKMethodCall")]
            public static bool PatchAntiCheatManagerInnerSDKMethodCall(ref Action<string> handler, ref string args)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "AntiCheat InnerSDKMethodCall feature is disabled.");
                return false;
            }
        }
    }
}
