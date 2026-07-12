using HarmonyLib;
using System.Reflection;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchLogArchive
        {
            [HarmonyTargetMethod]
            private static MethodInfo PublicLogArchiveLogPath()
            {
                return AccessTools.TypeByName("LogSessionConfig").GetMethod("get_LogSessionDirectory"); ;
            }

            [HarmonyPostfix]
            public static void PatchLogPathGetter(string __result)
            {
                if (hsLogPath.Value != __result)
                    hsLogPath.Value = __result;
            }
        }
    }
}
