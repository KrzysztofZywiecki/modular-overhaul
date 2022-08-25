﻿namespace DaLion.Stardew.Tweex.Framework.Patches;

#region using directives

using Common;
using Common.Attributes;
using Common.Extensions;
using Common.Extensions.Reflection;
using Common.Harmony;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

#endregion using directives

[UsedImplicitly, RequiresMod("Pathoschild.Automate")]
internal sealed class GenericObjectMachinePatches : Common.Harmony.HarmonyPatch
{
    /// <summary>Construct an instance.</summary>
    internal GenericObjectMachinePatches()
    {
        Transpiler!.before = new[] { "DaLion.ImmersiveProfessions" };
    }

    /// <inheritdoc />
    protected override void ApplyImpl(Harmony harmony)
    {
        foreach (var target in TargetMethods())
        {
            Target = target;
            base.ApplyImpl(harmony);
        }
    }

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return "Pathoschild.Stardew.Automate.Framework.GenericObjectMachine`1".ToType()
            .MakeGenericType(typeof(SObject))
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .First(m => m.Name == "GenericPullRecipe" && m.GetParameters().Length == 3);
        yield return "Pathoschild.Stardew.Automate.Framework.Machines.Objects.CheesePressMachine".ToType()
            .RequireMethod("SetInput");
    }

    #region harmony patches

    /// <summary>Replaces large egg output quality with quantity + add flower memory to automated kegs.</summary>
    [HarmonyTranspiler]
    [HarmonyBefore("DaLion.ImmersiveProfessions")]
    private static IEnumerable<CodeInstruction>? GenericObjectMachineTranspiler(
        IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var helper = new ILHelper(original, instructions);

        /// Injected: GenericPullRecipeSubroutine(this, consumable)
        /// Before: return true;

        try
        {
            helper
                .FindLast(
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ret)
                )
                .InsertInstructions(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        "Pathoschild.Stardew.Automate.Framework.BaseMachine`1".ToType().MakeGenericType(typeof(SObject))
                            .RequirePropertyGetter("Machine")),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        "Pathoschild.Stardew.Automate.IConsumable".ToType().RequirePropertyGetter("Sample")),
                    new CodeInstruction(OpCodes.Call,
                        typeof(GenericObjectMachinePatches).RequireMethod(
                            original.DeclaringType!.Name.Contains("CheesePress")
                                ? nameof(CheesePressMachineSubroutine)
                                : nameof(GenericMachineSubroutine)))
                );
        }
        catch (Exception ex)
        {
            Log.E("Immersive Tweaks failed while patching modded Artisan behavior to generic Automate machines." +
                  "\n—-- Do NOT report this to Automate's author. ---" +
                  $"\nHelper returned {ex}");
            return null;
        }

        return helper.Flush();
    }

    #endregion harmony patches

    #region injected subroutines

    private static void GenericMachineSubroutine(SObject machine, Item sample)
    {
        if (!machine.name.IsIn("Keg", "Cheese Press", "Mayonnaise Machine") || machine.heldObject.Value is null ||
            sample is not SObject input) return;

        var output = machine.heldObject.Value;
        switch (machine.name)
        {
            case "Keg" when input.ParentSheetIndex == 340 && input.preservedParentSheetIndex.Value > 0 &&
                            ModEntry.Config.KegsRememberHoneyFlower:
                output.name = input.name.Split(" Honey")[0] + " Mead";
                output.honeyType.Value = (SObject.HoneyType)input.preservedParentSheetIndex.Value;
                output.preservedParentSheetIndex.Value = input.preservedParentSheetIndex.Value;
                output.Price = input.Price * 2;
                break;
            case "Cheese Press" or "Mayonnaise Machine" when
                ModEntry.Config.LargeProducsYieldQuantityOverQuality:
                {
                    if (input.Name.ContainsAnyOf("Large", "L."))
                    {
                        output.Stack = 2;
                        output.Quality = SObject.lowQuality;
                    }
                    else if (machine.name == "Mayonnaise Machine")
                    {
                        switch (input.ParentSheetIndex)
                        {
                            // ostrich mayonnaise keeps giving x10 output but doesn't respect input quality without Artisan
                            case 289 when !ModEntry.ModHelper.ModRegistry.IsLoaded(
                                "ughitsmegan.ostrichmayoForProducerFrameworkMod"):
                                output.Quality = SObject.lowQuality;
                                break;
                            // golden mayonnaise keeps giving gives single output but keeps golden quality
                            case 928 when !ModEntry.ModHelper.ModRegistry.IsLoaded(
                                "ughitsmegan.goldenmayoForProducerFrameworkMod"):
                                output.Stack = 1;
                                break;
                        }
                    }

                    break;
                }
        }
    }

    private static void CheesePressMachineSubroutine(SObject machine, Item sample)
    {
        if (!ModEntry.Config.LargeProducsYieldQuantityOverQuality || machine.heldObject.Value is null ||
            sample is not SObject input || !input.Name.ContainsAnyOf("Large", "L.")) return;

        var output = machine.heldObject.Value;
        output.Stack = 2;
        output.Quality = SObject.lowQuality;
    }

    #endregion injected subroutines
}