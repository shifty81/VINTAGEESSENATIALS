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
        private int sortMode = 0; // 0 = none, 1 = name, 2 = quantity
        private const int RADIUS = 15;
        private const int SLOTS_PER_ROW = 8;
        private const int VISIBLE_ROWS = 6;
        private int scrollOffset = 0;
        
        public ChestRadiusInventoryDialog(ICoreClientAPI capi) : base("Nearby Chests", capi)
        {
            this.capi = capi;
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
                if (block?.BlockEntityClass != null)
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

            if (sortMode == 1) // Sort by name
            {
                filteredSlots = filteredSlots.OrderBy(slot => slot.Itemstack?.GetName() ?? "").ToList();
            }
            else if (sortMode == 2) // Sort by quantity
            {
                filteredSlots = filteredSlots.OrderByDescending(slot => slot.StackSize).ToList();
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
            
            ElementBounds gridBounds = ElementBounds.Fixed(0, 105, elemWidth - 20, 400);
            ElementBounds scrollbarBounds = ElementBounds.Fixed(elemWidth - 30, 105, 20, 400);

            ElementBounds closeButtonBounds = ElementBounds.Fixed(0, 520, 100, 30);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(searchBounds, sortButtonBounds, depositButtonBounds, takeAllButtonBounds, 
                                  gridBounds, scrollbarBounds, closeButtonBounds);

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
                    .AddInset(gridBounds, 2)
                    .AddVerticalScrollbar(OnScroll, scrollbarBounds, "scrollbar")
                    .AddSmallButton("Close", OnCloseClicked, closeButtonBounds)
                .EndChildElements()
                .Compose();

            SingleComposer.GetTextInput("searchbox").SetPlaceHolderText("Search items...");
            
            UpdateScrollbar();
            RenderItemGrid();
        }

        private void RenderItemGrid()
        {
            var composer = SingleComposer;
            if (composer == null) return;

            // Clear any existing item slot elements
            // Note: In a production mod, you'd want to use a custom render element for better performance
            // This is a simplified approach
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
            sortMode = (sortMode + 1) % 3;
            string[] modes = { "None", "Name", "Quantity" };
            capi.ShowChatMessage($"Sort mode: {modes[sortMode]}");
            ApplyFiltersAndSort();
            return true;
        }

        private bool OnDepositClicked()
        {
            IPlayer player = capi.World.Player;
            if (player?.InventoryManager?.CurrentHoveredSlot == null) return true;

            int deposited = 0;
            foreach (var slot in player.InventoryManager.OpenedInventories.SelectMany(inv => inv))
            {
                if (slot.Empty) continue;
                
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

            capi.ShowChatMessage($"Deposited {deposited} items");
            RefreshContainers();
            return true;
        }

        private bool OnTakeAllClicked()
        {
            IPlayer player = capi.World.Player;
            int taken = 0;

            foreach (var container in nearbyContainers)
            {
                foreach (var sourceSlot in container.Inventory)
                {
                    if (sourceSlot.Empty) continue;

                    foreach (var inv in player.InventoryManager.OpenedInventories)
                    {
                        foreach (var targetSlot in inv)
                        {
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
                        if (sourceSlot.Empty) break;
                    }
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
}
