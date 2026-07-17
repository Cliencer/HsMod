using HarmonyLib;
using System;
using System.Collections.Generic;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchGameMenu
        {
            private static UIBButton modSettingsButton;

            //复用游戏菜单模板创建"插件设置"按钮；文本不经过 GameStrings（未知 key 会原样返回）
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameMenu), "Awake")]
            public static void PatchGameMenuAwake(GameMenu __instance)
            {
                try
                {
                    modSettingsButton = (UIBButton)AccessTools.Method(typeof(ButtonListMenu), "CreateMenuButton",
                        new Type[] { typeof(string), typeof(string), typeof(UIEvent.Handler) })
                        .Invoke(__instance, new object[]
                        {
                            "ModSettingsButton",
                            LocalizationManager.GetLangValue("modSettingsButton.text"),
                            new UIEvent.Handler(OnModSettingsButtonReleased)
                        });
                }
                catch (Exception ex)
                {
                    modSettingsButton = null;
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, $"PatchGameMenuAwake => {ex.Message} \n{ex.InnerException}");
                }
            }

            //把按钮插到"选项"按钮之后（Esc 菜单与主界面齿轮菜单都走 GameMenu）
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameMenu), "GetButtons")]
            public static void PatchGameMenuGetButtons(ref List<UIBButton> __result, UIBButton ___m_optionsButton)
            {
                if (!isModSettingsButtonShow.Value || modSettingsButton == null)
                    return;
                int index = __result.IndexOf(___m_optionsButton);
                if (index >= 0)
                    __result.Insert(index + 1, modSettingsButton);
                else
                    __result.Add(modSettingsButton);
            }

            public static void OnModSettingsButtonReleased(UIEvent e)
            {
                GameMenu.Get()?.Hide();
                ModSettingsUI.Toggle();
            }
        }
    }
}
