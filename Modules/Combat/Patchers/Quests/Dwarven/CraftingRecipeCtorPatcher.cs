﻿namespace DaLion.Overhaul.Modules.Combat.Patchers.Quests.Dwarven;

#region using directives

using DaLion.Shared.Constants;
using DaLion.Shared.Harmony;
using HarmonyLib;

#endregion using directives

[UsedImplicitly]
internal sealed class CraftingRecipeCtorPatcher : HarmonyPatcher
{
    /// <summary>Initializes a new instance of the <see cref="CraftingRecipeCtorPatcher"/> class.</summary>
    internal CraftingRecipeCtorPatcher()
    {
        this.Target = this.RequireConstructor<CraftingRecipe>(typeof(string), typeof(bool));
    }

    #region harmony patches

    /// <summary>Remove Dragon Tooth from Warp Totem recipe.</summary>
    [HarmonyPostfix]
    private static void CraftingRecipeCtorPostfix(CraftingRecipe __instance)
    {
        if (CombatModule.Config.DwarvenLegacy && __instance.name == "Warp Totem: Island" &&
            __instance.recipeList.Remove(ObjectIds.DragonTooth))
        {
            __instance.recipeList[ObjectIds.RadioactiveOre] = 1;
        }
    }

    #endregion harmony patches
}
