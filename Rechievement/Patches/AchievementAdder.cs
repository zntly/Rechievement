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
                Func<IEnumerator> roleCoroutine;
                Func<IEnumerator> factionCoroutine;
                if (allRoleCoroutines.TryGetValue(role, out roleCoroutine))
                    StartCoroutine(roleCoroutine.Invoke());
                if (allFactionCoroutines.TryGetValue(faction, out factionCoroutine))
                    StartCoroutine(factionCoroutine.Invoke());
            } else if (gameInfo.gamePhase == GamePhase.NONE)
            {
                ClearCoroutines();
                ClearPatches();
                necessities.Clear();
            }
        }

        public static AchievementPanel achievementPanel;

        public static List<Coroutine> activeCoroutines = new List<Coroutine>();

        public static Dictionary<MethodInfo, Tuple<Type, string, Type[]>> harmonyPatches = new Dictionary<MethodInfo, Tuple<Type, string, Type[]>>();

        public static Dictionary<Role, Func<IEnumerator>> allRoleCoroutines = new Dictionary<Role, Func<IEnumerator>>
        {
            {
                Role.ADMIRER,
                Admirer
            }
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

        public static void StopCoroutine(Coroutine coroutine) {
            try
            {
                ApplicationController.ApplicationContext.StopCoroutine(coroutine);
            }
            catch { }
            if (activeCoroutines.Contains(coroutine))
                activeCoroutines.Remove(coroutine);
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
                harmonyPatches.Remove(newPatch);
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
        // Role Coroutines
        public static IEnumerator Admirer()
        {
            MethodInfo proposalStartPatch = NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.HandleProposalMessage), nameof(ProposalMessagePatch));
            MethodInfo proposalEndPatch = NewPostfix(typeof(ProposalCinematicPlayer), nameof(ProposalCinematicPlayer.Cleanup), nameof(ProposalCleanupPatch));
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
            if (__instance.cinematicData.winningFaction == FactionType.TOWN)
                RechievementData.allRechievements.GetValue("Written in the Stars", new RechievementData
                {
                    Name = "Written in the Stars",
                    Sprite = Utils.GetRoleSprite(Role.ADMIRER),
                    Description = "Propose to a Doomsayer and win the game",
                    Vanilla = true,
                    BToS2 = false
                }).ShowRechievement();
        }
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
