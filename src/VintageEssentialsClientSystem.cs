using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageEssentials
{
    public class VintageEssentialsClientSystem : ModSystem
    {
        private ICoreClientAPI clientApi;
        private ChestRadiusInventoryDialog chestRadiusDialog;
        private PlayerInventorySortDialog playerSortDialog;
        private InventoryLockDialog inventoryLockDialog;
        private LockedSlotsManager lockedSlotsManager;
        private ModConfig config;
        private KeybindConflictDialog keybindConflictDialog;
        private ModConfigDialog configDialog;
        private LockedSlotsHudOverlay lockedSlotsHudOverlay;
        private InventorySlotClickHandler slotClickHandler;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.clientApi = api;

            // Load configuration
            config = ModConfig.Load(api);

            // Create locked slots manager (client-side for UI, will sync with server)
            lockedSlotsManager = new LockedSlotsManager(api, config);

            // Create dialogs
            chestRadiusDialog = new ChestRadiusInventoryDialog(api);
            playerSortDialog = new PlayerInventorySortDialog(api, lockedSlotsManager);
            inventoryLockDialog = new InventoryLockDialog(api, lockedSlotsManager, config, OnLockingModeChanged);
            keybindConflictDialog = new KeybindConflictDialog(api);
            configDialog = new ModConfigDialog(api, config);

            // Create HUD overlay for locked slots
            lockedSlotsHudOverlay = new LockedSlotsHudOverlay(api, lockedSlotsManager);
            api.Gui.RegisterDialog(lockedSlotsHudOverlay);

            // Create and initialize slot click handler
            slotClickHandler = new InventorySlotClickHandler(api, inventoryLockDialog, lockedSlotsManager);
            slotClickHandler.Initialize();

            // Register keybind for chest radius inventory
            clientApi.Input.RegisterHotKey("chestradius", "Open Chest Radius Inventory", GlKeys.R, HotkeyType.GUIOrOtherControls);
            clientApi.Input.SetHotKeyHandler("chestradius", OnChestRadiusHotkey);

            // Register keybind for player inventory sort
            clientApi.Input.RegisterHotKey("playerinvsort", "Sort Player Inventory", GlKeys.S, HotkeyType.InventoryHotkeys, shiftPressed: true);
            clientApi.Input.SetHotKeyHandler("playerinvsort", OnPlayerSortHotkey);

            // Register keybind for toggling slot locking mode
            clientApi.Input.RegisterHotKey("toggleslotlock", "Toggle Inventory Slot Locking Mode", GlKeys.L, HotkeyType.InventoryHotkeys, ctrlPressed: true);
            clientApi.Input.SetHotKeyHandler("toggleslotlock", OnToggleSlotLockHotkey);

            // Register keybind for opening config dialog
            clientApi.Input.RegisterHotKey("veconfig", "Open VintageEssentials Settings", GlKeys.V, HotkeyType.GUIOrOtherControls, ctrlPressed: true, shiftPressed: true);
            clientApi.Input.SetHotKeyHandler("veconfig", OnConfigHotkey);

            // Register client chat command for config
            clientApi.RegisterCommand("veconfig", "Opens VintageEssentials configuration", "", (id, args) =>
            {
                configDialog.TryOpen();
            });

            // Check for keybind conflicts on player join
            clientApi.Event.PlayerEntitySpawn += OnPlayerSpawn;

            clientApi.Logger.Notification("VintageEssentials client-side loaded.");
            clientApi.ShowChatMessage("VintageEssentials loaded! Press Ctrl+Shift+V to open settings or use /veconfig");
        }

        private void OnPlayerSpawn(IClientPlayer player)
        {
            // Check for keybind conflicts
            List<string> conflicts = CheckKeybindConflicts();
            if (conflicts.Count > 0)
            {
                // Show conflict resolution dialog
                keybindConflictDialog.ShowConflicts(conflicts);
            }
        }

        private List<string> CheckKeybindConflicts()
        {
            List<string> conflicts = new List<string>();
            
            // Get all registered hotkeys
            var registeredKeys = new Dictionary<string, string>
            {
                { "chestradius", "R" },
                { "playerinvsort", "Shift+S" },
                { "toggleslotlock", "Ctrl+L" },
                { "veconfig", "Ctrl+Shift+V" }
            };

            // Check for conflicts with existing game keybinds
            // This is a simplified check - in a real implementation, you'd check against
            // all registered keybinds in the game
            foreach (var kvp in registeredKeys)
            {
                string hotkeyCode = kvp.Key;
                string keyCombination = kvp.Value;
                
                // Check if this keybind conflicts with vanilla game keybinds
                // For now, we'll just log potential conflicts
                // A more complete implementation would check clientApi.Input.HotKeys
            }

            return conflicts;
        }

        private void OnLockingModeChanged(bool lockingMode)
        {
            // This callback can be used to update UI or other systems when locking mode changes
            if (lockingMode)
            {
                clientApi.ShowChatMessage("Locking mode ON - Click inventory slots to lock/unlock them");
            }
            else
            {
                clientApi.ShowChatMessage("Locking mode OFF");
            }
        }

        private bool OnChestRadiusHotkey(KeyCombination keyCombination)
        {
            if (chestRadiusDialog.IsOpened())
            {
                chestRadiusDialog.TryClose();
            }
            else
            {
                chestRadiusDialog.TryOpen();
            }
            return true;
        }

        private bool OnPlayerSortHotkey(KeyCombination keyCombination)
        {
            playerSortDialog.SortPlayerInventory();
            return true;
        }

        private bool OnToggleSlotLockHotkey(KeyCombination keyCombination)
        {
            inventoryLockDialog.ToggleLockingMode();
            return true;
        }

        private bool OnConfigHotkey(KeyCombination keyCombination)
        {
            configDialog.TryOpen();
            return true;
        }

        public override void Dispose()
        {
            chestRadiusDialog?.Dispose();
            playerSortDialog?.Dispose();
            inventoryLockDialog?.Dispose();
            keybindConflictDialog?.Dispose();
            configDialog?.Dispose();
            lockedSlotsHudOverlay?.Dispose();
            slotClickHandler?.Dispose();
            base.Dispose();
        }
    }
}
