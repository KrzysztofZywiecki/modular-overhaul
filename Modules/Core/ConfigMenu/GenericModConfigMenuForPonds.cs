﻿namespace DaLion.Overhaul.Modules.Core.ConfigMenu;

/// <summary>Constructs the GenericModConfigMenu integration.</summary>
internal sealed partial class GenericModConfigMenu
{
    /// <summary>Register the config menu for PNDS.</summary>
    private void AddPondOptions()
    {
        this
            .AddPage(OverhaulModule.Ponds.Namespace, I18n.Gmcm_Pnds_Heading)

            .AddNumberField(
                I18n.Gmcm_Pnds_DaysUntilAlgaeSpawn_Title,
                I18n.Gmcm_Pnds_DaysUntilAlgaeSpawn_Desc,
                config => (int)config.Ponds.DaysUntilAlgaeSpawn,
                (config, value) => config.Ponds.DaysUntilAlgaeSpawn = (uint)value,
                1,
                5)
            .AddNumberField(
                I18n.Gmcm_Pnds_RoeProductionChanceMultiplier_Title,
                I18n.Gmcm_Pnds_RoeProductionChanceMultiplier_Desc,
                config => config.Ponds.RoeProductionChanceMultiplier,
                (config, value) => config.Ponds.RoeProductionChanceMultiplier = value,
                0.1f,
                2f)
            .AddCheckbox(
                I18n.Gmcm_Pnds_RoeAlwaysFishQuality_Title,
                I18n.Gmcm_Pnds_RoeAlwaysFishQuality_Desc,
                config => config.Ponds.RoeAlwaysFishQuality,
                (config, value) => config.Ponds.RoeAlwaysFishQuality = value);
    }
}
