﻿namespace DaLion.Stardew.Professions;

#region using directives

using Framework;
using Framework.TreasureHunts;
using Framework.Ultimates;
using StardewValley.Monsters;
using System.Collections.Generic;

#endregion using directives

internal class PlayerState
{
    private Ultimate? _registeredUltimate;

    internal Ultimate? RegisteredUltimate
    {
        get => _registeredUltimate;
        set
        {
            if (value is null) _registeredUltimate?.Dispose();
            _registeredUltimate = value;
        }
    }

    internal TreasureHunt ScavengerHunt { get; set; } = new ScavengerHunt();
    internal TreasureHunt ProspectorHunt { get; set; } = new ProspectorHunt();
    internal HudPointer Pointer { get; set; } = new();
    internal HashSet<GreenSlime> PipedSlimes { get; } = new();
    internal int[] AppliedPiperBuffs { get; } = new int[12];
    internal int BruteRageCounter { get; set; }
    internal int BruteKillCounter { get; set; }
    internal int SecondsSinceLastCombat { get; set; }
    internal int DemolitionistExcitedness { get; set; }
    internal int SpelunkerLadderStreak { get; set; }
    internal int SlimeContactTimer { get; set; }
    internal bool UsedDogStatueToday { get; set; }
    internal Queue<ISkill> SkillsToReset { get; } = new();
}