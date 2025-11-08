using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageEssentials
{
    /// <summary>
    /// HUD overlay that displays locked slot indicators on the player inventory
    /// </summary>
    public class LockedSlotsHudOverlay : HudElement
    {
        private LockedSlotsManager lockedSlotsManager;
        private LockedSlotRenderer renderer;
        private LoadedTexture lockedSlotTexture;
        private GuiDialog characterDialog;

        public LockedSlotsHudOverlay(ICoreClientAPI capi, LockedSlotsManager lockedSlotsManager) : base(capi)
        {
            this.lockedSlotsManager = lockedSlotsManager;
            this.renderer = new LockedSlotRenderer(capi, lockedSlotsManager);
            
            // Create the locked slot texture (60x60 is a typical slot size in Vintage Story)
            this.lockedSlotTexture = renderer.CreateLockedSlotTexture(60, 60);
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);

            // Only render if the character/inventory dialog is open
            if (!IsInventoryOpen()) return;

            string playerUid = capi.World.Player?.PlayerUID;
            if (playerUid == null) return;

            HashSet<int> lockedSlots = lockedSlotsManager.GetLockedSlots(playerUid);
            if (lockedSlots.Count == 0) return;

            // Get the player inventory
            IInventory playerInv = capi.World.Player?.InventoryManager?.GetOwnInventory(GlobalConstants.characterInvClassName);
            if (playerInv == null) return;

            // Try to find the inventory dialog to get slot positions
            // Note: This is a simplified approach. In a real implementation, you'd need to
            // hook into the actual inventory GUI to get exact slot positions
            GuiDialog invDialog = FindCharacterDialog();
            if (invDialog == null || !invDialog.IsOpened()) return;

            // Render locked slot overlays
            RenderLockedSlots(lockedSlots, invDialog);
        }

        private bool IsInventoryOpen()
        {
            // Check if the character dialog (inventory) is open
            GuiDialog dialog = FindCharacterDialog();
            return dialog != null && dialog.IsOpened();
        }

        private GuiDialog FindCharacterDialog()
        {
            // Try to find the character dialog
            // The character dialog is typically named "characterdlg" in Vintage Story
            foreach (var dialog in capi.Gui.OpenedGuis)
            {
                if (dialog is GuiDialog guiDialog)
                {
                    string toggleKey = guiDialog.ToggleKeyCombinationCode;
                    if (toggleKey != null && (toggleKey.Contains("character") || toggleKey.Contains("inventory")))
                    {
                        return guiDialog;
                    }
                }
            }
            return null;
        }

        private void RenderLockedSlots(HashSet<int> lockedSlots, GuiDialog invDialog)
        {
            // This is a simplified implementation
            // In a real implementation, you would need to:
            // 1. Get the exact position and size of each inventory slot from the dialog
            // 2. Calculate the screen position for each locked slot
            // 3. Render the overlay texture at those positions
            
            // For now, we'll just add a note that the visual overlay requires
            // more deep integration with the inventory system
            
            // Example of how it would work:
            // foreach (int slotId in lockedSlots)
            // {
            //     var slotBounds = GetSlotBounds(invDialog, slotId);
            //     if (slotBounds != null)
            //     {
            //         capi.Render.Render2DTexture(
            //             lockedSlotTexture.TextureId,
            //             slotBounds.renderX,
            //             slotBounds.renderY,
            //             slotBounds.OuterWidth,
            //             slotBounds.OuterHeight
            //         );
            //     }
            // }
        }

        public override void Dispose()
        {
            lockedSlotTexture?.Dispose();
            base.Dispose();
        }

        public override string ToggleKeyCombinationCode => null;
    }
}
