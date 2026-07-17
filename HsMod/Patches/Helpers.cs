using Blizzard.GameService.SDK.Client.Integration;
using Blizzard.T5.Core;
using Blizzard.T5.Core.Time;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static HsMod.PluginConfig;

namespace HsMod
{
    public static class TimeScaleMgrPatch
    {
        private static readonly MethodInfo updateInfo = typeof(TimeScaleMgr).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Update(this TimeScaleMgr __instance)
        {
            updateInfo?.Invoke(__instance, null);
        }
    }

    //收藏更新，目前测试无效
    public static class CollectionManagerPatch
    {
        private static readonly MethodInfo onCollectionChanged = typeof(CollectionManager).GetMethod("OnCollectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void OnCollectionChanged(this CollectionManager __instance)
        {
            onCollectionChanged?.Invoke(__instance, null);
        }
    }

    //静音管理
    public static class SoundManagerPatch
    {
        public static bool MuteKeyPressed = false;

        private static readonly MethodInfo updateAppMuteInfo = typeof(SoundManager).GetMethod("UpdateAppMute", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void OnMuteKeyPressed()
        {
            MuteKeyPressed = !MuteKeyPressed;
            SoundManager soundManager = SoundManager.Get();
            if (soundManager != null)
            {
                updateAppMuteInfo?.Invoke(soundManager, null);
            }
        }
    }

    //获取玩家信息
    public static class SharedPlayerInfoPatch
    {
        private static readonly FieldInfo m_gameAccountIdInfo = typeof(SharedPlayerInfo).GetField("m_gameAccountId", BindingFlags.Instance | BindingFlags.NonPublic);

        public static BnetGameAccountId GetGameAccountId(this SharedPlayerInfo __instance)
        {
            return m_gameAccountIdInfo?.GetValue(__instance) as BnetGameAccountId;
        }
    }

    //点击获取标签
    public static class PlayerLeaderboardManagerPatch
    {
        private static readonly FieldInfo m_currentlyMousedOverTileInfo = typeof(PlayerLeaderboardManager).GetField("m_currentlyMousedOverTile", BindingFlags.Instance | BindingFlags.NonPublic);
        private static BnetPlayer m_currentOpponent;

        public static void UpdateCurrentOpponent(int opponentPlayerId)
        {
            if (GameState.Get() == null || !GameState.Get().GetPlayerInfoMap().ContainsKey(opponentPlayerId))
            {
                PlayerLeaderboardManagerPatch.m_currentOpponent = null;
                return;
            }
            BnetGameAccountId gameAccountId = GameState.Get().GetPlayerInfoMap()[opponentPlayerId].GetGameAccountId();
            if (gameAccountId == null)
            {
                PlayerLeaderboardManagerPatch.m_currentOpponent = null;
                return;
            }
            PlayerLeaderboardManagerPatch.m_currentOpponent = BnetPresenceMgr.Get().GetPlayer(gameAccountId);
        }

        public static BnetPlayer GetCurrentOpponent(this PlayerLeaderboardManager __instance)
        {
            return PlayerLeaderboardManagerPatch.m_currentOpponent;
        }

        public static BnetPlayer GetSelectedOpponent(this PlayerLeaderboardManager __instance)
        {
            if (!(m_currentlyMousedOverTileInfo?.GetValue(__instance) is PlayerLeaderboardCard playerLeaderboardCard) || GameState.Get() == null)
            {
                return null;
            }
            int tag = playerLeaderboardCard.Entity.GetTag(GAME_TAG.PLAYER_ID);
            if (!GameState.Get().GetPlayerInfoMap().ContainsKey(tag))
            {
                return null;
            }
            BnetGameAccountId gameAccountId = GameState.Get().GetPlayerInfoMap()[tag].GetGameAccountId();
            if (gameAccountId == null)
            {
                return null;
            }
            return BnetPresenceMgr.Get().GetPlayer(gameAccountId);
        }
    }

    //表情发送
    public static class EmoteHandlerPatch
    {
        private static readonly FieldInfo m_totalEmotesInfo = typeof(EmoteHandler).GetField("m_totalEmotes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo m_availableEmotesInfo = typeof(EmoteHandler).GetField("m_availableEmotes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static List<EmoteOption> m_FoundedEmotes;
        public static void HandleKeyboardInput(this EmoteHandler __instance, int EmoteIndex, bool useExtended = false)
        {
            if (EmoteHandler.Get().EmoteSpamBlocked())
            {
                return;
            }
            List<EmoteOption> list = EmoteHandlerPatch.m_availableEmotesInfo.GetValue(__instance) as List<EmoteOption>;
            if (useExtended)
            {
                if (!isExtendedBMEnable.Value || EmoteIndex + 1 > EmoteHandlerPatch.m_FoundedEmotes.Count)
                {
                    return;
                }
                EmoteHandlerPatch.m_totalEmotesInfo.SetValue(__instance, (int)EmoteHandlerPatch.m_totalEmotesInfo.GetValue(__instance) + 1);
                //if (MixModConfig.Get().DisableRandomForEmotes || !GameState.Get().GetGameEntity().HasTag(GAME_TAG.ALL_TARGETS_RANDOM))
                if (!GameState.Get().GetGameEntity().HasTag(GAME_TAG.ALL_TARGETS_RANDOM))
                {
                    EmoteHandlerPatch.m_FoundedEmotes[EmoteIndex].DoClick();
                    return;
                }
                List<EmoteOption> list2 = new List<EmoteOption>();
                foreach (EmoteOption emoteOption in list.Concat(__instance.m_HiddenEmotes))
                {
                    if (emoteOption.CanPlayerUseEmoteType(GameState.Get().GetFriendlySidePlayer()))
                    {
                        list2.Add(emoteOption);
                    }
                }
                foreach (EmoteOption emoteOption2 in EmoteHandlerPatch.m_FoundedEmotes)
                {
                    if (emoteOption2 != null)
                    {
                        list2.Add(emoteOption2);
                    }
                }
                if (list2.Count > 0)
                {
                    list2[UnityEngine.Random.Range(0, list2.Count)].DoClick();
                    return;
                }
            }
            else
            {
                if (EmoteIndex + 1 > list.Count)
                {
                    return;
                }
                EmoteHandlerPatch.m_totalEmotesInfo.SetValue(__instance, (int)EmoteHandlerPatch.m_totalEmotesInfo.GetValue(__instance) + 1);
                //if (MixModConfig.Get().DisableRandomForEmotes || !GameState.Get().GetGameEntity().HasTag(GAME_TAG.ALL_TARGETS_RANDOM))
                if (!GameState.Get().GetGameEntity().HasTag(GAME_TAG.ALL_TARGETS_RANDOM))
                {
                    list[EmoteIndex].DoClick();
                    return;
                }
                List<EmoteOption> list3 = new List<EmoteOption>();
                foreach (EmoteOption emoteOption3 in list.Concat(__instance.m_HiddenEmotes))
                {
                    if (emoteOption3.CanPlayerUseEmoteType(GameState.Get().GetFriendlySidePlayer()))
                    {
                        list3.Add(emoteOption3);
                    }
                }
                if (list3.Count > 0)
                {
                    list3[UnityEngine.Random.Range(0, list3.Count)].DoClick();
                }
            }
        }

        public static void DetermineFoundedEmotes(this EmoteHandler __instance)
        {
            if (EmoteHandlerPatch.m_FoundedEmotes == null || EmoteHandlerPatch.m_FoundedEmotes.Count == 0)
            {
                EmoteHandlerPatch.m_FoundedEmotes = new List<EmoteOption>(11);
                EmoteOption emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.HAPPY_NEW_YEAR);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.HAPPY_NEW_YEAR,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_GREETINGS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.HAPPY_NEW_YEAR_LUNAR);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.HAPPY_NEW_YEAR_LUNAR,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_GREETINGS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.HAPPY_HOLIDAYS);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.HAPPY_HOLIDAYS,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_GREETINGS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.HAPPY_HALLOWEEN);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.HAPPY_HALLOWEEN,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_GREETINGS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.HAPPY_NOBLEGARDEN);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.HAPPY_NOBLEGARDEN,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_GREETINGS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_ONE);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_ONE,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_TWO);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_TWO,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_THREE);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FIRE_FESTIVAL_FIREWORKS_RANK_THREE,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_ONE);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_ONE,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_TWO);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_TWO,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                emoteOption = __instance.m_EmoteOverrides.FirstOrDefault((EmoteOption x) => x.m_EmoteType == EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_THREE);
                if (emoteOption == null)
                {
                    emoteOption = new EmoteOption
                    {
                        m_EmoteType = EmoteType.FROST_FESTIVAL_FIREWORKS_RANK_THREE,
                        m_StringTag = "GAMEPLAY_EMOTE_LABEL_FIREWORKS"
                    };
                }
                EmoteHandlerPatch.m_FoundedEmotes.Add(emoteOption);
                foreach (EmoteOption emoteOption2 in EmoteHandlerPatch.m_FoundedEmotes)
                {
                    emoteOption2.UpdateEmoteType();
                }
            }
        }
    }

    //表情静默
    public static class EnemyEmoteHandlerPatch
    {
        private static readonly MethodInfo doSquelchClickInfo = typeof(EnemyEmoteHandler).GetMethod("DoSquelchClick", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo m_squelchedInfo = typeof(EnemyEmoteHandler).GetField("m_squelched", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void DoSquelchClick(this EnemyEmoteHandler __instance)
        {
            doSquelchClickInfo?.Invoke(__instance, null);
        }

        public static void SquelchPlayer(this EnemyEmoteHandler __instance, int playerId)
        {
            if (m_squelchedInfo.GetValue(__instance) is Map<int, bool> map)
            {
                map[playerId] = true;
            }
        }

        //静默全部对手（保留自己、当前玩家与双人队友），返回被静默的数量
        public static int SquelchAllOpponents(this EnemyEmoteHandler __instance)
        {
            if (!(m_squelchedInfo.GetValue(__instance) is Map<int, bool> map)) return 0;
            int self = -1, current = -1, teammate = -1;
            try { self = GameState.Get()?.GetFriendlySidePlayer()?.GetPlayerId() ?? -1; } catch { }
            try { current = GameState.Get()?.GetCurrentPlayer()?.GetPlayerId() ?? -1; } catch { }
            try { teammate = GameState.Get()?.GetFriendlySidePlayer()?.GetTag((GAME_TAG)2939) ?? -1; } catch { }    //2939=双人队友PlayerId
            int count = 0;
            foreach (int id in map.Keys.ToList())
            {
                if (id == self || id == current || (teammate > 0 && id == teammate)) continue;
                if (!map[id])
                {
                    map[id] = true;
                    count++;
                }
            }
            return count;
        }
    }

    //开包
    public static class PackOpeningDirectorPatch
    {
        public static bool m_WaitingForCards;

        public static bool m_WaitingForAllCardsRevealed;

        private static readonly FieldInfo m_hiddenCardsInfo = typeof(PackOpeningDirector).GetField("m_hiddenCards", BindingFlags.Instance | BindingFlags.NonPublic);

        public static IEnumerator ForceRevealAllCards(this PackOpeningDirector __instance)
        {
            object value = m_hiddenCardsInfo.GetValue(__instance);
            if (value is Game.PackOpening.HiddenCards m_hiddenCards)
            {
                m_WaitingForAllCardsRevealed = true;
                while (m_WaitingForCards)
                {
                    yield return new WaitForSeconds(0.05f);
                }
                m_hiddenCards.ForceRevealAllCards();
                m_WaitingForAllCardsRevealed = false;
            }
        }
    }
    public static class FriendMgrPatch
    {
        private static BnetPlayer m_currentOpponent;
        public static BnetPlayer GetCurrentOpponent(this FriendMgr __instance)
        {
            return m_currentOpponent;
        }
        private static void UpdateCurrentOpponent()
        {
            if (GameState.Get() == null)
            {
                m_currentOpponent = null;
                return;
            }
            Player opposingSidePlayer = GameState.Get().GetOpposingSidePlayer();
            if (opposingSidePlayer == null)
            {
                FriendMgrPatch.m_currentOpponent = null;
                return;
            }
            FriendMgrPatch.m_currentOpponent = BnetPresenceMgr.Get().GetPlayer(opposingSidePlayer.GetGameAccountId());
            //if (opposingSidePlayer == null)
            //{
            //    m_currentOpponent = null;
            //}
            //else
            //{
            //    m_currentOpponent = BnetPresenceMgr.Get().GetPlayer(opposingSidePlayer.GetGameAccountId());
            //}
        }
        public static void OnGameCreated(GameState.CreateGamePhase phase, object userData)
        {
            GameState.Get().UnregisterCreateGameListener(new GameState.CreateGameCallback(FriendMgrPatch.OnGameCreated), null);

            FriendMgrPatch.UpdateCurrentOpponent();
        }
    }

    //模拟开包
    public static class PackOpeningPatch
    {
        private static readonly MethodInfo onReloginComplete = typeof(PackOpening).GetMethod("OnReloginComplete", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo updatePacks = typeof(PackOpening).GetMethod("UpdatePacks", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void OnReloginComplete(this PackOpening __instance)
        {
            onReloginComplete?.Invoke(__instance, null);
        }

        public static void UpdatePacks(this PackOpening __instance)
        {
            updatePacks?.Invoke(__instance, null);
        }
    }
}
