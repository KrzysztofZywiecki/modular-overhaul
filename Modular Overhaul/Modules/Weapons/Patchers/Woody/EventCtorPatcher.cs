﻿namespace DaLion.Overhaul.Modules.Weapons.Patchers.Woody;

#region using directives

using System.Linq;
using DaLion.Overhaul.Modules.Weapons.Integrations;
using DaLion.Shared.Harmony;
using HarmonyLib;
using StardewValley.Tools;

#endregion using directives

[UsedImplicitly]
internal sealed class EventCtorPatcher : HarmonyPatcher
{
    /// <summary>Initializes a new instance of the <see cref="EventCtorPatcher"/> class.</summary>
    internal EventCtorPatcher()
    {
        this.Target = this.RequireConstructor<Event>(typeof(string), typeof(int), typeof(Farmer));
    }

    #region harmony patches

    /// <summary>Immersively adjust Marlon's intro event.</summary>
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    private static void EventCtorPrefix(ref string eventString, int eventID)
    {
        if (!WeaponsModule.Config.WoodyReplacesRusty || eventID != 100162)
        {
            return;
        }

        var hasSword = Game1.player.Items.Any(item => item is MeleeWeapon weapon && !weapon.isScythe());
        eventString = StardewValleyExpandedIntegration.Instance?.IsLoaded == true
            ? hasSword
                ? I18n.Events_100162_Nosword_Sve()
                : I18n.Events_100162_Sword_Sve()
            : hasSword
                ? I18n.Events_100162_Nosword()
                : I18n.Events_100162_Sword();
    }

    #endregion harmony patches
}