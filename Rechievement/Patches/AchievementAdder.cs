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
using UnityEngine.UIElements;

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
                Debug.LogWarning(role);
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
                Utils.harmonyInstance.Unpatch(AccessTools.Method(typeof(RoleCardPanel), nameof(RoleCardPanel.HandleOnMyIdentityChanged)), IdentityPatch);
                IdentityPatch = null;
                necessities.Clear();
                shown.Clear();
            }
        }

        public static List<RechievementData> shown = new List<RechievementData>();

        public static void IdentityChangePatch(PlayerIdentityData playerIdentityData)
        {
            if (playerIdentityData.role != currentRole || playerIdentityData.faction != currentFaction)
            {
                ClearCoroutines();
                ClearPatches();
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
            { Role.ADMIRER, Admirer },
            { Role.AMNESIAC, Amnesiac },
            { Role.BODYGUARD, Bodyguard },
            { Role.CLERIC, Cleric },
            { Role.CORONER, Coroner },
            { Role.DEPUTY, Deputy },
            { Role.INVESTIGATOR, Investigator },
            { Role.MAYOR, Mayor },
            { Role.MONARCH, Monarch },
            { Role.RETRIBUTIONIST, Retributionist },
            { Role.SEER, Seer },
            { Role.SHERIFF, Sheriff }

        };

        public static Dictionary<FactionType, Func<IEnumerator>> allFactionCoroutines = new Dictionary<FactionType, Func<IEnumerator>>
        {
            { FactionType.NONE, Default }
        };

        public static List<Func<IEnumerator>> globalCoroutines = new List<Func<IEnumerator>>
        {
            WagaBabaBobo
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
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED || chatLog.messageId == GameFeedbackMessage.JAILED_TARGET || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || chatLog.messageId == GameFeedbackMessage.CANNOT_USE_ABILITY_WHILE_INSANE || chatLog.messageId == GameFeedbackMessage.WITCH_CONTROLLED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY)
                    necessities.SetValue("Current Target", -1);
            }
        }

        public static void GeneralRemoveTargetStartOfDiscussion(GameInfo gameInfo)
        {
            if (gameInfo.gamePhase == GamePhase.PLAY && gameInfo.playPhase == PlayPhase.DISCUSSION)
                necessities.SetValue("Current Target", -1);
        }

        // Role Coroutines
        // Admirer
        public static IEnumerator Admirer()
        {
            if (!Utils.IsBTOS2())
            {
                NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.HandleProposalMessage), nameof(ProposalMessagePatch));
                NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.Cleanup), nameof(ProposalCleanupPatch));
            }
            yield break;
        }
        public static void ProposalMessagePatch(ProposalMessage message)
        {
            if (message.status == ProposalStatus.Accepted)
            {
                necessities.SetValue("Accepted", true);
                necessities.SetValue("Proposed To", message.otherPosition);
            }
        }
        public static void ProposalCleanupPatch()
        {
            if ((bool)necessities.GetValue("Accepted", false))
            {
                if (Pepper.GetDiscussionPlayerRoleIfKnown((int)necessities.GetValue("Proposed To", -1)) == Role.DOOMSAYER)
                {
                    NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AdmirerFactionWinPatch));
                }
            }
        }

        public static void AdmirerFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            if (__instance.cinematicData.winningFaction == Service.Game.Sim.simulation.myIdentity.Data.faction)
            {
                RechievementData rechievement;
                if (!RechievementData.allRechievements.TryGetValue("Written in the Stars", out rechievement))
                {
                    rechievement = new RechievementData
                    {
                        Name = "Written in the Stars",
                        Sprite = Utils.GetRoleSprite(Role.ADMIRER), // Give Custom Sprite
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
            Debug.LogWarning("AMNE ------------------");
            NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AmnesiacFactionWinPatch));
            yield break;
        }
        public static void AmnesiacFactionWinPatch(FactionWinsStandardCinematicPlayer __instance)
        {
            Debug.LogWarning(__instance.cinematicData.winningFaction == Service.Game.Sim.simulation.myIdentity.Data.faction);
            if (Service.Game.Sim.simulation.myIdentity.Data.role == Role.AMNESIAC && __instance.cinematicData.winningFaction == Service.Game.Sim.simulation.myIdentity.Data.faction)
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

        // Bodyguard
        public static IEnumerator Bodyguard()
        {
            Debug.LogWarning("BG -------------------------");
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
                if (chatLog.messageId == GameFeedbackMessage.DEPUTY_SHOT_FAILED)
                    necessities.SetValue("Deputy Target", -1);
            }
        }

        public static void DeputyDetectTargetDeath(ChatLogMessage chatLogMessage)
        {
            ChatLogWhoDiedEntry chatLog = (ChatLogWhoDiedEntry)chatLogMessage.chatLogEntry;
            if (chatLog.killRecord.isDay && currentFaction == FactionType.TOWN && (int)chatLog.killRecord.playerId == (int)necessities.GetValue("Deputy Target", -1) && chatLog.killRecord.killedByReasons.Contains(KilledByReason.DEPUTY_SHOT) && chatLog.killRecord.playerRole.IsTownAligned() && chatLog.killRecord.playerFaction == FactionType.TOWN)
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
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.INVESTIGATOR_FIND_CRIMINAL_EVIDENCE)
                {
                    necessities.SetValue("Murder", true);
                    int totalCrimes = (int)necessities.GetValue("Total Crimes", 0);
                    necessities.SetValue("Total Crimes", totalCrimes + 1);
                } else if (chatLog.messageId == GameFeedbackMessage.INVESTIGATOR_GUILTY_OF_TRESPASSING)
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

        // Mayor
        public static IEnumerator Mayor()
        {
            NewPostfix(typeof(GameSimulation), nameof(GameSimulation.HandleOnGameInfoChanged), nameof(MayorCheckVotes));
            yield break;
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

        // Retributionist
        public static IEnumerator Retributionist()
        {
            necessities.SetValue("Using Role", Role.NONE);
            NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(RaiseTargetingPatch));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(RaiseRemoveTargetIfCantUse));
            NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(DeputyRemoveTargetOnMiss));
            NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(RaiseDeputyDetectTargetDeath));
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

        public static void RaiseRemoveTargetIfCantUse(ChatLogMessage chatLogMessage)
        {
            if (chatLogMessage.chatLogEntry.type == ChatType.GAME_MESSAGE)
            {
                ChatLogGameMessageEntry chatLog = chatLogMessage.chatLogEntry as ChatLogGameMessageEntry;
                if (chatLog.messageId == GameFeedbackMessage.REANIMATE_ROLE_CANT_BE_USED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE)
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
            NewPostfix(typeof(ProsecutionCinematicPlayer), nameof(ProsecutionCinematicPlayer.Init), nameof(SeerDetectProsecutorReveal));
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
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED || chatLog.messageId == GameFeedbackMessage.JAILED_TARGET || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || chatLog.messageId == GameFeedbackMessage.CANNOT_USE_ABILITY_WHILE_INSANE || chatLog.messageId == GameFeedbackMessage.WITCH_CONTROLLED || chatLog.messageId == GameFeedbackMessage.DID_NOT_PERFORM_NIGHT_ABILITY)
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

        // Faction Coroutines

        // Global Coroutines
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
                            Sprite = Utils.GetRoleSprite(Role.JAILOR), // Give Custom Sprite
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

        public static Role currentRole = Role.NONE;
        public static FactionType currentFaction = FactionType.NONE;
    }

    public class BToS2AchievementAdder
    {
        public static IEnumerator Default()
        {
            yield return new WaitForEndOfFrame();
            Debug.LogWarning("test -----------------");
            yield break;
        }

        public static Dictionary<Role, Func<IEnumerator>> btos2RoleCoroutines = new Dictionary<Role, Func<IEnumerator>>
        {
            {
                Role.NONE,
                Default
            }
        };

        public static Dictionary<FactionType, Func<IEnumerator>> btos2FactionCoroutines = new Dictionary<FactionType, Func<IEnumerator>>
        {
            {
                FactionType.NONE,
                Default
            }
        };
    }
}
