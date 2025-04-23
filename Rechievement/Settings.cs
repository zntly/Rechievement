using System;
using System.Collections.Generic;
using SML;
using UnityEngine;
using Server.Shared.Extensions;

namespace Rechievement
{
    // Token: 0x02000003 RID: 3
    [DynamicSettings]
    public class Settings
    {
        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000002 RID: 2 RVA: 0x00002058 File Offset: 0x0000025

        public ModSettings.CheckboxSetting BTOS2Achievements
        {
            get
            {
                return new ModSettings.CheckboxSetting
                {
                    Name = "BToS2 Extra Achievements",
                    Description = "Adds extra achievements that are obtainable in BToS2 only, alongside allowing you to get extra achievements (only ones added by Rechievement) in BToS2",
                    DefaultValue = true,
                    AvailableInGame = false,
                    Available = ModStates.IsEnabled("curtis.tuba.better.tos2"),
                    OnChanged = delegate (bool v)
                    {
                        Settings.SettingsCache.SetValue("BToS2 Extra Achievements", v);
                    }
                };
            }
        }

        public static Dictionary<string, bool> SettingsCache = new Dictionary<string, bool>()
        {
            {
                "BToS2 Extra Achievements",
                true
            }
        };
    }
}
