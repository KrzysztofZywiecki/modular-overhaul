﻿namespace DaLion.Stardew.Alchemy.Framework.Events.GameLoop;

#region using directives

using JetBrains.Annotations;
using StardewModdingAPI.Events;

#endregion using directives

[UsedImplicitly]
internal class StaticReturnedToTitleEvent : ReturnedToTitleEvent
{
    /// <summary>Construct an instance.</summary>
    internal StaticReturnedToTitleEvent()
    {
        Enable();
    }

    /// <inheritdoc />
    protected override void OnReturnedToTitleImpl(object sender, ReturnedToTitleEventArgs e)
    {
        // disable events
        EventManager.DisableAllForLocalPlayer();

        // reset mod state
        ModEntry.PlayerState = new();
    }
}