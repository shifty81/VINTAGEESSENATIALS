# Vintage Essentials

A server-side mod for Vintage Story that adds essential commands and quality of life improvements.

## Features

### Chat Commands

- **`/sethome`** - Sets your home location at your current position
- **`/home`** - Teleports you to your saved home location
- **`/rtp <direction>`** - Randomly teleports you 10,000-20,000 blocks in the specified direction (north, south, east, or west)

### Stack Size Increases

- All items now stack up to **1000** (increased from default values)
- Storage containers have increased capacity:
  - Chests: 32 slots
  - Storage Vessels: 24 slots
  - Crates: 48 slots

## Installation

1. Build the mod or download the compiled `.zip` file
2. Place `VintageEssentials.zip` in your `VintagestoryData/Mods` folder
3. The mod must be installed on the server for commands to work
4. Client installation is optional but recommended

## Building

Requirements:
- .NET 7.0 SDK or later
- Vintage Story installed

Set the `VINTAGE_STORY` environment variable to your Vintage Story installation path, then run:

```bash
dotnet build
```

The compiled mod will be in `bin/Release` or `bin/Debug` as `VintageEssentials.zip`.

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

## Data Storage

Home locations are saved persistently in the world save data and will survive server restarts.

## Permissions

All commands require the `chat` privilege, which all players have by default.

## License

See LICENSE file for details.
