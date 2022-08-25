﻿namespace DaLion.Common.Events;

#region using directives

using StardewModdingAPI.Events;

#endregion region using directives

/// <summary>Wrapper for <see cref="IWorldEvents.LocationListChanged"/> allowing dynamic enabling / disabling.</summary>
internal abstract class LocationListChangedEvent : ManagedEvent
{
    /// <summary>Construct an instance.</summary>
    /// <param name="manager">The <see cref="EventManager"/> instance that manages this event.</param>
    protected LocationListChangedEvent(EventManager manager)
        : base(manager) { }

    /// <inheritdoc cref="IWorldEvents.LocationListChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    internal void OnLocationListChanged(object? sender, LocationListChangedEventArgs e)
    {
        if (IsEnabled) OnLocationListChangedImpl(sender, e);
    }

    /// <inheritdoc cref="OnLocationListChanged" />
    protected abstract void OnLocationListChangedImpl(object? sender, LocationListChangedEventArgs e);
}