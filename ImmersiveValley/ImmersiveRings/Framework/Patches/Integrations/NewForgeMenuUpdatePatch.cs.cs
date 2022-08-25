﻿namespace DaLion.Stardew.Rings.Framework.Patches;

#region using directives

using Common;
using Common.Attributes;
using Common.Extensions.Reflection;
using Common.Harmony;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

#endregion using directives

[UsedImplicitly, RequiresMod("spacechase0.SpaceCore")]
internal sealed class NewForgeMenuUpdatePatch : Common.Harmony.HarmonyPatch
{
    /// <summary>Construct an instance.</summary>
    internal NewForgeMenuUpdatePatch()
    {
        Target = "SpaceCore.Interface.NewForgeMenu".ToType().RequireMethod("update", new[] { typeof(GameTime) });
    }

    #region harmony patches

    /// <summary>Modify unforge behavior of combined iridium band.</summary>
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction>? ForgeMenuUpdateTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator, MethodBase original)
    {
        var helper = new ILHelper(original, instructions);

        /// Injected: if (ModEntry.Config.TheOneIridiumBand && ring.ParentSheetIndex == Constants.IRIDIUM_BAND_INDEX_I)
        ///               UnforgeIridiumBand(ring);
        ///           else ...
        /// After: if (leftIngredientSpot.item is CombinedRing ring)

        var vanillaUnforge = generator.DefineLabel();
        try
        {
            helper
                .FindFirst(
                    new CodeInstruction(OpCodes.Stloc_S, helper.Locals[15]) // local 15 = CombinedRing ring
                )
                .AdvanceUntil(
                    new CodeInstruction(OpCodes.Brfalse_S)
                )
                .GetOperand(out var resumeExecution)
                .Advance()
                .AddLabels(vanillaUnforge)
                .InsertInstructions(
                    new CodeInstruction(OpCodes.Call, typeof(ModEntry).RequirePropertyGetter(nameof(ModEntry.Config))),
                    new CodeInstruction(OpCodes.Call,
                        typeof(ModConfig).RequirePropertyGetter(nameof(ModConfig.TheOneIridiumBand))),
                    new CodeInstruction(OpCodes.Brfalse_S, vanillaUnforge),
                    new CodeInstruction(OpCodes.Ldloc_S, helper.Locals[15]),
                    new CodeInstruction(OpCodes.Call,
                        typeof(Item).RequirePropertyGetter(nameof(Item.ParentSheetIndex))),
                    new CodeInstruction(OpCodes.Ldc_I4, Constants.IRIDIUM_BAND_INDEX_I),
                    new CodeInstruction(OpCodes.Bne_Un_S, vanillaUnforge),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_3, helper.Locals[15]),
                    new CodeInstruction(OpCodes.Call,
                        typeof(ForgeMenuUpdatePatch).RequireMethod(nameof(UnforgeIridiumBand))),
                    new CodeInstruction(OpCodes.Br_S, resumeExecution)
                );
        }
        catch (Exception ex)
        {
            Log.E("Immersive Rings failed modifying unforge behavior of combined iridium band." +
                  "\n—-- Do NOT report this to SpaceCore's author. ---" +
                  $"\nHelper returned {ex}");
            return null;
        }

        return helper.Flush();
    }

    #endregion harmony patches

    #region injected subroutines

    private static void UnforgeIridiumBand(ForgeMenu menu, CombinedRing iridiumBand)
    {
        var combinedRings = new List<Ring>(iridiumBand.combinedRings);
        iridiumBand.combinedRings.Clear();
        foreach (var ring in combinedRings)
        {
            var gemstone = Utils.GemstoneByRing[ring.ParentSheetIndex];
            StardewValley.Utility.CollectOrDrop(new SObject(gemstone, 1));
            StardewValley.Utility.CollectOrDrop(new SObject(848, 5));
        }
        StardewValley.Utility.CollectOrDrop(iridiumBand);
        menu.leftIngredientSpot.item = null;
        Game1.playSound("coin");
    }

    #endregion injected subroutines
}