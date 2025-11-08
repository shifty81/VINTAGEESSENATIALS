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
        
        public ChestRadiusInventoryDialog(ICoreClientAPI capi) : base("Nearby Chests", capi)
        {
            this.capi = capi;
            displayInventory = new DummyInventory(capi);
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
            // Handle inventory packet if needed for client-server sync
        }

        private void RenderItemGrid()
        {
            if (displayInventory == null) return;

            // Resize the display inventory to show the visible slots
            int visibleSlots = SLOTS_PER_ROW * VISIBLE_ROWS;
            displayInventory.ResizeSlots(visibleSlots);

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
                
                // Check if this item type already exists in any nearby container
                bool itemExistsInStorage = false;
                foreach (var container in nearbyContainers)
                {
                    foreach (var storageSlot in container.Inventory)
                    {
                        if (!storageSlot.Empty && storageSlot.Itemstack.Equals(capi.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes))
                        {
                            itemExistsInStorage = true;
                            break;
                        }
                    }
                    if (itemExistsInStorage) break;
                }
                
                // Only deposit if this item type exists in storage
                if (!itemExistsInStorage) continue;
                
                // Try to deposit the item into containers
                foreach (var container in nearbyContainers)
                {
                    foreach (var targetSlot in container.Inventory)
                    {
                        if (targetSlot.Empty || targetSlot.Itemstack.Equals(capi.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes))
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

            capi.ShowChatMessage($"Deposited {deposited} matching items");
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
            return base.TryOpen();
        }

        public override string ToggleKeyCombinationCode => "chestradius";
    }

    // Helper class to create a dummy inventory for display purposes
    public class DummyInventory : InventoryBase, IInventory
    {
        private ItemSlot[] slots;

        public DummyInventory(ICoreAPI api) : base("chestradius-dummy", api)
        {
            slots = new ItemSlot[0];
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
                    newSlots[i] = new DummySlot(this);
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

    // Helper class for dummy slots
    public class DummySlot : ItemSlot
    {
        public DummySlot(InventoryBase inventory) : base(inventory)
        {
        }
    }
}
