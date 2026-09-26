using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace OdinStorageFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.nicolai.odinstorage", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public class OdinStorageFixPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ntxfloy.odinstoragefix";
        public const string PluginName = "OdinStorage Fix";
        public const string PluginVersion = "1.0.5";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);

            SafePatch(typeof(Patches.PlayerInteractPatch));
            SafePatch(typeof(Patches.PieceAwakePatch));
            SafePatch(typeof(Patches.ZNetSceneAwakePatch));
            SafePatch(typeof(Patches.PluginUpdatePatch));

            Log.LogInfo($"{PluginName} v{PluginVersion} initialized.");
        }

        private void SafePatch(Type patchClass)
        {
            try
            {
                _harmony.CreateClassProcessor(patchClass).Patch();
                Log.LogInfo($"[OdinStorageFix] Patched: {patchClass.Name}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[OdinStorageFix] Patch skipped ({patchClass.Name}): {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
