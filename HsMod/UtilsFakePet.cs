using System.Collections.Generic;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Utils
    {
        public static bool m_fakePetActive = false;
        public static List<Network.PowerHistory> HandlePowerHistory(List<Network.PowerHistory> powerList)
        {
            if (skinPet.Value <= 0)
            {
                return powerList;
            }
            if (GameMgr.Get().IsMercenaries())
            {
                return powerList;
            }
            Utils.HandlePowerHistoryTagChange(powerList);
            List<Network.PowerHistory> list = new List<Network.PowerHistory>();
            for (int i = 0; i < powerList.Count; i++)
            {
                Network.PowerHistory powerHistory = powerList[i];
                if (powerHistory != null && powerHistory.Type == Network.PowerType.CREATE_GAME)
                {
                    Network.HistCreateGame histCreateGame = (Network.HistCreateGame)powerHistory;
                    Network.Entity game = histCreateGame.Game;
                    bool flag = false;
                    for (int j = 0; j < game.Tags.Count; j++)
                    {
                        Network.Entity.Tag tag = game.Tags[j];
                        if (tag.Name == 1808 && tag.Value > 0)
                        {
                            flag = true;
                        }
                    }
                    if (!flag)
                    {
                        List<Network.HistCreateGame.PlayerData> list2 = new List<Network.HistCreateGame.PlayerData>();
                        int num = 0;
                        int num2 = 0;
                        bool flag2 = false;
                        PetVariantDbfRecord record = GameDbf.PetVariant.GetRecord(skinPet.Value);
                        foreach (Network.HistCreateGame.PlayerData playerData in histCreateGame.Players)
                        {
                            if (playerData.GameAccountId.Low == BnetPresenceMgr.Get().GetMyGameAccountId().Low)
                            {
                                for (int k = 0; k < playerData.Player.Tags.Count; k++)
                                {
                                    Network.Entity.Tag tag2 = playerData.Player.Tags[k];
                                    if (tag2.Name == 50)
                                    {
                                        num = tag2.Value;
                                    }
                                    if (tag2.Name == 30)
                                    {
                                        num2 = tag2.Value;
                                    }
                                    if (tag2.Name == 4017)
                                    {
                                        flag2 = true;
                                    }
                                }
                                if (!flag2 && record != null)
                                {
                                    playerData.Player.Tags.Add(new Network.Entity.Tag
                                    {
                                        Name = 4017,
                                        Value = 1000001
                                    });
                                    playerData.Player.Tags.Add(new Network.Entity.Tag
                                    {
                                        Name = 4037,
                                        Value = record.ID
                                    });
                                    playerData.Player.Tags.Add(new Network.Entity.Tag
                                    {
                                        Name = 4079,
                                        Value = record.PetId
                                    });
                                }
                            }
                            list2.Add(playerData);
                        }
                        histCreateGame.Players = list2;
                        list.Add(histCreateGame);
                        if (num != 0 && num2 != 0 && !flag2 && record != null)
                        {
                            global::Entity entity = new global::Entity();
                            entity.SetTag(GAME_TAG.CONTROLLER, num);
                            entity.SetTag<TAG_CARDTYPE>(GAME_TAG.CARDTYPE, TAG_CARDTYPE.PET);
                            entity.SetTag(GAME_TAG.PLAYER_ID, num2);
                            entity.SetTag(GAME_TAG.TRIGGER_VISUAL, 1);
                            entity.SetTag<TAG_ZONE>(GAME_TAG.ZONE, TAG_ZONE.COSMETIC);
                            entity.SetTag(GAME_TAG.ENTITY_ID, 1000001);
                            entity.SetTag<TAG_CLASS>(GAME_TAG.CLASS, TAG_CLASS.NEUTRAL);
                            entity.SetTag(GAME_TAG.PET_VARIANT_ID, record.ID);
                            entity.SetTag(GAME_TAG.PET_ID, record.PetId);
                            entity.SetTag(GAME_TAG.PET_TREATS_GENERATED, 2);
                            entity.SetCardId(GameUtils.TranslateDbIdToCardId(record.CardId, false));
                            List<Network.Entity.Tag> list3 = new List<Network.Entity.Tag>();
                            foreach (KeyValuePair<int, int> keyValuePair in entity.GetTags().GetMap())
                            {
                                Network.Entity.Tag tag3 = new Network.Entity.Tag
                                {
                                    Name = keyValuePair.Key,
                                    Value = keyValuePair.Value
                                };
                                list3.Add(tag3);
                            }
                            Network.Entity entity2 = new Network.Entity
                            {
                                CardID = entity.GetCardId(),
                                ID = entity.GetEntityId(),
                                Tags = list3,
                                TagLists = new List<Network.Entity.TagList>()
                            };
                            Network.HistFullEntity histFullEntity = new Network.HistFullEntity
                            {
                                Entity = entity2
                            };
                            list.Add(histFullEntity);
                            Utils.m_fakePetActive = true;
                        }
                    }
                    else
                    {
                        list.Add(powerHistory);
                    }
                }
                else
                {
                    list.Add(powerHistory);
                }
            }
            return list;
        }

        public static void HandlePowerHistoryTagChange(List<Network.PowerHistory> powerList)
        {
            GameState gameState = GameState.Get();
            if (gameState == null || powerList == null)
            {
                return;
            }
            for (int i = 0; i < powerList.Count; i++)
            {
                Network.PowerHistory powerHistory = powerList[i];
                if (powerHistory != null && powerHistory.Type == Network.PowerType.TAG_CHANGE)
                {
                    Network.HistTagChange histTagChange = (Network.HistTagChange)powerHistory;
                    int entity = histTagChange.Entity;
                    int tag = histTagChange.Tag;
                    int value = histTagChange.Value;
                    global::Entity entity2 = gameState.GetEntity(entity);
                    PetEventType petEventType = PetEventType.INVALID;
                    if (entity2 != null)
                    {
                        if (entity2.GetZone() == TAG_ZONE.PLAY && tag == 318 && value > 0)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && value <= 5)
                            {
                                petEventType = PetEventType.OPPONENT_DAMAGED_5ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && value >= 6)
                            {
                                petEventType = PetEventType.OPPONENT_DAMAGED_6PLUS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && value <= 5)
                            {
                                petEventType = PetEventType.FRIENDLY_DAMAGED_5ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && value >= 6)
                            {
                                petEventType = PetEventType.FRIENDLY_DAMAGED_6PLUS;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.IsMinion() && tag == 49 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() <= 3)
                            {
                                petEventType = PetEventType.FRIENDLY_MINIONPLAYED_MANAVALUE_3ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() <= 3 && entity2.GetCost() <= 6)
                            {
                                petEventType = PetEventType.FRIENDLY_MINIONPLAYED_MANAVALUE_4TO6;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() >= 7)
                            {
                                petEventType = PetEventType.FRIENDLY_MINIONPLAYED_MANAVALUE_7PLUS;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.IsSpell() && tag == 49 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() <= 3)
                            {
                                petEventType = PetEventType.FRIENDLY_SPELLCAST_MANAVALUE_3ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() >= 4)
                            {
                                petEventType = PetEventType.FRIENDLY_SPELLCAST_MANAVALUE_4PLUS;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.PLAY && entity2.IsPlayer() && tag == 23 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_TURN_START;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_TURN_START;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.PLAY && entity2.IsMinion() && tag == 49 && value == 4)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && entity2.GetCost() <= 3)
                            {
                                petEventType = PetEventType.OPPONENT_MINIONDESTROYED_MANAVALUE_3ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && entity2.GetCost() >= 4)
                            {
                                petEventType = PetEventType.OPPONENT_MINIONDESTROYED_MANAVALUE_4PLUS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() <= 3)
                            {
                                petEventType = PetEventType.FRIENDLY_MINIONDESTROYED_MANAVALUE_3ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() >= 4)
                            {
                                petEventType = PetEventType.FRIENDLY_MINIONDESTROYED_MANAVALUE_4PLUS;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.IsWeapon() && tag == 49 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_WEAPONEQUIPPED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_WEAPONEQUIPPED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.PLAY && entity2.IsHero() && tag == 292 && value > 0)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_ARMORGAINED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_ARMORGAINED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.PLAY && tag == 425 && value > 0)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && value <= 5)
                            {
                                petEventType = PetEventType.OPPONENT_HEALTHRESTORED_5ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING && value >= 6)
                            {
                                petEventType = PetEventType.OPPONENT_HEALTHRESTORED_6PLUS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && value <= 5)
                            {
                                petEventType = PetEventType.FRIENDLY_HEALTHRESTORED_5ORLESS;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY && value >= 6)
                            {
                                petEventType = PetEventType.FRIENDLY_HEALTHRESTORED_6PLUS;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.PLAY && entity2.IsHeroPower() && tag == 43 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_HEROPOWER;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_HEROPOWER;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.IsLocation() && tag == 49 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_LOCATIONUSED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_LOCATIONUSED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.IsLocation() && tag == 49 && value == 4)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_CARDDISCARDED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_CARDDISCARDED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && entity2.HasTag(GAME_TAG.DISCOVER) && tag == 49 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_CARDDISCOVERED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_CARDDISCOVERED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.HAND && tag == 3070 && value == 1)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_CARDFORGED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_CARDFORGED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.SECRET && entity2.IsSecret() && tag == 49 && value == 4)
                        {
                            if (entity2.GetControllerSide() == global::Player.Side.OPPOSING)
                            {
                                petEventType = PetEventType.OPPONENT_SECRETTRIGGERED;
                            }
                            if (entity2.GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                petEventType = PetEventType.FRIENDLY_SECRETTRIGGERED;
                            }
                        }
                        if (entity2.GetZone() == TAG_ZONE.DECK && tag == 49 && value == 3 && entity2.GetControllerSide() == global::Player.Side.FRIENDLY && entity2.GetCost() >= 9)
                        {
                            petEventType = PetEventType.FRIENDLY_CARDDRAWN_9PLUS;
                        }
                    }
                    if (Utils.m_fakePetActive && petEventType > PetEventType.INVALID && petEventType != PetEventType.IDLE)
                    {
                        global::Entity entity3 = gameState.GetEntity(1000001);
                        if (entity3 != null)
                        {
                            PetControllerGame petControllerGame = entity3.GetCard().GetActor().m_petController as PetControllerGame;
                            if (petControllerGame != null)
                            {
                                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "触发：" + petEventType.ToString());
                                petControllerGame.HandleEvent(petEventType);
                            }
                        }
                    }
                }
                if (powerHistory != null && powerHistory.Type == Network.PowerType.SHOW_ENTITY)
                {
                    int id = ((Network.HistShowEntity)powerHistory).Entity.ID;
                    Network.Entity entity4 = ((Network.HistShowEntity)powerHistory).Entity;
                    global::Entity entity5 = gameState.GetEntity(id);
                    PetEventType petEventType2 = PetEventType.INVALID;
                    if (entity5 != null)
                    {
                        bool flag = false;
                        bool flag2 = false;
                        int num = 0;
                        int num2 = 0;
                        for (int j = 0; j < entity4.Tags.Count; j++)
                        {
                            Network.Entity.Tag tag2 = entity4.Tags[j];
                            if (tag2.Name == 202 && tag2.Value == 4)
                            {
                                flag = true;
                            }
                            if (tag2.Name == 202 && tag2.Value == 5)
                            {
                                flag2 = true;
                            }
                            if (tag2.Name == 48)
                            {
                                num = tag2.Value;
                            }
                            if (tag2.Name == 50)
                            {
                                num2 = tag2.Value;
                            }
                        }
                        if (entity5.GetZone() == TAG_ZONE.HAND && flag)
                        {
                            if (num2 == 2 && num <= 3)
                            {
                                petEventType2 = PetEventType.OPPONENT_MINIONPLAYED_MANAVALUE_3ORLESS;
                            }
                            if (num2 == 2 && num >= 4 && num <= 6)
                            {
                                petEventType2 = PetEventType.OPPONENT_MINIONPLAYED_MANAVALUE_4TO6;
                            }
                            if (num2 == 2 && num >= 7)
                            {
                                petEventType2 = PetEventType.OPPONENT_MINIONPLAYED_MANAVALUE_7PLUS;
                            }
                        }
                        if (entity5.GetZone() == TAG_ZONE.HAND && flag2)
                        {
                            if (num2 == 2 && num <= 3)
                            {
                                petEventType2 = PetEventType.OPPONENT_SPELLCAST_MANAVALUE_3ORLESS;
                            }
                            if (num2 == 2 && num >= 4)
                            {
                                petEventType2 = PetEventType.OPPONENENT_SPELLCAST_MANAVALUE_4PLUS;
                            }
                        }
                    }
                    if (Utils.m_fakePetActive && petEventType2 > PetEventType.INVALID && petEventType2 != PetEventType.IDLE)
                    {
                        global::Entity entity6 = gameState.GetEntity(1000001);
                        if (entity6 != null)
                        {
                            PetControllerGame petControllerGame2 = entity6.GetCard().GetActor().m_petController as PetControllerGame;
                            if (petControllerGame2 != null)
                            {
                                Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "触发：" + petEventType2.ToString());
                                petControllerGame2.HandleEvent(petEventType2);
                            }
                        }
                    }
                }
            }
        }

        public static void HandleEmote(EmoteType emoteType)
        {

            if (GameMgr.Get().IsMercenaries())
            {
                return;
            }
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                return;
            }
            PetEventType petEventType = PetEventType.INVALID;
            if (emoteType == EmoteType.GREETINGS)
            {
                petEventType = PetEventType.EMOTE_GREETINGS;
            }
            if (emoteType == EmoteType.WELL_PLAYED)
            {
                petEventType = PetEventType.EMOTE_WELLPLAYED;
            }
            if (emoteType == EmoteType.THANKS)
            {
                petEventType = PetEventType.EMOTE_THANKS;
            }
            if (emoteType == EmoteType.WOW)
            {
                petEventType = PetEventType.EMOTE_WOW;
            }
            if (emoteType == EmoteType.OOPS)
            {
                petEventType = PetEventType.EMOTE_OOPS;
            }
            if (emoteType == EmoteType.THREATEN)
            {
                petEventType = PetEventType.EMOTE_THREATEN;
            }
            if (emoteType == EmoteType.CONCEDE)
            {
                petEventType = PetEventType.EMOTE_CONCEDE;
            }
            if (Utils.m_fakePetActive && petEventType > PetEventType.INVALID && petEventType != PetEventType.IDLE)
            {
                global::Entity entity = gameState.GetEntity(1000001);
                if (entity != null)
                {
                    PetControllerGame petControllerGame = entity.GetCard().GetActor().m_petController as PetControllerGame;
                    if (petControllerGame != null)
                    {
                        Utils.MyLogger(BepInEx.Logging.LogLevel.Debug, "触发：" + petEventType.ToString());
                        petControllerGame.HandleEvent(petEventType);
                    }
                }
            }
        }

    }
}
