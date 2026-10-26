using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKS2BathNPCPatch.Patches;

namespace PKS2BathNPCPatch;

[BepInPlugin("faospark.pks2bathnpcpatch", "PKS2 Bath NPC Patch", "1.0.0")]
[BepInDependency("faospark.pkcore", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo("Loading PKS2BathNPCPatch (Suikoden II NPC Bathhouse Sprites & Castle Visitors Restoration)...");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        Harmony harmony = new Harmony("faospark.pks2bathnpcpatch");

        if (Config.EnableNPCBathVisitors.Value)
        {
            Log.LogInfo("Applying Suikoden II NPC Bathhouse Sprites & Castle Visitors patch...");
            NPCBathVisitorPatch.Initialize(harmony);
        }

        Log.LogInfo("PKS2BathNPCPatch loaded successfully!");
    }
}