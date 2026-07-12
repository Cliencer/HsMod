using HarmonyLib;
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchFakeDevice
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Network), "GetPlatformBuilder")]
            public static void PatchGetPlatformBuilder(Network __instance, ref PegasusShared.Platform __result)
            {
                OSCategory oscategory;
                ScreenCategory screenCategory;
                string model;
                switch (fakeDevicePreset.Value)
                {
                    case Utils.DevicePreset.Default:
                        return;
                    case Utils.DevicePreset.iPad:
                        oscategory = OSCategory.iOS;
                        screenCategory = ScreenCategory.Tablet;
                        model = "iPad13,11";
                        break;
                    case Utils.DevicePreset.iPhone:
                        oscategory = OSCategory.iOS;
                        screenCategory = ScreenCategory.Phone;
                        model = "iPhone13,4";
                        break;
                    case Utils.DevicePreset.Phone:
                        oscategory = OSCategory.Android;
                        screenCategory = ScreenCategory.Phone;
                        model = "SAMSUNG-SM-G930FD";
                        break;
                    case Utils.DevicePreset.Tablet:
                        oscategory = OSCategory.Android;
                        screenCategory = ScreenCategory.Tablet;
                        model = "SAMSUNG-SM-G920F";
                        break;
                    case Utils.DevicePreset.HuaweiPhone:
                        oscategory = OSCategory.Android;
                        screenCategory = ScreenCategory.Phone;
                        model = "Huawei Nova 8";
                        break;
                    case Utils.DevicePreset.Custom:
                        oscategory = fakeDeviceOs.Value;
                        screenCategory = fakeDeviceScreen.Value;
                        model = fakeDeviceName.Value;
                        break;
                    default:
                        return;
                }
                __result.Os = (int)oscategory;
                __result.Screen = (int)screenCategory;
                __result.Name = model;
                __result.UniqueDeviceIdentifier = PatchFakeDevice.GetUniqueDeviceID(oscategory, screenCategory, model);
            }

            private static string GetMD5(string message)
            {
                byte[] array = MD5.Create().ComputeHash(Encoding.Default.GetBytes(message));
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < array.Length; i++)
                {
                    stringBuilder.Append(array[i].ToString("x2"));
                }
                return stringBuilder.ToString();
            }

            private static string GetUniqueDeviceID(OSCategory os, ScreenCategory screen, string deviceName)
            {
                switch (os)
                {
                    case OSCategory.PC:
                        return Crypto.SHA1.Calc(Encoding.Default.GetBytes(string.Format("HsModeD{0}{1}{2}{3}", new object[]
                        {
                                SystemInfo.deviceUniqueIdentifier,
                                os,
                                screen,
                                deviceName
                        })));
                    case OSCategory.Mac:
                    case OSCategory.iOS:
                        return new Guid(PatchFakeDevice.GetMD5(string.Format("HsModeD{0}{1}{2}{3}", new object[]
                        {
                                SystemInfo.deviceUniqueIdentifier,
                                os,
                                screen,
                                deviceName
                        }))).ToString().ToUpper();
                    case OSCategory.Android:
                        return PatchFakeDevice.GetMD5(string.Format("HsModeD_{0}{1}{2}{3}", new object[]
                        {
                                SystemInfo.deviceUniqueIdentifier,
                                os,
                                screen,
                                deviceName
                        }));
                    default:
                        return "n/a";
                }
            }
        }
    }
}
