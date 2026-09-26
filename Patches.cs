using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OdinStorageFix
{
    /// <summary>
    /// РџР°С‚С‡Рё OdinStorageFix v1.0.4
    /// РќРµ С‚СЂРѕРіР°РµРј OdinStorage.StorageTerminalUI С‡РµСЂРµР· typeof() вЂ” РІС‹Р·С‹РІР°Р»Рѕ РЅР°С‚РёРІРЅС‹Р№ РєСЂСЌС€ Mono.
    /// OdinStorage.dll РїР°С‚С‡РёС‚СЃСЏ РЅР°РїСЂСЏРјСѓСЋ С‡РµСЂРµР· ValheimEffectListCompat (Cecil IL patcher).
    /// Р—РґРµСЃСЊ С‚РѕР»СЊРєРѕ Р±РµР·РѕРїР°СЃРЅС‹Рµ РїР°С‚С‡Рё: Player, Piece, ZNetScene, OdinStorage.Plugin.
    /// </summary>
    public static class Patches
    {
        private static FieldInfo _hotkeyField;
        private static FieldInfo _devToolsField;
        private static FieldInfo _spawnKeyField;
        private static FieldInfo _clearKeyField;

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
                    // Р’С‹Р·С‹РІР°РµРј С‡РµСЂРµР· СЂРµС„Р»РµРєСЃРёСЋ вЂ” Р±РµР· typeof(StorageTerminalUI)
                    var uiType = AccessTools.TypeByName("OdinStorage.StorageTerminalUI");
                    if (uiType == null) return true;

                    var createMethod = AccessTools.Method(uiType, "Create");
                    var openMethod = AccessTools.Method(uiType, "Open");
                    if (createMethod == null || openMethod == null) return true;

                    createMethod.Invoke(null, null);
                    openMethod.Invoke(null, new object[] { __instance.transform.position });
                    return false;
                }
                catch (Exception ex)
                {
                    OdinStorageFixPlugin.Log.LogWarning($"[OdinStorageFix] PlayerInteract fallback: {ex.Message}");
                    return true;
                }
            }
        }

        // ===================== Piece.Awake вЂ” РїСЂРёРєСЂРµРїР»СЏРµРј FixedStorageTerminal =====================

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

        // ===================== ZNetScene.Awake вЂ” РїСЂРёРєСЂРµРїР»СЏРµРј Рє РїСЂРµС„Р°Р±Сѓ =====================

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

        // ===================== OdinStorage.Plugin.Update вЂ” РїРµСЂРµС…РІР°С‚ С…РѕС‚РєРµСЏ =====================

        [HarmonyPatch(typeof(OdinStorage.Plugin), "Update")]
        public static class PluginUpdatePatch
        {
            public static bool Prefix(OdinStorage.Plugin __instance)
            {
                if (Player.m_localPlayer == null)
                    return false;

                if (_hotkeyField == null)
                {
                    _hotkeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "Hotkey");
                    _devToolsField = AccessTools.Field(typeof(OdinStorage.Plugin), "DevTools");
                    _spawnKeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "SpawnKey");
                    _clearKeyField = AccessTools.Field(typeof(OdinStorage.Plugin), "ClearKey");
                }

                try
                {
                    var uiType = AccessTools.TypeByName("OdinStorage.StorageTerminalUI");
                    if (uiType == null) return false;

                    var isOpenProp = AccessTools.Property(uiType, "IsOpen");
                    var createMethod = AccessTools.Method(uiType, "Create");
                    var openMethod = AccessTools.Method(uiType, "Open");
                    var closeMethod = AccessTools.Method(uiType, "Close");

                    bool isOpen = isOpenProp != null && (bool)isOpenProp.GetValue(null);

                    var hotkeyEntry = _hotkeyField?.GetValue(null) as ConfigEntry<KeyboardShortcut>;
                    if (hotkeyEntry != null && hotkeyEntry.Value.IsDown())
                    {
                        if (isOpen)
                            closeMethod?.Invoke(null, null);
                        else
                        {
                            createMethod?.Invoke(null, null);
                            openMethod?.Invoke(null, new object[] { Player.m_localPlayer.transform.position });
                        }
                        return false;
                    }

                    if (isOpen && Input.GetKeyDown(KeyCode.Escape))
                    {
                        closeMethod?.Invoke(null, null);
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