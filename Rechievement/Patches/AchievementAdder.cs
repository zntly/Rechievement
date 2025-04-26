using HarmonyLib;
using Game.Achievements;
using Home.Shared;
using UnityEngine;
using Server.Shared.State;
using System.Collections.Generic;
using System.Collections;
using System;
using Game.Simulation;
using Server.Shared.Info;
using Server.Shared.Extensions;
using System.Reflection;
using Services;
using Cinematics.Players;
using Server.Shared.Messages;
using System.Security.Cryptography;
using Game.Interface;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Server.Shared.State.Chat;
using Game.Chat.Decoders;
using Game.Tos;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Server.Shared.Cinematics.Data;

namespace Rechievement.Patches
{
    [HarmonyPatch(typeof(GameSimulation), "HandleOnGameInfoChanged")]
    public class AchievementAdder
    {
        // Token: 0x06000030 RID: 48 RVA: 0x00002389 File Offset: 0x00000589
        [HarmonyPostfix]
        public static void Postfix(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.FIRST_DAY)
            {
                PlayerIdentityData playerIdentityData = Service.Game.Sim.simulation.myIdentity.Data;
                Role role = playerIdentityData.role;
                FactionType faction = playerIdentityData.faction;
                currentRole = role;
                currentFaction = faction;
                Func<IEnumerator> roleCoroutine;
                Func<IEnumerator> factionCoroutine;
                if (allRoleCoroutines.TryGetValue(role, out roleCoroutine))
                    StartCoroutine(roleCoroutine.Invoke());
                if (allFactionCoroutines.TryGetValue(faction, out factionCoroutine))
                    StartCoroutine(factionCoroutine.Invoke());
                foreach (Func<IEnumerator> func in globalCoroutines)
                    StartCoroutine(func.Invoke());
                MethodInfo mOriginal = AccessTools.Method(typeof(RoleCardPanel), nameof(RoleCardPanel.HandleOnMyIdentityChanged));
                MethodInfo mPostfix = AccessTools.Method(typeof(AchievementAdder), nameof(IdentityChangePatch));
                IdentityPatch = Utils.harmonyInstance.Patch(mOriginal, null, new HarmonyMethod(mPostfix));
            }
            else if (gameInfo.gamePhase == GamePhase.RESULTS && gameInfo.resultsPhase == ResultsPhase.WRAP_UP)
            {
                ClearCoroutines();
                ClearPatches();
                IdentityPatch = null;
                necessities.Clear();
                shown.Clear();
                processed.Clear();
            }
        }

        public static List<RechievementData> shown = new List<RechievementData>();

        public static void IdentityChangePatch(PlayerIdentityData playerIdentityData)
        {
            if (playerIdentityData.role != currentRole || playerIdentityData.faction != currentFaction)
            {
                necessities.SetValue("Current Target", -1);
                ClearCoroutines();
                ClearPatches();
                MethodInfo mOriginal = AccessTools.Method(typeof(RoleCardPanel), nameof(RoleCardPanel.HandleOnMyIdentityChanged));
                MethodInfo mPostfix = AccessTools.Method(typeof(AchievementAdder), nameof(IdentityChangePatch));
                IdentityPatch = Utils.harmonyInstance.Patch(mOriginal, null, new HarmonyMethod(mPostfix));
                Role role = playerIdentityData.role;
                FactionType faction = playerIdentityData.faction;
                currentRole = role;
                currentFaction = faction;
                Func<IEnumerator> roleCoroutine;
                Func<IEnumerator> factionCoroutine;
                if (allRoleCoroutines.TryGetValue(role, out roleCoroutine))
                    StartCoroutine(roleCoroutine.Invoke());
                if (allFactionCoroutines.TryGetValue(faction, out factionCoroutine))
                    StartCoroutine(factionCoroutine.Invoke());
                foreach (Func<IEnumerator> func in globalCoroutines)
                    StartCoroutine(func.Invoke());
            }
        }

        public static MethodInfo IdentityPatch;

        public static List<Coroutine> activeCoroutines = new List<Coroutine>();

        public static Dictionary<MethodInfo, Tuple<Type, string, Type[]>> harmonyPatches = new Dictionary<MethodInfo, Tuple<Type, string, Type[]>>();

        public static Dictionary<Role, Func<IEnumerator>> allRoleCoroutines = new Dictionary<Role, Func<IEnumerator>>
        {
            // Main Town
            { Role.ADMIRER, Admirer },
            { Role.AMNESIAC, Amnesiac },
            { Role.BODYGUARD, Bodyguard },
            { Role.CLERIC, Cleric },
            { Role.CORONER, Coroner },
            { Role.CRUSADER, Crusader },
            { Role.DEPUTY, Deputy },
            { Role.INVESTIGATOR, Investigator },
            { Role.JAILOR, Jailor },
            { Role.LOOKOUT, Lookout },
            { Role.MAYOR, Mayor },
            { Role.MONARCH, Monarch },
            { Role.PROSECUTOR, Prosecutor },
            { Role.PSYCHIC, Psychic },
            { Role.RETRIBUTIONIST, Retributionist },
            { Role.SEER, Seer },
            { Role.SHERIFF, Sheriff },
            { Role.SPY, Spy },
            { Role.TAVERNKEEPER, TavernKeeper },
            { Role.TRACKER, Tracker },
            { Role.TRAPPER, Trapper },
            { Role.TRICKSTER, Trickster },
            { Role.VETERAN, Veteran },
            { Role.VIGILANTE, Vigilante },
            // Main Coven
            { Role.CONJURER, Conjurer },
            { Role.COVENLEADER, CovenLeader },
            { Role.DREAMWEAVER, Dreamweaver },
            { Role.ENCHANTER, Enchanter },
            { Role.HEXMASTER, HexMaster },
            { Role.ILLUSIONIST, Illusionist },
            { Role.JINX, Jinx },
            { Role.MEDUSA, Medusa },
            { Role.NECROMANCER, Necromancer },
            { Role.POISONER, Poisoner },
            { Role.POTIONMASTER, PotionMaster },
            { Role.RITUALIST, Ritualist },
            { Role.VOODOOMASTER, VoodooMaster },
            { Role.WILDLING, Wildling },
            { Role.WITCH, Witch },
            // Main Neutrals/Apocalypse
            { Role.ARSONIST, Arsonist },
            { Role.BAKER, Baker },
            { Role.BERSERKER, Berserker },
            { Role.DOOMSAYER, Doomsayer },
            { Role.EXECUTIONER, Executioner },
            { Role.JESTER, Jester },
            { Role.PIRATE, Pirate },
            { Role.PLAGUEBEARER, Plaguebearer },
            { Role.SERIALKILLER, SerialKiller },
            { Role.SHROUD, Shroud },
            { Role.SOULCOLLECTOR, SoulCollector },
            { Role.WEREWOLF, Werewolf },
            // Main Specials
            { Role.VAMPIRE, Vampire },
            { Role.CURSED_SOUL, CursedSoul },
            // Outliers
            { Role.SOCIALITE, SocialiteOrBanshee }, // Socialite (Vanilla) or Banshee (BToS2)
            { Role.MARSHAL, MarshalOrJackal }, // Marshal (Vanilla) or Jackal (BToS2)
            { Role.ORACLE, OracleOrMarshal }, // Oracle (Vanilla) or Marshal (BToS2)
            // BToS2
            { BToS2Roles.Judge, Judge },
            { BToS2Roles.Auditor, Auditor },
            { BToS2Roles.Inquisitor, Inquisitor },
            { BToS2Roles.Starspawn, Starspawn },
            { BToS2Roles.Warlock, Warlock }
        };

        public static Dictionary<FactionType, Func<IEnumerator>> allFactionCoroutines = new Dictionary<FactionType, Func<IEnumerator>>
        {
            { FactionType.TOWN, Town },
            { FactionType.COVEN, Coven },
            { FactionType.APOCALYPSE, Apocalypse },
            { FactionType.VAMPIRE, VampireFaction },
            { BToS2Factions.Jackal, JackalOrRecruit },
            { BToS2Factions.Egotist, Egotist },
            { BToS2Factions.Pandora, Pandora },
            { BToS2Factions.Compliance, Compliance }
        };

        public static List<Func<IEnumerator>> globalCoroutines = new List<Func<IEnumerator>>
        {
            WagaBabaBobo, BalancedList, Revolution, MyLifeIsAnUnknownObstacle, HaHaImInDanger, WhyMe, LJudge
        };

        public static Dictionary<string, object> necessities = new Dictionary<string, object>();

        public static void StartCoroutine(IEnumerator enumerator)
        {
            Coroutine coroutine = ApplicationController.ApplicationContext.StartCoroutine(enumerator);
            activeCoroutines.Add(coroutine);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            try
            {
                ApplicationController.ApplicationContext.StopCoroutine(coroutine);
            }
            catch { }
            // if (activeCoroutines.Contains(coroutine))
            // activeCoroutines.Remove(coroutine);
        }
        public static IEnumerator Default()
        {
            yield return new WaitForEndOfFrame();
            Debug.LogWarning("test -----------------");
            yield break;
        }

        public static MethodInfo NewPrefix(Type type, string methodName, string func, Type[] parameters = null)
        {
            MethodInfo mOriginal = AccessTools.Method(type, methodName, parameters);
            MethodInfo mPrefix = AccessTools.Method(typeof(AchievementAdder), func);
            MethodInfo newPatch = Utils.harmonyInstance.Patch(mOriginal, new HarmonyMethod(mPrefix));
            harmonyPatches.SetValue(newPatch, new Tuple<Type, string, Type[]>(type, methodName, parameters));
            return newPatch;
        }
        public static MethodInfo NewPostfix(Type type, string methodName, string func, Type[] parameters = null)
        {
            MethodInfo mOriginal = AccessTools.Method(type, methodName, parameters);
            MethodInfo mPostfix = AccessTools.Method(typeof(AchievementAdder), func);
            MethodInfo newPatch = Utils.harmonyInstance.Patch(mOriginal, null, new HarmonyMethod(mPostfix));
            harmonyPatches.SetValue(newPatch, new Tuple<Type, string, Type[]>(type, methodName, parameters));
            return newPatch;
        }
        public static void Unpatch(MethodInfo newPatch)
        {
            if (harmonyPatches.ContainsKey(newPatch))
            {
                Tuple<Type, string, Type[]> stuff = harmonyPatches.GetValue(newPatch, null);
                Utils.harmonyInstance.Unpatch(AccessTools.Method(stuff.Item1, stuff.Item2, stuff.Item3), newPatch);
                // harmonyPatches.Remove(newPatch);
            }
        }
        public static void ClearCoroutines()
        {
            foreach (Coroutine coroutine in activeCoroutines)
                StopCoroutine(coroutine);
            activeCoroutines.Clear();
        }
        public static void ClearPatches()
        {
            foreach (KeyValuePair<MethodInfo, Tuple<Type, string, Type[]>> keyValuePair in harmonyPatches)
                Unpatch(keyValuePair.Key);
            harmonyPatches.Clear();
            Utils.harmonyInstance.UnpatchSelf();
        }

        // General Patches
        public static void GeneralTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.NightAbility)
                    necessities.SetValue("Current Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
            }
        }
        public static void GeneralRemoveTargetIfImpededPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED || chatLog.messageId == GameFeedbackMessage.JAILED_TARGET || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || chatLog.messageId == GameFeedbackMessage.CANNOT_USE_ABILITY_WHILE_INSANE || chatLog.messageId == GameFeedbackMessage.WITCH_CONTROLLED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY || (Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1100))
                    necessities.SetValue("Current Target", -1);
            }
        }
        public static void GeneralRemoveTargetStartOfDiscussion(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.DISCUSSION)
                necessities.SetValue("Current Target", -1);
        }
        public static void GeneralNecronomiconTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION && Service.Game.Sim.simulation.observations.roleCardObservation.Data.powerUp == POWER_UP_TYPE.NECRONOMICON)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.NightAbility)
                    necessities.SetValue("Necronomicon Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
            }
            else if (chatLogMessage.chatLogEntry.type == ChatType.FACTION_TARGET_SELECTION)
            {
                ChatLogFactionTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogFactionTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.NightAbility && chatLog.bHasNecronomicon)
                    necessities.SetValue("Necronomicon Target", chatLog.bIsCancel ? -1 : chatLog.teammateTargetingPosition1);
                else if (chatLog.teammateRole == Role.ILLUSIONIST && chatLog.menuChoiceType == MenuChoiceType.NightAbility2 && chatLog.bHasNecronomicon)
                    necessities.SetValue("Necronomicon Target", chatLog.bIsCancel ? -1 : chatLog.teammateTargetingPosition2);
            }
        }
        public static void GeneralDetectApocalypse(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.PESTILENCE_HAS_EMERGED)
                    necessities.SetValue("Pestilence", true);
                else if (chatLog.messageId == GameFeedbackMessage.WAR_HAS_EMERGED)
                    necessities.SetValue("War", true);
                else if (chatLog.messageId == GameFeedbackMessage.FAMINE_HAS_EMERGED)
                    necessities.SetValue("Famine", true);
                else if (chatLog.messageId == GameFeedbackMessage.DEATH_HAS_EMERGED || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1097)
                    necessities.SetValue("Death", true);
            }
        }
        public static void GeneralDetectApocalypseDeath(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
                Role deadRole = chatLog.killRecord.playerRole;
                if (deadRole == Role.PLAGUEBEARER || deadRole == Role.PESTILENCE)
                    necessities.SetValue("Pestilence", false);
                else if (deadRole == Role.BERSERKER || deadRole == Role.WAR)
                    necessities.SetValue("War", false);
                else if (deadRole == Role.BAKER || deadRole == Role.FAMINE)
                    necessities.SetValue("Famine", false);
                else if (deadRole == Role.SOULCOLLECTOR || deadRole == BToS2Roles.Warlock || deadRole == Role.DEATH)
                    necessities.SetValue("Death", false);
            }
        }
        public static void GeneralDetectTownTraitor(ChatLogMessage chatLogMessage)
        {
            if (processed.Contains(chatLogMessage))
                return;
            processed.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TOWN_TRAITOR_IS)
                {
                    int tt = (int)necessities.GetValue("Town Traitor", -1);
                    if (tt == -1)
                        necessities.SetValue("Town Traitor", chatLog.playerNumber1);
                    else
                        necessities.SetValue("Town Traitor 2", chatLog.playerNumber1);
                }
            }
        }
        public static bool GeneralGetIfTownTraitor(int playerNumber)
        {
            return (int)necessities.GetValue("Town Traitor", -1) == playerNumber || (int)necessities.GetValue("Town Traitor 2", -1) == playerNumber;
        }
        // Role Coroutines
        // Admirer
        public static IEnumerator Admirer()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(AdmirerMessagePatch));
            yield break;
        }
        public static void AdmirerMessagePatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ADMIRER_PROPOSAL_ACCEPTED_ROLE_REVEAL && chatLog.role1 == Role.DOOMSAYER)
                    NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AdmirerFactionWinPatch));
                else if (Utils.IsBTOS2())
                {
                    if (chatLog.messageId == (GameFeedbackMessage)1111)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Happy to Help!", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Happy to Help!",
                                Sprite = Utils.GetAssetBundleSprite("TownAdmirer"), // Give Custom Sprite - TownAdmirer
                                Description = "Prevent an evil ability on a player",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    } else if (chatLog.messageId == (GameFeedbackMessage)1109)
                    {
                        if ((currentFaction == FactionType.APOCALYPSE || currentFaction == BToS2Factions.Pandora) && (Service.Game.Sim.simulation.knownRolesAndFactions.Data.GetValue(chatLog.playerNumber1, new Tuple<Role, FactionType>(Role.NONE, FactionType.NONE)).Item1 == Role.BERSERKER || Service.Game.Sim.simulation.knownRolesAndFactions.Data.GetValue(chatLog.playerNumber1, new Tuple<Role, FactionType>(Role.NONE, FactionType.NONE)).Item1 == Role.WAR))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("Love and War", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "Love and War",
                                    Sprite = Utils.GetAssetBundleSprite("ApocAdmirer"), // Give Custom Sprite - ApocAdmirer
                                    Description = "Bestow your Berserker",
                                    Vanilla = false,
                                    BToS2 = true
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        } else if (currentFaction == BToS2Factions.Jackal && Service.Game.Sim.simulation.observations.playerEffects[chatLog.playerNumber1].Data.effects.Contains((EffectType)100))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("Strictly Business", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "Strictly Business",
                                    Sprite = Utils.GetAssetBundleSprite("RecAdmirer"), // Give Custom Sprite - RecAdmirer
                                    Description = "Bestow your other Recruit",
                                    Vanilla = false,
                                    BToS2 = true
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        }
                    }
                }
            }
        }

        public static void AdmirerFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == currentFaction)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Written in the Stars", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Written in the Stars",
                        Sprite = Utils.GetAssetBundleSprite("TownAdmirer"), // Give Custom Sprite - TownAdmirer
                        Description = "Propose to a Doomsayer and win the game",
                        Vanilla = true,
                        BToS2 = false
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Amnesiac
        public static IEnumerator Amnesiac()
        {
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AmnesiacFactionWinPatch));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(AmnesiacDetectRememberNonTown));
            yield break;
        }
        public static void AmnesiacFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (currentRole == Role.AMNESIAC && __instance.cinematicData.winningFaction == currentFaction)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("What Happened?", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "What Happened?",
                        Sprite = Utils.GetRoleSprite(Role.AMNESIAC),
                        Description = "Win without remembering a role",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void AmnesiacDetectRememberNonTown(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.AMNESIAC_REMEMBERED_THEY_WERE_LIKE && !chatLog.role1.IsTownAligned())
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Alchemory", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Alchemory",
                            Sprite = Utils.GetRoleSprite(Role.AMNESIAC),
                            Description = "Remember an evil role",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Bodyguard
        public static IEnumerator Bodyguard()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(BodyguardDetectTargetDeath));
            yield break;
        }

        public static void BodyguardDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (!chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Mission Failed", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Mission Failed",
                        Sprite = Utils.GetRoleSprite(Role.BODYGUARD),
                        Description = "Have your target die while protecting them",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Cleric
        public static IEnumerator Cleric()
        {
            if (Service.Game.Sim.simulation.roleDeckBuilder.Data.bannedRoles.Contains(Role.CLERIC) && Service.Game.Sim.simulation.observations.gameInfo.Data.playPhase == PlayPhase.FIRST_DAY)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("You Shouldn't Be Here", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "You Shouldn't Be Here",
                        Sprite = Utils.GetRoleSprite(Role.CLERIC),
                        Description = "Spawn as Cleric while it is banned from spawning",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            yield break;
        }

        // Coroner
        public static IEnumerator Coroner()
        {
            necessities.SetValue("Killing Townies", new List<Role> { Role.BODYGUARD, Role.CRUSADER, Role.TRAPPER, Role.VIGILANTE, Role.VETERAN, Role.TRICKSTER, Role.DEPUTY, Role.PROSECUTOR });
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(AutopsyCheck));
            yield break;
        }

        public static void AutopsyCheck(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.CORONER_PERFORMED_AUTOPSY && chatLog.role1.IsTownAligned())
                {
                    List<Role> theList = (List<Role>)necessities.GetValue("Killing Townies");
                    if (!theList.Contains(chatLog.role1))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Ghoulish Trickery", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Ghoulish Trickery",
                                Sprite = Utils.GetRoleSprite(Role.CORONER),
                                Description = "Find a Town role that cannot kill on your Autopsy",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        // Crusader
        public static IEnumerator Crusader()
        {
            if (Utils.IsBTOS2())
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(CrusaderCheckAchievements));
            yield break;
        }
        public static void CrusaderCheckAchievements(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1004)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("The Thing About Crusader", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "The Thing About Crusader",
                            Sprite = Utils.GetRoleSprite(Role.CRUSADER),
                            Description = "Block a player with an Unknown Obstacle",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                } else if (chatLog.messageId == (GameFeedbackMessage)1100)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Known Obstacle", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Known Obstacle",
                            Sprite = Utils.GetRoleSprite(Role.CRUSADER),
                            Description = "See an Unknown Obstacle",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Deputy
        public static IEnumerator Deputy()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(DeputyTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(DeputyRemoveTargetOnMiss));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(DeputyDetectTargetDeath));
            yield break;
        }

        public static void DeputyTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Deputy Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
            }
        }

        public static void DeputyRemoveTargetOnMiss(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.DEPUTY_SHOT_FAILED || (Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1101))
                    necessities.SetValue("Deputy Target", -1);
            }
        }

        public static void DeputyDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Deputy Target", -1) && chatLog.killRecord.playerRole.IsTownAligned() && chatLog.killRecord.killedByReasons.Contains(KilledByReason.DEPUTY_SHOT))
            {
                if (currentFaction == FactionType.TOWN && chatLog.killRecord.playerFaction == FactionType.TOWN)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Framed Fatality", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Framed Fatality",
                            Sprite = Utils.GetRoleSprite(Role.DEPUTY),
                            Description = "Shoot a Town member as a Town-aligned Deputy",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                } else if (Utils.IsBTOS2() && chatLog.killRecord.playerFaction == BToS2Factions.Jackal)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("False Allegiance", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "False Allegiance",
                            Sprite = Utils.GetRoleSprite(Role.DEPUTY),
                            Description = "Shoot a recruited Town member",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Investigator
        public static IEnumerator Investigator()
        {
            necessities.SetValue("Total Crimes", 0);
            necessities.SetValue("Murder", false);
            necessities.SetValue("Trespassing", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(InvestigatorCountCrimes));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(InvestigatorCheckAchievements));
            yield break;
        }

        public static void InvestigatorCountCrimes(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.INVESTIGATOR_FIND_CRIMINAL_EVIDENCE)
                {
                    necessities.SetValue("Murder", true);
                    int totalCrimes = (int)necessities.GetValue("Total Crimes", 0);
                    necessities.SetValue("Total Crimes", totalCrimes + 1);
                }
                else if (chatLog.messageId == GameFeedbackMessage.INVESTIGATOR_GUILTY_OF_TRESPASSING)
                {
                    necessities.SetValue("Trespassing", true);
                    int totalCrimes = (int)necessities.GetValue("Total Crimes", 0);
                    necessities.SetValue("Total Crimes", totalCrimes + 1);
                }
            }
        }

        public static void InvestigatorCheckAchievements()
        {
            if ((bool)necessities.GetValue("Murder", false) && !(bool)necessities.GetValue("Trespassing", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Friendly Fire", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Friendly Fire",
                        Sprite = Utils.GetRoleSprite(Role.INVESTIGATOR),
                        Description = "Find someone to be guilty of Murder, but not Trespassing",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            if ((int)necessities.GetValue("Total Crimes", 0) >= 12)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("A Crime a Dozen", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "A Crime a Dozen",
                        Sprite = Utils.GetRoleSprite(Role.INVESTIGATOR),
                        Description = "Find 12 crimes in one game",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            necessities.SetValue("Murder", false);
            necessities.SetValue("Trespassing", false);
        }
        // Jailor
        public static IEnumerator Jailor()
        {
            necessities.SetValue("Executed Faction List", new List<FactionType>());
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(JailorDetectTargetDeath));
            yield break;
        }
        public static void JailorDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerFaction != FactionType.TOWN && chatLog.killRecord.killedByReasons.Contains(KilledByReason.EXECUTED) && chatLog.killRecord.playerRole.IsTownAligned())
            {
                List<FactionType> executedFactions = (List<FactionType>)necessities.GetValue("Executed Faction List");
                if (!executedFactions.Contains(chatLog.killRecord.playerFaction))
                {
                    executedFactions.Add(chatLog.killRecord.playerFaction);
                    necessities.SetValue("Executed Faction List", executedFactions);
                }
                else
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Stunning Riot", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Stunning Riot",
                            Sprite = Utils.GetRoleSprite(Role.JAILOR),
                            Description = "Execute two evils from the same faction",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (Utils.IsBTOS2() && chatLog.killRecord.playerFaction == BToS2Factions.Jackal && currentFaction != BToS2Factions.Jackal)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Criminal Conspiracy", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Criminal Conspiracy",
                            Sprite = Utils.GetRoleSprite(Role.JAILOR),
                            Description = "Execute the Jackal or one of their recruits",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Lookout
        public static IEnumerator Lookout()
        {
            necessities.SetValue("Visits", 0);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(LookoutCountVisitors));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(LookoutResetVisitors));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(LookoutDetectTargetDeath));
            yield break;
        }
        public static void LookoutCountVisitors(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.LOOKOUT_PLAYER_VISITED_TARGET)
                {
                    int visits = (int)necessities.GetValue("Visits", 0);
                    necessities.SetValue("Visits", visits + 1);
                }
            }
        }
        public static void LookoutResetVisitors() => necessities.SetValue("Visits", 0);
        public static void LookoutDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (!chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && (int)necessities.GetValue("Visits", 0) == 1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Key Witness", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Key Witness",
                        Sprite = Utils.GetRoleSprite(Role.LOOKOUT),
                        Description = "See one visitor to your target as they die",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Mayor
        public static IEnumerator Mayor()
        {
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(MayorCheckVotes));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(MayorDetectAudit));
            yield break;
        }
        public static void MayorDetectAudit(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1013 && !Service.Game.Sim.simulation.observations.playerEffects[Service.Game.Sim.simulation.myPosition].Data.effects.Contains(EffectType.REVEALED_MAYOR))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Impeachment", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Impeachment",
                            Sprite = Utils.GetRoleSprite(Role.MAYOR),
                            Description = "Have the Auditor steal your Reveal charge",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void MayorCheckVotes()
        {
            if (Service.Game.Sim.simulation.observations.playerVotingObservations.GetElement(Service.Game.Sim.simulation.myPosition).Data.voteWeight >= 5)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("I Love Democracy", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "I Love Democracy",
                        Sprite = Utils.GetRoleSprite(Role.MAYOR),
                        Description = "Accumulate 5x voting power",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Monarch
        public static IEnumerator Monarch()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(MonarchDetectTargetDeath));
            yield break;
        }
        public static void MonarchDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (!chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Late Inauguration", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Late Inauguration",
                        Sprite = Utils.GetRoleSprite(Role.MONARCH),
                        Description = "Attempt to knight someone who dies the same night",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Prosecutor
        public static IEnumerator Prosecutor()
        {
            necessities.SetValue("Prosecute Target", -1);
            NewPostfix(typeof(ProsecutionCinematicPlayer), nameof(ProsecutionCinematicPlayer.Init), nameof(ProsecutorDetectProsecution));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(ProsecutorDetectTargetDeath));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(TrialVerdictDecoder), nameof(TrialVerdictDecoder.Encode), nameof(ProsecutorInno));
            yield break;
        }
        public static void ProsecutorDetectProsecution(ProsecutionCinematicPlayer __instance)
        {
            if (__instance.prosecutionCinematic.targetPosition != -1)
                necessities.SetValue("Prosecute Target", __instance.prosecutionCinematic.targetPosition);
        }
        public static void ProsecutorDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Prosecute Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.PROSECUTION))
            {
                if (chatLog.killRecord.playerFaction == FactionType.TOWN && currentFaction != FactionType.TOWN)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Organ Grinder", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Organ Grinder",
                            Sprite = Utils.GetRoleSprite(Role.PROSECUTOR),
                            Description = "Prosecute a town member as a non-town Prosecutor",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (Utils.IsBTOS2() && chatLog.killRecord.playerRole == BToS2Roles.Judge && currentFaction == FactionType.TOWN)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("I Rest My Case", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "I Rest My Case",
                            Sprite = Utils.GetRoleSprite(Role.PROSECUTOR),
                            Description = "Prosecute a Judge",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void ProsecutorInno(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TRIAL_VERDICT)
            {
                ChatLogTrialVerdictEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTrialVerdictEntry;
                if ((int)necessities.GetValue("Prosecute Target", -1) != -1 && chatLog.trialVerdict == TrialVerdict.INNOCENT)
                {
                    necessities.SetValue("Prosecute Target", -1);
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Reasonable Doubt", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Reasonable Doubt",
                            Sprite = Utils.GetRoleSprite(Role.PROSECUTOR),
                            Description = "Prosecute someone, then find them innocent",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Psychic
        public static IEnumerator Psychic()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(PsychicDetectTooGood));
            yield break;
        }
        public static void PsychicDetectTooGood(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.PSYCHIC_NO_EVIL_PLAYERS)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Yeah Right It Is", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Yeah Right It Is",
                            Sprite = Utils.GetRoleSprite(Role.PSYCHIC),
                            Description = "Have an Evil vision fail due to the Town being \"too good\"",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Retributionist
        public static IEnumerator Retributionist()
        {
            necessities.SetValue("Using Role", Role.NONE);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(RaiseTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(RaiseRemoveTargetIfCantUse));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(DeputyRemoveTargetOnMiss));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(RaiseDeputyDetectTargetDeath));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(RetributionistVeteranDetectShotVisitor));
            yield break;
        }
        public static void RaiseTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.NightAbility2)
                {
                    necessities.SetValue("Using Role", chatLog.targetRoleId);
                    necessities.SetValue("Current Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber2);
                }
            }
        }
        public static void RetributionistVeteranDetectShotVisitor(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.VETERAN_SHOT_VISITOR)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Postmortem Paranoia", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Postmortem Paranoia",
                            Sprite = Utils.GetRoleSprite(Role.RETRIBUTIONIST),
                            Description = "Resurrect a Veteran and kill someone",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void RaiseRemoveTargetIfCantUse(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.REANIMATE_ROLE_CANT_BE_USED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || (Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1100))
                {
                    necessities.SetValue("Using Role", Role.NONE);
                    necessities.SetValue("Current Target", -1);
                }
            }
        }

        public static void RaiseDeputyDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.DEPUTY_SHOT))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Death Noon", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Death Noon",
                        Sprite = Utils.GetRoleSprite(Role.RETRIBUTIONIST),
                        Description = "Resurrect a Deputy and kill someone",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Seer
        public static IEnumerator Seer()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(SeerTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(SeerRemoveTargetIfImpededPatch));
            NewPostfix(typeof(MayorRevealCinematicPlayer), nameof(MayorRevealCinematicPlayer.Init), nameof(SeerDetectMayorReveal));
            NewPostfix(typeof(TribunalCinematicPlayer), nameof(TribunalCinematicPlayer.Init), nameof(SeerDetectMarshalReveal));
            NewPostfix(typeof(ProsecutionCinematicPlayer), nameof(ProsecutionCinematicPlayer.Cleanup), nameof(SeerDetectProsecutorReveal));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(SeerResetCompare));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(SeerDetectTargetDeath));
            yield break;
        }

        public static void SeerTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.NightAbility)
                    necessities.SetValue("Intuit", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
                else if (chatLog.menuChoiceType == MenuChoiceType.NightAbility2)
                    necessities.SetValue("Gaze", chatLog.bIsCancel ? -1 : chatLog.playerNumber2);
            }
        }

        public static void SeerRemoveTargetIfImpededPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED || chatLog.messageId == GameFeedbackMessage.JAILED_TARGET || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || chatLog.messageId == GameFeedbackMessage.CANNOT_USE_ABILITY_WHILE_INSANE || chatLog.messageId == GameFeedbackMessage.WITCH_CONTROLLED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY || (Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1100))
                {
                    necessities.SetValue("Intuit", -1);
                    necessities.SetValue("Gaze", -1);
                }
            }
        }
        public static void SeerDetectMayorReveal(MayorRevealCinematicPlayer __instance)
        {
            if ((__instance.roleRevealCinematic.playerPosition == (int)necessities.GetValue("Intuit", -1) || __instance.roleRevealCinematic.playerPosition == (int)necessities.GetValue("Gaze", -1)) && (int)necessities.GetValue("Intuit", -1) != -1 && (int)necessities.GetValue("Gaze", -1) != -1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Simple", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Simple",
                        Sprite = Utils.GetRoleSprite(Role.SEER),
                        Description = "Witness a player publicly reveal as Town Power the day after you compare them",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void SeerDetectMarshalReveal(TribunalCinematicPlayer __instance)
        {
            if ((__instance.roleRevealCinematic.playerPosition == (int)necessities.GetValue("Intuit", -1) || __instance.roleRevealCinematic.playerPosition == (int)necessities.GetValue("Gaze", -1)) && (int)necessities.GetValue("Intuit", -1) != -1 && (int)necessities.GetValue("Gaze", -1) != -1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Simple", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Simple",
                        Sprite = Utils.GetRoleSprite(Role.SEER),
                        Description = "Witness a player publicly reveal as Town Power the day after you compare them",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void SeerDetectProsecutorReveal(ProsecutionCinematicPlayer __instance)
        {
            if ((__instance.prosecutionCinematic.prosecutorPostion == (int)necessities.GetValue("Intuit", -1) || __instance.prosecutionCinematic.prosecutorPostion == (int)necessities.GetValue("Gaze", -1)) && (int)necessities.GetValue("Intuit", -1) != -1 && (int)necessities.GetValue("Gaze", -1) != -1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Simple", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Simple",
                        Sprite = Utils.GetRoleSprite(Role.SEER),
                        Description = "Witness a player publicly reveal as Town Power the day after you compare them",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void SeerDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && (chatLog.killRecord.killedByReasons.Contains(KilledByReason.LYNCHED) || chatLog.killRecord.killedByReasons.Contains(KilledByReason.PROSECUTION)) && chatLog.killRecord.playerRole != BToS2Roles.Jackal && chatLog.killRecord.playerFaction == BToS2Factions.Jackal && ((int)chatLog.killRecord.playerId == (int)necessities.GetValue("Intuit", -1) || (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Gaze", -1)))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Gray Scale", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Gray Scale",
                        Sprite = Utils.GetRoleSprite(Role.SEER),
                        Description = "Hang a recruit the day after you compare them",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void SeerResetCompare()
        {
            necessities.SetValue("Intuit", -1);
            necessities.SetValue("Gaze", -1);
        }

        // Sheriff
        public static IEnumerator Sheriff()
        {
            necessities.SetValue("Suspicious", false);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(SheriffDetectSuspicious));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(SheriffResetSuspicious));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(SheriffDeputyDetectTargetDeath));
            yield break;
        }

        public static void SheriffDetectSuspicious(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.SHERIFF_SUSPICIOUS)
                {
                    necessities.SetValue("Suspicious", true);
                }
            }
        }

        public static void SheriffResetSuspicious() => necessities.SetValue("Suspicious", false);

        public static void SheriffDeputyDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && (bool)necessities.GetValue("Suspicious") && chatLog.killRecord.killedByReasons.Contains(KilledByReason.DEPUTY_SHOT))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Keeping the Peace", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Keeping the Peace",
                        Sprite = Utils.GetRoleSprite(Role.SHERIFF),
                        Description = "Witness a player being shot by a Deputy the day after you find them suspicious",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Spy
        public static IEnumerator Spy()
        {
            necessities.SetValue("Bug Index", -1);
            necessities.SetValue("Bug Results", new List<List<Role>>());
            necessities.SetValue("Saw Town", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(SpyBugCounting));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(SpyAchievementCheck));
            yield break;
        }

        public static void SpyBugCounting(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.SPY_BUG_WAS_TRIGGERED)
                {
                    int bugIndex = (int)necessities.GetValue("Bug Index");
                    List<List<Role>> bugResults = (List<List<Role>>)necessities.GetValue("Bug Results");
                    bugResults.Add(new List<Role>());
                    necessities.SetValue("Bug Index", bugIndex + 1);
                    necessities.SetValue("Bug Results", bugResults);
                }
                else if ((int)necessities.GetValue("Bug Index", -1) > -1 && (chatLog.messageId == GameFeedbackMessage.INVESTIGATOR_PERCEPTION_SAW_VISITOR_WITH_ROLE || (Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1081)))
                {
                    if (!chatLog.role1.IsTownAligned())
                    {
                        int bugIndex = (int)necessities.GetValue("Bug Index");
                        List<List<Role>> bugResults = (List<List<Role>>)necessities.GetValue("Bug Results");
                        bugResults[bugIndex].Add(chatLog.role1);
                        necessities.SetValue("Bug Results", bugResults);
                    }
                    else
                    {
                        necessities.SetValue("Saw Town", true);
                    }
                }
            }
        }
        public static void SpyAchievementCheck()
        {
            List<List<Role>> bugResults = (List<List<Role>>)necessities.GetValue("Bug Results");
            bool threeEvilsVisited = false;
            foreach (List<Role> roles in bugResults)
                if (!threeEvilsVisited && roles.Count >= 3)
                    threeEvilsVisited = true;
            if (threeEvilsVisited)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("High-Profile Target", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "High-Profile Target",
                        Sprite = Utils.GetRoleSprite(Role.SPY),
                        Description = "See 3 or more evil roles visit one player",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            if ((bool)necessities.GetValue("Saw Town", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Green Herring", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Green Herring",
                        Sprite = Utils.GetRoleSprite(Role.SPY),
                        Description = "See a Town role visit a bug",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            necessities.SetValue("Bug Index", -1);
            necessities.SetValue("Bug Results", new List<List<Role>>());
            necessities.SetValue("Saw Town", false);
        }

        // Tavern Keeper
        public static IEnumerator TavernKeeper()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(TavernKeeperDetectRoleblock));
            yield break;
        }
        public static void TavernKeeperDetectRoleblock(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED_BUT_IMMUNE)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Too Drunk to Get Drunk", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Too Drunk to Get Drunk",
                            Sprite = Utils.GetRoleSprite(Role.TAVERNKEEPER),
                            Description = "Have someone attempt to roleblock you",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Tracker
        public static IEnumerator Tracker()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(TrackerDetectVisitSelf));
            yield break;
        }
        public static void TrackerDetectVisitSelf(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TRACKER_TARGET_VISITED && chatLog.playerNumber1 == (int)necessities.GetValue("Current Target", -1))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Narcissist", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Narcissist",
                            Sprite = Utils.GetRoleSprite(Role.TRACKER),
                            Description = "See your target visit themselves",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Trapper
        public static IEnumerator Trapper()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(TrapperDetectJinx));
            yield break;
        }
        public static void TrapperDetectJinx(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TRAP_TRIGGERED_BY_VISITOR_WITH_ROLE && chatLog.role1 == Role.JINX)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Stray Cat", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Stray Cat",
                            Sprite = Utils.GetRoleSprite(Role.TRAPPER),
                            Description = "See Jinx visit your Trap",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Trickster
        public static IEnumerator Trickster()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(TricksterDetectAbsorbedVigilante));
            yield break;
        }
        public static void TricksterDetectAbsorbedVigilante(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if ((chatLog.messageId == GameFeedbackMessage.YOU_HAVE_STOLEN_THE_ATTACK_OF_X || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1065) && chatLog.role1 == Role.VIGILANTE)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Hippity Hoppity", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Hippity Hoppity",
                            Sprite = Utils.GetRoleSprite(Role.TRICKSTER),
                            Description = "Absorb a Vigilante’s Attack",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Veteran
        public static IEnumerator Veteran()
        {
            necessities.SetValue("Shot Visitor", false);
            NewPostfix(typeof(ChatDecoder), nameof(ChatDecoder.Encode), nameof(VeteranCheckIfCalledTPLO));
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(VeteranAlertPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(VeteranDetectShotVisitor));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(VeteranCheckAchievement));
            yield break;
        }

        public static void VeteranCheckIfCalledTPLO(ChatLogMessage chatLogMessage)
        {
            
            if (Service.Game.Sim.simulation.observations.gameInfo.Data.playPhase != PlayPhase.FIRST_DISCUSSION)
                return;
            ChatLogChatMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogChatMessageEntry;
            if (chatLog.speakerId == Service.Game.Sim.simulation.myPosition && (chatLog.message.ToLower().Contains("tplo") || chatLog.message.ToLower().Contains("tp/lo")))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("The Classic", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "The Classic",
                        Sprite = Utils.GetRoleSprite(Role.VETERAN),
                        Description = "Let the demons win",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        public static void VeteranAlertPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Current Target", chatLog.bIsCancel ? -1 : 0);
            }
        }

        public static void VeteranDetectShotVisitor(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.VETERAN_SHOT_VISITOR)
                    necessities.SetValue("Shot Visitor", true);
            }
        }

        public static void VeteranCheckAchievement()
        {
            if (!(bool)necessities.GetValue("Shot Visitor", false) && (int)necessities.GetValue("Current Target", -1) != -1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Swing and a Miss", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Swing and a Miss",
                        Sprite = Utils.GetRoleSprite(Role.VETERAN),
                        Description = "Have no visitors to your Alert",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            necessities.SetValue("Shot Visitor", false);
            necessities.SetValue("Current Target", -1);
        }

        // Vigilante
        public static IEnumerator Vigilante()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(VigilanteDetectDefense));
            yield break;
        }
        public static void VigilanteDetectDefense(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.DEFENSE_STRONGER_THAN_ATTACK)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Bulletproof Flesh", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Bulletproof Flesh",
                            Sprite = Utils.GetRoleSprite(Role.VIGILANTE),
                            Description = "Shoot someone with defense",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Conjurer
        public static IEnumerator Conjurer()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralNecronomiconTargetingPatch));
            NewPostfix(typeof(FactionTargetSelectionDecoder), nameof(FactionTargetSelectionDecoder.Encode), nameof(GeneralNecronomiconTargetingPatch));
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(ConjurerTargetingPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(ConjurerDetectTargetDeath));
            yield break;
        }
        public static void ConjurerTargetingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                {
                    necessities.SetValue("Conjurer Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
                    if (chatLog.playerNumber1 == (int)necessities.GetValue("Necronomicon Target", 16))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("You Won’t Get Away", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "You Won’t Get Away",
                                Sprite = Utils.GetRoleSprite(Role.CONJURER),
                                Description = "Conjure someone the Coven attempted to kill last night",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void ConjurerDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (Utils.IsBTOS2() && chatLog.killRecord.isDay && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Conjurer Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.CONJURER_ATTACKED) && chatLog.killRecord.playerRole == BToS2Roles.Jackal && currentFaction != BToS2Factions.Jackal)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("He Deserved It!", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "He Deserved It!",
                        Sprite = Utils.GetRoleSprite(Role.CONJURER),
                        Description = "Conjure the Jackal",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }

        // Coven Leader
        public static IEnumerator CovenLeader()
        {
            necessities.SetValue("Has Retrained", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(CovenLeaderDetectRetrain));
            yield break;
        }
        public static void CovenLeaderDetectRetrain(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ROLE_RETRAIN_ACCEPTED)
                {
                    if (chatLog.role1 == Role.VOODOOMASTER)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Metamancer", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Metamancer",
                                Sprite = Utils.GetRoleSprite(Role.COVENLEADER),
                                Description = "Retrain someone into a Voodoo Master",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                    if (chatLog.role1 == Role.CONJURER && chatLog.playerNumber1 == Service.Game.Sim.simulation.myPosition && (bool)necessities.GetValue("Has Retrained", false))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Rock Solid", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Rock Solid",
                                Sprite = Utils.GetRoleSprite(Role.COVENLEADER),
                                Description = "Using your last retrain charge, retrain yourself into Conjurer",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                    necessities.SetValue("Has Retrained", true);
                }
            }
        }
        // Dreamweaver
        public static IEnumerator Dreamweaver()
        {
            necessities.SetValue("Current Target", -1);
            necessities.SetValue("Dreamweaved N1", false);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(DreamweaverCheckAchievement));
            yield break;
        }
        public static void DreamweaverCheckAchievement(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.DISCUSSION)
            {
                if ((int)Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber == 2 && (int)necessities.GetValue("Current Target", -1) != -1)
                    necessities.SetValue("Dreamweaved N1", true);
                else if ((int)Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber > 2 && (int)necessities.GetValue("Current Target", -1) != -1 && !(bool)necessities.GetValue("Dreamweaved N1", false))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("No One Will Believe You", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "No One Will Believe You",
                            Sprite = Utils.GetRoleSprite(Role.DREAMWEAVER),
                            Description = "Intentionally wait until Night 2+ to dreamweave someone",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Enchanter
        public static IEnumerator Enchanter()
        {
            necessities.SetValue("Current Target", -1);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(EnchanterAlteratingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(EnchanterDetectTargetDeath));
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(EnchanterDetectTargetNotDeath));
            yield break;
        }
        public static void EnchanterAlteratingPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                {
                    necessities.SetValue("Current Target", chatLog.bIsCancel ? -1 : chatLog.playerNumber1);
                }
            }
        }
        public static void EnchanterDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target"))
            {
                necessities.SetValue("Current Target", -1);
                if (chatLog.killRecord.playerRole == Role.JESTER)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Clowner", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Clowner",
                            Sprite = Utils.GetRoleSprite(Role.ENCHANTER),
                            Description = "Alterate someone as a Jester",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (Service.Game.Sim.simulation.roleDeckBuilder.Data.bannedRoles.Contains(chatLog.killRecord.playerRole))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Forbidden Enchantment", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Forbidden Enchantment",
                            Sprite = Utils.GetRoleSprite(Role.ENCHANTER),
                            Description = "Alterate someone as a role that cannot spawn",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (chatLog.killRecord.playerRole == chatLog.killRecord.hiddenPlayerRole)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Let Me Cook?", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Let Me Cook?",
                            Sprite = Utils.GetRoleSprite(Role.ENCHANTER),
                            Description = "Alterate someone as their real role",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void EnchanterDetectTargetNotDeath(GameInfo gameInfo)
        {
            if (gameInfo.playPhase == PlayPhase.DISCUSSION && (int)necessities.GetValue("Current Target", -1) != -1)
            {
                necessities.SetValue("Current Target", -1);
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Torn Paper", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Torn Paper",
                        Sprite = Utils.GetRoleSprite(Role.ENCHANTER),
                        Description = "Have your Alterate target survive",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Hex Master
        public static IEnumerator HexMaster()
        {
            if (!Utils.IsBTOS2())
            {
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralDetectApocalypse));
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(GeneralDetectApocalypseDeath));
            }
            NewPostfix(typeof(HexBombCinematicPlayer), nameof(HexBombCinematicPlayer.Cleanup), nameof(HexMasterDetectBomb));
            yield break;
        }

        public static void HexMasterDetectBomb()
        {
            if (Utils.ApocCheck())
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Skill Issue", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Skill Issue",
                        Sprite = Utils.GetRoleSprite(Role.HEXMASTER),
                        Description = "Perform a Hex Bomb while a Horseman of the Apocalypse is alive",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Illusionist
        public static IEnumerator Illusionist()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(IllusionistDetectTargetDeath));
            yield break;
        }
        public static void IllusionistDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target"))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Brawn over Brains", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Brawn over Brains",
                        Sprite = Utils.GetRoleSprite(Role.ILLUSIONIST),
                        Description = "Have your illusioned target be killed that night",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Jinx
        public static IEnumerator Jinx()
        {
            necessities.SetValue("Jinxed", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(JinxDetectAttack));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(JinxDetectTargetDeath));
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(JinxDetectTargetNotDeath));
            yield break;
        }
        public static void JinxDetectAttack(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.JINX_YOU_ATTACKED_PLAYER_WHO_VISITED_YOUR_TARGET)
                    necessities.SetValue("Jinxed", true);
            }
        }
        public static void JinxDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.killedByReasons.Contains(KilledByReason.JINX_ATTACKED))
                necessities.SetValue("Jinxed", false);
        }
        public static void JinxDetectTargetNotDeath(GameInfo gameInfo)
        {
            if (gameInfo.playPhase == PlayPhase.DISCUSSION && (bool)necessities.GetValue("Jinxed", false))
            {
                necessities.SetValue("Jinxed", false);
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Iron Hearted", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Iron Hearted",
                        Sprite = Utils.GetRoleSprite(Role.JINX),
                        Description = "Jinx someone who survives",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Medusa
        public static IEnumerator Medusa()
        {
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(MedusaDetectTargetDeath));
            yield break;
        }
        public static void MedusaDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerRole == Role.STONED && chatLog.killRecord.hiddenPlayerRole.IsCovenAligned())
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Nobody's Gonna Know", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Nobody's Gonna Know",
                        Sprite = Utils.GetRoleSprite(Role.MEDUSA),
                        Description = "Successfully stone a Coven member",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Necromancer
        public static IEnumerator Necromancer()
        {
            necessities.SetValue("Using Role", Role.NONE);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(RaiseTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(RaiseRemoveTargetIfCantUse));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NecromancerDetectFamine));
            yield break;
        }
        public static void NecromancerDetectFamine(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.DIED_OF_FAMINE && (Role)necessities.GetValue("Using Role", Role.NONE) == Role.FAMINE)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Skin and Bones", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Skin and Bones",
                            Sprite = Utils.GetRoleSprite(Role.NECROMANCER),
                            Description = "Resurrect Famine and starve to death",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Poisoner
        public static IEnumerator Poisoner()
        {
            necessities.SetValue("Smog", false);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(PoisonerSmogPatch));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(PoisonerDetectSmogWithBook));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(PoisonerDetectTargetDeath));
            yield break;
        }
        public static void PoisonerSmogPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Smog", !chatLog.bIsCancel);
            }
        }
        public static void PoisonerDetectSmogWithBook()
        {
            if (Service.Game.Sim.simulation.observations.roleCardObservation.Data.powerUp == POWER_UP_TYPE.NECRONOMICON && (bool)necessities.GetValue("Smog", false) && (int)necessities.GetValue("Current Target", -1) != -1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("I Don't Read Books", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "I Don't Read Books",
                        Sprite = Utils.GetRoleSprite(Role.POISONER),
                        Description = "Smog with the Necronomicon",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void PoisonerDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && !(bool)necessities.GetValue("Smog", false) && (chatLog.killRecord.hiddenPlayerRole == Role.VETERAN || chatLog.killRecord.playerRole == Role.VETERAN && (chatLog.killRecord.hiddenPlayerRole == Role.NONE || chatLog.killRecord.hiddenPlayerRole == Role.HIDDEN)))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Numbed Nerves", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Numbed Nerves",
                        Sprite = Utils.GetRoleSprite(Role.POISONER),
                        Description = "Roleblock a Veteran as they die",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Potion Master
        public static IEnumerator PotionMaster()
        {
            if (Utils.IsBTOS2())
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(PotionMasterDetectPariah));
            yield break;
        }
        public static void PotionMasterDetectPariah(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1020 || chatLog.messageId == (GameFeedbackMessage)1030 || chatLog.messageId == (GameFeedbackMessage)1012)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Dubious Alchemy", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Dubious Alchemy",
                            Sprite = Utils.GetRoleSprite(Role.POTIONMASTER),
                            Description = "Reveal a Neutral Pariah",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Ritualist
        public static IEnumerator Ritualist()
        {
            necessities.SetValue("Ritual", false);
            necessities.SetValue("Revealed TPow", new List<int>());
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(RitualistDetectRitual));
            NewPostfix(typeof(MayorRevealCinematicPlayer), nameof(MayorRevealCinematicPlayer.Init), nameof(RitualistDetectMayorReveal));
            NewPostfix(typeof(TribunalCinematicPlayer), nameof(TribunalCinematicPlayer.Init), nameof(RitualistDetectMarshalReveal));
            NewPostfix(typeof(ProsecutionCinematicPlayer), nameof(ProsecutionCinematicPlayer.Cleanup), nameof(RitualistDetectProsecutorReveal));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(RitualistDetectTargetDeath));
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(RitualistDetectTargetNotDeath));
            yield break;
        }
        public static void RitualistDetectRitual(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.RITUALIST_SUCCESSFUL_ROLE_GUESS)
                    necessities.SetValue("Ritual", true);
            }
        }
        public static void RitualistDetectMayorReveal(MayorRevealCinematicPlayer __instance)
        {
            if (__instance.roleRevealCinematic.playerPosition != -1)
            {
                List<int> revealedTPow = (List<int>)necessities.GetValue("Revealed TPow");
                revealedTPow.Add(__instance.roleRevealCinematic.playerPosition);
                necessities.SetValue("Revealed TPow", revealedTPow);
            }
        }
        public static void RitualistDetectMarshalReveal(TribunalCinematicPlayer __instance)
        {
            if (__instance.roleRevealCinematic.playerPosition != -1)
            {
                List<int> revealedTPow = (List<int>)necessities.GetValue("Revealed TPow");
                revealedTPow.Add(__instance.roleRevealCinematic.playerPosition);
                necessities.SetValue("Revealed TPow", revealedTPow);
            }
        }
        public static void RitualistDetectProsecutorReveal(ProsecutionCinematicPlayer __instance)
        {
            if (__instance.prosecutionCinematic.prosecutorPostion != -1)
            {
                List<int> revealedTPow = (List<int>)necessities.GetValue("Revealed TPow");
                revealedTPow.Add(__instance.prosecutionCinematic.prosecutorPostion);
                necessities.SetValue("Revealed TPow", revealedTPow);
            }
        }
        public static void RitualistDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.killedByReasons.Contains(KilledByReason.RITUALIST_ATTACKED))
            {
                necessities.SetValue("Ritual", false);
                bool roleIsTPow = chatLog.killRecord.hiddenPlayerRole.GetSubAlignment() == SubAlignment.POWER && chatLog.killRecord.hiddenPlayerRole.IsTownAligned() && chatLog.killRecord.hiddenPlayerRole != Role.JAILOR && chatLog.killRecord.hiddenPlayerRole != Role.MONARCH || chatLog.killRecord.playerRole.GetSubAlignment() == SubAlignment.POWER && chatLog.killRecord.playerRole.IsTownAligned() && chatLog.killRecord.playerRole != Role.JAILOR && chatLog.killRecord.playerRole != Role.MONARCH && (chatLog.killRecord.hiddenPlayerRole == Role.NONE || chatLog.killRecord.hiddenPlayerRole == Role.HIDDEN);
                List<int> revealedTPow = (List<int>)necessities.GetValue("Revealed TPow");
                if (roleIsTPow && !revealedTPow.Contains(chatLog.killRecord.playerId))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("The Price Is Right", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "The Price Is Right",
                            Sprite = Utils.GetRoleSprite(Role.RITUALIST),
                            Description = "Perform a successful Blood Ritual on an unrevealed Town Power",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void RitualistDetectTargetNotDeath(GameInfo gameInfo)
        {
            if (gameInfo.playPhase == PlayPhase.DISCUSSION && (bool)necessities.GetValue("Ritual", false))
            {
                necessities.SetValue("Ritual", false);
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Not Quite Unstoppable", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Not Quite Unstoppable",
                        Sprite = Utils.GetRoleSprite(Role.RITUALIST),
                        Description = "Perform a successful Blood Ritual on someone who survives",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Voodoo Master
        public static IEnumerator VoodooMaster()
        {
            necessities.SetValue("Admirer", -1);
            if (!Utils.IsBTOS2())
            {
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(VoodooMasterProposalMessagePatch));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(VoodooMasterDetectSilencedAdmirer));
            }
            NewPostfix(typeof(ExecutionerLeavesFeatures), nameof(ExecutionerLeavesFeatures.Init), nameof(VoodooMasterDetectSilencedExecutioner));
            yield break;
        }
        public static void VoodooMasterProposalMessagePatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.YOU_ACCEPTED_ADMIRER_PROPOSAL_ROLE_REVEAL)
                    necessities.SetValue("Admirer", chatLog.playerNumber1);
            }
        }
        public static void VoodooMasterDetectSilencedAdmirer()
        {
            if ((int)necessities.GetValue("Admirer", -1) != -1 && Service.Game.Sim.simulation.observations.playerEffects[(int)necessities.GetValue("Admirer", -1)].Data.effects.Contains(EffectType.VOODOOED))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Inaudible Infatuation", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Inaudible Infatuation",
                        Sprite = Utils.GetRoleSprite(Role.VOODOOMASTER),
                        Description = "Silence your Admirer",
                        Vanilla = true,
                        BToS2 = false
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void VoodooMasterDetectSilencedExecutioner(TrialExecutionerData trialExecutionerData)
        {
            if (Service.Game.Sim.simulation.observations.playerEffects[trialExecutionerData.executionEntries[0].executionerId].Data.effects.Contains(EffectType.VOODOOED))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("We'll Take It From Here", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "We'll Take It From Here",
                        Sprite = Utils.GetRoleSprite(Role.VOODOOMASTER),
                        Description = "Silence the Executioner, then hang their target",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Wildling
        public static IEnumerator Wildling()
        {
            necessities.SetValue("Whispers", 0);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(PlayerWhisperDecoder), nameof(PlayerWhisperDecoder.Encode), nameof(WildlingCountWhispers));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(WildlingDayHandler));
            yield break;
        }
        public static void WildlingCountWhispers(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.WHISPER)
            {
                ChatLogWhisperMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhisperMessageEntry;
                int myPosition = Service.Game.Sim.simulation.myPosition;
                if (chatLog.speakerId != myPosition && chatLog.targetId != myPosition)
                {
                    int whispers = (int)necessities.GetValue("Whispers", 0);
                    necessities.SetValue("Whispers", whispers + 1);
                    if (whispers >= 19)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Information Overload", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Information Overload",
                                Sprite = Utils.GetRoleSprite(Role.WILDLING),
                                Description = "Overhear 20 whispers in one day",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void WildlingDayHandler() => necessities.SetValue("Whispers", 0);
        // Witch
        public static IEnumerator Witch()
        {
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(SeerTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(SeerRemoveTargetIfImpededPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(WitchControlHandler));
            yield break;
        }
        public static void WitchControlHandler(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TARGET_IS_ARSONIST && (int)necessities.GetValue("Intuit", -1) == (int)necessities.GetValue("Gaze", -1) && (int)necessities.GetValue("Intuit", -1) != -1)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Taste of Your Own Gasoline", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Taste of Your Own Gasoline",
                            Sprite = Utils.GetRoleSprite(Role.WITCH),
                            Description = "Control an Arsonist into dousing themselves",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                else if (chatLog.messageId.IsBetweenInclusive(GameFeedbackMessage.TARGET_IS_CONJURER, GameFeedbackMessage.TARGET_IS_WITCH) || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1027)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Magician's Wrath", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Magician's Wrath",
                            Sprite = Utils.GetRoleSprite(Role.WITCH),
                            Description = "Control a Coven role",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Arsonist
        public static IEnumerator Arsonist()
        {
            necessities.SetValue("Ignite", false);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(ArsonistIgnitePatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(ArsonistCheckAchievements));
            yield break;
        }
        public static void ArsonistIgnitePatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Ignite", !chatLog.bIsCancel);
                else if (chatLog.menuChoiceType == MenuChoiceType.NightAbility)
                    necessities.SetValue("Ignite", false);
            }
        }
        public static void ArsonistCheckAchievements(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if ((bool)necessities.GetValue("Ignite", false) && chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Inflammable Alcohol", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Inflammable Alcohol",
                            Sprite = Utils.GetRoleSprite(Role.ARSONIST),
                            Description = "Get roleblocked while igniting",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                else if (chatLog.messageId == GameFeedbackMessage.ATTACKED_BY_ARSONIST)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Intentional Game Design", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Intentional Game Design",
                            Sprite = Utils.GetRoleSprite(Role.ARSONIST),
                            Description = "Get ignited",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }

            }
        }
        // Baker
        public static IEnumerator Baker()
        {
            if (Utils.IsBTOS2())
            {
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(BakerDetectPariah));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(BakerDetectTTBread));
            }
            yield break;
        }
        public static void BakerDetectPariah(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1020 || chatLog.messageId == (GameFeedbackMessage)1030 || chatLog.messageId == (GameFeedbackMessage)1012)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Free Samples!", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Free Samples!",
                            Sprite = Utils.GetRoleSprite(Role.BAKER),
                            Description = "Reveal a Neutral Pariah",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void BakerDetectTTBread()
        {
            if ((int)necessities.GetValue("Town Traitor", -1) != -1 && Service.Game.Sim.simulation.observations.playerEffects[(int)necessities.GetValue("Town Traitor", -1)].Data.effects.Contains(EffectType.BREAD) || (int)necessities.GetValue("Town Traitor 2", -1) != -1 && Service.Game.Sim.simulation.observations.playerEffects[(int)necessities.GetValue("Town Traitor 2", -1)].Data.effects.Contains(EffectType.BREAD))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Traitor's Dozen", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Traitor's Dozen",
                        Sprite = Utils.GetRoleSprite(Role.BAKER),
                        Description = "Give Bread to the Town Traitor",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Berserker
        public static IEnumerator Berserker()
        {
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(BerserkerDetectMonarchDeath));
            yield break;
        }
        public static void BerserkerDetectMonarchDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerRole == Role.MONARCH && chatLog.killRecord.killedByReasons.Contains(KilledByReason.BERSERKER_ATTACKED))
            {
                bool knight = false;
                foreach (PlayerEffectsObservation observation in Service.Game.Sim.simulation.observations.playerEffects)
                    if (!knight && observation.Data.effects.Contains(EffectType.KNIGHTED))
                        knight = true;
                if (knight)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Kingslayer", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Kingslayer",
                            Sprite = Utils.GetRoleSprite(Role.BERSERKER),
                            Description = "Kill the Monarch with an alive knight",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Doomsayer
        public static IEnumerator Doomsayer()
        {
            necessities.GetValue("NELeave", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NeutralEvilDetectLeave));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(NeutralEvilFactionWinPatch));
            if (!Utils.IsBTOS2())
            {
                necessities.SetValue("Dooms", new List<int>());
                necessities.SetValue("Admirer", -1);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(VoodooMasterProposalMessagePatch));
            } else
            {
                necessities.SetValue("Dooms", new List<Role>());
                necessities.SetValue("Lives", new List<bool>());
            }
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(DoomsayerTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(DoomsayerDetectDooms));
            yield break;
        }
        public static void DoomsayerTargetingPatch(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                {
                    if (!Utils.IsBTOS2())
                    {
                        List<int> dooms = (List<int>)necessities.GetValue("Dooms");
                        if (chatLog.bIsCancel)
                        {
                            dooms.Clear();
                        }
                        else
                        {
                            if (dooms.Count >= 3)
                                dooms.Clear();
                            dooms.Add(chatLog.playerNumber1);
                        }
                        necessities.SetValue("Dooms", dooms);
                    } else
                    {
                        List<Role> dooms = (List<Role>)necessities.GetValue("Dooms");
                        if (chatLog.bIsCancel)
                        {
                            dooms.Clear();
                        }
                        else
                        {
                            if (dooms.Count >= 3)
                                dooms.Clear();
                            dooms.Add(chatLog.targetRoleId);
                        }
                        necessities.SetValue("Dooms", dooms);
                    }
                }
            }
        }
        public static void DoomsayerDetectDooms(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.DOOMSAYER_GUESSED_CORRECTLY)
                {
                    if (!Utils.IsBTOS2())
                    {
                        List<int> dooms = (List<int>)necessities.GetValue("Dooms");
                        if (dooms.Contains((int)necessities.GetValue("Admirer", -1)))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("To the Moon and Back", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "To the Moon and Back",
                                    Sprite = Utils.GetRoleSprite(Role.DOOMSAYER),
                                    Description = "Doom your Admirer",
                                    Vanilla = true,
                                    BToS2 = false
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        }
                    } else
                    {
                        List<Role> dooms = (List<Role>)necessities.GetValue("Dooms");
                        if (dooms.Contains(BToS2Roles.Judge) || dooms.Contains(BToS2Roles.Auditor) || dooms.Contains(BToS2Roles.Starspawn))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("Evil Amongst Evils", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "Evil Amongst Evils",
                                    Sprite = Utils.GetRoleSprite(Role.DOOMSAYER),
                                    Description = "Doom a Neutral Pariah",
                                    Vanilla = false,
                                    BToS2 = true
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        }
                    }
                } else if (Utils.IsBTOS2() && (chatLog.messageId == (GameFeedbackMessage)1008 || chatLog.messageId == (GameFeedbackMessage)1009))
                {
                    List<bool> lives = (List<bool>)necessities.GetValue("Lives");
                    lives.Add(chatLog.messageId == (GameFeedbackMessage)1008);
                    if (lives.Count >= 3)
                    {
                        if (!lives.Contains(false))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("Dark Dilemma", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "Dark Dilemma",
                                    Sprite = Utils.GetRoleSprite(Role.DOOMSAYER),
                                    Description = "Witness all three doomed targets choose to live - and die anyway",
                                    Vanilla = false,
                                    BToS2 = true
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        } else if (!lives.Contains(true))
                        {
                            RechievementData rechievement;
                            if (!RechievementData.allRechievements.TryGetValue("Sorrow Souls", out rechievement))
                            {
                                rechievement = new RechievementData
                                {
                                    Name = "Sorrow Souls",
                                    Sprite = Utils.GetRoleSprite(Role.DOOMSAYER),
                                    Description = "Witness all three doomed targets choose to die",
                                    Vanilla = false,
                                    BToS2 = true
                                };
                                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                            }
                            rechievement.ShowRechievement();
                        }
                    }
                }
            }
        }
        // Executioner
        public static IEnumerator Executioner()
        {
            necessities.GetValue("NELeave", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NeutralEvilDetectLeave));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(NeutralEvilFactionWinPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(ExecutionerDetectHorribleGameDesign));
            if (Utils.IsBTOS2())
                NewPostfix(typeof(ExecutionerLeavesFeatures), nameof(ExecutionerLeavesFeatures.Init), nameof(ExecutionerDetectCourt));
            yield break;
        }
        public static void ExecutionerDetectHorribleGameDesign(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.EXE_YOU_ARE_A_JESTER)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Horrible Game Design", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Horrible Game Design",
                            Sprite = Utils.GetRoleSprite(Role.EXECUTIONER),
                            Description = "Turn into a Jester",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void ExecutionerDetectCourt(TrialExecutionerData trialExecutionerData)
        {
            if (Utils.CourtCheck() && trialExecutionerData.executionEntries[0].executionerId == Service.Game.Sim.simulation.myPosition)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Judge, Jury, and Executioner", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Judge, Jury, and Executioner",
                        Sprite = Utils.GetRoleSprite(Role.EXECUTIONER),
                        Description = "Hang your target during Court",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Jester
        public static IEnumerator Jester()
        {
            necessities.GetValue("NELeave", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NeutralEvilDetectLeave));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(NeutralEvilFactionWinPatch));
            necessities.SetValue("Hanged", false);
            necessities.SetValue("Silenced", false);
            NewPostfix(typeof(TrialVerdictDecoder), nameof(TrialVerdictDecoder.Encode), nameof(JesterDetectHanged));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(JesterCheckAchievements));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(JesterResetSilenced));
            yield break;
        }
        public static void JesterDetectHanged(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TRIAL_VERDICT)
            {
                ChatLogTrialVerdictEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTrialVerdictEntry;
                if (chatLog.defendantPosition == Service.Game.Sim.simulation.myPosition && chatLog.trialVerdict == TrialVerdict.GUILTY)
                    necessities.SetValue("Hanged", true);
            }
        }
        public static void JesterResetSilenced() => necessities.SetValue("Silenced", false);
        public static void JesterCheckAchievements(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.SILENCED)
                    necessities.SetValue("Silenced", true);
                else if (chatLog.messageId == GameFeedbackMessage.HANGED_JESTER && (bool)necessities.GetValue("Silenced", false))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Twisted Charades", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Twisted Charades",
                            Sprite = Utils.GetRoleSprite(Role.JESTER),
                            Description = "Win while Silenced",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Pirate
        public static IEnumerator Pirate()
        {
            if (!Utils.IsBTOS2())
            {
                necessities.GetValue("NELeave", false);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NeutralEvilDetectLeave));
                NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(NeutralEvilFactionWinPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(PirateDetectLandlubbers));
            }
            else
                NewPostfix(typeof(ChatDecoder), nameof(ChatDecoder.Encode), nameof(PirateSpeak));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(PirateDetectDefense));
            yield break;
        }
        public static void PirateDetectLandlubbers(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.LANDLUBBERS_ARE_X_Y_Z)
                {
                    List<Role> landlubbers = new List<Role> { chatLog.role1, chatLog.role2, chatLog.role3 };
                    Role townLL = Role.NONE;
                    Role covenLL = Role.NONE;
                    Role apocLL = Role.NONE;
                    foreach (Role landlubber in landlubbers)
                        if (landlubber.IsTownAligned())
                            townLL = landlubber;
                    if (townLL != Role.NONE)
                        landlubbers.Remove(townLL);
                    foreach (Role landlubber in landlubbers)
                        if (landlubber.IsCovenAligned())
                            covenLL = landlubber;
                    if (covenLL != Role.NONE)
                        landlubbers.Remove(covenLL);
                    foreach (Role landlubber in landlubbers)
                        if (landlubber.GetSubAlignment() == SubAlignment.APOCALYPSE)
                            apocLL = landlubber;
                    if (apocLL != Role.NONE)
                        landlubbers.Remove(apocLL);
                    if (landlubbers.Count == 0)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Staying Neutral", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Horrible Game Design",
                                Sprite = Utils.GetRoleSprite(Role.PIRATE),
                                Description = "Have a Town, Coven, and Apocalypse role as your landlubbers",
                                Vanilla = true,
                                BToS2 = false
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void PirateDetectDefense(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.DEFENSE_STRONGER_THAN_ATTACK)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Blow Me Down!", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Blow Me Down!",
                            Sprite = Utils.GetRoleSprite(Role.PIRATE),
                            Description = "Attack someone with defense",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void PirateSpeak(ChatLogMessage chatLogMessage)
        {
            
            ChatLogChatMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogChatMessageEntry;
            if (Service.Game.Sim.simulation.observations.daytime.Data.daytimeType == DaytimeType.NIGHT)
            {
                if (chatLog.speakerId == 71)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Avast Ye!", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Avast Ye!",
                            Sprite = Utils.GetRoleSprite(Role.PIRATE),
                            Description = "Speak to your target at night",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Plaguebearer
        public static IEnumerator Plaguebearer()
        {
            necessities.SetValue("Eligible", false);
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(PlaguebearerChangeEligibility));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(PlaguebearerDetectTransform));
            yield break;
        }
        public static void PlaguebearerChangeEligibility(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.DISCUSSION)
                necessities.SetValue("Eligible", true);
            else if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.NIGHT)
                necessities.SetValue("Eligible", false);
        }
        public static void PlaguebearerDetectTransform(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.PESTILENCE_HAS_EMERGED && (bool)necessities.GetValue("Eligible", false))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Were You Surprised?", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Were You Surprised?",
                            Sprite = Utils.GetRoleSprite(Role.PLAGUEBEARER),
                            Description = "Transform in the middle of the day",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Serial Killer
        public static IEnumerator SerialKiller()
        {
            necessities.SetValue("Evils Killed", 0);
            necessities.SetValue("Current Target", -1);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(SerialKillerDetectTargetDeath));
            if (Utils.IsBTOS2())
            {
                necessities.SetValue("Disguise", false);
                necessities.SetValue("Player", Service.Game.Sim.simulation.myPosition);
                NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(SerialKillerDetectDisguise));
                NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(SerialKillerUpdatePlayer));
            }
            yield break;
        }
        public static void SerialKillerDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if ((int)necessities.GetValue("Evils Killed", 0) != -1 && chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.SERIALKILLER_ATTACKED))
            {
                int evilsKilled = (int)necessities.GetValue("Evils Killed", 0);
                if (chatLog.killRecord.playerRole.IsTownAligned())
                    evilsKilled = -1;
                else
                    evilsKilled += 1;
                if (evilsKilled >= 2)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Blue Vigilante", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Blue Vigilante",
                            Sprite = Utils.GetRoleSprite(Role.SERIALKILLER),
                            Description = "Kill two evils before killing any townies",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                necessities.SetValue("Evils Killed", evilsKilled);
            }
            if (Utils.IsBTOS2() && (bool)necessities.GetValue("Disguise", false) && chatLog.killRecord.playerId == (int)necessities.GetValue("Player", -1) && (int)necessities.GetValue("Player", -1) != Service.Game.Sim.simulation.myPosition)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Skinwalker", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Skinwalker",
                        Sprite = Utils.GetRoleSprite(Role.SERIALKILLER),
                        Description = "Successfully disguise as another player",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
                if (chatLog.killRecord.hiddenPlayerRole.IsCovenAligned())
                {
                    if (!RechievementData.allRechievements.TryGetValue("Busted Disguise", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Busted Disguise",
                            Sprite = Utils.GetRoleSprite(Role.SERIALKILLER),
                            Description = "Jump into a Coven member's body",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (chatLog.killRecord.playerRole == Role.JESTER)
                {
                    if (!RechievementData.allRechievements.TryGetValue("All Smiles", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "All Smiles",
                            Sprite = Utils.GetRoleSprite(Role.SERIALKILLER),
                            Description = "Disguise as a Jester",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                else if (chatLog.killRecord.playerRole == BToS2Roles.Jackal)
                {
                    if (!RechievementData.allRechievements.TryGetValue("Code Blue", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Code Blue",
                            Sprite = Utils.GetRoleSprite(Role.SERIALKILLER),
                            Description = "Disguise as a Jackal",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void SerialKillerDetectDisguise(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Disguise", !chatLog.bIsCancel);
            }
        }
        public static void SerialKillerUpdatePlayer(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.DISCUSSION)
            {
                necessities.SetValue("Disguise", false);
                necessities.SetValue("Player", Service.Game.Sim.simulation.myPosition);
            }
        }
        // Shroud
        public static IEnumerator Shroud()
        {
            necessities.SetValue("Current Target", -1);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(ShroudDetectShroudedVisit));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(ShroudDetectTargetDeath));
            yield break;
        }
        public static void ShroudDetectShroudedVisit(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.SHROUD_SHROUDED_ATTACKED_PLAYER)
                    necessities.SetValue("Current Target", chatLog.playerNumber1);
            }
        }
        public static void ShroudDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.SHROUD_ATTACKED) && chatLog.killRecord.killedByReasons.Count > 1)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Spooky Secrets", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Spooky Secretse",
                        Sprite = Utils.GetRoleSprite(Role.SHROUD),
                        Description = "Compel your shrouded target to attack a player that was already dying",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            if (Utils.IsBTOS2() && chatLog.killRecord.playerFaction == BToS2Factions.Compliance && chatLog.killRecord.playerId != Service.Game.Sim.simulation.myPosition && currentFaction == BToS2Factions.Compliance)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Downright Ghastly", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Downright Ghastly",
                        Sprite = Utils.GetAssetBundleSprite("ComkShroud"), // Give Custom Sprite - ComkShroud
                        Description = "Kill your own Compliance teammate",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Soul Collector
        public static IEnumerator SoulCollector()
        {
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(SoulCollectorDetectDeputyShot));
            yield break;
        }
        public static void SoulCollectorDetectDeputyShot(ChatLogMessage chatLogMessage)
        {
            
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && Service.Game.Sim.simulation.observations.playerEffects[(int)chatLog.killRecord.playerId].Data.effects.Contains(EffectType.REAPED) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.DEPUTY_SHOT) && chatLog.killRecord.playerRole.IsTownAligned() && chatLog.killRecord.playerFaction == FactionType.TOWN)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Rowdy Reaper", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Rowdy Reaper",
                        Sprite = Utils.GetRoleSprite(Role.SOULCOLLECTOR),
                        Description = "Collect the soul of a Town-aligned player that was shot by a Deputy",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Werewolf
        public static IEnumerator Werewolf()
        {
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(WerewolfDetectUnknownObstacle));
            yield break;
        }
        public static void WerewolfDetectUnknownObstacle(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1100)
                {
                    int nightNumber = Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber;
                    if (nightNumber == 2 || nightNumber >= 4)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Down Doggy", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Down Doggy",
                                Sprite = Utils.GetRoleSprite(Role.WEREWOLF),
                                Description = "See an Unknown Obstacle on a full moon",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        // Vampire
        public static IEnumerator Vampire()
        {
            if (Utils.IsBTOS2())
            {
                necessities.SetValue("Current Target", -1);
                necessities.SetValue("Convert", false);
                NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
                NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(VampireConvertPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(VampireDetectConvert));
                if (Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber > 1)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Honor of Dracula", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Honor of Dracula",
                            Sprite = Utils.GetRoleSprite(Role.VAMPIRE),
                            Description = "Get promoted to Vampire",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
            yield break;
        }
        public static void VampireConvertPatch(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TARGET_SELECTION)
            {
                ChatLogTargetSelectionFeedbackEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTargetSelectionFeedbackEntry;
                if (chatLog.menuChoiceType == MenuChoiceType.SpecialAbility)
                    necessities.SetValue("Convert", !chatLog.bIsCancel);
            }
        }
        public static void VampireDetectConvert()
        {
            if ((bool)necessities.GetValue("Convert", false) && (int)necessities.GetValue("Current Target", -1) != -1)
            {
                Tuple<Role, FactionType> tuple;
                if (Service.Game.Sim.simulation.knownRolesAndFactions.Data.TryGetValue((int)necessities.GetValue("Current Target", -1), out tuple))
                {
                    Role convertRole = tuple.Item1;
                    FactionType convertFaction = tuple.Item2;
                    if (convertRole.IsTownAligned() && convertRole.GetSubAlignment() == SubAlignment.POWER && convertFaction == FactionType.VAMPIRE)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Blood Bank", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Blood Bank",
                                Sprite = Utils.GetRoleSprite(Role.VAMPIRE),
                                Description = "Convert a Town Power",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
            necessities.SetValue("Convert", false);
        }
        // Cursed Soul
        public static IEnumerator CursedSoul()
        {
            if (Utils.IsBTOS2())
            {
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(CursedSoulDetectSwap));
            }
            yield break;
        }
        public static void CursedSoulDetectSwap(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.YOU_SWAPPED_SOULS_WITH_A_PLAYER)
                {
                    if (Service.Game.Sim.simulation.myIdentity.Data.role == Role.CONJURER && Service.Game.Sim.simulation.observations.roleCardObservation.Data.specialAbilityRemaining == 0)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("I'll Take That, It's Mine Now", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "I'll Take That, It's Mine Now",
                                Sprite = Utils.GetRoleSprite(Role.CURSED_SOUL),
                                Description = "Swap with a Conjurer that still has a charge",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    } else if (!Service.Game.Sim.simulation.myIdentity.Data.role.IsTownAligned() && currentFaction == FactionType.TOWN)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Freaky Friday", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Freaky Friday",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.CommonTown),
                                Description = "Steal an evil role as a Town-aligned Cursed Soul",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        // Socialite or Banshee
        public static IEnumerator SocialiteOrBanshee()
        {
            if (!Utils.IsBTOS2())
            {
                // Socialite (Vanilla)
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(SocialiteCheckGuestList));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(SocialiteCheckGuestList));
            } else
            {
                // Banshee (BToS2)
                NewPostfix(typeof(TrialVerdictDecoder), nameof(TrialVerdictDecoder.Encode), nameof(BansheeDetectDeafenedHanged));
            }
            yield break;
        }
        public static void BansheeDetectDeafenedHanged(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TRIAL_VERDICT)
            {
                ChatLogTrialVerdictEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTrialVerdictEntry;
                if (Utils.CourtCheck() && chatLog.trialVerdict == TrialVerdict.GUILTY && Service.Game.Sim.simulation.observations.playerEffects[chatLog.defendantPosition].Data.effects.Contains((EffectType)101))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Corrupt and Unhearing Court", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Corrupt and Unhearing Court",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Banshee),
                            Description = "Hang a Deafened player during Court",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void SocialiteCheckGuestList()
        {
            bool allOnList = true;
            foreach (DiscussionPlayerObservation discussionPlayer in Service.Game.Sim.simulation.observations.discussionPlayers)
                if (discussionPlayer.Data.position != Service.Game.Sim.simulation.myPosition && discussionPlayer.Data.alive && !Service.Game.Sim.simulation.observations.playerEffects[discussionPlayer.Data.position].Data.effects.Contains(EffectType.SOCIALITE_GUEST))
                    allOnList = false;
            if (allOnList)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Public Party", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Public Party",
                        Sprite = Utils.GetRoleSprite(Role.SOCIALITE),
                        Description = "Have every alive player on your Guest List",
                        Vanilla = true,
                        BToS2 = false
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Marshal or Jackal
        public static IEnumerator MarshalOrJackal()
        {
            if (!Utils.IsBTOS2())
            {
                // Marshal (Vanilla) - No Vanilla Marshal achievements
            } else {
                // Jackal (BToS2)
                necessities.SetValue("Recruits", false);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(JackalDetectRecruits));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(JackalDetectNotRecruits));
            }
            yield break;
        }
        public static void JackalDetectRecruits(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1000)
                    necessities.SetValue("Recruits", true);
            }
        }
        public static void JackalDetectNotRecruits()
        {
            if (!(bool)necessities.GetValue("Recruits", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Lone Wolf", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Lone Wolf",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.Jackal),
                        Description = "Spawn without Recruits",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Oracle or Marshal
        public static IEnumerator OracleOrMarshal()
        {
            if (!Utils.IsBTOS2())
            {
                // Oracle (Vanilla) - No Oracle Achievements
            } else
            {
                // Marshal (BToS2)
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(MarshalDetectJackalHangTribunal));
            }
            yield break;
        }
        public static void MarshalDetectJackalHangTribunal(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (Service.Game.Sim.simulation.trialData.Data.isTribunal && chatLog.killRecord.killedByReasons.Contains(KilledByReason.EXECUTED) && chatLog.killRecord.playerRole == BToS2Roles.Marshal)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Rival Commander Down", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Rival Commander Down",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Marshal),
                            Description = "Hang the Jackal during Tribunal",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Judge
        public static IEnumerator Judge()
        {
            if (Utils.IsBTOS2())
            {
                necessities.SetValue("Court Hang", false);
                necessities.SetValue("Court", false);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(JudgeDetectCourtAndProsecutor));
                NewPostfix(typeof(ExecutionerLeavesFeatures), nameof(ExecutionerLeavesFeatures.Init), nameof(JudgeDetectExecutionerInCourt));
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(JudgeDetectCourtHang));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(JudgeDetectCourtNotHang));
            }
            yield break;
        }
        public static void JudgeDetectCourtAndProsecutor(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TARGET_IS_PROSECUTOR)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Rival Attorney", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Rival Attorney",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Judge),
                            Description = "Subpoena a Prosecutor",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                } else if (chatLog.messageId == (GameFeedbackMessage)1021)
                {
                    necessities.SetValue("Court", true);
                    if (Service.Game.Sim.simulation.trialData.Data.isTribunal)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Overruled", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Overruled",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Judge),
                                Description = "Call Court during a Tribunal",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void JudgeDetectExecutionerInCourt(TrialExecutionerData trialExecutionerData)
        {
            if (Utils.CourtCheck())
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Bailiff", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Bailiff",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.Judge),
                        Description = "Hang the Executioner’s target during Court",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static void JudgeDetectCourtHang(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (Utils.CourtCheck() && chatLog.killRecord.killedByReasons.Contains(KilledByReason.EXECUTED))
                    necessities.SetValue("Court Hang", true);
            }
        }
        public static void JudgeDetectCourtNotHang()
        {
            if ((bool)necessities.GetValue("Court", false) && !(bool)necessities.GetValue("Court Hang", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Jury Nullification", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Jury Nullification",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.Judge),
                        Description = "Hang nobody during Court",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            necessities.SetValue("Court", false);
            necessities.SetValue("Court Hang", false);
        }
        // Auditor
        public static IEnumerator Auditor()
        {
            if (Utils.IsBTOS2())
            {
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(AuditorCheckAudits));
            }
            yield break;
        }
        public static void AuditorCheckAudits()
        {
            int audits = 0;
            foreach (DiscussionPlayerObservation discussionPlayer in Service.Game.Sim.simulation.observations.discussionPlayers)
                if (discussionPlayer.Data.position != Service.Game.Sim.simulation.myPosition && Service.Game.Sim.simulation.observations.playerEffects[discussionPlayer.Data.position].Data.effects.Contains((EffectType)104))
                {
                    audits += 1;
                    if (Service.Game.Sim.simulation.observations.playerEffects[discussionPlayer.Data.position].Data.effects.Contains(EffectType.REVEALED_MARSHAL))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Immobilized Command", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Immobilized Command",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Auditor),
                                Description = "Audit a revealed Marshal",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            if (audits >= 3)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Internal Revenue Service", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Internal Revenue Service",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.Auditor),
                        Description = "Audit the charges of 3 players in one game",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Inquisitor
        public static IEnumerator Inquisitor()
        {
            if (Utils.IsBTOS2())
            {
                necessities.GetValue("NELeave", false);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(NeutralEvilDetectLeave));
                NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(NeutralEvilFactionWinPatch));
                necessities.SetValue("Heretic", false);
                NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(InquisitorCheckAchievements));
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(InquisitorDetectTargetDeath));
                NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToNight), nameof(InquisitorResetHeretic));
            }
            yield break;
        }
        public static void InquisitorCheckAchievements(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.LEFT_TOWN && chatLog.playerNumber1 == Service.Game.Sim.simulation.myPosition && chatLog.role1 == BToS2Roles.Inquisitor && Service.Game.Sim.simulation.observations.roleCardObservation.Data.specialAbilityRemaining == 3)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Divine Will", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Divine Will",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Inquisitor),
                            Description = "Win without vanquishing anyone",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                } else if (chatLog.messageId == (GameFeedbackMessage)1024)
                {
                    necessities.SetValue("Heretic", true);
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("By the Pope!", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "By the Pope!",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Inquisitor),
                            Description = "Find a Heretic",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        public static void InquisitorDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1))
                {
                    if (chatLog.killRecord.killedByReasons.Contains((KilledByReason)72) && (chatLog.killRecord.playerRole == Role.RETRIBUTIONIST || chatLog.killRecord.playerRole == Role.NECROMANCER))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Abomination!", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Abomination!",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Inquisitor),
                                Description = "Vanquish a heretic Retributionist or Necromancer",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    } else if ((bool)necessities.GetValue("Heretic", false) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.SHROUD_ATTACKED))
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Holy Ghost", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Holy Ghost",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Inquisitor),
                                Description = "Kill a heretic by being Shrouded",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void InquisitorResetHeretic() => necessities.SetValue("Heretic", false);
        // Starspawn
        public static IEnumerator Starspawn()
        {
            if (Utils.IsBTOS2())
            {
                necessities.SetValue("Starbound TP", false);
                NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(StarspawnDetectStarbound));
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(StarspawnDetectTargetDeath));
            }
            yield break;
        }
        public static void StarspawnDetectStarbound(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1031 && chatLog.role1.GetSubAlignment() == SubAlignment.PROTECTIVE)
                {
                    necessities.SetValue("Starbound TP", true);
                    if (chatLog.role1 == Role.CRUSADER)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Crus a Fiction", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Crus a Fiction",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Starspawn),
                                Description = "Starbound a Crusader",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void StarspawnDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (chatLog.killRecord.playerId == (int)necessities.GetValue("Current Target", -1) && (bool)necessities.GetValue("Starbound TP", false))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Denied!", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Denied!",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.Starspawn),
                            Description = "Prevent a Town Protective from saving their target",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Warlock
        public static IEnumerator Warlock()
        {
            if (Utils.IsBTOS2())
            {
                necessities.SetValue("Curse Visit", 0);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(WarlockDetectCurse));
            }
            yield break;
        }
        public static void WarlockDetectCurse(ChatLogMessage chatLogMessage)
        {
            if (processed2.Contains(chatLogMessage))
                return;
            processed2.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == (GameFeedbackMessage)1112)
                {
                    int curseVisit = (int)necessities.GetValue("Curse Visit", 0);
                    necessities.SetValue("Curse Visit", curseVisit + 1);
                    if (curseVisit >= 1)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Inverse Curse", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Inverse Curse",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Warlock),
                                Description = "Curse a Seer and cause two players to appear friendly",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                    if (chatLog.playerNumber2 == Service.Game.Sim.simulation.myPosition)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Excuse Me?", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Excuse Me?",
                                Sprite = Utils.GetRoleSprite(BToS2Roles.Warlock),
                                Description = "See a cursed player visit you",
                                Vanilla = false,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        // Faction Coroutines
        // Town
        public static IEnumerator Town()
        {
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(TownDetectTTHangedD2));
            // Freaky Friday ach in Cursed Soul role
            yield break;
        }
        public static void TownDetectTTHangedD2(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber == 2 && chatLog.killRecord.killedByReasons.Contains(KilledByReason.EXECUTED) && chatLog.killRecord.playerRole.IsTownAligned() && (chatLog.killRecord.playerFaction == FactionType.COVEN || chatLog.killRecord.playerFaction == FactionType.APOCALYPSE || chatLog.killRecord.playerFaction == BToS2Factions.Pandora))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Massive L", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Massive L",
                            Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.CommonTown) : Utils.GetRoleSprite(Role.COMMON_TOWN),
                            Description = "Hang the Jackal during Tribunal",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Coven
        public static IEnumerator Coven()
        {
            if (necessities.GetValue("Town Traitor", null) == null)
                necessities.SetValue("Town Traitor", -1);
            if (necessities.GetValue("Town Traitor 2", null) == null)
                necessities.SetValue("Town Traitor 2", -1);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralDetectTownTraitor));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(CovenFactionWinPatch));
            if (currentRole.IsTownAligned() && Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber < 2)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Apprentice Magician", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Apprentice Magician",
                        Sprite = Utils.GetRoleSprite(Role.TOWN_TRAITOR),
                        Description = "Betray the Town by joining the Coven",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            yield break;
        }
        public static void CovenFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == FactionType.COVEN)
            {
                bool isSolo = __instance.cinematicData.entries.Count == 1;
                bool allDead = true;
                foreach (FactionWinsCinematicData.Entry entry in __instance.cinematicData.entries)
                    if (entry.alive && entry.position != Service.Game.Sim.simulation.myPosition)
                        allDead = false;
                if (isSolo)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Magus Perfectus", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Magus Perfectus",
                            Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.CommonCoven) : Utils.GetRoleSprite(Role.COMMON_COVEN),
                            Description = "Win as solo Coven",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (allDead && GeneralGetIfTownTraitor(Service.Game.Sim.simulation.myPosition))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Coven's Concoction", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Coven's Concoction",
                            Sprite = Utils.GetRoleSprite(Role.TOWN_TRAITOR),
                            Description = "Win the Town Traitor hunt",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            } else if (Utils.IsBTOS2() && __instance.cinematicData.winningFaction == BToS2Factions.Pandora && !Service.Game.Sim.simulation.roleDeckBuilder.Data.modifierCards.Contains(BToS2Roles.PandorasBox))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Greater Darkness", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Greater Darkness",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                        Description = "Win the Town Traitor hunt with the opposing TT, becoming Pandora",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Apocalypse
        public static IEnumerator Apocalypse()
        {
            if (necessities.GetValue("InvalidApoc", null) == null)
                necessities.SetValue("InvalidApoc", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(ApocalypseDetectApocalypse));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(ApocalypseFactionWinPatch));
            if (Utils.IsBTOS2())
            {
                if (necessities.GetValue("Town Traitor", null) == null)
                    necessities.SetValue("Town Traitor", -1);
                if (necessities.GetValue("Town Traitor 2", null) == null)
                    necessities.SetValue("Town Traitor 2", -1);
                NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralDetectTownTraitor));
                if (currentRole.IsTownAligned() && Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber < 2)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Apprentice Worshipper", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Apprentice Worshipper",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.ApocTownTraitor),
                            Description = "Betray the Town by joining the Apocalypse",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
            yield break;
        }
        public static void ApocalypseDetectApocalypse(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.PESTILENCE_HAS_EMERGED)
                    necessities.SetValue("InvalidApoc", true);
                else if (chatLog.messageId == GameFeedbackMessage.WAR_HAS_EMERGED)
                    necessities.SetValue("InvalidApoc", true);
                else if (chatLog.messageId == GameFeedbackMessage.FAMINE_HAS_EMERGED)
                    necessities.SetValue("InvalidApoc", true);
                else if (chatLog.messageId == GameFeedbackMessage.DEATH_HAS_EMERGED || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1097)
                    necessities.SetValue("InvalidApoc", true);
            }
        }
        public static void ApocalypseFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == FactionType.APOCALYPSE)
            {
                bool allDead = true;
                foreach (FactionWinsCinematicData.Entry entry in __instance.cinematicData.entries)
                    if (entry.alive && entry.position != Service.Game.Sim.simulation.myPosition)
                        allDead = false;
                if (!(bool)necessities.GetValue("InvalidApoc", false))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Atheist", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Atheist",
                            Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.RandomApocalypse) : Utils.GetRoleSprite(Role.NEUTRAL_APOCALYPSE),
                            Description = "Win without any member of the Apocalypse transforming",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (Utils.IsBTOS2() && allDead && GeneralGetIfTownTraitor(Service.Game.Sim.simulation.myPosition))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Final Pact", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Final Pact",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.ApocTownTraitor),
                            Description = "Win the Town Traitor hunt",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
            else if (Utils.IsBTOS2() && __instance.cinematicData.winningFaction == BToS2Factions.Pandora && !Service.Game.Sim.simulation.roleDeckBuilder.Data.modifierCards.Contains(BToS2Roles.PandorasBox))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Greater Darkness", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Greater Darkness",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                        Description = "Win the Town Traitor hunt with the opposing TT, becoming Pandora",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Neutral Evil
        public static void NeutralEvilDetectLeave(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.LEFT_TOWN && chatLog.playerNumber1 == Service.Game.Sim.simulation.myPosition && chatLog.role1.GetSubAlignment() == SubAlignment.EVIL)
                    necessities.SetValue("NELeave", true);
            }
        }
        public static void NeutralEvilFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == FactionType.NONE && (bool)necessities.GetValue("NELeave", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Who Needs a Faction?", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Who Needs a Faction?",
                        Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.NeutralEvil) : Utils.GetRoleSprite(Role.NEUTRAL_EVIL),
                        Description = "Win as Neutral Evil during a Match Draw",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Vampire
        public static IEnumerator VampireFaction()
        {
            if (Utils.IsBTOS2() && currentRole.IsTownAligned())
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Blood Drinker", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Blood Drinker",
                        Sprite = Utils.GetRoleSprite(Role.VAMPIRE),
                        Description = "Betray the Town and embrace immortality",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            yield break;
        }
        // Jackal or Recruit
        public static IEnumerator JackalOrRecruit()
        {
            if (currentRole != BToS2Roles.Marshal)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Contracted Traitor", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Contracted Traitor",
                        Sprite = Utils.GetBToS2Sprite("EffectIcon_Recruit"),
                        Description = "Get recruited by the Jackal",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(JackalFactionWinPatch));
            yield break;
        }
        public static void JackalFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == BToS2Factions.Jackal)
            {
                bool allAlive = true;
                foreach (FactionWinsCinematicData.Entry entry in __instance.cinematicData.entries)
                    if (!entry.alive)
                        allAlive = false;
                if (allAlive)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Smooth Operators", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Smooth Operators",
                            Sprite = Utils.GetBToS2Sprite("EffectIcon_Recruit"),
                            Description = "Win without the Jackal or any recruited players dying",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Egotist
        public static IEnumerator Egotist()
        {
            RechievementData rechievement;
            if (!RechievementData.allRechievements.TryGetValue("Pumping Adrenaline", out rechievement))
            {
                rechievement = new RechievementData
                {
                    Name = "Pumping Adrenaline",
                    Sprite = Utils.GetRoleSprite(BToS2Roles.Egotist),
                    Description = "Let your inner ego overwhelm you",
                    Vanilla = false,
                    BToS2 = true
                };
                RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
            }
            rechievement.ShowRechievement();
            yield break;
        }
        // Pandora
        public static IEnumerator Pandora()
        {
            if (necessities.GetValue("Town Traitor", null) == null)
                necessities.SetValue("Town Traitor", -1);
            if (necessities.GetValue("Town Traitor 2", null) == null)
                necessities.SetValue("Town Traitor 2", -1);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralDetectTownTraitor));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(PandoraFactionWinPatch));
            if (currentRole.IsTownAligned() && Service.Game.Sim.simulation.observations.daytime.Data.daynightNumber < 2)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Apprentice Cultist", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Apprentice Cultist",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                        Description = "Betray the Town by joining Pandora",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            yield break;
        }
        public static void PandoraFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == BToS2Factions.Pandora)
            {
                bool allAlive = true;
                bool allDead = true;
                foreach (FactionWinsCinematicData.Entry entry in __instance.cinematicData.entries)
                    if (!entry.alive)
                        allAlive = false;
                    else if (entry.alive && entry.position != Service.Game.Sim.simulation.myPosition)
                        allDead = false;
                if (allAlive)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Perfect Pandora", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Perfect Pandora",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                            Description = "Win the game without any members of Pandora’s Box dying",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
                if (allDead && GeneralGetIfTownTraitor(Service.Game.Sim.simulation.myPosition))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Ruination", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Ruination",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                            Description = "Win the Town Traitor hunt",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
            else if (__instance.cinematicData.winningFaction == BToS2Factions.Pandora && !Service.Game.Sim.simulation.roleDeckBuilder.Data.modifierCards.Contains(BToS2Roles.PandorasBox))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Greater Darkness", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Greater Darkness",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.PandorasBox),
                        Description = "Win the Town Traitor hunt with the opposing TT, becoming Pandora",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // Compliance
        public static IEnumerator Compliance()
        {
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(ComplianceFactionWinPatch));
            yield break;
        }
        public static void ComplianceFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == BToS2Factions.Compliance)
            {
                bool allAlive = true;
                foreach (FactionWinsCinematicData.Entry entry in __instance.cinematicData.entries)
                    if (!entry.alive)
                        allAlive = false;
                if (allAlive)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("Perfect Compliance", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "Perfect Compliance",
                            Sprite = Utils.GetRoleSprite(BToS2Roles.CompliantKillers),
                            Description = "Win the game without any members of the Compliant Killers dying",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
        // Global Coroutines

        // Waga Baba Bobo
        public static IEnumerator WagaBabaBobo()
        {
            NewPostfix(typeof(ChatDecoder), nameof(ChatDecoder.Encode), nameof(CheckIfSaidWagaBabaBobo));
            yield break;
        }

        public static void CheckIfSaidWagaBabaBobo(ChatLogMessage chatLogMessage)
        {
            
            ChatLogChatMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogChatMessageEntry;
            if (Service.Game.Sim.simulation.observations.roleAlteringEffectsObservation.Data.bIsJailing || Service.Game.Sim.simulation.observations.roleAlteringEffectsObservation.Data.bIsJailed)
            {
                if (chatLog.speakerId != ChatLogChatMessageEntry.JAILOR_SPEAKING_ID && chatLog.message.ToLower().Contains("waga baba bobo"))
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("waga baba bobo", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "waga baba bobo",
                            Sprite = Utils.GetAssetBundleSprite("WagaBabaBobo"),
                            Description = "waga baba bobo",
                            Vanilla = true,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }

        // Balanced List
        public static IEnumerator BalancedList()
        {
            if (Service.Game.Sim.simulation.roleDeckBuilder.Data.roles.Contains(Role.PESTILENCE) || Service.Game.Sim.simulation.roleDeckBuilder.Data.roles.Contains(Role.WAR))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Balanced List", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Balanced List",
                        Sprite = Utils.GetRoleSprite(BToS2Roles.RandomApocalypse),
                        Description = "Witness a Horseman of the Apocalypse transform Day 1",
                        Vanilla = false,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
            yield break;
        }

        public static Role currentRole = Role.NONE;
        public static FactionType currentFaction = FactionType.NONE;
        public static List<ChatLogMessage> processed = new List<ChatLogMessage>();
        public static List<ChatLogMessage> processed2 = new List<ChatLogMessage>();
        public static List<ChatLogMessage> processed3 = new List<ChatLogMessage>();
        public static List<ChatLogMessage> processed4 = new List<ChatLogMessage>();
        // Revolution
        public static IEnumerator Revolution()
        {
            if (necessities.GetValue("Revolution", null) == null)
                necessities.SetValue("Revolution", false);
            NewPostfix(typeof(TrialVerdictDecoder), nameof(TrialVerdictDecoder.Encode), nameof(RevolutionMarshalHanged));
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(RevolutionFactionWinPatch));
            yield break;
        }
        public static void RevolutionMarshalHanged(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.TRIAL_VERDICT)
            {
                ChatLogTrialVerdictEntry chatLog = chatLogMessage.chatLogEntry as ChatLogTrialVerdictEntry;
                
                if (chatLog.trialVerdict == TrialVerdict.GUILTY && Service.Game.Sim.simulation.observations.playerEffects[chatLog.defendantPosition].Data.effects.Contains(EffectType.REVEALED_MARSHAL))
                    necessities.SetValue("Revolution", true);
            }
        }
        public static void RevolutionFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == currentFaction && (bool)necessities.GetValue("Revolution", false))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Revolution", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Revolution",
                        Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.Marshal) : Utils.GetRoleSprite(Role.MARSHAL),
                        Description = "Hang a revealed Marshal and win the game",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        // My Life is an Unknown Obstacle
        public static IEnumerator MyLifeIsAnUnknownObstacle()
        {
            if (necessities.GetValue("UOCount", null) == null)
                necessities.SetValue("UOCount", 0);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(LifeIsUOCount));
            yield break;
        }
        public static void LifeIsUOCount(ChatLogMessage chatLogMessage)
        {
            if (processed3.Contains(chatLogMessage))
                return;
            processed3.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || Utils.IsBTOS2() && chatLog.messageId == (GameFeedbackMessage)1100)
                {
                    int uoCount = (int)necessities.GetValue("UOCount", 0);
                    necessities.SetValue("UOCount", uoCount + 1);
                    if (uoCount >= 2)
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("My Life is an Unknown Obstacle", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "My Life is an Unknown Obstacle",
                                Sprite = Utils.IsBTOS2() ? Utils.GetRoleSprite(BToS2Roles.AnonNames) : Utils.GetRoleSprite(Role.ANON_PLAYERS),
                                Description = "See an Unknown Obstacle 3 times in one game",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        // Ha-ha, I'm In Danger
        public static IEnumerator HaHaImInDanger()
        {
            necessities.SetValue("DangerElement", false);
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(DangerCheckRevealedVeteran));
            NewPostfix(typeof(GlobalShaderColors), nameof(GlobalShaderColors.SetToDay), nameof(DangerReset));
            yield break;
        }
        public static void DangerCheckRevealedVeteran(ChatLogMessage chatLogMessage)
        {
            if (processed4.Contains(chatLogMessage))
                return;
            processed4.Add(chatLogMessage);
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.TARGET_IS_VETERAN || chatLog.messageId == GameFeedbackMessage.KILLED_BY_VETERAN)
                {
                    if (!(bool)necessities.GetValue("DangerElement", false))
                        necessities.SetValue("DangerElement", true);
                    else
                    {
                        RechievementData rechievement;
                        if (!RechievementData.allRechievements.TryGetValue("Ha-ha, I'm In Danger", out rechievement))
                        {
                            rechievement = new RechievementData
                            {
                                Name = "Ha-ha, I'm In Danger",
                                Sprite = Utils.GetRoleSprite(Role.ROLES_ON_DEATH_HIDDEN),
                                Description = "Get shot by a Veteran you revealed that night",
                                Vanilla = true,
                                BToS2 = true
                            };
                            RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                        }
                        rechievement.ShowRechievement();
                    }
                }
            }
        }
        public static void DangerReset() => necessities.SetValue("DangerElement", false);
        // Why Me?!
        public static IEnumerator WhyMe()
        {
            NewPostfix(typeof(ChatDecoder), nameof(ChatDecoder.Encode), nameof(CheckIfSaidWhyMe));
            yield break;
        }
        public static void CheckIfSaidWhyMe(ChatLogMessage chatLogMessage)
        {
            
            ChatLogChatMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogChatMessageEntry;
            if (chatLog.speakerId == Service.Game.Sim.simulation.myPosition && Service.Game.Sim.simulation.trialData.Data.defendantPosition == Service.Game.Sim.simulation.myPosition && chatLog.message.ToLower().Contains("why me"))
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Why Me?!", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Why Me?!",
                        Sprite = Utils.GetRoleSprite(Role.FAST_MODE),
                        Description = "You know what you did",
                        Vanilla = true,
                        BToS2 = true
                    };
                    RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                }
                rechievement.ShowRechievement();
            }
        }
        public static IEnumerator LJudge()
        {
            if (Utils.IsBTOS2())
                NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(DetectLJudge));
            yield break;
        }
        public static void DetectLJudge(ChatLogMessage chatLogMessage)
        {
            
            if (chatLogMessage.chatLogEntry.type == ChatType.WHO_DIED)
            {
                ChatLogWhoDiedEntry chatLog = chatLogMessage.chatLogEntry as ChatLogWhoDiedEntry;
                if (Utils.CourtCheck() && chatLog.killRecord.killedByReasons.Contains(KilledByReason.EXECUTED) && chatLog.killRecord.playerRole == BToS2Roles.Judge)
                {
                    RechievementData rechievement;
                    if (!RechievementData.allRechievements.TryGetValue("L Judge", out rechievement))
                    {
                        rechievement = new RechievementData
                        {
                            Name = "L Judge",
                            Sprite = Utils.GetBToS2Sprite("RoleCard_Judge_SpecialAbility"),
                            Description = "Witness the Judge be hanged in Court",
                            Vanilla = false,
                            BToS2 = true
                        };
                        RechievementData.allRechievements.SetValue(rechievement.Name, rechievement);
                    }
                    rechievement.ShowRechievement();
                }
            }
        }
    }
}