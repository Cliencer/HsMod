using HarmonyLib;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchMercenaries
        {
            public static readonly float m_FieldOfViewDefault = BoardCameras.Get().m_FieldOfViewDefault;
            public static readonly float m_FieldOfViewZoomed = BoardCameras.Get().m_FieldOfViewZoomed;
            //禁止战斗缩放
            [HarmonyPrefix]
            [HarmonyPatch(typeof(LettuceMissionEntity), "SetFullScreenFXForCombat")]
            public static bool PatchSetFullScreenFXForCombat()
            {
                if (isMercenaryBattleZoom.Value)
                {
                    return true;
                }
                else
                {
                    BoardCameras.Get().m_FieldOfViewDefault = m_FieldOfViewDefault;
                    BoardCameras.Get().m_FieldOfViewZoomed = m_FieldOfViewDefault;
                    return false;
                }
            }


        }
    }
}
