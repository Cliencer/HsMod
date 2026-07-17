using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    //酒馆战棋对手MMR的排行榜加载器。
    //思路来自HDT插件HDT_BGrank（IBM5100）。数据为https://bgrank.fly.dev的公开榜单
    //（暴雪官方排行榜的聚合服务，见github.com/IBM5100o/BGrank_bot）。
    //不上传任何用户信息——只按地区GET榜单。
    //MMR直接绘制在游戏排行榜浮层里——见Patcher.PatchBgRank。
    public class BgRankOverlay : MonoBehaviour
    {
        public static BgRankOverlay Instance;

        private static readonly HttpClient http = CreateHttpClient();

        //榜单按"US"/"EU_duo"等键分开存放——单人和双人互不混用，
        //MMR查询永远用当前对局的键（否则切换模式后会显示错误榜单的分数）。
        //下载任务同样按键区分：单人与双人可同时下载互不阻塞。
        //时间用DateTime.UtcNow（线程安全，后台线程不能调用Time.*）。
        private static readonly object boardsLock = new object();
        private static readonly Dictionary<string, Dictionary<string, int>> boards = new Dictionary<string, Dictionary<string, int>>();
        private static readonly Dictionary<string, DateTime> boardTime = new Dictionary<string, DateTime>();
        private static readonly HashSet<string> fetchingKeys = new HashSet<string>();
        private static readonly Dictionary<string, DateTime> lastAttempt = new Dictionary<string, DateTime>();

        private const double BoardTtlSeconds = 1800;      //榜单半小时内视为新鲜
        private const double RetryCooldownSeconds = 60;   //失败后重试的冷却时间

        private bool inMatch;

        public static void Init()
        {
            if (Instance != null) return;
            Instance = new GameObject("HsModBgRankSceneObject", new Type[] { typeof(HSDontDestroyOnLoad) }).AddComponent<BgRankOverlay>();
        }

        private static HttpClient CreateHttpClient()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            HttpClient c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            c.DefaultRequestHeaders.Add("User-Agent", "HsMod-BgRank");
            return c;
        }

        //当前对局的榜单键：地区+模式（单人/双人）
        private static string GetMatchKey()
        {
            string region = GetRegionStr();
            if (region == "UNKNOWN") return null;
            bool duo = false;
            try { duo = GameMgr.Get().IsBattlegroundDuoGame(); } catch { }
            return duo ? region + "_duo" : region;
        }

        private static Dictionary<string, int> GetBoard(string key)
        {
            if (key == null) return null;
            lock (boardsLock)
            {
                boards.TryGetValue(key, out Dictionary<string, int> board);
                return board;
            }
        }

        //当前对局（区分单人/双人）的榜单已加载，可供查询
        public static bool IsReady => GetBoard(GetMatchKey()) != null;

        //按玩家显示名查询MMR（由浮层补丁调用）
        public static bool TryGetMmr(string displayName, out int mmr)
        {
            mmr = 0;
            Dictionary<string, int> board = GetBoard(GetMatchKey());
            if (board == null || string.IsNullOrWhiteSpace(displayName)) return false;
            string name = displayName;
            int hash = name.IndexOf('#');    //兼容显示完整BattleTag（带#1234）的功能
            if (hash > 0) name = name.Substring(0, hash);
            return board.TryGetValue(name, out mmr);
        }

        private void Update()
        {
            if (!isPluginEnable.Value || !isBgRankEnable.Value)
            {
                inMatch = false;
                return;
            }

            bool nowInMatch = false;
            try { nowInMatch = GameMgr.Get() != null && GameMgr.Get().IsBattlegrounds(); }
            catch { nowInMatch = false; }

            inMatch = nowInMatch;
            //选英雄阶段即开始加载；重复调用开销极小——
            //内部有新鲜度、重试冷却和进行中下载的多重防护，
            //失败的下载会在对局中自动重试
            if (inMatch) StartFetch();
        }

        //确定地区与模式，在后台拉取榜单（带文件缓存）
        private void StartFetch()
        {
            string key = GetMatchKey();
            if (key == null) return;

            DateTime now = DateTime.UtcNow;
            lock (boardsLock)
            {
                if (fetchingKeys.Contains(key)) return;
                if (boards.ContainsKey(key) && boardTime.TryGetValue(key, out DateTime loaded)
                    && (now - loaded).TotalSeconds < BoardTtlSeconds) return;    //已有新鲜榜单
                if (lastAttempt.TryGetValue(key, out DateTime attempt)
                    && (now - attempt).TotalSeconds < RetryCooldownSeconds) return;
                lastAttempt[key] = now;
                fetchingKeys.Add(key);
            }

            string url = $"https://bgrank.fly.dev/{key}/";
            string cachePath = Path.Combine(PluginConfig.HsModWebSite, "bgrank", $"LeaderBoard_{key}.txt");
            _ = FetchAsync(url, cachePath, key);
        }

        private async Task FetchAsync(string url, string cachePath, string key)
        {
            Dictionary<string, int> result = null;
            for (int tries = 0; tries < 3 && result == null; tries++)
            {
                try
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Info, $"BgRank: fetching {url} (try {tries + 1}/3)");
                    string response = await http.GetStringAsync(url);
                    Dictionary<string, int> parsed = ParseLeaderboard(response);
                    if (parsed.Count > 0) result = parsed;
                    else if (tries < 2) await Task.Delay(4000);
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"BgRank: fetch failed: {ex.Message}");
                    if (tries < 2) await Task.Delay(4000);
                }
            }

            if (result != null)
            {
                SaveCache(cachePath, result);
            }
            else
            {
                result = LoadCache(cachePath);    //后备方案——本地缓存
                if (result != null)
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Info, "BgRank: using local cache");
            }

            lock (boardsLock)
            {
                if (result != null)
                {
                    boards[key] = result;
                    boardTime[key] = DateTime.UtcNow;
                }
                fetchingKeys.Remove(key);
            }
        }

        private static Dictionary<string, int> ParseLeaderboard(string response)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(response)) return dict;
            string[] lines = response.Split(new[] { "\n<br />" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                //分数在最后一个空格之后：带空格的名字不会丢失
                int idx = line.LastIndexOf(' ');
                if (idx <= 0 || idx >= line.Length - 1) continue;
                string name = line.Substring(0, idx).Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (int.TryParse(line.Substring(idx + 1), out int rating) && !dict.ContainsKey(name))
                    dict.Add(name, rating);
            }
            return dict;
        }

        private static void SaveCache(string path, Dictionary<string, int> data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (StreamWriter w = new StreamWriter(path))
                    foreach (var kv in data) w.WriteLine($"{kv.Key} {kv.Value}");
            }
            catch (Exception ex)
            {
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"BgRank: save cache failed: {ex.Message}");
            }
        }

        private static Dictionary<string, int> LoadCache(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                Dictionary<string, int> dict = new Dictionary<string, int>();
                foreach (string line in File.ReadLines(path))
                {
                    int idx = line.LastIndexOf(' ');
                    if (idx <= 0 || idx >= line.Length - 1) continue;
                    string name = line.Substring(0, idx).Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (int.TryParse(line.Substring(idx + 1), out int rating) && !dict.ContainsKey(name))
                        dict.Add(name, rating);
                }
                return dict.Count > 0 ? dict : null;
            }
            catch { return null; }
        }

        private static string GetRegionStr()
        {
            try
            {
                switch (Hearthstone.Util.RegionUtils.CurrentRegion)
                {
                    case Region.US: return "US";
                    case Region.EU: return "EU";
                    case Region.KR: return "AP";
                    case Region.TW: return "AP";
                    case Region.CN: return "CN";
                    default: return "UNKNOWN";
                }
            }
            catch { return "UNKNOWN"; }
        }
    }
}
