﻿namespace DaLion.Overhaul.Modules.Rings.Patchers;

#region using directives

using DaLion.Overhaul.Modules.Rings.VirtualProperties;
using DaLion.Shared.Harmony;
using HarmonyLib;
using StardewValley;
using StardewValley.Tools;

#endregion using directives

[UsedImplicitly]
internal sealed class ToolActionWhenStopBeingHeldPatcher : HarmonyPatcher
{
    /// <summary>Initializes a new instance of the <see cref="ToolActionWhenStopBeingHeldPatcher"/> class.</summary>
    internal ToolActionWhenStopBeingHeldPatcher()
    {
        this.Target = this.RequireMethod<Tool>(nameof(Tool.actionWhenStopBeingHeld));
    }

    #region harmony patches

    /// <summary>Reset applied arsenal resonances.</summary>
    [HarmonyPostfix]
    private static void ToolActionWhenStopBeingHeldPostfix(Tool __instance)
    {
        if ((__instance is MeleeWeapon && !WeaponsModule.ShouldEnable) ||
            (__instance is Slingshot && !SlingshotsModule.ShouldEnable))
        {
            return;
        }

        __instance.UnsetAllResonatingChords();
    }

    #endregion harmony patches
}