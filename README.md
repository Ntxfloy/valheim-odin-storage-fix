# OdinStorage — Modern Valheim Fix (Ashlands & 1.0+)

A lightweight compatibility and usability patch for **[OdinStorage](https://thunderstore.io/c/valheim/p/Koehlerworks/OdinStorage/) by Koehlerworks**.

This mod **does not replace or re-host** the original mod files. It acts as a clean Harmony patch on top of `Koehlerworks-OdinStorage`.

---

## 🛠 What This Fix Does

1. **Restores Physical Terminal Interaction (`[E]`):**
   * In modern Valheim (0.217+ / Ashlands / 1.0+), the game updated the `Hoverable` interface by requiring `GetHoverOffset()`.
   * Because the original OdinStorage was compiled before this update, Unity failed to instantiate the script on the terminal block, leaving the block uninteractable.
   * This patch intercepts interactions on `OdinTerminal` and displays `[E] Open Storage` with full interaction support.

2. **Fixes Item Withdraw, Deposit, and Search Actions:**
   * In modern Valheim, `Character.Message` gained an additional parameter, causing `MissingMethodException` crashes whenever withdrawing (`1`, `10`, `All`), depositing (`Store all`), or locating items (`Find`).
   * This patch handles these operations and restores full item transfer functionality cleanly.

3. **Direct UI Opening via Hotkey:**
   * In the original mod, the configured `Hotkey` (default `O`) was coded only to run a diagnostic chest count into the BepInEx console log.
   * This patch enhances the hotkey to directly open and toggle the Storage Terminal GUI from anywhere within chest radius. Pressing the hotkey opens the window; pressing it again or pressing `Escape` closes it.

4. **Retroactive World Compatibility:**
   * Any Odin Terminal pieces already built in your world will immediately become functional upon loading — no need to dismantle and rebuild them.

---

## 📦 Requirements

* **BepInExPack Valheim**
* **Jötunn, the Valheim Library**
* **OdinStorage (by Koehlerworks)** (installed automatically as dependency via r2modman / Thunderstore)

---

## ⚙️ Configuration

The patch respects your existing `com.nicolai.odinstorage.cfg` configuration file:
* `Hotkey` — Key that toggles the storage window (default: `O`).
* `Radius` — Search radius in meters for nearby chests (default: `20`).

---

## 👤 Credits

* Original mod and central storage logic by **Koehlerworks**.
* Compatibility patch & modern interface restoration by **Ntxfloy**.
