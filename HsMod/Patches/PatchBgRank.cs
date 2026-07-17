using HarmonyLib;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        //在排行榜浮层玩家名字下方追加其MMR
        //（游戏自身会以显示名调用UpdatePlayerNameText）。
        public class PatchBgRank
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerLeaderboardMainCardActor), "UpdatePlayerNameText")]
            public static void PatchUpdatePlayerNameText(ref string text)
            {
                if (!isBgRankEnable.Value) return;
                if (!BgRankOverlay.IsReady) return;    //榜单尚未加载——不处理
                if (string.IsNullOrWhiteSpace(text)) return;
                //BattleTag不含空格：带空格的名字是隐藏/占位昵称，
                //对其显示"8000↓"会造成误导，因此不追加任何内容
                if (text.IndexOf(' ') >= 0) return;

                string mmrStr = BgRankOverlay.TryGetMmr(text, out int mmr)
                    ? mmr.ToString()
                    : LocalizationManager.GetLangValue("bgRank.unranked");

                text = $"{text}\n{LocalizationManager.GetLangValue("bgRank.label")} {mmrStr}";
            }
        }
    }
}
