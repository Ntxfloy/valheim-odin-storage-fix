using UnityEngine;

namespace OdinStorageFix
{
    /// <summary>
    /// Исправленный компонент терминала хранилища, совместимый с современными версиями Valheim (Ashlands / 1.0+).
    /// Релизует недостающий метод GetHoverOffset(), благодаря чему Unity без ошибок инстанциирует скрипт.
    /// </summary>
    public class FixedStorageTerminal : MonoBehaviour, Interactable, Hoverable
    {
        public string GetHoverName()
        {
            if (Localization.instance != null)
            {
                string text = Localization.instance.Localize("$piece_odinterminal_name");
                if (!string.IsNullOrEmpty(text) && !text.StartsWith("$"))
                {
                    return text;
                }
            }
            return "Odin Terminal";
        }

        public string GetHoverText()
        {
            if (Localization.instance != null)
            {
                return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Open Storage");
            }
            return "[E] Open Storage";
        }

        public float GetHoverOffset()
        {
            return 0f;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }

            OdinStorage.StorageTerminalUI.Open(transform.position);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
