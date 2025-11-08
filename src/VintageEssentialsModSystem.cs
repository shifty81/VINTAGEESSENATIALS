using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

namespace VintageEssentials
{
    public class VintageEssentialsModSystem : ModSystem
    {
        private ICoreServerAPI serverApi;
        private Dictionary<string, Vec3d> playerHomes = new Dictionary<string, Vec3d>();
        private const string HOMES_DATA_KEY = "vintageessentials:homes";

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.serverApi = api;

            // Load saved home locations
            LoadHomes();

            // Register chat commands
            api.ChatCommands.Create("sethome")
                .WithDescription("Sets your home location")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnSetHome);

            api.ChatCommands.Create("home")
                .WithDescription("Warps you to your home location")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnHome);

            api.ChatCommands.Create("rtp")
                .WithDescription("Randomly teleports you in a direction")
                .WithArgs(api.ChatCommands.Parsers.Word("direction"))
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnRtp);

            // Save homes periodically and on shutdown
            api.Event.SaveGameLoaded += OnSaveGameLoaded;
            api.Event.GameWorldSave += OnGameWorldSave;
        }

        private void OnSaveGameLoaded()
        {
            LoadHomes();
        }

        private void OnGameWorldSave()
        {
            SaveHomes();
        }

        private void LoadHomes()
        {
            byte[] data = serverApi.WorldManager.SaveGame.GetData(HOMES_DATA_KEY);
            if (data != null)
            {
                try
                {
                    string json = System.Text.Encoding.UTF8.GetString(data);
                    var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Vec3d>>(json);
                    if (dict != null)
                    {
                        playerHomes = dict;
                    }
                }
                catch (Exception e)
                {
                    serverApi.Logger.Error("VintageEssentials: Failed to load homes data: " + e.Message);
                }
            }
        }

        private void SaveHomes()
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(playerHomes);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                serverApi.WorldManager.SaveGame.StoreData(HOMES_DATA_KEY, data);
            }
            catch (Exception e)
            {
                serverApi.Logger.Error("VintageEssentials: Failed to save homes data: " + e.Message);
            }
        }

        private TextCommandResult OnSetHome(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player == null)
            {
                return TextCommandResult.Error("Only players can use this command.");
            }

            Vec3d pos = player.Entity.Pos.XYZ;
            playerHomes[player.PlayerUID] = pos;
            SaveHomes();

            return TextCommandResult.Success($"Home set at {pos.X:F0}, {pos.Y:F0}, {pos.Z:F0}");
        }

        private TextCommandResult OnHome(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player == null)
            {
                return TextCommandResult.Error("Only players can use this command.");
            }

            if (playerHomes.TryGetValue(player.PlayerUID, out Vec3d homePos))
            {
                player.Entity.TeleportTo(homePos);
                return TextCommandResult.Success("Welcome home!");
            }
            else
            {
                return TextCommandResult.Error("You haven't set a home yet! Use /sethome first.");
            }
        }

        private TextCommandResult OnRtp(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            if (player == null)
            {
                return TextCommandResult.Error("Only players can use this command.");
            }

            string direction = args[0] as string;
            if (string.IsNullOrEmpty(direction))
            {
                return TextCommandResult.Error("Usage: /rtp <north|south|east|west>");
            }

            direction = direction.ToLower();
            if (direction != "north" && direction != "south" && direction != "east" && direction != "west")
            {
                return TextCommandResult.Error("Invalid direction. Use: north, south, east, or west");
            }

            // Calculate random distance between 10,000 and 20,000 blocks
            double minDistance = 10000;
            double maxDistance = 20000;
            Random rand = new Random();
            double distance = rand.NextDouble() * (maxDistance - minDistance) + minDistance;

            Vec3d currentPos = player.Entity.Pos.XYZ;
            double newX = currentPos.X;
            double newZ = currentPos.Z;

            // Calculate new position based on direction
            // In Vintage Story, negative Z is North, positive Z is South
            switch (direction)
            {
                case "north":
                    newZ -= distance;
                    break;
                case "south":
                    newZ += distance;
                    break;
                case "east":
                    newX += distance;
                    break;
                case "west":
                    newX -= distance;
                    break;
            }

            // Find a safe Y coordinate (ground level)
            BlockPos targetPos = new BlockPos((int)newX, 0, (int)newZ);
            int surfaceY = FindSurfaceY(targetPos);

            if (surfaceY > 0)
            {
                Vec3d teleportPos = new Vec3d(newX, surfaceY + 1, newZ);
                player.Entity.TeleportTo(teleportPos);
                return TextCommandResult.Success($"Randomly warped {direction} to {newX:F0}, {surfaceY}, {newZ:F0} ({distance:F0} blocks away)");
            }
            else
            {
                return TextCommandResult.Error("Failed to find a safe location. Please try again.");
            }
        }

        private int FindSurfaceY(BlockPos pos)
        {
            IBlockAccessor blockAccessor = serverApi.World.BlockAccessor;
            int mapHeight = serverApi.World.BlockAccessor.MapSizeY;

            // Start from the top and search downward for the first solid block
            for (int y = mapHeight - 1; y > 0; y--)
            {
                BlockPos checkPos = new BlockPos(pos.X, y, pos.Z);
                Block block = blockAccessor.GetBlock(checkPos);

                if (block != null && block.Id != 0 && block.BlockMaterial != EnumBlockMaterial.Air)
                {
                    // Found a solid block, check if there's air above it
                    Block blockAbove = blockAccessor.GetBlock(checkPos.X, checkPos.Y + 1, checkPos.Z);
                    Block blockAbove2 = blockAccessor.GetBlock(checkPos.X, checkPos.Y + 2, checkPos.Z);
                    
                    if (blockAbove != null && blockAbove.BlockMaterial == EnumBlockMaterial.Air &&
                        blockAbove2 != null && blockAbove2.BlockMaterial == EnumBlockMaterial.Air)
                    {
                        return y;
                    }
                }
            }

            return -1; // No safe location found
        }

        public override void Dispose()
        {
            SaveHomes();
            base.Dispose();
        }
    }
}
