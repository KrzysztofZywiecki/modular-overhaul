﻿namespace DaLion.Common.Events;

#region using directives

using StardewModdingAPI.Events;

#endregion using directives

/// <summary>Wrapper for <see cref="IGameLoopEvents.TimeChanged"/> allowing dynamic enabling / disabling.</summary>
internal abstract class TimeChangedEvent : ManagedEvent
{
    /// <summary>Construct an instance.</summary>
    /// <param name="manager">The <see cref="EventManager"/> instance that manages this event.</param>
    protected TimeChangedEvent(EventManager manager)
        : base(manager) { }

    /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event data.</param>
    internal void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (IsEnabled) OnTimeChangedImpl(sender, e);
    }

    /// <inheritdoc cref="OnTimeChanged" />
    protected abstract void OnTimeChangedImpl(object? sender, TimeChangedEventArgs e);
}