using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static HsMod.PluginConfig;

namespace HsMod
{
    public class LocalizationManager
    {
        public static string GetCurrentLang()
        {
            //优先使用炉石客户端自身的语言：Unity的CurrentCulture在部分环境下不可靠
            //（可能返回Invariant或与系统区域不符），导致语言"偶尔检测错误"
            string res = null;
            string gameLocale = "N/A";
            try
            {
                Locale locale = Localization.GetLocale();
                gameLocale = locale.ToString();
                if (locale != Locale.UNKNOWN && STRING_TO_LOCALE.ContainsKey(gameLocale))
                    res = gameLocale;
            }
            catch { }

            string cultureCode = "";
            if (string.IsNullOrEmpty(res))
            {
                try
                {
                    cultureCode = System.Globalization.CultureInfo.CurrentCulture.Name.Replace("-", "");
                    if (STRING_TO_LOCALE.ContainsKey(cultureCode))
                        res = cultureCode;
                }
                catch { }
            }

            //都取不到有效值时返回UNKNOWN：不持久化错误结果，下次启动可重新检测
            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"CurrentCulture: {cultureCode}, Hearthstone {gameLocale}, detected {res ?? "UNKNOWN"}, HsMod {pluginInitLanague.Value}.");
            return string.IsNullOrEmpty(res) ? "UNKNOWN" : res;
        }

        public static string GetLangFileContext(string lang)
        {
            //string localName = GetCurrentLang();
            string fileName = $"./Languages/{lang}.json";
            string context = FileManager.ReadEmbeddedFile(fileName);
            if (String.IsNullOrEmpty(context))
            {
                fileName = $"./Languages/enUS.json";
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod languages file not found or empty, now using {fileName}");
            }
            context = FileManager.ReadEmbeddedFile(fileName);
            return context;
        }

        public static string CacheCurrentLangJson = "";
        public static string CacheEnUSLangJson = "";
        public static Dictionary<string, string> CacheCurrentLangJsonObj = null;
        public static Dictionary<string, string> CacheEnUSLangJsonObj = null;

        public static string GetLangValue(string lang_key)
        {
            if (String.IsNullOrEmpty(CacheCurrentLangJson))
            {
                CacheCurrentLangJson = GetLangFileContext(pluginInitLanague.Value);
                CacheCurrentLangJsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(CacheCurrentLangJson);
            }

            string res;
            if (CacheCurrentLangJsonObj.TryGetValue(lang_key, out res))
            {
                return res;
            }
            else
            {
                //Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"Languages key '{lang_key}' not found.");
                if (String.IsNullOrEmpty(CacheEnUSLangJson))
                {
                    CacheEnUSLangJson = GetLangFileContext("enUS");
                    CacheEnUSLangJsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(CacheEnUSLangJson);
                }
                if (CacheEnUSLangJsonObj.TryGetValue(lang_key, out res))
                {
                    return res;
                }
                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"Languages key '{lang_key}' not found.");
                return "VALUE_NOT_FOUND";
            }
        }

        public static string GetLangKey(string lang_value)
        {
            if (String.IsNullOrEmpty(CacheCurrentLangJson))
            {
                CacheCurrentLangJson = GetLangFileContext(pluginInitLanague.Value);
                CacheCurrentLangJsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(CacheCurrentLangJson);
            }

            foreach (var key in CacheCurrentLangJsonObj)
            {
                if (key.Value == lang_value)
                {
                    if (key.Key.EndsWith(".name"))
                    {
                        return key.Key;
                    }
                }
            }
            if (String.IsNullOrEmpty(CacheEnUSLangJson))
            {
                CacheEnUSLangJson = GetLangFileContext("enUS");
                CacheEnUSLangJsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(CacheEnUSLangJson);
            }

            foreach (var key in CacheEnUSLangJsonObj)
            {
                if (key.Value == lang_value)
                {
                    if (key.Key.EndsWith("name"))
                    {
                        return key.Key;
                    }
                }
            }

            Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"Languages value '{lang_value}' not found.");
            return "KEY_NOT_FOUND";

        }

        public static Locale StrToLocale(string lang)
        {
            STRING_TO_LOCALE.TryGetValue(lang, out var value);
            return value;
        }
        public static readonly Dictionary<string, Locale> STRING_TO_LOCALE = new Dictionary<string, Locale>
    {
        {
            "enUS",
            Locale.enUS
        },
        {
            "enGB",
            Locale.enGB
        },
        {
            "frFR",
            Locale.frFR
        },
        {
            "deDE",
            Locale.deDE
        },
        {
            "koKR",
            Locale.koKR
        },
        {
            "esES",
            Locale.esES
        },
        {
            "esMX",
            Locale.esMX
        },
        {
            "ruRU",
            Locale.ruRU
        },
        {
            "zhTW",
            Locale.zhTW
        },
        {
            "zhCN",
            Locale.zhCN
        },
        {
            "itIT",
            Locale.itIT
        },
        {
            "ptBR",
            Locale.ptBR
        },
        {
            "plPL",
            Locale.plPL
        },
        {
            "jaJP",
            Locale.jaJP
        },
        {
            "thTH",
            Locale.thTH
        }
    };

    }


}