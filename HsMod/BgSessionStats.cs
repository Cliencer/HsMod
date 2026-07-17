using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    //酒馆战棋对局统计：记录每场的名次与分数增减。
    //分数在对局前从NetCache读取，对局结束等待其更新后再取一次；
    //名次来自己方英雄的PLAYER_LEADERBOARD_PLACE标签。结果写入csv，
    //并在游戏内配置菜单（ModSettingsUI）的战绩页展示。
    //
    //MMR对账：单独记录"上次见到的MMR"（单人/双人分开，存于bg_last_mmr.txt）。
    //不在对局中时定期把当前MMR与其比较，若不同（末回合崩溃丢记录、换设备打分等
    //导致的未追踪变化），补一条Synced记录把该增减计入历史，使统计始终与实际一致。
    public class BgSessionStats : MonoBehaviour
    {
        public static BgSessionStats Instance;

        public class GameRecord
        {
            public DateTime Time;
            public bool Duo;
            public int Place;
            public int RatingBefore;
            public int RatingAfter;
            public bool Uncertain;    //分数没等到更新，增减未知
            public bool Synced;       //对账补记：真实MMR增减，但非本进程追踪的单场（名次未知）
            public string Hero = "";
            public int Delta => RatingAfter - RatingBefore;
        }

        //已结束、等待服务器分数更新的对局。
        //用独立对象保存，避免快速再排（含单人/双人切换）覆盖上一场的数据。
        private class PendingGame
        {
            public bool Duo;
            public int Place;
            public int RatingBefore;
            public string Hero;
            public float StartTime;
        }

        //全部历史记录：启动时从csv加载，本次会话的新对局实时追加
        public static readonly List<GameRecord> AllGames = new List<GameRecord>();
        public static bool AwaitingRating => Instance != null && Instance.pending != null;

        private static string CsvPath => Path.Combine(PluginConfig.HsModWebSite, "bg_session.csv");
        private static string StatePath => Path.Combine(PluginConfig.HsModWebSite, "bg_last_mmr.txt");

        private bool inMatch;
        private bool duo;
        private int ratingBefore = -1;
        private int place;
        private string hero = "";
        private PendingGame pending;

        private int lastSeenSolo = -1;    //上次见到的单人MMR（未知为-1）
        private int lastSeenDuo = -1;
        private float lastReconcile;

        private const float RatingWaitTimeout = 90f;    //秒：对局结束后等待分数更新的时长
        private const float ReconcileInterval = 4f;     //秒：不在对局中时的对账频率

        public static void Init()
        {
            if (Instance != null) return;
            LoadHistory();
            Instance = new GameObject("HsModBgStatsSceneObject", new Type[] { typeof(HSDontDestroyOnLoad) }).AddComponent<BgSessionStats>();
        }

        private void Awake()
        {
            LoadState();
        }

        //从csv加载历史记录（兼容旧格式：列数不足的行按缺省处理）
        private static void LoadHistory()
        {
            try
            {
                if (!File.Exists(CsvPath)) return;
                foreach (string line in File.ReadLines(CsvPath))
                {
                    string[] parts = line.Split(';');
                    if (parts.Length < 6) continue;
                    if (!DateTime.TryParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime time)) continue;
                    if (!int.TryParse(parts[2], out int place)) continue;
                    if (!int.TryParse(parts[3], out int before)) continue;
                    if (!int.TryParse(parts[4], out int after)) continue;
                    string note = parts.Length > 6 ? parts[6] : "";
                    AllGames.Add(new GameRecord
                    {
                        Time = time,
                        Duo = parts[1] == "duo",
                        Place = place,
                        RatingBefore = before,
                        RatingAfter = after,
                        Uncertain = note == "uncertain",
                        Synced = note == "synced",
                        Hero = parts.Length > 7 ? parts[7] : ""
                    });
                }
                Utils.MyLogger(BepInEx.Logging.LogLevel.Info, $"BgSessionStats: loaded {AllGames.Count} games from history");
            }
            catch (Exception ex)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"BgSessionStats load history: {ex.Message}");
            }
        }

        private void LoadState()
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    foreach (string line in File.ReadAllLines(StatePath))
                    {
                        string[] p = line.Split('=');
                        if (p.Length != 2) continue;
                        if (p[0] == "solo" && int.TryParse(p[1], out int s)) lastSeenSolo = s;
                        else if (p[0] == "duo" && int.TryParse(p[1], out int d)) lastSeenDuo = d;
                    }
                }
            }
            catch { }

            //基准未知时用csv里该模式最后一场的赛后分数（而非当前MMR），
            //这样首次运行也能把"最后记录 -> 当前"的漂移作为漏记对局补上
            if (lastSeenSolo <= 0) lastSeenSolo = LastRecordedRating(false);
            if (lastSeenDuo <= 0) lastSeenDuo = LastRecordedRating(true);
        }

        private static int LastRecordedRating(bool duoMode)
        {
            for (int i = AllGames.Count - 1; i >= 0; i--)
                if (AllGames[i].Duo == duoMode && AllGames[i].RatingAfter > 0)
                    return AllGames[i].RatingAfter;
            return -1;
        }

        private void SaveState()
        {
            try { File.WriteAllText(StatePath, $"solo={lastSeenSolo}\nduo={lastSeenDuo}"); }
            catch { }
        }

        private int GetLastSeen(bool duoMode) => duoMode ? lastSeenDuo : lastSeenSolo;

        private void SetLastSeen(bool duoMode, int value)
        {
            if (duoMode) lastSeenDuo = value; else lastSeenSolo = value;
            SaveState();
        }

        private void Update()
        {
            if (!isPluginEnable.Value || !isBgSessionStatsEnable.Value) return;

            bool nowInMatch = false;
            try
            {
                GameMgr mgr = GameMgr.Get();
                //只统计排名对局：排除AI、教程和好友对战
                nowInMatch = mgr != null && mgr.IsBattlegrounds()
                    && !mgr.IsBattlegroundsTutorial()
                    && !mgr.IsBattlegroundVsAIGame()
                    && !mgr.IsFriendlyBattlegrounds();
            }
            catch { nowInMatch = false; }

            if (nowInMatch && !inMatch)
            {
                inMatch = true;
                place = 0;
                hero = "";
                try { duo = GameMgr.Get().IsBattlegroundDuoGame(); } catch { duo = false; }
                Reconcile();    //开赛前先把此前的漂移补记为独立条目，本场基准才干净
                ratingBefore = GetCurrentRating(duo);
            }
            else if (!nowInMatch && inMatch)
            {
                inMatch = false;
                if (ratingBefore > 0 && place > 0)
                {
                    //上一场若仍在等分数（快速再排两局），强制按现状补记，腾出槽位
                    FinalizePending(true);
                    pending = new PendingGame
                    {
                        Duo = duo,
                        Place = place,
                        RatingBefore = ratingBefore,
                        Hero = hero,
                        StartTime = Time.realtimeSinceStartup
                    };
                }
                ratingBefore = -1;
                place = 0;
                hero = "";
            }

            if (inMatch)
            {
                //赛前分数每帧刷新：对局期间MMR不会变，唯一可能的变化是
                //上一场的分数更新迟到（快速再排/断线重连），刷新可自动取到正确基准
                int current = GetCurrentRating(duo);
                if (current > 0) ratingBefore = current;
                TryCapturePlace();
            }
            else
            {
                //等待中的上一场随时可能等到分数更新
                FinalizePending(false);
                //不在对局、且没有等待中的对局时，定期对账未追踪的MMR变化
                if (pending == null && Time.realtimeSinceStartup - lastReconcile > ReconcileInterval)
                {
                    lastReconcile = Time.realtimeSinceStartup;
                    Reconcile();
                }
            }
        }

        //把当前MMR与"上次见到"比较，未追踪的增减补一条Synced记录
        private void Reconcile()
        {
            for (int m = 0; m < 2; m++)
            {
                bool duoMode = m == 1;
                int current = GetCurrentRating(duoMode);
                if (current <= 0) continue;    //取不到（未打过该模式等）跳过
                int last = GetLastSeen(duoMode);
                if (last <= 0)
                {
                    SetLastSeen(duoMode, current);    //首次见到，仅记基准不补条目
                    continue;
                }
                if (current != last)
                {
                    AddRecord(new GameRecord
                    {
                        Time = DateTime.Now,
                        Duo = duoMode,
                        Place = 0,
                        RatingBefore = last,
                        RatingAfter = current,
                        Synced = true,
                        Hero = ""
                    });
                    SetLastSeen(duoMode, current);
                }
            }
        }

        //落盘等待中的对局：force时立即写入（即使分数还没到，该记录标记为不确定），
        //否则等分数更新或超时后写入
        private void FinalizePending(bool force)
        {
            if (pending == null) return;
            int current = GetCurrentRating(pending.Duo);
            bool changed = current > 0 && current != pending.RatingBefore;
            bool timedOut = force || Time.realtimeSinceStartup - pending.StartTime > RatingWaitTimeout;
            if (!changed && !timedOut) return;

            int after = changed ? current : pending.RatingBefore;
            AddRecord(new GameRecord
            {
                Time = DateTime.Now,
                Duo = pending.Duo,
                Place = pending.Place,
                RatingBefore = pending.RatingBefore,
                RatingAfter = after,
                Uncertain = !changed,
                Hero = pending.Hero ?? ""
            });
            //更新基准，避免对账把这场的增减再补记一次；分数迟到时仍会由对账补上差额
            SetLastSeen(pending.Duo, after);
            pending = null;
        }

        //名次随玩家淘汰逐步更新，最终名次在对局结束前定格；顺便记录所玩英雄
        private void TryCapturePlace()
        {
            try
            {
                Entity heroEntity = GameState.Get()?.GetFriendlySidePlayer()?.GetHero();
                if (heroEntity != null)
                {
                    int current = heroEntity.GetTag(GAME_TAG.PLAYER_LEADERBOARD_PLACE);
                    if (current > 0) place = current;
                }
                //每帧取最新的有效英雄名（忽略占位英雄），最终定格为真实所选英雄
                string h = GetBgHeroName();
                if (!string.IsNullOrEmpty(h)) hero = h.Replace(';', ',');
            }
            catch { }
        }

        //酒馆战棋真实英雄名：优先取己方排行榜卡（与画面一致），回退到玩家英雄实体。
        //Player.GetHero()在选英雄阶段会返回占位"BaconPHhero"，需过滤
        private static string GetBgHeroName()
        {
            try
            {
                Player me = GameState.Get()?.GetFriendlySidePlayer();
                if (me == null) return null;

                string name = null;
                try
                {
                    PlayerLeaderboardManager mgr = PlayerLeaderboardManager.Get();
                    PlayerLeaderboardCard tile = mgr?.GetTileForPlayerId(me.GetPlayerId());
                    name = tile?.GetHeroName();
                }
                catch { }

                if (IsPlaceholderHero(name))
                    name = me.GetHero()?.GetName();

                return IsPlaceholderHero(name) ? null : name;
            }
            catch { return null; }
        }

        private static bool IsPlaceholderHero(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            if (name.StartsWith("TB_") || name.StartsWith("HERO_")) return true;
            //占位英雄形如"BaconPHhero"：同时含Bacon与PH
            if (name.IndexOf("Bacon", StringComparison.OrdinalIgnoreCase) >= 0
                && name.IndexOf("PH", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static int GetCurrentRating(bool duoMode)
        {
            try
            {
                NetCache.NetCacheBaconRatingInfo info = NetCache.Get()?.GetNetObject<NetCache.NetCacheBaconRatingInfo>();
                if (info == null) return -1;
                return duoMode ? info.DuosRating : info.Rating;
            }
            catch { return -1; }
        }

        private static void AddRecord(GameRecord record)
        {
            AllGames.Add(record);
            string note = record.Synced ? "synced" : (record.Uncertain ? "uncertain" : "");
            try
            {
                string path = CsvPath;
                bool writeHeader = !File.Exists(path);
                using (StreamWriter w = new StreamWriter(path, true))
                {
                    if (writeHeader) w.WriteLine("time;mode;place;rating_before;rating_after;delta;note;hero");
                    w.WriteLine($"{record.Time:yyyy-MM-dd HH:mm:ss};{(record.Duo ? "duo" : "solo")};{record.Place};{record.RatingBefore};{record.RatingAfter};{record.Delta};{note};{record.Hero}");
                }
            }
            catch (Exception ex)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"BgSessionStats save: {ex.Message}");
            }
            Utils.MyLogger(BepInEx.Logging.LogLevel.Info,
                $"BgSessionStats: {(record.Synced ? "sync" : "place " + record.Place)}, rating {record.RatingBefore} -> {record.RatingAfter} ({(record.Uncertain ? "?" : $"{record.Delta:+#;-#;0}")})");
        }
    }
}
