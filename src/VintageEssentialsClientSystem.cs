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

            // Register keybind for chest radius inventory
            clientApi.Input.RegisterHotKey("chestradius", "Open Chest Radius Inventory", GlKeys.R, HotkeyType.GUIOrOtherControls);
            clientApi.Input.SetHotKeyHandler("chestradius", OnChestRadiusHotkey);

            // Register keybind for player inventory sort
            clientApi.Input.RegisterHotKey("playerinvsort", "Sort Player Inventory", GlKeys.S, HotkeyType.InventoryHotkeys, shiftPressed: true);
            clientApi.Input.SetHotKeyHandler("playerinvsort", OnPlayerSortHotkey);

            // Register keybind for toggling slot locking mode
            clientApi.Input.RegisterHotKey("toggleslotlock", "Toggle Inventory Slot Locking Mode", GlKeys.L, HotkeyType.InventoryHotkeys, ctrlPressed: true);
            clientApi.Input.SetHotKeyHandler("toggleslotlock", OnToggleSlotLockHotkey);

            // Check for keybind conflicts on player join
            clientApi.Event.PlayerEntitySpawn += OnPlayerSpawn;

            clientApi.Logger.Notification("VintageEssentials client-side loaded. Press 'R' to open chest radius inventory, 'Shift+S' to sort, 'Ctrl+L' to toggle slot locking.");
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
                { "toggleslotlock", "Ctrl+L" }
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

        public override void Dispose()
        {
            chestRadiusDialog?.Dispose();
            playerSortDialog?.Dispose();
            inventoryLockDialog?.Dispose();
            keybindConflictDialog?.Dispose();
            base.Dispose();
        }
    }
}
