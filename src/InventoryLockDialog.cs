using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VintageEssentials
{
    public class InventoryLockDialog : GuiDialogGeneric
    {
        private new ICoreClientAPI capi;
        private LockedSlotsManager lockedSlotsManager;
        private ModConfig config;
        private bool lockingMode = false;
        private Action<bool> onModeChanged;

        public InventoryLockDialog(ICoreClientAPI capi, LockedSlotsManager lockedSlotsManager, ModConfig config, Action<bool> onModeChanged) 
            : base("Inventory Slot Locking", capi)
        {
            this.capi = capi;
            this.lockedSlotsManager = lockedSlotsManager;
            this.config = config;
            this.onModeChanged = onModeChanged;
        }

        public bool IsLockingMode()
        {
            return lockingMode;
        }

        public void ToggleLockingMode()
        {
            lockingMode = !lockingMode;
            onModeChanged?.Invoke(lockingMode);
            
            if (lockingMode)
            {
                string playerUid = capi.World.Player.PlayerUID;
                int lockedCount = lockedSlotsManager.GetLockedSlotsCount(playerUid);
                int maxSlots = config.MaxLockedSlots;
                capi.ShowChatMessage($"Slot locking mode ENABLED. Click inventory slots to lock them. ({lockedCount}/{maxSlots} locked)");
            }
            else
            {
                capi.ShowChatMessage("Slot locking mode DISABLED.");
            }
        }

        public bool TryToggleSlotLock(int slotId)
        {
            if (!lockingMode) return false;

            string playerUid = capi.World.Player.PlayerUID;
            bool nowLocked = lockedSlotsManager.ToggleSlotLock(playerUid, slotId);
            
            if (nowLocked)
            {
                capi.ShowChatMessage($"Slot {slotId} locked.");
            }
            else
            {
                int lockedCount = lockedSlotsManager.GetLockedSlotsCount(playerUid);
                if (lockedSlotsManager.IsSlotLocked(playerUid, slotId))
                {
                    capi.ShowChatMessage($"Cannot lock more slots. Maximum is {config.MaxLockedSlots}.");
                    return false;
                }
                else
                {
                    capi.ShowChatMessage($"Slot {slotId} unlocked.");
                }
            }
            
            return true;
        }

        public bool IsSlotLocked(int slotId)
        {
            string playerUid = capi.World.Player.PlayerUID;
            return lockedSlotsManager.IsSlotLocked(playerUid, slotId);
        }
    }
}
