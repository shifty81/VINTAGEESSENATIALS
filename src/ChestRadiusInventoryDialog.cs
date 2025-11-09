using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageEssentials
{
    public class ChestRadiusInventoryDialog : GuiDialogGeneric
    {
        private new ICoreClientAPI capi;
        private List<BlockEntityContainer> nearbyContainers = new List<BlockEntityContainer>();
        private List<ItemSlot> allSlots = new List<ItemSlot>();
        private List<ItemSlot> filteredSlots = new List<ItemSlot>();
        private string searchText = "";
        private bool isSorted = false;
        private const int RADIUS = 15;
        private const int SLOTS_PER_ROW = 8;
        private const int VISIBLE_ROWS = 6;
        private int scrollOffset = 0;
        private DummyInventory displayInventory;
        private Dictionary<int, ItemSlot> displaySlotToActualSlot = new Dictionary<int, ItemSlot>();
        
        public ChestRadiusInventoryDialog(ICoreClientAPI capi) : base("Nearby Chests", capi)
        {
            this.capi = capi;
            displayInventory = new DummyInventory(capi, this);
        }

        public void RefreshContainers()
        {
            nearbyContainers.Clear();
            allSlots.Clear();
            
            IPlayer player = capi.World.Player;
            if (player?.Entity == null) return;

            Vec3d playerPos = player.Entity.Pos.XYZ;
            BlockPos minPos = new BlockPos((int)(playerPos.X - RADIUS), (int)(playerPos.Y - RADIUS), (int)(playerPos.Z - RADIUS));
            BlockPos maxPos = new BlockPos((int)(playerPos.X + RADIUS), (int)(playerPos.Y + RADIUS), (int)(playerPos.Z + RADIUS));

            capi.World.BlockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
            {
                BlockPos pos = new BlockPos(x, y, z);
                BlockEntity be = capi.World.BlockAccessor.GetBlockEntity(pos);
                if (be is BlockEntityContainer container && container.Inventory != null)
                {
                    nearbyContainers.Add(container);
                    foreach (ItemSlot slot in container.Inventory)
                    {
                        if (slot != null && !slot.Empty)
                        {
                            allSlots.Add(slot);
                        }
                    }
                }
            });

            ApplyFiltersAndSort();
        }

        private void ApplyFiltersAndSort()
        {
            filteredSlots = allSlots.Where(slot =>
            {
                if (string.IsNullOrEmpty(searchText)) return true;
                if (slot.Empty) return false;
                string itemName = slot.Itemstack?.GetName()?.ToLower() ?? "";
                return itemName.Contains(searchText.ToLower());
            }).ToList();

            // Sort by name A-Z if sorting is enabled
            if (isSorted)
            {
                filteredSlots = filteredSlots.OrderBy(slot => slot.Itemstack?.GetName() ?? "").ToList();
            }

            scrollOffset = Math.Min(scrollOffset, Math.Max(0, (filteredSlots.Count / SLOTS_PER_ROW) - VISIBLE_ROWS));
            
            if (IsOpened())
            {
                ComposeDialog();
            }
        }

        private void ComposeDialog()
        {
            double elemWidth = 500;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            
            ElementBounds searchBounds = ElementBounds.Fixed(0, 30, elemWidth - 20, 30);
            ElementBounds sortButtonBounds = ElementBounds.Fixed(0, 65, 150, 30);
            ElementBounds depositButtonBounds = ElementBounds.Fixed(160, 65, 150, 30);
            ElementBounds takeAllButtonBounds = ElementBounds.Fixed(320, 65, 150, 30);
            
            // Create bounds for the item slot grid
            double slotSize = 60;
            double gridWidth = SLOTS_PER_ROW * slotSize;
            double gridHeight = VISIBLE_ROWS * slotSize;
            ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 10, 105, SLOTS_PER_ROW, VISIBLE_ROWS);
            ElementBounds gridBounds = ElementBounds.Fixed(0, 105, gridWidth, gridHeight);
            ElementBounds scrollbarBounds = ElementBounds.Fixed(gridWidth + 10, 105, 20, gridHeight);

            ElementBounds closeButtonBounds = ElementBounds.Fixed(0, gridHeight + 115, 100, 30);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(searchBounds, sortButtonBounds, depositButtonBounds, takeAllButtonBounds, 
                                  slotBounds, scrollbarBounds, closeButtonBounds);

            SingleComposer = capi.Gui.CreateCompo("chestradius", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Nearby Chests (15 block radius)", OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddStaticText($"Found {nearbyContainers.Count} containers with {allSlots.Count} items", 
                                   CairoFont.WhiteDetailText(), ElementBounds.Fixed(0, 5, elemWidth, 20))
                    .AddTextInput(searchBounds, OnSearchChanged, CairoFont.WhiteDetailText(), "searchbox")
                    .AddSmallButton("Sort", OnSortClicked, sortButtonBounds, EnumButtonStyle.Normal, "sortbtn")
                    .AddSmallButton("Deposit All", OnDepositClicked, depositButtonBounds, EnumButtonStyle.Normal, "depositbtn")
                    .AddSmallButton("Take All", OnTakeAllClicked, takeAllButtonBounds, EnumButtonStyle.Normal, "takebtn")
                    .AddItemSlotGrid(displayInventory, SendInvPacket, SLOTS_PER_ROW, slotBounds, "slotgrid")
                    .AddVerticalScrollbar(OnScroll, scrollbarBounds, "scrollbar")
                    .AddSmallButton("Close", OnCloseClicked, closeButtonBounds)
                .EndChildElements()
                .Compose();

            SingleComposer.GetTextInput("searchbox").SetPlaceHolderText("Search items...");
            
            UpdateScrollbar();
            RenderItemGrid();
        }

        private void SendInvPacket(object packet)
        {
            // Handle inventory packet - this is called when slots are interacted with
            // The packet contains information about the slot interaction
            if (packet is IClientNetworkChannel)
            {
                // This is a network packet for client-server communication
                // For now, we handle interactions directly through the slot overrides
            }
        }

        // Method to handle taking an item from a display slot (when shift-clicked or moved to player inventory)
        public bool TryTakeFromDisplaySlot(int displaySlotId, ItemSlot targetSlot)
        {
            // Find the actual chest slot that this display slot represents
            if (!displaySlotToActualSlot.TryGetValue(displaySlotId, out ItemSlot actualSlot))
            {
                return false;
            }

            if (actualSlot == null || actualSlot.Empty)
            {
                return false;
            }

            // Try to move items from the actual slot to the target slot
            int moved = actualSlot.TryPutInto(capi.World, targetSlot);
            if (moved > 0)
            {
                actualSlot.MarkDirty();
                targetSlot.MarkDirty();
                
                // Refresh the display after the transfer
                RefreshContainers();
                return true;
            }

            return false;
        }

        // Method to handle depositing an item to storage (when shift-clicked from player inventory)
        public bool TryDepositToStorage(ItemSlot sourceSlot)
        {
            if (sourceSlot == null || sourceSlot.Empty)
            {
                return false;
            }

            // Try to deposit into nearby containers
            foreach (var container in nearbyContainers)
            {
                foreach (var targetSlot in container.Inventory)
                {
                    // First try to stack with existing items
                    if (!targetSlot.Empty && targetSlot.Itemstack.Equals(capi.World, sourceSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
                    {
                        int moved = sourceSlot.TryPutInto(capi.World, targetSlot);
                        if (moved > 0)
                        {
                            sourceSlot.MarkDirty();
                            targetSlot.MarkDirty();
                            if (sourceSlot.Empty)
                            {
                                RefreshContainers();
                                return true;
                            }
                        }
                    }
                }
            }

            // If no matching stacks found, try to find an empty slot
            foreach (var container in nearbyContainers)
            {
                foreach (var targetSlot in container.Inventory)
                {
                    if (targetSlot.Empty)
                    {
                        int moved = sourceSlot.TryPutInto(capi.World, targetSlot);
                        if (moved > 0)
                        {
                            sourceSlot.MarkDirty();
                            targetSlot.MarkDirty();
                            RefreshContainers();
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void RenderItemGrid()
        {
            if (displayInventory == null) return;

            // Resize the display inventory to show the visible slots
            int visibleSlots = SLOTS_PER_ROW * VISIBLE_ROWS;
            displayInventory.ResizeSlots(visibleSlots);

            // Clear the mapping
            displaySlotToActualSlot.Clear();

            // Calculate which items to show based on scroll offset
            int startIndex = scrollOffset * SLOTS_PER_ROW;
            
            // Populate the display inventory with filtered slots
            for (int i = 0; i < visibleSlots; i++)
            {
                int sourceIndex = startIndex + i;
                if (sourceIndex < filteredSlots.Count)
                {
                    // Copy the itemstack to the display slot
                    displayInventory[i].Itemstack = filteredSlots[sourceIndex].Itemstack?.Clone();
                    // Map the display slot to the actual chest slot
                    displaySlotToActualSlot[i] = filteredSlots[sourceIndex];
                }
                else
                {
                    // Clear empty slots
                    displayInventory[i].Itemstack = null;
                }
                displayInventory[i].MarkDirty();
            }
        }

        private void UpdateScrollbar()
        {
            if (SingleComposer == null) return;
            
            int totalRows = (int)Math.Ceiling((double)filteredSlots.Count / SLOTS_PER_ROW);
            int maxScroll = Math.Max(0, totalRows - VISIBLE_ROWS);
            
            // Update scrollbar (simplified - actual implementation would need proper scrollbar handling)
        }

        private void OnSearchChanged(string text)
        {
            searchText = text;
            ApplyFiltersAndSort();
        }

        private bool OnSortClicked()
        {
            isSorted = !isSorted;
            capi.ShowChatMessage(isSorted ? "Sorting enabled (A-Z)" : "Sorting disabled");
            ApplyFiltersAndSort();
            return true;
        }

        private bool OnDepositClicked()
        {
            IPlayer player = capi.World.Player;
            if (player?.InventoryManager == null) return true;

            // Get the player's main inventory
            IInventory playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return true;

            int deposited = 0;
            
            // Iterate through player inventory slots
            foreach (var slot in playerInv)
            {
                if (slot == null || slot.Empty) continue;
                
                // Try to deposit into nearby containers
                // First pass: try to stack with existing items
                foreach (var container in nearbyContainers)
                {
                    foreach (var targetSlot in container.Inventory)
                    {
                        if (!targetSlot.Empty && targetSlot.Itemstack.Equals(capi.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes))
                        {
                            int moved = slot.TryPutInto(capi.World, targetSlot);
                            if (moved > 0)
                            {
                                deposited += moved;
                                slot.MarkDirty();
                                targetSlot.MarkDirty();
                                if (slot.Empty) break;
                            }
                        }
                    }
                    if (slot.Empty) break;
                }
                
                // Second pass: if item still not empty, try to find empty slots
                if (!slot.Empty)
                {
                    foreach (var container in nearbyContainers)
                    {
                        foreach (var targetSlot in container.Inventory)
                        {
                            if (targetSlot.Empty)
                            {
                                int moved = slot.TryPutInto(capi.World, targetSlot);
                                if (moved > 0)
                                {
                                    deposited += moved;
                                    slot.MarkDirty();
                                    targetSlot.MarkDirty();
                                    if (slot.Empty) break;
                                }
                            }
                        }
                        if (slot.Empty) break;
                    }
                }
            }

            capi.ShowChatMessage($"Deposited {deposited} items");
            RefreshContainers();
            return true;
        }

        private bool OnTakeAllClicked()
        {
            IPlayer player = capi.World.Player;
            if (player?.InventoryManager == null) return true;

            // Get the player's main inventory
            IInventory playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return true;

            int taken = 0;

            foreach (var container in nearbyContainers)
            {
                foreach (var sourceSlot in container.Inventory)
                {
                    if (sourceSlot.Empty) continue;

                    foreach (var targetSlot in playerInv)
                    {
                        if (targetSlot == null) continue;
                        
                        if (targetSlot.Empty || targetSlot.Itemstack.Equals(capi.World, sourceSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
                        {
                            int moved = sourceSlot.TryPutInto(capi.World, targetSlot);
                            if (moved > 0)
                            {
                                taken += moved;
                                sourceSlot.MarkDirty();
                                targetSlot.MarkDirty();
                                if (sourceSlot.Empty) break;
                            }
                        }
                    }
                    if (sourceSlot.Empty) continue;
                }
            }

            capi.ShowChatMessage($"Took {taken} items");
            RefreshContainers();
            return true;
        }

        private void OnScroll(float scrollOffset)
        {
            this.scrollOffset = (int)(scrollOffset * Math.Max(0, (filteredSlots.Count / SLOTS_PER_ROW) - VISIBLE_ROWS));
            RenderItemGrid();
        }

        private bool OnCloseClicked()
        {
            TryClose();
            return true;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }

        public override bool TryOpen()
        {
            RefreshContainers();
            ComposeDialog();
            bool result = base.TryOpen();
            
            if (result)
            {
                // Hook into player inventory to handle shift-clicking FROM player inventory TO storage
                SetupPlayerInventoryHook();
            }
            
            return result;
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            // Clean up player inventory hook
            CleanupPlayerInventoryHook();
        }

        private void SetupPlayerInventoryHook()
        {
            // Register our display inventory as a potential shift-click target
            // Note: In Vintage Story, this is handled through the InventoryManager
            // We'll monitor for slot modifications to keep our display in sync
            var player = capi.World.Player;
            if (player?.InventoryManager == null) return;

            var playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return;

            playerInv.SlotModified += OnPlayerInventorySlotModified;

            // Additionally, we can register a mouse up event handler to intercept shift-clicks
            capi.Event.MouseUp += OnMouseUpEvent;
        }

        private void CleanupPlayerInventoryHook()
        {
            var player = capi.World.Player;
            if (player?.InventoryManager == null) return;

            var playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return;

            playerInv.SlotModified -= OnPlayerInventorySlotModified;
            capi.Event.MouseUp -= OnMouseUpEvent;
        }

        private void OnMouseUpEvent(MouseEvent e)
        {
            // Check if this is a shift-click
            if (!e.ShiftPressed) return;
            
            // Only handle left clicks
            if (e.Button != EnumMouseButton.Left) return;

            // Get the player's inventory
            var player = capi.World.Player;
            if (player?.InventoryManager == null) return;

            var playerInv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return;

            // Try to find which slot was clicked by checking the mouse position against slot bounds
            // This is approximate, as we don't have direct access to the character GUI
            // The game's built-in shift-click should ideally work, but if not, users can use Deposit All button
            
            // For now, we'll let the refresh handler keep the display in sync
            // The main shift-click functionality (from storage to player) is handled by DummySlot.TryPutInto
        }

        private void OnPlayerInventorySlotModified(int slotId)
        {
            // Refresh the container display when player inventory changes
            // This ensures the display stays up-to-date
            if (IsOpened())
            {
                RefreshContainers();
            }
        }

        public override string ToggleKeyCombinationCode => "chestradius";
    }

    // Helper class to create an interactive inventory for the chest radius dialog
    public class DummyInventory : InventoryBase, IInventory
    {
        private ItemSlot[] slots;
        private ChestRadiusInventoryDialog dialog;

        public DummyInventory(ICoreAPI api, ChestRadiusInventoryDialog dialog) : base("chestradius-dummy", api)
        {
            slots = new ItemSlot[0];
            this.dialog = dialog;
        }

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= slots.Length) return null;
                return slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= slots.Length) return;
                slots[slotId] = value;
            }
        }

        public void ResizeSlots(int count)
        {
            ItemSlot[] newSlots = new ItemSlot[count];
            for (int i = 0; i < count; i++)
            {
                if (i < slots.Length && slots[i] != null)
                {
                    newSlots[i] = slots[i];
                }
                else
                {
                    newSlots[i] = new DummySlot(this, dialog, i);
                }
            }
            slots = newSlots;
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            // Not needed for display-only inventory
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            // Not needed for display-only inventory
        }
    }

    // Helper class for interactive slots that take items from the chest display
    public class DummySlot : ItemSlot
    {
        private ChestRadiusInventoryDialog dialog;
        private int displaySlotId;

        public DummySlot(InventoryBase inventory, ChestRadiusInventoryDialog dialog, int displaySlotId) : base(inventory)
        {
            this.dialog = dialog;
            this.displaySlotId = displaySlotId;
        }

        // Override to handle taking items from this display slot
        public override ItemStack TakeOut(int quantity)
        {
            // We want to take from the actual chest slot, not the display copy
            // Return null here and handle it in TryPutInto instead
            return null;
        }

        // Override to handle transferring from this slot to another
        public override int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if (this.Empty || dialog == null) return 0;

            // Transfer from the actual chest slot to the sink slot (typically player inventory)
            bool success = dialog.TryTakeFromDisplaySlot(displaySlotId, sinkSlot);
            if (success)
            {
                // Calculate how much was transferred
                int transferred = Math.Min(this.StackSize, op?.RequestedQuantity ?? this.StackSize);
                
                // Update the display - the actual work was done in TryTakeFromDisplaySlot
                // Don't modify this slot directly, as it's just a display copy
                
                return transferred;
            }

            return 0;
        }

        // Override TryFlipWith to handle picking up items with mouse
        public override void TryFlipWith(ItemSlot targetSlot)
        {
            if (this.Empty || dialog == null) return;

            // When clicking (not shift-clicking) to pick up an item
            if (targetSlot.Empty)
            {
                // Try to take from the actual chest slot
                var world = inventory?.Api?.World;
                if (world != null)
                {
                    ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, this.StackSize);
                    dialog.TryTakeFromDisplaySlot(displaySlotId, targetSlot);
                }
            }
        }

        // Prevent items from being placed directly into display slots
        public override bool CanHold(ItemSlot sourceSlot)
        {
            return false;
        }

        // Allow taking items from display slots
        public override bool CanTake()
        {
            return !this.Empty;
        }
    }
}
