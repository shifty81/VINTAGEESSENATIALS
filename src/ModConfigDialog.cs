using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace VintageEssentials
{
    public class ModConfigDialog : GuiDialogGeneric
    {
        private new ICoreClientAPI capi;
        private ModConfig config;

        public ModConfigDialog(ICoreClientAPI capi, ModConfig config) : base("Mod Settings", capi)
        {
            this.capi = capi;
            this.config = config;
        }

        private void ComposeDialog()
        {
            double elemWidth = 400;
            
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            
            ElementBounds maxSlotsLabelBounds = ElementBounds.Fixed(0, 30, elemWidth / 2, 30);
            ElementBounds maxSlotsInputBounds = ElementBounds.Fixed(elemWidth / 2 + 10, 30, elemWidth / 2 - 10, 30);
            
            ElementBounds keybindsLabelBounds = ElementBounds.Fixed(0, 70, elemWidth, 30);
            ElementBounds keybindsTextBounds = ElementBounds.Fixed(0, 100, elemWidth, 100);
            
            ElementBounds saveButtonBounds = ElementBounds.Fixed(0, 210, 180, 30);
            ElementBounds cancelButtonBounds = ElementBounds.Fixed(190, 210, 180, 30);
            
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(maxSlotsLabelBounds, maxSlotsInputBounds, keybindsLabelBounds, 
                                  keybindsTextBounds, saveButtonBounds, cancelButtonBounds);

            SingleComposer = capi.Gui.CreateCompo("modconfig", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("VintageEssentials Settings", OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddStaticText("Max Locked Slots:", CairoFont.WhiteDetailText(), maxSlotsLabelBounds)
                    .AddNumberInput(maxSlotsInputBounds, OnMaxSlotsChanged, CairoFont.WhiteDetailText(), "maxslotsInput")
                    .AddStaticText("Keybinds (configure in Options > Controls):", CairoFont.WhiteDetailText(), keybindsLabelBounds)
                    .AddStaticText(
                        "• R - Open Chest Radius Inventory\n" +
                        "• Shift+S - Sort Player Inventory\n" +
                        "• Ctrl+L - Toggle Slot Locking Mode",
                        CairoFont.WhiteSmallText(), keybindsTextBounds)
                    .AddSmallButton("Save", OnSaveClicked, saveButtonBounds)
                    .AddSmallButton("Cancel", OnCancelClicked, cancelButtonBounds)
                .EndChildElements()
                .Compose();

            // Set initial value
            SingleComposer.GetNumberInput("maxslotsInput").SetValue(config.MaxLockedSlots);
        }

        private void OnMaxSlotsChanged(string value)
        {
            if (int.TryParse(value, out int slots))
            {
                config.MaxLockedSlots = Math.Max(1, Math.Min(20, slots)); // Clamp between 1 and 20
            }
        }

        private bool OnSaveClicked()
        {
            config.Save(capi);
            capi.ShowChatMessage("Settings saved successfully!");
            TryClose();
            return true;
        }

        private bool OnCancelClicked()
        {
            // Reload config to discard changes
            config = ModConfig.Load(capi);
            TryClose();
            return true;
        }

        private void OnTitleBarClose()
        {
            OnCancelClicked();
        }

        public override bool TryOpen()
        {
            ComposeDialog();
            return base.TryOpen();
        }

        public override string ToggleKeyCombinationCode => "modconfig";
    }
}
