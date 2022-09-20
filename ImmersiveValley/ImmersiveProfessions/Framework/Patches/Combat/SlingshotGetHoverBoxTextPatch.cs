﻿namespace DaLion.Stardew.Professions.Framework.Patches.Combat;

#region using directives

using DaLion.Common;
using HarmonyLib;
using StardewValley.Tools;
using System;
using System.Reflection;

#endregion using directives

[UsedImplicitly]
internal sealed class SlingshotGetHoverBoxTextPatch : DaLion.Common.Harmony.HarmonyPatch
{
    /// <summary>Construct an instance.</summary>
    internal SlingshotGetHoverBoxTextPatch()
    {
        Target = RequireMethod<Slingshot>(nameof(Slingshot.getHoverBoxText));
    }

    #region harmony patches

    /// <summary>Adjust tooltip for equipping secondary ammo.</summary>
    [HarmonyPrefix]
    private static bool SlingshotGetHoverBoxTextPrefix(Slingshot __instance, ref string? __result, Item? hoveredItem)
    {
        try
        {
            switch (hoveredItem)
            {
                case SObject @object when __instance.canThisBeAttached(@object):
                    __result = Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14256", __instance.DisplayName, hoveredItem.DisplayName);
                    break;
                case null when __instance.attachments.Count > 0:
                    if (__instance.attachments[0] is not null)
                        __result =  Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14258", __instance.attachments[0].DisplayName);
                    else if (__instance.numAttachmentSlots.Value > 1 && __instance.attachments[1] is not null)
                        __result = Game1.content.LoadString("Strings\\StringsFromCSFiles:Slingshot.cs.14258", __instance.attachments[1].DisplayName);
                    break;
                default:
                    __result = null;
                    break;
            }

            return false; // don't run original logic
        }
        catch (Exception ex)
        {
            Log.E($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            return true; // default to original logic
        }
    }

    #endregion harmony patches
}