using HarmonyLib;
using System;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchAntiCheat
        {
            //禁用反作弊
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
