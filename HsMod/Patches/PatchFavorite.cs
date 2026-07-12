using Blizzard.T5.Core;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchFavorite
        {
            private static bool IsBgsLocalCollectionFavoriteOverrideEnabled()
            {
                return isBgsUnlockCollectionEnable.Value;
            }

            private static void SetBgsBoardFavoriteState(Hearthstone.DataModels.BattlegroundsBoardSkinCollectionPageDataModel pageModel, int favoriteDbid)
            {
                if (pageModel?.BoardSkinList == null)
                    return;

                foreach (Hearthstone.DataModels.BattlegroundsBoardSkinDataModel model in pageModel.BoardSkinList)
                    model.IsFavorite = model.BoardDbiId == favoriteDbid;
            }

            private static void SetBgsFinisherFavoriteState(Hearthstone.DataModels.BattlegroundsFinisherCollectionPageDataModel pageModel, int favoriteDbid)
            {
                if (pageModel?.FinisherList == null)
                    return;

                foreach (Hearthstone.DataModels.BattlegroundsFinisherDataModel model in pageModel.FinisherList)
                    model.IsFavorite = model.FinisherDbiId == favoriteDbid;
            }

            private static int GetPetIdFromVariant(int petVariantId)
            {
                PetVariantDbfRecord record = GameDbf.PetVariant.GetRecord(petVariantId);
                return record != null ? record.PetId : -1;
            }

            private static int GetFirstPetVariantId(int petId)
            {
                PetDbfRecord record = GameDbf.Pet.GetRecord(petId);
                PetVariantDbfRecord variant = record?.Variants?.FirstOrDefault();
                return variant != null ? variant.ID : -1;
            }

            private static string LocalBgsEmoteLoadoutPath => System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "HsBgsEmotes.cfg");

            private static List<int> LoadLocalBgsEmoteLoadout()
            {
                try
                {
                    if (!System.IO.File.Exists(LocalBgsEmoteLoadoutPath))
                        return new List<int>();

                    return System.IO.File.ReadAllText(LocalBgsEmoteLoadoutPath, Encoding.UTF8)
                        .Split(new[] { ',', '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            int id;
                            return int.TryParse(x.Trim(), out id) ? id : -1;
                        })
                        .Where(x => x > 0)
                        .Distinct()
                        .ToList();
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                    return new List<int>();
                }
            }

            private static void SaveLocalBgsEmoteLoadout(Hearthstone.BattlegroundsEmoteLoadout loadout)
            {
                try
                {
                    string content = "";
                    if (loadout?.Emotes != null)
                    {
                        content = String.Join(",", loadout.Emotes.Select(x => x.ToValue()).Where(x => x > 0));
                    }
                    System.IO.File.WriteAllText(LocalBgsEmoteLoadoutPath, content, new UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                }
            }

            private static void SaveHeroSkinMapping(int baseHeroCardDbid, int skinHeroCardDbid, bool favorite)
            {
                try
                {
                    string file = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, "HsSkins.cfg");
                    if (!System.IO.File.Exists(file))
                        System.IO.File.WriteAllText(file, LocalizationManager.GetLangValue("HsSkins.cfg"), new UTF8Encoding(false));

                    List<string> lines = System.IO.File.ReadAllLines(file, Encoding.UTF8).ToList();
                    lines.RemoveAll(line =>
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains(":"))
                            return false;

                        string[] parts = trimmed.Split(':');
                        int key;
                        return parts.Length == 2 && int.TryParse(parts[0].Trim(), out key) && key == baseHeroCardDbid;
                    });

                    if (favorite && skinHeroCardDbid != baseHeroCardDbid)
                        lines.Add($"{baseHeroCardDbid}:{skinHeroCardDbid}");

                    System.IO.File.WriteAllLines(file, lines, new UTF8Encoding(false));
                    LoadSkinsConfigFromFile();
                    Utils.UpdateHeroPowerMapping();
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManager), "IsFavoriteBattlegroundsBoardSkin")]
            public static bool PatchIsFavoriteBattlegroundsBoardSkin(Hearthstone.BattlegroundsBoardSkinId skinId, ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                __result = skinBgsBoard.Value != -1 && skinBgsBoard.Value == skinId.ToValue();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManager), "IsFavoriteBattlegroundsFinisher")]
            public static bool PatchIsFavoriteBattlegroundsFinisher(Hearthstone.BattlegroundsFinisherId finisherId, ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                __result = skinBgsFinisher.Value != -1 && skinBgsFinisher.Value == finisherId.ToValue();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManager), "IsFavoriteBattlegroundsGuideSkin")]
            public static bool PatchIsFavoriteBattlegroundsGuideSkin(CollectionManager __instance, Hearthstone.BattlegroundsGuideSkinId skinId, ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                int guideCardDbid;
                __result = skinBob.Value != -1
                           && __instance.GetBattlegroundsGuideSkinCardIdForSkinId(skinId, out guideCardDbid)
                           && guideCardDbid == skinBob.Value;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.BattlegroundsBoardSkinDataModel), "get_IsFavorite")]
            public static void PatchBattlegroundsBoardSkinDataModelIsFavorite(Hearthstone.DataModels.BattlegroundsBoardSkinDataModel __instance, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = skinBgsBoard.Value != -1 && __instance.BoardDbiId == skinBgsBoard.Value;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.BattlegroundsFinisherDataModel), "get_IsFavorite")]
            public static void PatchBattlegroundsFinisherDataModelIsFavorite(Hearthstone.DataModels.BattlegroundsFinisherDataModel __instance, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = skinBgsFinisher.Value != -1 && __instance.FinisherDbiId == skinBgsFinisher.Value;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.BattlegroundsEmoteDataModel), "get_IsEquipped")]
            public static void PatchBattlegroundsEmoteDataModelIsEquipped(Hearthstone.DataModels.BattlegroundsEmoteDataModel __instance, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = LoadLocalBgsEmoteLoadout().Contains(__instance.EmoteDbiId);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.PetSkinDataModel), "get_IsFavorite")]
            public static void PatchPetSkinDataModelIsFavorite(Hearthstone.DataModels.PetSkinDataModel __instance, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = skinPet.Value != -1 && __instance.PetVariantDbiId == skinPet.Value;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.PetDataModel), "get_IsFavorite")]
            public static void PatchPetDataModelIsFavorite(Hearthstone.DataModels.PetDataModel __instance, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = skinPet.Value != -1 && __instance.PetDbiId == GetPetIdFromVariant(skinPet.Value);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconBoardCollectionDetails), "ToggleFavorite")]
            public static bool PatchBaconBoardCollectionDetailsToggleFavorite(BaconBoardCollectionDetails __instance)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                var dataModel = Traverse.Create(__instance).Field("m_dataModel").GetValue<Hearthstone.DataModels.BattlegroundsBoardSkinDataModel>();
                if (dataModel == null || !dataModel.IsOwned)
                    return false;

                int selectedDbid = dataModel.BoardDbiId;
                int favoriteDbid = skinBgsBoard.Value == selectedDbid ? -1 : selectedDbid;
                skinBgsBoard.Value = favoriteDbid;
                skinBgsBoard.ConfigFile.Save();

                var pageModel = Traverse.Create(__instance).Field("m_pageDataModel").GetValue<Hearthstone.DataModels.BattlegroundsBoardSkinCollectionPageDataModel>();
                SetBgsBoardFavoriteState(pageModel, favoriteDbid);
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG board favorite => {favoriteDbid}");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetBattlegroundsFavoriteBoardSkin")]
            public static bool PatchSetBattlegroundsFavoriteBoardSkin(Hearthstone.BattlegroundsBoardSkinId bgFavoriteBoardSkinID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                skinBgsBoard.Value = bgFavoriteBoardSkinID.ToValue();
                skinBgsBoard.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "ClearBattlegroundsFavoriteBoardSkin")]
            public static bool PatchClearBattlegroundsFavoriteBoardSkin(Hearthstone.BattlegroundsBoardSkinId bgFavoriteBoardSkinID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                if (skinBgsBoard.Value == bgFavoriteBoardSkinID.ToValue())
                    skinBgsBoard.Value = -1;
                skinBgsBoard.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconGuideSkinInfoManager), "ToggleFavoriteSkin")]
            public static bool PatchBaconGuideSkinInfoManagerToggleFavoriteSkin(BaconGuideSkinInfoManager __instance)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                EntityDef entityDef = Traverse.Create(__instance).Field("m_currentEntityDef").GetValue<EntityDef>();
                string cardId = entityDef?.GetCardId();
                if (String.IsNullOrEmpty(cardId) || !CollectionManager.Get().IsBattlegroundsGuideCardId(cardId))
                    return false;

                int selectedDbid = GameUtils.TranslateCardIdToDbId(cardId);
                Hearthstone.BattlegroundsGuideSkinId guideSkinId;
                if (!CollectionManager.Get().GetBattlegroundsGuideSkinIdForCardId(selectedDbid, out guideSkinId))
                    return false;

                int favoriteDbid = skinBob.Value == selectedDbid ? -1 : selectedDbid;
                skinBob.Value = favoriteDbid;
                skinBob.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();

                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG guide favorite => {favoriteDbid}");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetBattlegroundsFavoriteGuideSkin")]
            public static bool PatchSetBattlegroundsFavoriteGuideSkin(Hearthstone.BattlegroundsGuideSkinId guideID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                int guideCardDbid;
                if (CollectionManager.Get().GetBattlegroundsGuideSkinCardIdForSkinId(guideID, out guideCardDbid))
                {
                    skinBob.Value = guideCardDbid;
                    skinBob.ConfigFile.Save();
                    CollectionManager.Get()?.OnCollectionChanged();
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "ClearBattlegroundsFavoriteGuideSkin")]
            public static bool PatchClearBattlegroundsFavoriteGuideSkin(Hearthstone.BattlegroundsGuideSkinId guideID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                int guideCardDbid;
                if (CollectionManager.Get().GetBattlegroundsGuideSkinCardIdForSkinId(guideID, out guideCardDbid) && skinBob.Value == guideCardDbid)
                {
                    skinBob.Value = -1;
                    skinBob.ConfigFile.Save();
                    CollectionManager.Get()?.OnCollectionChanged();
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconHeroSkinUtils), "IsBattlegroundsHeroSkinFavorited")]
            public static bool PatchIsBattlegroundsHeroSkinFavorited(EntityDef entityDef, ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                string cardId = entityDef?.GetCardId();
                if (String.IsNullOrEmpty(cardId))
                {
                    __result = false;
                    return false;
                }

                int selectedDbid = GameUtils.TranslateCardIdToDbId(cardId);
                int baseHeroDbid = GameUtils.TranslateCardIdToDbId(CollectionManager.Get().GetBattlegroundsBaseHeroCardId(cardId));
                int mappedDbid;
                __result = HeroesMapping.TryGetValue(baseHeroDbid, out mappedDbid) && mappedDbid == selectedDbid;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "UpdateFavoriteBattlegroundsHeroSkin")]
            public static bool PatchUpdateFavoriteBattlegroundsHeroSkin(int baseHeroCardDbid, int battlegroundsHeroSkinId, bool favorite)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                int skinHeroCardDbid;
                Hearthstone.BattlegroundsHeroSkinId skinId = Hearthstone.BattlegroundsHeroSkinId.FromTrustedValue(battlegroundsHeroSkinId);
                if (CollectionManager.Get().GetBattlegroundsHeroSkinCardIdForSkinId(skinId, out skinHeroCardDbid))
                {
                    SaveHeroSkinMapping(baseHeroCardDbid, skinHeroCardDbid, favorite);
                    CollectionManager.Get()?.OnCollectionChanged();
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG hero favorite => {baseHeroCardDbid}:{(favorite ? skinHeroCardDbid : -1)}");
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconFinisherCollectionDetails), "MakeFavorite")]
            public static bool PatchBaconFinisherCollectionDetailsMakeFavorite(BaconFinisherCollectionDetails __instance, Hearthstone.BattlegroundsFinisherId battlegroundsFinisherId)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                int favoriteDbid = battlegroundsFinisherId.ToValue();
                skinBgsFinisher.Value = favoriteDbid;
                skinBgsFinisher.ConfigFile.Save();

                var pageModel = Traverse.Create(__instance).Field("m_pageDataModel").GetValue<Hearthstone.DataModels.BattlegroundsFinisherCollectionPageDataModel>();
                SetBgsFinisherFavoriteState(pageModel, favoriteDbid);
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG finisher favorite => {favoriteDbid}");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconFinisherCollectionDetails), "ClearFavorite")]
            public static bool PatchBaconFinisherCollectionDetailsClearFavorite(BaconFinisherCollectionDetails __instance, Hearthstone.BattlegroundsFinisherId battlegroundsFinisherId)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                skinBgsFinisher.Value = -1;
                skinBgsFinisher.ConfigFile.Save();

                var pageModel = Traverse.Create(__instance).Field("m_pageDataModel").GetValue<Hearthstone.DataModels.BattlegroundsFinisherCollectionPageDataModel>();
                SetBgsFinisherFavoriteState(pageModel, -1);
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "HsMod BG finisher favorite => -1");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetBattlegroundsFavoriteFinisher")]
            public static bool PatchSetBattlegroundsFavoriteFinisher(Hearthstone.BattlegroundsFinisherId bgFavoriteFinisherID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                skinBgsFinisher.Value = bgFavoriteFinisherID.ToValue();
                skinBgsFinisher.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "ClearBattlegroundsFavoriteFinisher")]
            public static bool PatchClearBattlegroundsFavoriteFinisher(Hearthstone.BattlegroundsFinisherId bgFavoriteFinisherID)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                if (skinBgsFinisher.Value == bgFavoriteFinisherID.ToValue())
                    skinBgsFinisher.Value = -1;
                skinBgsFinisher.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CollectionManager), "IsEquippedBattlegroundsEmote")]
            public static void PatchIsEquippedBattlegroundsEmote(Hearthstone.BattlegroundsEmoteId emoteId, ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = LoadLocalBgsEmoteLoadout().Contains(emoteId.ToValue());
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CollectionManager), "CreateEmoteLoadoutDataModel")]
            public static void PatchCreateEmoteLoadoutDataModel(ref Hearthstone.DataModels.BattlegroundsEmoteLoadoutDataModel __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return;

                List<int> emoteIds = LoadLocalBgsEmoteLoadout();
                if (emoteIds.Count == 0 || __result == null)
                    return;

                __result.EmoteList = new Hearthstone.UI.DataModelList<Hearthstone.DataModels.BattlegroundsEmoteDataModel>();
                foreach (int emoteId in emoteIds)
                {
                    BattlegroundsEmoteDbfRecord record = GameDbf.BattlegroundsEmote.GetRecord(emoteId);
                    if (record != null)
                        __result.EmoteList.Add(new CollectibleBattlegroundsEmote(record).CreateEmoteDataModel());
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetBattlegroundsEmoteLoadout")]
            public static bool PatchSetBattlegroundsEmoteLoadout(Hearthstone.BattlegroundsEmoteLoadout loadout)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                SaveLocalBgsEmoteLoadout(loadout);
                CollectionManager.Get()?.OnCollectionChanged();
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, "HsMod BG emote loadout saved locally");
                return false;
            }

            private static int SaveLocalBgsPetFavorite(int petId, bool favorite)
            {
                int petVariantId = GetFirstPetVariantId(petId);
                if (petVariantId == -1)
                    return -1;

                skinPet.Value = favorite ? petVariantId : (GetPetIdFromVariant(skinPet.Value) == petId ? -1 : skinPet.Value);
                skinPet.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();

                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG pet favorite => {skinPet.Value}");
                return skinPet.Value;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetDetailDisplay), "OnWidgetEvent")]
            public static bool PatchPetDetailDisplayOnWidgetEvent(PetDetailDisplay __instance, string e, ref bool ___m_isWaitingForFavoritePetResponse, bool ___m_isBattlegroundsMode)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled() || !___m_isBattlegroundsMode || e != "TogglePetFavorite")
                    return true;

                Hearthstone.DataModels.PetDataModel petDataModel = __instance.PetDataModel;
                if (petDataModel == null || petDataModel.PetDbiId <= 0)
                    return false;

                bool favorite = !petDataModel.IsFavorite;
                int petVariantId = SaveLocalBgsPetFavorite(petDataModel.PetDbiId, favorite);
                if (petVariantId != -1)
                {
                    petDataModel.IsFavorite = favorite;
                    __instance.UpdatePetScene(favorite ? petVariantId : 0);
                }

                ___m_isWaitingForFavoritePetResponse = false;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PetPreviewDetail), "OnWidgetEvent")]
            public static void PatchPetPreviewDetailOnWidgetEvent(string e, ref bool ___m_isWaitingForFavoritePetResponse, bool ___m_isBattlegroundsMode)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled() && ___m_isBattlegroundsMode && e == "CODE_FAVORITE_VARIANT")
                    ___m_isWaitingForFavoritePetResponse = false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "FetchPetVariantFavoriteState")]
            public static bool PatchFetchPetVariantFavoriteState(int petVariantId, ref bool isHsFav, ref bool isBgFav)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                isHsFav = false;
                isBgFav = skinPet.Value != -1 && skinPet.Value == petVariantId;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "FetchPetFavoriteState")]
            public static bool PatchFetchPetFavoriteState(int petId, ref bool isHsFav, ref bool isBgFav)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                isHsFav = false;
                isBgFav = skinPet.Value != -1 && GetPetIdFromVariant(skinPet.Value) == petId;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "FindPetVariantToUse")]
            public static bool PatchFindPetVariantToUse(ref System.Nullable<int> petVariantId, ref System.Nullable<int> deckPetVariantId)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled() || skinPet.Value == -1)
                    return true;

                petVariantId = skinPet.Value;
                deckPetVariantId = null;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "CanUnfavoritePetVariant")]
            public static bool PatchCanUnfavoritePetVariant(ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                __result = true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetFavoritePetVariant")]
            public static bool PatchSetFavoritePetVariant(int petVariantId, System.Nullable<bool> isHsFavorite, System.Nullable<bool> isBgFavorite)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled() || !isBgFavorite.HasValue)
                    return true;

                skinPet.Value = isBgFavorite.Value ? petVariantId : (skinPet.Value == petVariantId ? -1 : skinPet.Value);
                skinPet.ConfigFile.Save();
                CollectionManager.Get()?.OnCollectionChanged();

                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"HsMod BG pet favorite => {skinPet.Value}");
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Network), "SetFavoritePet")]
            public static bool PatchSetFavoritePet(int petId, System.Nullable<bool> isHsFavorite, System.Nullable<bool> isBgFavorite)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled() || !isBgFavorite.HasValue)
                    return true;

                SaveLocalBgsPetFavorite(petId, isBgFavorite.Value);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "IsPetOwned")]
            [HarmonyPatch(typeof(PetsManager), "IsPetVariantOwned")]
            [HarmonyPatch(typeof(PetsManager), "IsPetCardOwned")]
            public static bool PatchPetsManagerOwned(ref bool __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                __result = true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PetsManager), "GetTotalPetsOwned")]
            public static bool PatchGetTotalPetsOwned(ref int __result)
            {
                if (!IsBgsLocalCollectionFavoriteOverrideEnabled())
                    return true;

                __result = Math.Max(1, GameDbf.Pet.GetRecords().Count());
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CollectiblePet), "get_OwnedCount")]
            public static void PatchCollectiblePetOwnedCount(ref int __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = Math.Max(1, __result);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Hearthstone.DataModels.PetLevelDataModel), "get_HasCompleted")]
            public static void PatchPetLevelDataModelHasCompleted(ref bool __result)
            {
                if (IsBgsLocalCollectionFavoriteOverrideEnabled())
                    __result = true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CornerSpellReplacementManager), "UpdateCornerReplacements")]
            private static void PatchUpdateCornerReplacements(ref CornerReplacementContext friendlyNewContext)
            {
                try
                {
                    if (skinPet.Value != -1)
                    {
                        Player playerBySide2 = GameState.Get()?.GetPlayerBySide(Player.Side.FRIENDLY);
                        playerBySide2?.SetTag(GAME_TAG.PET_VARIANT_ID, skinPet.Value);
                    }

                    if (skinOpposingPet.Value != -1)
                    {
                        Player playerBySide2 = GameState.Get()?.GetPlayerBySide(Player.Side.OPPOSING);
                        playerBySide2?.SetTag(GAME_TAG.PET_VARIANT_ID, skinOpposingPet.Value);
                    }
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                }
            }


            //加载处理
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Entity), "LoadCard")]
            public static void PatchLoadCard(Entity __instance, ref string cardId)
            {
                string rawCardID = cardId;
                if (cardId != null
                    && Utils.CheckInfo.IsMercenarySkin(cardId, out Utils.MercenarySkin skin))
                {
                    if ((goldenCardState.Value == Utils.CardState.Disabled) && (maxCardState.Value == Utils.CardState.Disabled))
                    {
                        cardId = GameUtils.TranslateDbIdToCardId(skin.Default);
                        goto LoadCardEnd;
                    }
                    if ((maxCardState.Value == Utils.CardState.Disabled) || (mercenaryDiamondCardState.Value == Utils.CardState.Disabled))
                    {
                        if (GameUtils.TranslateCardIdToDbId(cardId) == skin.Diamond)
                        {
                            cardId = GameUtils.TranslateDbIdToCardId(skin.Default);
                            goto LoadCardEnd;
                        }
                    }
                    if (!isOpponentGoldenCardShow.Value)
                    {
                        if (__instance.GetCard().GetControllerSide() == global::Player.Side.OPPOSING)
                        {
                            cardId = GameUtils.TranslateDbIdToCardId(skin.Default);
                            goto LoadCardEnd;
                        }
                    }
                    if ((maxCardState.Value == Utils.CardState.All) || (mercenaryDiamondCardState.Value == Utils.CardState.All))
                    {
                        if (skin.hasDiamond)
                        {
                            cardId = GameUtils.TranslateDbIdToCardId(skin.Diamond);
                            goto LoadCardEnd;
                        }
                    }
                    if ((maxCardState.Value == Utils.CardState.OnlyMy) || (mercenaryDiamondCardState.Value == Utils.CardState.OnlyMy))
                    {
                        if (skin.hasDiamond && (__instance.GetCard().GetControllerSide() == global::Player.Side.FRIENDLY))
                        {
                            cardId = GameUtils.TranslateDbIdToCardId(skin.Diamond);
                            goto LoadCardEnd;
                        }
                    }
                    if ((randomMercenarySkinEnable.Value == Utils.CardState.OnlyMy) || (randomMercenarySkinEnable.Value == Utils.CardState.All))
                    {
                        List<int> dbids = new List<int>();
                        dbids.AddRange(skin.Id);
                        dbids.Remove(skin.Diamond);
                        var dbid = dbids[UnityEngine.Random.Range(0, dbids.Count)];
                        if (randomMercenarySkinEnable.Value == Utils.CardState.OnlyMy)
                        {
                            if (__instance.GetCard().GetControllerSide() == global::Player.Side.FRIENDLY)
                            {
                                cardId = GameUtils.TranslateDbIdToCardId(dbid);
                                goto LoadCardEnd;
                            }
                        }
                        cardId = GameUtils.TranslateDbIdToCardId(dbid);
                        goto LoadCardEnd;
                    }
                }
                //string rawCardId = cardId;
                else if (cardId != null && DefLoader.Get()?.GetEntityDef(cardId)?.GetCardType() == TAG_CARDTYPE.HERO_POWER)
                {
                    if (isSkinDefalutHeroEnable.Value && !GameMgr.Get().IsBattlegrounds())
                    {
                        try
                        {
                            TAG_CLASS tagClass = DefLoader.Get().GetEntityDef(cardId).GetClass();
                            if (GameUtils.ORDERED_HERO_CLASSES.Contains(tagClass))
                            {
                                cardId = GameUtils.GetHeroPowerCardIdFromHero(Enumerable.FirstOrDefault(Enumerable.Where(GameDbf.CardHero.GetRecords().OrderBy(x => x.CardId).ToList(), (CardHeroDbfRecord x) => DefLoader.Get().GetEntityDef(x.CardId).GetClass() == tagClass)).CardId);
                                goto LoadCardEnd;
                            }
                        }
                        catch (Exception ex)
                        {
                            Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                        }
                    }
                    if (skinHero.Value != -1 && __instance.GetCard().GetControllerSide() == global::Player.Side.FRIENDLY)
                    {
                        cardId = GameUtils.GetHeroPowerCardIdFromHero(skinHero.Value);
                    }
                    else if (skinOpposingHero.Value != -1 && __instance.GetCard().GetControllerSide() == global::Player.Side.OPPOSING)
                    {
                        cardId = GameUtils.GetHeroPowerCardIdFromHero(skinOpposingHero.Value);
                    }

                    else if (__instance.GetCard().GetControllerSide() == global::Player.Side.FRIENDLY && HeroesMapping.Count != 0)

                    {
                        // Replace HeroPower
                        //UpdateCardsMappingReal(cardId, Utils.SkinType.HEROPOWER);
                        Utils.UpdateHeroPowerMapping();
                        HeroesPowerMapping.TryGetValue(cardId, out string res);
                        cardId = (res != null && res != "" && res != string.Empty) ? res : cardId;
                        goto LoadCardEnd;
                    }
                }

                else if (Utils.CheckInfo.IsHero(cardId, out Assets.CardHero.HeroType heroType))
                {
                    if (skinBob.Value != -1 && heroType == Assets.CardHero.HeroType.BATTLEGROUNDS_GUIDE)
                    {
                        //UpdateCardsMappingReal(cardId, Utils.SkinType.BOB);
                        if (Utils.CheckInfo.IsHero(skinBob.Value, out Assets.CardHero.HeroType _))
                            cardId = GameUtils.TranslateDbIdToCardId(skinBob.Value);
                    }
                    else if (heroType == Assets.CardHero.HeroType.BATTLEGROUNDS_HERO
                            && __instance.GetCard().GetControllerSide() == Player.Side.FRIENDLY
                        )
                    {
                        //UpdateCardsMappingReal(cardId, Utils.SkinType.BATTLEGROUNDSHERO);
                        if (skinHero.Value != -1)
                            cardId = GameUtils.TranslateDbIdToCardId(skinHero.Value);
                        else
                        {
                            //LoadSkinsConfigFromFile();
                            if (HeroesMapping.TryGetValue(GameUtils.TranslateCardIdToDbId(cardId), out int dbid))
                            {
                                if (Utils.CheckInfo.IsHero(dbid, out Assets.CardHero.HeroType res))
                                    if (res == Assets.CardHero.HeroType.BATTLEGROUNDS_HERO)
                                        cardId = GameUtils.TranslateDbIdToCardId(dbid);
                            }
                        }
                    }
                    else if (cardId.Substring(0, 5) == "HERO_"
                        && DefLoader.Get().GetEntityDef(cardId).GetCardType() == TAG_CARDTYPE.HERO
                        )
                    {
                        if (isSkinDefalutHeroEnable.Value && !GameMgr.Get().IsBattlegrounds())
                        {
                            try
                            {
                                TAG_CLASS tagClass = DefLoader.Get().GetEntityDef(cardId).GetClass();
                                if (GameUtils.ORDERED_HERO_CLASSES.Contains(tagClass))
                                {
                                    cardId = GameUtils.TranslateDbIdToCardId(Enumerable.FirstOrDefault(Enumerable.Where(GameDbf.CardHero.GetRecords().OrderBy(x => x.CardId).ToList(), (CardHeroDbfRecord x) => DefLoader.Get().GetEntityDef(x.CardId).GetClass() == tagClass)).CardId);
                                    goto LoadCardEnd;
                                }
                            }
                            catch (Exception ex)
                            {
                                Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                            }
                        }

                        if (__instance.GetCard().GetControllerSide() == Player.Side.FRIENDLY)
                        {
                            Utils.CacheRawHeroCardId = rawCardID;
                            //UpdateCardsMappingReal(cardId, Utils.SkinType.HERO);
                            if (skinHero.Value != -1)
                                cardId = GameUtils.TranslateDbIdToCardId(skinHero.Value);
                            else
                            {
                                //LoadSkinsConfigFromFile();
                                if (HeroesMapping.TryGetValue(GameUtils.TranslateCardIdToDbId(cardId), out int dbid))
                                {
                                    if (Utils.CheckInfo.IsHero(dbid, out Assets.CardHero.HeroType res))
                                        if (res != Assets.CardHero.HeroType.BATTLEGROUNDS_HERO || res != Assets.CardHero.HeroType.BATTLEGROUNDS_GUIDE)
                                            cardId = GameUtils.TranslateDbIdToCardId(dbid);
                                }
                            }
                        }
                        else if (__instance.GetCard().GetControllerSide() == Player.Side.OPPOSING)
                        {
                            if (skinOpposingHero.Value != -1)
                                cardId = GameUtils.TranslateDbIdToCardId(skinOpposingHero.Value);
                        }
                    }
                }
                else if (skinCoin.Value != -1
                        //&& !GameMgr.Get().IsBattlegrounds()
                        && cardId != null && cardId.Length > 4
                        && Utils.CheckInfo.IsCoin(cardId)
                        && __instance.GetCard().GetControllerSide() == Player.Side.FRIENDLY)
                {
                    //int coin = skinCoin.Value;
                    //UpdateCardsMappingReal(cardId, Utils.SkinType.COIN);
                    cardId = GameUtils.TranslateDbIdToCardId(skinCoin.Value);
                }
            LoadCardEnd:    // todo: check Signature
                try
                {
                    if (__instance.GetCard()?.GetControllerSide() == Player.Side.FRIENDLY)
                        Utils.UpdateHeroTag(cardId);
                    __instance?.SetCardId(cardId);
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                    cardId = rawCardID;
                    __instance?.SetCardId(rawCardID);
                }
                finally
                {
                    __instance?.SetRealTimePremium(__instance.GetPremiumType());
                }
                //return;
            }

            //刷新卡牌画面，解决进化、退化异常
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Card), "RefreshActor")]
            public static void RefreshActor(Card __instance)
            {
                try
                {
                    // Todo: 添加更细致化的判断条件。
                    if ((__instance?.GetEntity()?.GetZone() == TAG_ZONE.PLAY) || (__instance?.GetEntity()?.GetZone() == TAG_ZONE.HAND))
                    {
                        __instance?.GetActor()?.SetCard(__instance);
                        __instance?.GetActor()?.SetCardDefFromEntity(__instance.GetEntity());
                        __instance?.GetActor()?.SetEntity(__instance.GetEntity());
                        __instance?.GetActor()?.UpdateAllComponents();
                    }
                    //if (__instance?.GetEntity()?.GetCard()?.GetControllerSide() == Player.Side.FRIENDLY)
                    //{
                    //    string cardId = __instance?.GetEntity()?.GetCardId();
                    //    var cardType = __instance?.GetEntity()?.GetCardType();
                    //    var cardPremium = __instance.GetEntity().GetPremiumType();
                    //    if (cardType == TAG_CARDTYPE.HERO || cardType == TAG_CARDTYPE.HERO_POWER)
                    //    {
                    //        DefLoader.DisposableCardDef cardDef = DefLoader.Get().GetCardDef(cardId, cardPremium);

                    //        if (!DefLoader.Get().HasLoadedEntityDefs())
                    //        {
                    //            DefLoader.Get().LoadAllEntityDefs();
                    //        }
                    //        __instance?.GetActor()?.SetCard(__instance);
                    //        __instance?.GetActor()?.SetCardDef(cardDef);
                    //        __instance?.GetActor()?.SetEntity(__instance.GetEntity());
                    //        __instance?.GetActor()?.SetPremium(cardPremium);
                    //        __instance?.GetActor()?.UpdateAllComponents();
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Error, ex);
                }
            }


            //判断存在异画是否存在，缓解异画问题 Signature frame for RLK_Prologue_RLK_653 not found.
            //private static readonly MethodInfo getSignatureActor = typeof(ActorNames).GetMethod("GetSignatureActor", BindingFlags.Instance | BindingFlags.NonPublic);
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ActorNames), "GetNameWithPremiumType")]
            public static void PatchGetNameWithPremiumType(ActorNames __instance, ref string __result,
                                                            ref ActorNames.ACTOR_ASSET actorName, ref TAG_PREMIUM premiumType, ref string cardId)
            {
                if (__result != null)
                {
                    return;
                }
                string text = null;
                Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"Function return null\nGetNameWithPremiumType(ActorNames.ACTOR_ASSET {actorName}, TAG_PREMIUM {premiumType}, string {cardId});");
                if (String.IsNullOrEmpty(__result))
                {
                    //ActorNames.s_diamondActorAssets.TryGetValue(actorName, out text);
                    //if (!String.IsNullOrEmpty(text))
                    //{
                    //    goto PatchGetNameWithPremiumTypeEnd;
                    //}
                    //ActorNames.s_actorAssets.TryGetValue(actorName, out text);
                    //if (!String.IsNullOrEmpty(text))
                    //{
                    //    goto PatchGetNameWithPremiumTypeEnd;
                    //}
                    //text = (string)getSignatureActor?.Invoke(__instance, new object[] { cardId, actorName });
                    //if (!String.IsNullOrEmpty(text))
                    //{
                    //    goto PatchGetNameWithPremiumTypeEnd;
                    //}
                    ActorNames.s_premiumActorAssets.TryGetValue(actorName, out text);
                    if (!String.IsNullOrEmpty(text))
                    {
                        goto PatchGetNameWithPremiumTypeEnd;
                    }
                }
            PatchGetNameWithPremiumTypeEnd:
                __result = text;
            }


            //鲍勃替换语音
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TB_BaconShop), "GetBattlegroundsGuideSkinCardId")]
            public static bool PatchGetFavoriteBattlegroundsGuideSkinCardId(ref string __result)
            {
                if (skinBob.Value == -1)
                    return true;
                else
                {
                    if (Utils.CheckInfo.IsHero(skinBob.Value, out Assets.CardHero.HeroType _))
                    {
                        __result = GameUtils.TranslateDbIdToCardId(skinBob.Value);
                        return false;
                    }
                    else return true;
                }
            }

            //游戏面板替换
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GameMgr), "ChangeBoardIfNecessary")]
            public static bool PatchChangeBoardIfNecessary(ref Network.GameSetup ___m_gameSetup)
            {
                if ((skinBoard.Value != -1) && Utils.CheckInfo.IsBoard())
                {
                    ___m_gameSetup.Board = skinBoard.Value;
                    return false;
                }
                return true;
            }

            //卡背替换
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Gameplay), "InitCardBacks")]
            public static bool PatchGameplayInitCardBacks()
            {
                if (skinCardBack.Value != -1 && Utils.CheckInfo.IsCardBack())
                {
                    Player friendlySidePlayer = GameState.Get()?.GetFriendlySidePlayer();
                    if (friendlySidePlayer != null)
                        _ = friendlySidePlayer.GetCardBackId();
                    int opponentCardBackID = 0;
                    Player opposingSidePlayer = GameState.Get()?.GetOpposingSidePlayer();
                    if (opposingSidePlayer != null)
                        opponentCardBackID = opposingSidePlayer.GetCardBackId();
                    int friendlyCardBackID = skinCardBack.Value;
                    if (GameMgr.Get().IsBattlegrounds())   // FIXME: 酒馆对战中可能无法正常显示对手卡背
                    {
                        opponentCardBackID = friendlyCardBackID;
                    }

                    CardBackManager.Get().SetGameCardBackIDs(friendlyCardBackID, opponentCardBackID);
                    return false;
                }
                else return true;
            }
            //替换开包卡背
            private static readonly MethodInfo getValidCardBackID = typeof(CardBackManager).GetMethod("GetValidCardBackID", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo loadCardBackPrefabIntoSlot = typeof(CardBackManager).GetMethod("LoadCardBackPrefabIntoSlot", BindingFlags.Instance | BindingFlags.NonPublic);
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CardBackManager), "LoadCardBackIdIntoSlot")]
            public static bool PatchGameplayInitCardBacks(ref int cardBackId,
                                                          ref CardBackManager.CardBackSlot slot,
                                                          ref Map<int, CardBackData> ___m_cardBackData,
                                                          CardBackManager __instance
                                                          )
            {
                if (skinCardBack.Value != -1 && Utils.CheckInfo.IsCardBack() && SceneMgr.Get().GetMode() == SceneMgr.Mode.PACKOPENING && slot == CardBackManager.CardBackSlot.FAVORITE)
                {
                    int validCardBackID = (int)getValidCardBackID.Invoke(__instance, new object[] { skinCardBack.Value });
                    //int validCardBackID = skinCardBack.Value;
                    if (___m_cardBackData.TryGetValue(validCardBackID, out CardBackData cardBackData))
                    {
                        loadCardBackPrefabIntoSlot?.Invoke(__instance, new object[] { (AssetReference)cardBackData.PrefabName, slot });
                    }
                    return false;
                }
                else return true;
            }

            //酒馆对战面板
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaconBoard), "OnBoardSkinChosen")]
            [HarmonyPatch(typeof(BaconBoard), "LoadInitialTavernBoard")]
            public static void PatchOnBoardSkinChosen(ref int chosenBoardSkinId)
            {
                if (skinBgsBoard.Value != 0 && Utils.CheckInfo.IsBgsBoard())
                    chosenBoardSkinId = skinBgsBoard.Value;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(FinisherGameplaySettings), "GetFinisherGameplaySettings")]
            public static bool PatchGetFinisherGameplaySettings(ref Entity hero, ref FinisherGameplaySettings __result)
            {
                int num;
                if (skinBgsFinisher.Value != -1
                    && Utils.CheckInfo.IsBgsFinisher()
                    && hero.GetControllerSide() == Player.Side.FRIENDLY
                    )
                {
                    num = skinBgsFinisher.Value;
                }
                else
                {
                    return true;
                }
                if (num <= 0)
                {
                    Log.Spells.PrintError(hero.GetDebugName() + " has no tag BATTLEGROUNDS_FAVORITE_FINISHER. Using Default Finisher.", Array.Empty<object>());
                    num = 1;
                }
                BattlegroundsFinisherDbfRecord battlegroundsFinisherDbfRecord = GameDbf.BattlegroundsFinisher.GetRecord(num);
                if (battlegroundsFinisherDbfRecord == null)
                {
                    Log.Spells.PrintError(string.Format("No Finisher was found for Finisher ID {0}. Using default finisher.", num), Array.Empty<object>());
                    battlegroundsFinisherDbfRecord = GameDbf.BattlegroundsFinisher.GetRecord(1);
                }
                AssetReference assetReference = AssetReference.CreateFromAssetString(battlegroundsFinisherDbfRecord.GameplaySettings);
                Blizzard.T5.AssetManager.AssetHandle<FinisherGameplaySettings> assetHandle = ((assetReference != null) ? AssetLoader.Get().LoadAsset<FinisherGameplaySettings>(assetReference, AssetLoadingOptions.None) : null);
                FinisherGameplaySettings finisherGameplaySettings = (assetHandle ? assetHandle.Asset : null);
                if (finisherGameplaySettings == null)
                {
                    Log.Spells.PrintError(string.Format("Finisher ID {0} is missing its finisher settings entirely in HE2. Using default finisher.", num), Array.Empty<object>());
                    battlegroundsFinisherDbfRecord = GameDbf.BattlegroundsFinisher.GetRecord(1);
                    assetReference = AssetReference.CreateFromAssetString(battlegroundsFinisherDbfRecord.GameplaySettings);
                    assetHandle = AssetLoader.Get().LoadAsset<FinisherGameplaySettings>(assetReference, AssetLoadingOptions.None);
                    finisherGameplaySettings = assetHandle.Asset;
                }
                __result = finisherGameplaySettings;
                return false;
            }

            //反和谐
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AssetLoader), "GetRuntimeAssetVariant", new Type[]
            {
                    typeof(AssetReference),
                    typeof(Hearthstone.AssetVariantTags.Quality),
                    typeof(bool)
            })]
            public static bool PatchAssetLoader(ref AssetReference assetRef, ref bool disableLocalization)
            {
                if (isPatchAssetLoader.Value)
                {
                    if (assetRef.FileName != null && assetRef.FileName.Length > 7 && !assetRef.FileName.ToLower().Contains("logo") && assetRef.FileName.Substring(assetRef.FileName.Length - 7) != ".prefab" && assetRef.FileName.Substring(assetRef.FileName.Length - 4) != ".wav" && assetRef.FileName.Contains("."))
                    {
                        disableLocalization = true;
                    }
                }
                return true;
            }


            //偏好硬币修改，不需要patch
            //[HarmonyPrefix]
            //[HarmonyPatch(typeof(CosmeticCoinManager), "GetFavoriteCoinId")]
            //public static bool PatchGetFavoriteCoinId(ref int __result)
            //{
            //    if (skinCoin.Value == 0) return true;
            //    if (Utils.CheckInfo.IsCoin())
            //    {
            //        int res = 0;
            //        foreach (var record in GameDbf.CosmeticCoin.GetRecords())
            //        {
            //            if (record != null)
            //            {
            //                if (record.CardId == skinCoin.Value)
            //                {
            //                    res = record.ID;
            //                    break;
            //                }
            //            }
            //        }
            //        __result = res;
            //        return false;
            //    }
            //    return true;
            //}
            //[HarmonyPrefix]
            //[HarmonyPatch(typeof(CoinManager), "GetFavoriteCoinCardId")]
            //public static bool PatchGetFavoriteCoinCardId(ref string __result)
            //{
            //    if (skinCoin.Value == 0) return true;
            //    if (Utils.CheckInfo.IsCoin())
            //    {
            //        __result = GameUtils.TranslateDbIdToCardId(skinCoin.Value);
            //        return false;
            //    }
            //    return true;
            //}
        }
    }
}
