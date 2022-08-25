﻿namespace DaLion.Stardew.Arsenal.Framework.Events;

#region using directives

using Common.Events;
using StardewModdingAPI.Events;

#endregion using directives

[UsedImplicitly]
internal sealed class HostPeerConnectedEvent : PeerConnectedEvent
{
    /// <inheritdoc />
    public override bool IsEnabled => Context.IsMultiplayer && Context.IsMainPlayer;

    /// <summary>Construct an instance.</summary>
    /// <param name="manager">The <see cref=EventManager"/> instance that manages this event.</param>
    internal HostPeerConnectedEvent(EventManager manager)
        : base(manager) { }

    /// <inheritdoc />
    public override bool Enable() => false;

    /// <inheritdoc />
    protected override void OnPeerConnectedImpl(object? sender, PeerConnectedEventArgs e)
    {
        if (!e.Peer.IsSplitScreen || !e.Peer.ScreenID.HasValue) return;

        ModEntry.Events.EnableForScreen<ArsenalSaveLoadedEvent>(e.Peer.ScreenID.Value);
        if (ModEntry.Config.FaceMouseCursor)
            ModEntry.Events.EnableForScreen<ArsenalButtonPressedEvent>(e.Peer.ScreenID.Value);
    }
}