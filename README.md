# Vintage Essentials

A comprehensive mod for Vintage Story that adds essential commands and quality of life improvements.

## Features

### Chat Commands

- **`/sethome`** - Sets your home location at your current position
- **`/home`** - Teleports you to your saved home location
- **`/rtp <direction>`** - Randomly teleports you 10,000-20,000 blocks in the specified direction (north, south, east, or west)

### Chest Radius Inventory (NEW!)

- **Press `R`** - Opens a special inventory dialog showing all items in chests within 15 blocks
- **Searchable** - Type to filter items by name
- **Sortable** - Cycle through sort modes (none, name, quantity)
- **Scrollable Grid** - View all items in an organized grid layout
- **Deposit All** - Quickly move items from your inventory to nearby chests
- **Take All** - Pull items from nearby chests into your inventory
- Integrates seamlessly with the game's inventory system

### Player Inventory Sorting

- **Press `Shift+S`** - Sort your player inventory
- Cycles through sorting modes: Name → Quantity → Type
- Quick and convenient organization

### Stack Size Increases

- All items now stack up to **1000** (increased from default values)
- Storage containers have increased capacity:
  - Chests: 32 slots
  - Storage Vessels: 24 slots
  - Crates: 48 slots

## Installation

1. Build the mod or download the compiled `.zip` file
2. Place `VintageEssentials.zip` in your `VintagestoryData/Mods` folder
3. **The mod must be installed on both client and server** for all features to work
4. Restart your game/server

## Keybinds

- **`R`** - Open Chest Radius Inventory (shows all items in chests within 15 blocks)
- **`Shift+S`** - Sort Player Inventory (cycles through sort modes)

You can rebind these keys in the game's Controls settings.

## Building

### Requirements
- .NET 7.0 SDK or later ([Download here](https://dotnet.microsoft.com/download))
- Vintage Story installed ([Get it here](https://www.vintagestory.at/))

### Quick Build

If your Vintage Story installation is in one of these default locations, you can simply run:

```bash
dotnet build
```

The project will automatically detect Vintage Story in:
- Windows: `C:\Program Files\Vintagestory`
- Linux: `/usr/share/vintagestory` or `~/.local/share/Vintagestory`

The compiled mod will be in `bin/Debug/VintageEssentials.zip`.

### Custom Installation Path

If Vintage Story is installed in a different location, set the `VINTAGE_STORY` environment variable:

**Windows (PowerShell):**
```powershell
$env:VINTAGE_STORY = "C:\Path\To\Vintagestory"
dotnet build
```

**Windows (Command Prompt):**
```cmd
set VINTAGE_STORY=C:\Path\To\Vintagestory
dotnet build
```

**Linux/macOS (Bash):**
```bash
export VINTAGE_STORY="/path/to/Vintagestory"
dotnet build
```

Or set it inline:
```bash
VINTAGE_STORY="/path/to/Vintagestory" dotnet build
```

### Build Configurations

**Debug Build (default):**
```bash
dotnet build
```
Output: `bin/Debug/VintageEssentials.zip`

**Release Build (optimized):**
```bash
dotnet build -c Release
```
Output: `bin/Release/VintageEssentials.zip`

### Finding Your Vintage Story Installation

**Windows:**
- Default: `C:\Program Files\Vintagestory`
- Steam: Right-click Vintage Story in Steam → Manage → Browse local files

**Linux:**
- System install: `/usr/share/vintagestory`
- User install: `~/.local/share/Vintagestory`
- Steam: `~/.steam/steam/steamapps/common/VintageStory`

**macOS:**
- Default: `~/Library/Application Support/Vintagestory`
- Steam: `~/Library/Application Support/Steam/steamapps/common/VintageStory`

### Testing Your Build

1. Build the mod using one of the methods above
2. Locate the generated `VintageEssentials.zip` file in `bin/Debug/` or `bin/Release/`
3. Copy it to your Vintage Story mods folder:
   - Windows: `%APPDATA%\VintagestoryData\Mods`
   - Linux: `~/.config/VintagestoryData/Mods`
   - macOS: `~/Library/Application Support/VintagestoryData/Mods`
4. Launch Vintage Story
5. Check the Mod Manager (Esc → Mod Manager) to verify the mod loaded successfully

### Troubleshooting

**Error: "Could not find a part of the path"**
- The `VINTAGE_STORY` environment variable is not set correctly
- Verify your Vintage Story installation path exists
- Check that `VintagestoryAPI.dll` exists in that directory

**Error: "The specified framework 'Microsoft.NETCore.App', version '7.0.0' was not found"**
- Install .NET 7.0 SDK or later from https://dotnet.microsoft.com/download
- Verify installation with `dotnet --version`

**Build succeeds but mod doesn't work in-game:**
- Ensure you copied the mod to the correct mods folder
- Check the Vintage Story logs for errors (found in the `Logs` folder next to `Mods`)
- Make sure you're using a compatible Vintage Story version (1.19.0 or later)

## Usage

### Setting and Using Home

1. Navigate to where you want your home to be
2. Type `/sethome` in chat
3. Use `/home` anytime to teleport back

### Random Teleport

Use `/rtp` with a direction to explore:
- `/rtp north` - Teleport 10,000-20,000 blocks north
- `/rtp south` - Teleport 10,000-20,000 blocks south
- `/rtp east` - Teleport 10,000-20,000 blocks east
- `/rtp west` - Teleport 10,000-20,000 blocks west

The mod will find a safe landing spot at ground level.

### Chest Radius Inventory

1. Stand near your storage area (chests within 15 blocks)
2. Press `R` to open the Chest Radius Inventory
3. Use the search box to find specific items
4. Click "Sort" to cycle through sorting modes
5. Use "Deposit All" to move items from your inventory to chests
6. Use "Take All" to retrieve items from chests to your inventory
7. Scroll through the grid to see all available items

### Sorting Your Inventory

1. Open your player inventory
2. Press `Shift+S` to sort
3. Press again to cycle through different sort modes (Name → Quantity → Type)

## Data Storage

Home locations are saved persistently in the world save data and will survive server restarts.

## Permissions

All commands require the `chat` privilege, which all players have by default.

## License

See LICENSE file for details.
