using BepInEx.Configuration;

namespace PKS2BathNPCPatch;

public sealed class ModConfiguration
{
    private readonly ConfigFile _config;

    public ConfigEntry<bool> EnableNPCBathVisitors { get; private set; }
    public ConfigEntry<bool> EnableEightBatherCapacity { get; private set; }
    public ConfigEntry<bool> DetailedLogs { get; private set; }

    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        EnableNPCBathVisitors = _config.Bind(
            "Suikoden 2 NPC Bath Visitors",
            "EnableNPCBathVisitors",
            true,
            "Restores the cut 1998 Suikoden II feature allowing recruited castle companions to visit and take a bath with you, automatically filling unoccupied bath slots."
        );

        EnableEightBatherCapacity = _config.Bind(
            "Suikoden 2 NPC Bath Visitors",
            "EnableEightBatherCapacity",
            true,
            "Unlocks the bathhouse internal 8-character capacity (up to 8 bathers across 12 tub positions), allowing 2 castle companions to join a full 6-man battle party."
        );

        DetailedLogs = _config.Bind(
            "Diagnostics",
            "DetailedLogs",
            false,
            "Enables verbose diagnostics in LogOutput.log for character tub assignments, slot placement, and party restoration."
        );
    }
}