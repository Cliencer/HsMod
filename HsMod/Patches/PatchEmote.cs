using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchEmote
        {
            //思考表情
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(MatchingQueueTab), "Update")]
            [HarmonyPatch(typeof(ThinkEmoteManager), "Update")]
            public static IEnumerable<CodeInstruction> PatchThinkEmoteManagerOrMatchingQueueTabUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                MethodInfo deltaTimeInfo = typeof(Time).GetProperty("deltaTime", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
                MethodInfo getMethod = typeof(Time).GetProperty("unscaledDeltaTime", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Call && x.operand as MethodInfo == deltaTimeInfo);
                if (num > 0)
                {
                    list[num].operand = getMethod;
                }
                return list;
            }

            //思考表情
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ThinkEmoteManager), "Update")]
            public static bool PatchThinkEmoteManagerUpdate()
            {
                return isThinkEmotesEnable.Value;
            }

            //表情冷却
            [HarmonyPrefix]
            [HarmonyPatch(typeof(EmoteHandler), "EmoteSpamBlocked")]
            public static bool PatchUpdateAllMutes(ref float ___m_timeSinceLastEmote, ref bool __result)
            {
                if (isExtendedBMEnable.Value)
                {
                    if (___m_timeSinceLastEmote < 1.5f)
                    {
                        __result = true;
                        return false;
                    }
                    __result = false;
                    return false;
                }

                return true;
            }

            //开局自动禁止表情
            [HarmonyPrefix]
            [HarmonyPatch(typeof(EnemyEmoteHandler), "IsSquelched")]
            public static bool PatchIsSquelched(ref int playerId, ref bool __result)
            {
                if (receiveEnemyEmoteLimit.Value == 0)
                {
                    __result = true;
                    return false;
                }
                return true;
            }


            //表情管理，数量限制存在小误差
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RemoteActionHandler), "CanReceiveEnemyEmote")]
            public static bool PatchPreCanReceiveEnemyEmote(ref EmoteType emoteType, ref int playerId, ref bool __result)
            {
                if (receiveEnemyEmoteLimit.Value == 0)
                {
                    __result = false;
                    return false;
                }
                else return true;
            }
            private static int m_lastPlayerId;
            private static float m_lastEnemyEmoteTime;
            private static int m_chainedEnemyEmotes;
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RemoteActionHandler), "CanReceiveEnemyEmote")]
            public static void PatchPostCanReceiveEnemyEmote(ref EmoteType emoteType, ref int playerId, ref bool __result)
            {
                int emotesBeforeBlock;
                if (!__result || (emotesBeforeBlock = receiveEnemyEmoteLimit.Value) <= 0)
                {
                    return;
                }
                float time = Time.time;
                if (m_lastPlayerId == playerId)
                {
                    if (m_lastEnemyEmoteTime != 0f && time - m_lastEnemyEmoteTime < 9f)
                    {
                        m_chainedEnemyEmotes++;
                        if (m_chainedEnemyEmotes > emotesBeforeBlock)
                        {
                            EnemyEmoteHandler.Get().SquelchPlayer(playerId);
                        }
                    }
                    else
                    {
                        m_chainedEnemyEmotes = 1;
                    }
                }
                else
                {
                    m_chainedEnemyEmotes = 1;
                    m_lastPlayerId = playerId;
                }
                m_lastEnemyEmoteTime = time;
            }
        }
    }
}
