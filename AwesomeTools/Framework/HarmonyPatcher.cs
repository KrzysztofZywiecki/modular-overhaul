﻿using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TheLion.Stardew.Common.Classes;
using TheLion.Stardew.Tools.Framework.Events;

namespace TheLion.Stardew.Tools.Framework;

/// <summary>Patches the game code to implement modded tool behavior.</summary>
[UsedImplicitly]
internal static class HarmonyPatcher
{
    private static readonly List<int> AxeAffectedTilesRadii = ModEntry.Config.AxeConfig.RadiusAtEachPowerLevel;
    private static readonly List<int> PickaxeAffectedTilesRadii = ModEntry.Config.PickaxeConfig.RadiusAtEachPowerLevel;

    // do shockwave
    [HarmonyPatch(typeof(Tool), nameof(Tool.endUsing))]
    internal class ToolEndUsingPatch
    {
        [HarmonyPostfix]
        protected static void Postfix(Farmer who)
        {
            Tool tool = who.CurrentTool;
            if (who.toolPower <= 0 || (tool is not Axe || !ModEntry.AxeFx.Config.EnableAxeCharging) &&
                (tool is not Pickaxe || !ModEntry.PickaxeFx.Config.EnablePickaxeCharging)) return;

            new UpdateTickedEvent().Hook();
        }
    }

    // enable Axe power level increase
    [HarmonyPatch(typeof(Axe), "beginUsing")]
    internal class AxeBeginUsingPatch
    {
        [HarmonyPrefix]
        protected static bool Prefix(Tool __instance, Farmer who)
        {
            if (!ModEntry.Config.AxeConfig.EnableAxeCharging || !Utility.ShouldCharge() ||
                __instance.UpgradeLevel < ModEntry.Config.AxeConfig.RequiredUpgradeForCharging)
                return true; // run original logic

            who.Halt();
            __instance.Update(who.FacingDirection, 0, who);
            switch (who.FacingDirection)
            {
                case 0:
                    who.FarmerSprite.setCurrentFrame(176);
                    __instance.Update(0, 0, who);
                    break;

                case 1:
                    who.FarmerSprite.setCurrentFrame(168);
                    __instance.Update(1, 0, who);
                    break;

                case 2:
                    who.FarmerSprite.setCurrentFrame(160);
                    __instance.Update(2, 0, who);
                    break;

                case 3:
                    who.FarmerSprite.setCurrentFrame(184);
                    __instance.Update(3, 0, who);
                    break;
            }

            return false; // don't run original logic
        }
    }

    // enable Pickaxe power level increase
    [HarmonyPatch(typeof(Pickaxe), "beginUsing")]
    internal class PickaxeBeginUsingPatch
    {
        [HarmonyPrefix]
        protected static bool Prefix(Tool __instance, Farmer who)
        {
            if (!ModEntry.Config.PickaxeConfig.EnablePickaxeCharging || !Utility.ShouldCharge() ||
                __instance.UpgradeLevel < ModEntry.Config.PickaxeConfig.RequiredUpgradeForCharging)
                return true; // run original logic

            who.Halt();
            __instance.Update(who.FacingDirection, 0, who);
            switch (who.FacingDirection)
            {
                case 0: // up
                    who.FarmerSprite.setCurrentFrame(176);
                    __instance.Update(0, 0, who);
                    break;

                case 1: // right
                    who.FarmerSprite.setCurrentFrame(168);
                    __instance.Update(1, 0, who);
                    break;

                case 2: // down
                    who.FarmerSprite.setCurrentFrame(160);
                    __instance.Update(2, 0, who);
                    break;

                case 3: // left
                    who.FarmerSprite.setCurrentFrame(184);
                    __instance.Update(3, 0, who);
                    break;
            }

            return false; // don't run original logic
        }
    }

    // allow first two power levels on Pickaxe
    [HarmonyPatch(typeof(Farmer), "toolPowerIncrease")]
    internal class FarmerToolPowerIncreasePatch
    {
        [HarmonyTranspiler]
        protected static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var l = instructions.ToList();
            for (int i = 0; i < l.Count; ++i)
            {
                if (l[i].opcode != OpCodes.Isinst ||
                    l[i].operand?.ToString() != "StardewValley.Tools.Pickaxe") continue;
                
                // inject branch over toolPower += 2
                l.Insert(i - 2, new CodeInstruction(OpCodes.Br_S, l[i + 1].operand));
                break;
            }

            return l.AsEnumerable();
        }
    }

    // set affected tiles for Axe and Pickaxe power levels
    [HarmonyPatch(typeof(Tool), "tilesAffected")]
    internal class ToolTileseAffectedPatch
    {
        [HarmonyPostfix]
        protected static void Postfix(Tool __instance, ref List<Vector2> __result, Vector2 tileLocation, int power)
        {
            if (__instance.UpgradeLevel < Tool.copper)
                return;

            if (__instance is not (Axe or Pickaxe)) return;
            
            __result.Clear();
            int radius = __instance is Axe
                ? AxeAffectedTilesRadii[Math.Min(power - 2, 4)]
                : PickaxeAffectedTilesRadii[Math.Min(power - 2, 4)];
            if (radius == 0)
                return;

            var circle = new CircleTileGrid(tileLocation, radius);
            __result.AddRange(circle.Tiles);
        }
    }

    // hide affected tiles overlay for Axe or Pickaxe
    [HarmonyPatch(typeof(Tool), "draw")]
    internal class ToolDrawPatch
    {
        [HarmonyPrefix]
        protected static bool Prefix(Tool __instance)
        {
            return (__instance is not Axe || ModEntry.Config.AxeConfig.ShowAxeAffectedTiles) &&
                   (__instance is not Pickaxe || ModEntry.Config.PickaxeConfig.ShowPickaxeAffectedTiles);
        }
    }
}