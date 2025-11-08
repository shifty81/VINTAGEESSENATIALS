using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VintageEssentials
{
    public class PlayerInventorySortDialog
    {
        private ICoreClientAPI capi;

        public PlayerInventorySortDialog(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public void SortPlayerInventory()
        {
            IPlayer player = capi.World.Player;
            if (player?.InventoryManager == null) return;

            // Get the player's inventory
            IInventory playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return;

            // Get the offhand slot for comparison
            ItemSlot offhandSlot = player.Entity?.LeftHandItemSlot;

            // Collect all non-empty slots and their contents (excluding hotbar and offhand)
            List<ItemSlot> slots = new List<ItemSlot>();
            List<ItemStack> stacks = new List<ItemStack>();

            int slotIndex = 0;
            foreach (ItemSlot slot in playerInv)
            {
                // Skip hotbar slots (0-9) and offhand slot
                bool isHotbarSlot = slotIndex < 10;
                bool isOffhandSlot = (offhandSlot != null && slot == offhandSlot);
                
                if (!isHotbarSlot && !isOffhandSlot && slot != null && !slot.Empty && slot.Itemstack != null)
                {
                    slots.Add(slot);
                    stacks.Add(slot.Itemstack.Clone());
                }
                
                slotIndex++;
            }

            if (stacks.Count == 0)
            {
                capi.ShowChatMessage("No items to sort!");
                return;
            }

            // Sort by name A-Z
            stacks = stacks.OrderBy(stack => stack.GetName()).ToList();

            // Clear the original slots
            foreach (ItemSlot slot in slots)
            {
                slot.Itemstack = null;
                slot.MarkDirty();
            }

            // Put sorted items back
            int stackIndex = 0;
            foreach (ItemSlot slot in slots)
            {
                if (stackIndex < stacks.Count)
                {
                    slot.Itemstack = stacks[stackIndex];
                    slot.MarkDirty();
                    stackIndex++;
                }
            }

            capi.ShowChatMessage("Inventory sorted by name (A-Z) - Hotbar and offhand preserved");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
