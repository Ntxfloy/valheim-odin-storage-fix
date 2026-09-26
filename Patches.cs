using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OdinStorageFix
{
    /// <summary>
    /// OdinStorageFix v1.0.5
    /// Fixed FPS collapse caused by uncached AccessTools.TypeByName calls in Update().
    /// Calls OdinStorage.StorageTerminalUI directly with zero reflection overhead.
    /// </summary>
    public static class Patches
    {
        private static readonly FieldInfo _hotkeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "Hotkey");
        private static readonly FieldInfo _devToolsField = AccessTools.Field(typeof(OdinStorage.Plugin), "DevTools");
        private static readonly FieldInfo _spawnKeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "SpawnKey");
        private static readonly FieldInfo _clearKeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "ClearKey");

        // ===================== Player interact patch =====================

        [HarmonyPatch(typeof(Player), "Interact")]
        public static class PlayerInteractPatch
        {
            public static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
            {
                if (hold) return true;
                if (go == null) return true;

                var piece = go.GetComponentInParent<Piece>();
                if (piece == null) return true;
                string pieceName = piece.name ?? "";
                if (!pieceName.StartsWith("OdinTerminal", StringComparison.OrdinalIgnoreCase))
                    return true;

                try
                {
                    OdinStorage.StorageTerminalUI.Create();
                    OdinStorage.StorageTerminalUI.Open(__instance.transform.position);
                    return false;
                }
                catch (Exception ex)
                {
                    OdinStorageFixPlugin.Log.LogWarning($"[OdinStorageFix] PlayerInteract error: {ex.Message}");
                    return true;
                }
            }
        }

        // ===================== Piece.Awake - Attach FixedStorageTerminal =====================

        [HarmonyPatch(typeof(Piece), "Awake")]
        public static class PieceAwakePatch
        {
            public static void Postfix(Piece __instance)
            {
                if (__instance == null) return;
                string name = __instance.name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("OdinTerminal", StringComparison.OrdinalIgnoreCase))
                {
                    if (__instance.GetComponent<FixedStorageTerminal>() == null)
                    {
                        __instance.gameObject.AddComponent<FixedStorageTerminal>();
                        OdinStorageFixPlugin.Log.LogInfo($"[OdinStorageFix] Attached FixedStorageTerminal to world piece: {name}");
                    }
                }
            }
        }

        // ===================== ZNetScene.Awake - Attach to prefab =====================

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwakePatch
        {
            public static void Postfix(ZNetScene __instance)
            {
                if (__instance == null) return;
                GameObject prefab = __instance.GetPrefab("OdinTerminal");
                if (prefab != null && prefab.GetComponent<FixedStorageTerminal>() == null)
                {
                    prefab.AddComponent<FixedStorageTerminal>();
                    OdinStorageFixPlugin.Log.LogInfo("[OdinStorageFix] Attached FixedStorageTerminal to prefab 'OdinTerminal'.");
                }
            }
        }

        // ===================== OdinStorage.Plugin.Update - Direct call, zero reflection overhead =====================

        [HarmonyPatch(typeof(OdinStorage.Plugin), "Update")]
        public static class PluginUpdatePatch
        {
            public static bool Prefix(OdinStorage.Plugin __instance)
            {
                if (Player.m_localPlayer == null)
                    return false;

                try
                {
                    bool isOpen = OdinStorage.StorageTerminalUI.IsOpen;

                    var hotkeyEntry = _hotkeyField?.GetValue(null) as ConfigEntry<KeyboardShortcut>;
                    if (hotkeyEntry != null && hotkeyEntry.Value.IsDown())
                    {
                        if (isOpen)
                            OdinStorage.StorageTerminalUI.Close();
                        else
                        {
                            OdinStorage.StorageTerminalUI.Create();
                            OdinStorage.StorageTerminalUI.Open(Player.m_localPlayer.transform.position);
                        }
                        return false;
                    }

                    if (isOpen && Input.GetKeyDown(KeyCode.Escape))
                    {
                        OdinStorage.StorageTerminalUI.Close();
                        return false;
                    }

                    var devToolsEntry = _devToolsField?.GetValue(null) as ConfigEntry<bool>;
                    if (devToolsEntry != null && devToolsEntry.Value)
                    {
                        var spawnKey = _spawnKeyField?.GetValue(null) as ConfigEntry<KeyboardShortcut>;
                        if (spawnKey != null && spawnKey.Value.IsDown())
                            OdinStorage.DevTools.SpawnTestStorage();

                        var clearKey = _clearKeyField?.GetValue(null) as ConfigEntry<KeyboardShortcut>;
                        if (clearKey != null && clearKey.Value.IsDown())
                            OdinStorage.DevTools.ClearTestStorage();
                    }
                }
                catch (Exception ex)
                {
                    OdinStorageFixPlugin.Log.LogWarning($"[OdinStorageFix] PluginUpdatePatch error: {ex.Message}");
                }

                return false;
            }
        }
    }
}
