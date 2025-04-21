using System;
using BetterTOS2;
using Home.Shared;
using Server.Shared.State;
using Services;
using SML;
using UnityEngine;

namespace Rechievement
{
    // Token: 0x02000006 RID: 6
    public static class Utils
    {
        // Token: 0x06000028 RID: 40
        public static bool BTOS2Exists()
        {
            return ModStates.IsEnabled("curtis.tuba.better.tos2");
        }

        // Token: 0x06000029 RID: 41
        public static bool IsBTOS2()
        {
            bool result;
            try
            {
                result = Utils.IsBTOS2Bypass();
            }
            catch
            {
                result = false;
            }
            return result;
        }

        // Token: 0x0600002A RID: 42
        private static bool IsBTOS2Bypass()
        {
            return Utils.BTOS2Exists() && BTOSInfo.IS_MODDED;
        }

        // Token: 0x0600002B RID: 43
        public static Sprite GetRoleSprite(Role role) => Service.Game.Roles.GetRoleInfo(role).sprite;
    }
}
