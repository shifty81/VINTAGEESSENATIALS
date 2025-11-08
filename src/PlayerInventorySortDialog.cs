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
        private int sortMode = 0; // 0 = by name, 1 = by quantity, 2 = by type

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

            // Collect all non-empty slots and their contents
            List<ItemSlot> slots = new List<ItemSlot>();
            List<ItemStack> stacks = new List<ItemStack>();

            foreach (ItemSlot slot in playerInv)
            {
                if (slot != null && !slot.Empty && slot.Itemstack != null)
                {
                    slots.Add(slot);
                    stacks.Add(slot.Itemstack.Clone());
                }
            }

            if (stacks.Count == 0)
            {
                capi.ShowChatMessage("No items to sort!");
                return;
            }

            // Store the current mode name before sorting
            string[] modes = { "Name", "Quantity", "Type" };
            string currentMode = modes[sortMode];

            // Sort the stacks based on current sort mode
            switch (sortMode)
            {
                case 0: // Sort by name
                    stacks = stacks.OrderBy(stack => stack.GetName()).ToList();
                    break;
                case 1: // Sort by quantity
                    stacks = stacks.OrderByDescending(stack => stack.StackSize).ToList();
                    break;
                case 2: // Sort by type (collectible type and code)
                    stacks = stacks.OrderBy(stack => stack.Collectible.Code.Domain)
                                   .ThenBy(stack => stack.Collectible.Code.Path)
                                   .ToList();
                    break;
            }

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

            // Cycle sort mode for next time
            sortMode = (sortMode + 1) % 3;
            string nextMode = modes[sortMode];
            capi.ShowChatMessage($"Inventory sorted by {currentMode}. Next sort: {nextMode}");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
