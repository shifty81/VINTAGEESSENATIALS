using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace VintageEssentials
{
    public class KeybindConflictDialog : GuiDialogGeneric
    {
        private new ICoreClientAPI capi;
        private List<string> conflicts;
        private int currentConflictIndex = 0;

        public KeybindConflictDialog(ICoreClientAPI capi) : base("Keybind Conflicts", capi)
        {
            this.capi = capi;
        }

        public void ShowConflicts(List<string> conflicts)
        {
            this.conflicts = conflicts;
            this.currentConflictIndex = 0;
            
            if (conflicts.Count > 0)
            {
                ComposeDialog();
                TryOpen();
            }
        }

        private void ComposeDialog()
        {
            if (conflicts == null || currentConflictIndex >= conflicts.Count)
            {
                TryClose();
                return;
            }

            double elemWidth = 400;
            
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            
            ElementBounds textBounds = ElementBounds.Fixed(0, 30, elemWidth, 100);
            ElementBounds reconfigureButtonBounds = ElementBounds.Fixed(0, 140, 180, 30);
            ElementBounds skipButtonBounds = ElementBounds.Fixed(190, 140, 180, 30);
            
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds, reconfigureButtonBounds, skipButtonBounds);

            string conflictMessage = GetConflictMessage(currentConflictIndex);
            
            SingleComposer = capi.Gui.CreateCompo("keybindconflict", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Keybind Conflict Detected", OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddStaticText("VintageEssentials Keybind Conflict", CairoFont.WhiteSmallText(), 
                                   ElementBounds.Fixed(0, 5, elemWidth, 20))
                    .AddStaticText(conflictMessage, CairoFont.WhiteDetailText(), textBounds)
                    .AddSmallButton("Reconfigure", OnReconfigureClicked, reconfigureButtonBounds)
                    .AddSmallButton("Skip", OnSkipClicked, skipButtonBounds)
                .EndChildElements()
                .Compose();
        }

        private string GetConflictMessage(int index)
        {
            if (conflicts == null || index >= conflicts.Count)
                return "";
            
            string conflict = conflicts[index];
            
            // Generate explanation based on the conflict
            if (conflict.Contains("chestradius"))
            {
                return "The 'R' key for opening Chest Radius Inventory may conflict with existing keybinds.\n\n" +
                       "This feature allows you to see all items in chests within 15 blocks and manage them quickly.";
            }
            else if (conflict.Contains("playerinvsort"))
            {
                return "The 'Shift+S' key for sorting your inventory may conflict with existing keybinds.\n\n" +
                       "This feature sorts your player inventory alphabetically.";
            }
            else if (conflict.Contains("toggleslotlock"))
            {
                return "The 'Ctrl+L' key for toggling slot locking mode may conflict with existing keybinds.\n\n" +
                       "This feature allows you to lock inventory slots to prevent them from being sorted.";
            }
            
            return conflict;
        }

        private bool OnReconfigureClicked()
        {
            // Open the game's keybind configuration screen
            capi.ShowChatMessage("Please go to Options > Controls to reconfigure your keybinds.");
            
            // Move to next conflict
            currentConflictIndex++;
            if (currentConflictIndex < conflicts.Count)
            {
                ComposeDialog();
            }
            else
            {
                TryClose();
            }
            
            return true;
        }

        private bool OnSkipClicked()
        {
            // Skip this conflict and move to next
            currentConflictIndex++;
            if (currentConflictIndex < conflicts.Count)
            {
                ComposeDialog();
            }
            else
            {
                TryClose();
            }
            
            return true;
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
    }
}
