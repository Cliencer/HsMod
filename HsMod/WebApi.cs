using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public class WebApi
    {

        public static async Task<string> RunShellCommandAsync(string command)
        {
            if (!isWebshellEnable.Value)
            {
                return string.Empty;
            }

            var processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Platform-specific settings for command execution
            if ((Environment.OSVersion.Platform == PlatformID.MacOSX) || (Environment.OSVersion.Platform == PlatformID.Unix))
            {
                processInfo.FileName = "/bin/sh";
                processInfo.Arguments = $"-c \"{command}\"";
            }
            else
            {
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = "/C chcp 65001 & " + command;
            }

            using (var process = new Process { StartInfo = processInfo })
            {
                var outputBuilder = new StringBuilder();
                var tcs = new TaskCompletionSource<bool>();

                // Set up asynchronous reading of output and error streams
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        tcs.TrySetResult(true); // Mark as complete when output ends
                    else
                        outputBuilder.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        tcs.TrySetResult(true); // Mark as complete when error output ends
                    else
                        outputBuilder.AppendLine(e.Data);
                };

                // Start the process and begin reading output and error
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Create a task to wait for the process to exit
                var processTask = Task.Run(() =>
                {
                    process.WaitForExit();
                    tcs.TrySetResult(true);
                });

                // Wait for the process to complete or timeout after 5 seconds
                var completedTask = await Task.WhenAny(processTask, Task.Delay(TimeSpan.FromSeconds(5)));
                if (completedTask == processTask)
                {
                    // Process completed within timeout
                    return outputBuilder.ToString();
                }
                else
                {
                    // Timeout occurred
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill(); // Ensure the process is terminated on timeout
                        }
                        catch
                        {
                            // Ignore any exceptions if the process is already terminated
                        }
                    }
                    return string.Empty; // Return empty string on timeout
                }
            }
        }

        public static int UpdateHsSkinsCfg(string content, out string res)
        {
            res = string.Empty;

            try
            {
                File.WriteAllText(Path.Combine(BepInEx.Paths.ConfigPath, "HsSkins.cfg"), content);
                LoadSkinsConfigFromFile();
                res = WebPage.HsModCfgPage("HsSkins.cfg").ToString();
                return 200;
            }
            catch (Exception ex)
            {
                res = ex.Message;
                return 500;
            }
        }

        public static int RunPluginConfigAsync(string key, string value, out string res)
        {
            res = string.Empty;

            if (!string.IsNullOrEmpty(key) && (key.Length > 5))
            {
                key = key.Substring(0, key.Length - 5); // remove .name
                if (key.Equals("isWebshellEnable"))
                {
                    res = "not allow.";
                    return 403;
                }
                var configKeyProp = typeof(PluginConfig).GetField(key, BindingFlags.Public | BindingFlags.Static);
                if (configKeyProp == null)
                {
                    res = "key not found.";
                    return 501;

                }
                var configEntry = (ConfigEntryBase)configKeyProp.GetValue(null);
                var converter = TomlTypeConverter.GetConverter(configEntry.SettingType);
                if (converter != null)
                {
                    configEntry.SetSerializedValue(value);
                    res = configEntry.GetSerializedValue();
                    return 200;
                }
            }
            return 500;
        }

        public static string GetAllConfigMetadata(string lang = null)
        {
            // Use specified language or fall back to plugin default
            string targetLang = string.IsNullOrEmpty(lang) ? pluginInitLanague.Value : lang;

            // Load language file for the target language
            Dictionary<string, string> langDict = null;
            Dictionary<string, string> fallbackDict = null;

            try
            {
                string langJson = FileManager.ReadEmbeddedFile($"./Languages/{targetLang}.json");
                if (!string.IsNullOrEmpty(langJson))
                {
                    langDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(langJson);
                }
            }
            catch { }

            // Always load enUS as fallback
            try
            {
                string enUSJson = FileManager.ReadEmbeddedFile("./Languages/enUS.json");
                if (!string.IsNullOrEmpty(enUSJson))
                {
                    fallbackDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(enUSJson);
                }
            }
            catch { }

            // Helper function to get localized value
            Func<string, string, string> getLangValue = (key, defaultValue) =>
            {
                if (langDict != null && langDict.TryGetValue(key, out var val))
                    return val;
                if (fallbackDict != null && fallbackDict.TryGetValue(key, out var fallbackVal))
                    return fallbackVal;
                return defaultValue;
            };

            var configList = new List<Dictionary<string, object>>();
            var fields = typeof(PluginConfig).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(ConfigEntry<>))
                    continue;

                var configEntry = field.GetValue(null) as ConfigEntryBase;
                if (configEntry == null)
                    continue;

                // Mark internal/advanced configs
                string fieldName = field.Name;
                bool isAdvanced = (fieldName == "pluginInitLanague" || fieldName == "isEulaRead" || fieldName == "isDynamicFpsEnable");

                // Get localized name, label, description
                string localizedName = getLangValue($"{fieldName}.name", configEntry.Definition.Key);
                string localizedLabel = getLangValue($"{fieldName}.label", configEntry.Definition.Section);
                string localizedDesc = getLangValue($"{fieldName}.description", configEntry.Description?.Description ?? "");

                var configItem = new Dictionary<string, object>
                {
                    ["key"] = fieldName,
                    ["name"] = localizedName,
                    ["label"] = localizedLabel,
                    ["description"] = localizedDesc,
                    ["type"] = GetConfigType(configEntry.SettingType),
                    ["value"] = configEntry.GetSerializedValue(),
                    ["isAdvanced"] = isAdvanced
                };

                // Handle enum types
                if (configEntry.SettingType.IsEnum)
                {
                    configItem["enumValues"] = Enum.GetNames(configEntry.SettingType);
                }

                // Handle AcceptableValueRange
                if (configEntry.Description?.AcceptableValues != null)
                {
                    var acceptableValues = configEntry.Description.AcceptableValues;
                    var acceptableType = acceptableValues.GetType();

                    if (acceptableType.IsGenericType)
                    {
                        var minProp = acceptableType.GetProperty("MinValue");
                        var maxProp = acceptableType.GetProperty("MaxValue");

                        if (minProp != null && maxProp != null)
                        {
                            configItem["min"] = minProp.GetValue(acceptableValues);
                            configItem["max"] = maxProp.GetValue(acceptableValues);
                        }
                    }
                }

                // Handle KeyboardShortcut type
                if (configEntry.SettingType == typeof(KeyboardShortcut))
                {
                    configItem["keyCodes"] = Enum.GetNames(typeof(KeyCode));
                }

                configList.Add(configItem);
            }

            // Group by label
            var grouped = configList
                .GroupBy(c => c["label"].ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<string, object>
            {
                ["language"] = pluginInitLanague.Value,
                ["groups"] = grouped
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }

        private static string GetConfigType(Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(KeyboardShortcut)) return "keyboard";
            if (type.IsEnum) return "enum";
            return "string";
        }

    }
}
