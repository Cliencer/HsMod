using System;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    //酒馆战棋自动静默：对局开始时自动静默全部对手的表情。
    //每场只执行一次，之后玩家手动取消静默不会被覆盖。
    public class BgAutoSquelch : MonoBehaviour
    {
        public static BgAutoSquelch Instance;

        private bool inMatch;
        private bool doneThisMatch;
        private float lastTry;

        private const float RetryInterval = 2f;    //秒：EnemyEmoteHandler可能尚未就绪，定期重试

        public static void Init()
        {
            if (Instance != null) return;
            Instance = new GameObject("HsModBgSquelchSceneObject", new Type[] { typeof(HSDontDestroyOnLoad) }).AddComponent<BgAutoSquelch>();
        }

        private void Update()
        {
            if (!isPluginEnable.Value || !isBgAutoSquelchEnable.Value)
            {
                inMatch = false;
                return;
            }

            bool nowInMatch = false;
            try { nowInMatch = GameMgr.Get() != null && GameMgr.Get().IsBattlegrounds(); }
            catch { nowInMatch = false; }

            if (nowInMatch && !inMatch)
            {
                inMatch = true;
                doneThisMatch = false;
                lastTry = 0f;
            }
            else if (!nowInMatch && inMatch)
            {
                inMatch = false;
            }

            if (!inMatch || doneThisMatch) return;
            if (Time.realtimeSinceStartup - lastTry < RetryInterval) return;
            lastTry = Time.realtimeSinceStartup;

            try
            {
                EnemyEmoteHandler handler = EnemyEmoteHandler.Get();
                if (handler == null) return;
                int count = handler.SquelchAllOpponents();
                if (count > 0)
                {
                    doneThisMatch = true;
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Info, $"BgAutoSquelch: squelched {count} opponents");
                }
            }
            catch (Exception ex)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, $"BgAutoSquelch: {ex.Message}");
            }
        }
    }
}
