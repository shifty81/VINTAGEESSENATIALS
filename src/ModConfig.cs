using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace VintageEssentials
{
    public class ModConfig
    {
        public int MaxLockedSlots { get; set; } = 10;
        public Dictionary<string, string> CustomKeybinds { get; set; } = new Dictionary<string, string>();
        
        public static ModConfig Load(ICoreAPI api)
        {
            try
            {
                var config = api.LoadModConfig<ModConfig>("vintageessentials.json");
                if (config == null)
                {
                    config = new ModConfig();
                    api.StoreModConfig(config, "vintageessentials.json");
                }
                return config;
            }
            catch (Exception e)
            {
                api.Logger.Error("VintageEssentials: Failed to load config, using defaults: " + e.Message);
                return new ModConfig();
            }
        }
        
        public void Save(ICoreAPI api)
        {
            try
            {
                api.StoreModConfig(this, "vintageessentials.json");
            }
            catch (Exception e)
            {
                api.Logger.Error("VintageEssentials: Failed to save config: " + e.Message);
            }
        }
    }
}
