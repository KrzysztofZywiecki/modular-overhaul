﻿namespace DaLion.Stardew.Tools.Framework.Patches;

#region using directives

using HarmonyLib;
using StardewValley.Tools;

#endregion using directives

[UsedImplicitly]
internal sealed class PickaxeBeginUsingPatch : Common.Harmony.HarmonyPatch
{
    /// <summary>Construct an instance.</summary>
    internal PickaxeBeginUsingPatch()
    {
        Target = RequireMethod<Pickaxe>("beginUsing");
    }

    #region harmony patches

    /// <summary>Enable Pickaxe power level increase.</summary>
    [HarmonyPrefix]
    private static bool PickaxeBeginUsingPrefix(Tool __instance, Farmer who)
    {
        if (!ModEntry.Config.PickaxeConfig.EnableCharging ||
            ModEntry.Config.RequireModkey && !ModEntry.Config.Modkey.IsDown() ||
            __instance.UpgradeLevel < (int)ModEntry.Config.PickaxeConfig.RequiredUpgradeForCharging)
            return true; // run original logic

        who.Halt();
        __instance.Update(who.FacingDirection, 0, who);
        switch (who.FacingDirection)
        {
            case Game1.up:
                who.FarmerSprite.setCurrentFrame(176);
                __instance.Update(0, 0, who);
                break;

            case Game1.right:
                who.FarmerSprite.setCurrentFrame(168);
                __instance.Update(1, 0, who);
                break;

            case Game1.down:
                who.FarmerSprite.setCurrentFrame(160);
                __instance.Update(2, 0, who);
                break;

            case Game1.left:
                who.FarmerSprite.setCurrentFrame(184);
                __instance.Update(3, 0, who);
                break;
        }

        return false; // don't run original logic
    }

    #endregion harmony patches
}