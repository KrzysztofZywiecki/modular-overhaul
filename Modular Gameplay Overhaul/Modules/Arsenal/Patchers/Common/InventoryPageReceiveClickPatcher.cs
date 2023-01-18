﻿namespace DaLion.Overhaul.Modules.Arsenal.Patchers.Common;

#region using directives

using DaLion.Shared.Harmony;
using HarmonyLib;
using StardewValley.Menus;
using StardewValley.Tools;

#endregion using directives

[UsedImplicitly]
internal sealed class InventoryPageReceiveClickPatcher : HarmonyPatcher
{
    /// <summary>Initializes a new instance of the <see cref="InventoryPageReceiveClickPatcher"/> class.</summary>
    internal InventoryPageReceiveClickPatcher()
    {
        this.Prefix!.before = new[] { OverhaulModule.Tools.Namespace };
    }

    /// <inheritdoc />
    protected override void ApplyImpl(Harmony harmony)
    {
        this.Target = this.RequireMethod<InventoryPage>(nameof(InventoryPage.receiveLeftClick));
        base.ApplyImpl(harmony);

        this.Target = this.RequireMethod<InventoryPage>(nameof(InventoryPage.receiveRightClick));
        base.ApplyImpl(harmony);
    }

    /// <inheritdoc />
    protected override void UnapplyImpl(Harmony harmony)
    {
        this.Target = this.RequireMethod<InventoryPage>(nameof(InventoryPage.receiveLeftClick));
        base.UnapplyImpl(harmony);

        this.Target = this.RequireMethod<InventoryPage>(nameof(InventoryPage.receiveRightClick));
        base.UnapplyImpl(harmony);
    }

    #region harmony patches

    /// <summary>Toggle selectable tool.</summary>
    [HarmonyPrefix]
    [HarmonyBefore("DaLion.Overhaul.Modules.Tools")]
    private static bool InventoryPageReceiveClickPrefix(Item? ___hoveredItem, bool playSound)
    {
        if (!ArsenalModule.Config.EnableAutoSelection || !ArsenalModule.Config.ModKey.IsDown())
        {
            return true; // run original logic
        }

        if (___hoveredItem is not (Tool tool and (MeleeWeapon or Slingshot)))
        {
            return ToolsModule.IsEnabled;
        }

        if (tool is MeleeWeapon weapon && weapon.isScythe())
        {
            return ToolsModule.IsEnabled;
        }

        if (ArsenalModule.State.SelectableArsenal == tool)
        {
            ArsenalModule.State.SelectableArsenal = null;
            if (playSound)
            {
                Game1.playSound("smallSelect");
            }

            return false; // don't run original logic
        }

        ArsenalModule.State.SelectableArsenal = tool;
        if (playSound)
        {
            Game1.playSound("smallSelect");
        }

        return ToolsModule.IsEnabled;
    }

    #endregion harmony patches
}