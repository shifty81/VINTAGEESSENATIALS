using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageEssentials
{
    public class LockedSlotsManager
    {
        private Dictionary<string, HashSet<int>> playerLockedSlots = new Dictionary<string, HashSet<int>>();
        private const string LOCKED_SLOTS_DATA_KEY = "vintageessentials:lockedslots";
        private ICoreAPI api;
        private ModConfig config;

        public LockedSlotsManager(ICoreAPI api, ModConfig config)
        {
            this.api = api;
            this.config = config;
        }

        public void Load(ICoreServerAPI serverApi)
        {
            if (serverApi == null) return;
            
            byte[] data = serverApi.WorldManager.SaveGame.GetData(LOCKED_SLOTS_DATA_KEY);
            if (data != null)
            {
                try
                {
                    string json = System.Text.Encoding.UTF8.GetString(data);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, HashSet<int>>>(json);
                    if (dict != null)
                    {
                        playerLockedSlots = dict;
                    }
                }
                catch (Exception e)
                {
                    api.Logger.Error("VintageEssentials: Failed to load locked slots data: " + e.Message);
                }
            }
        }

        public void Save(ICoreServerAPI serverApi)
        {
            if (serverApi == null) return;
            
            try
            {
                string json = JsonSerializer.Serialize(playerLockedSlots);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                serverApi.WorldManager.SaveGame.StoreData(LOCKED_SLOTS_DATA_KEY, data);
            }
            catch (Exception e)
            {
                api.Logger.Error("VintageEssentials: Failed to save locked slots data: " + e.Message);
            }
        }

        public bool IsSlotLocked(string playerUid, int slotId)
        {
            if (playerLockedSlots.TryGetValue(playerUid, out HashSet<int> lockedSlots))
            {
                return lockedSlots.Contains(slotId);
            }
            return false;
        }

        public bool ToggleSlotLock(string playerUid, int slotId)
        {
            if (!playerLockedSlots.ContainsKey(playerUid))
            {
                playerLockedSlots[playerUid] = new HashSet<int>();
            }

            HashSet<int> lockedSlots = playerLockedSlots[playerUid];
            
            if (lockedSlots.Contains(slotId))
            {
                // Unlock
                lockedSlots.Remove(slotId);
                return false;
            }
            else
            {
                // Lock
                if (lockedSlots.Count >= config.MaxLockedSlots)
                {
                    return false; // Cannot lock more slots
                }
                lockedSlots.Add(slotId);
                return true;
            }
        }

        public int GetLockedSlotsCount(string playerUid)
        {
            if (playerLockedSlots.TryGetValue(playerUid, out HashSet<int> lockedSlots))
            {
                return lockedSlots.Count;
            }
            return 0;
        }

        public HashSet<int> GetLockedSlots(string playerUid)
        {
            if (playerLockedSlots.TryGetValue(playerUid, out HashSet<int> lockedSlots))
            {
                return new HashSet<int>(lockedSlots);
            }
            return new HashSet<int>();
        }

        public void ClearLockedSlots(string playerUid)
        {
            if (playerLockedSlots.ContainsKey(playerUid))
            {
                playerLockedSlots[playerUid].Clear();
            }
        }
    }
}
