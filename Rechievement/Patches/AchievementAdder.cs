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
                MethodInfo mOriginal = AccessTools.Method(typeof(RoleCardPanel), nameof(RoleCardPanel.HandleOnMyIdentityChanged));
                MethodInfo mPostfix = AccessTools.Method(typeof(AchievementAdder), nameof(IdentityChangePatch));
                IdentityPatch = Utils.harmonyInstance.Patch(mOriginal, null, new HarmonyMethod(mPostfix));
                if (AchievementPanelPatch.achievementPanel == null)
                    try
                    {
                        AchievementPanelPatch.achievementPanel = GameObject.Find("Hud/AchivementsElementsUI(Clone)/MainCanvasGroup/AchievementPanel").GetComponent<AchievementPanel>();
                    }
                    catch { }
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
            }
        }

        public static MethodInfo IdentityPatch;

        public static AchievementPanel achievementPanel;

        public static List<Coroutine> activeCoroutines = new List<Coroutine>();

        public static Dictionary<MethodInfo, Tuple<Type, string, Type[]>> harmonyPatches = new Dictionary<MethodInfo, Tuple<Type, string, Type[]>>();

        public static Dictionary<Role, Func<IEnumerator>> allRoleCoroutines = new Dictionary<Role, Func<IEnumerator>>
        {
            { Role.ADMIRER, Admirer },
            { Role.AMNESIAC, Amnesiac },
            { Role.BODYGUARD, Bodyguard }
        };

        public static Dictionary<FactionType, Func<IEnumerator>> allFactionCoroutines = new Dictionary<FactionType, Func<IEnumerator>>
        {
            {
                FactionType.NONE,
                Default
            }
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
                if (chatLog.messageId == GameFeedbackMessage.ROLEBLOCKED || chatLog.messageId == GameFeedbackMessage.JAILED_TARGET || chatLog.messageId == GameFeedbackMessage.ABILITY_FAILED_DUE_TO_AN_UNKNOWN_OBSTACLE || chatLog.messageId == GameFeedbackMessage.CANNOT_USE_ABILITY_WHILE_INSANE)
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
                MethodInfo proposalStartPatch = NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.HandleProposalMessage), nameof(ProposalMessagePatch));
                MethodInfo proposalEndPatch = NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.Cleanup), nameof(ProposalCleanupPatch));
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
                    MethodInfo factionWinPatch = NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AdmirerFactionWinPatch));
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
                        Sprite = Utils.GetRoleSprite(Role.ADMIRER),
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
            MethodInfo factionWinPatch = NewPostfix(typeof(FactionWinsStandardCinematicPlayer), nameof(FactionWinsStandardCinematicPlayer.Init), nameof(AmnesiacFactionWinPatch));
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
            MethodInfo targetingPatch = NewPostfix(typeof(TargetSelectionDecoder), nameof(TargetSelectionDecoder.Encode), nameof(GeneralTargetingPatch));
            MethodInfo impededPatch = NewPostfix(typeof(GameMessageDecoder), nameof(GameMessageDecoder.Encode), nameof(GeneralRemoveTargetIfImpededPatch));
            MethodInfo detectTargetDeath = NewPostfix(typeof(WhoDiedDecoder), nameof(WhoDiedDecoder.Encode), nameof(BodyguardDetectTargetDeath));
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
