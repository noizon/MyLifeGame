using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MyLifeGame.Models;

namespace MyLifeGame.Services
{
    public class SaveSlotInfo
    {
        public int SlotIndex { get; set; }
        public string PlayerName { get; set; }
        public int Chapter { get; set; }
        public int Money { get; set; }
        public DateTime SavedAt { get; set; }
    }

    public class SaveService
    {
        private const int MaxSlots = 5;
        private const string Folder = "Saves";

        public SaveService()
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
        }

        private string SlotPath(int index) => Path.Combine(Folder, $"save_{index}.json");

        public void SaveToSlot(GameState state, int slotIndex)
        {
            if (state == null) return;

            var data = new SaveData
            {
                State = state,
                SavedAt = DateTime.Now
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SlotPath(slotIndex), json);
        }

        public GameState LoadFromSlot(int slotIndex)
        {
            var path = SlotPath(slotIndex);
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<SaveData>(json);
                return data?.State;
            }
            catch
            {
                return null;
            }
        }

        public void DeleteSlot(int slotIndex)
        {
            var path = SlotPath(slotIndex);
            if (File.Exists(path))
                File.Delete(path);
        }

        public List<SaveSlotInfo> GetSaveSlots()
        {
            var list = new List<SaveSlotInfo>();

            for (int i = 0; i < MaxSlots; i++)
            {
                var path = SlotPath(i);
                if (!File.Exists(path)) continue;

                try
                {
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<SaveData>(json);

                    if (data?.State?.Player != null)
                    {
                        list.Add(new SaveSlotInfo
                        {
                            SlotIndex = i,
                            PlayerName = data.State.Player.Name,
                            Chapter = data.State.Player.Chapter,
                            Money = data.State.Player.Money,
                            SavedAt = data.SavedAt
                        });
                    }
                }
                catch { }
            }

            return list;
        }

        // ⚠️ Совместимость со старым кодом (MainForm использует Save(state))
        public void Save(GameState state)
        {
            // Находим свободный слот или перезаписываем 0
            int slot = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!File.Exists(SlotPath(i))) { slot = i; break; }
                slot = i;
            }
            SaveToSlot(state, slot);
        }

        private class SaveData
        {
            public GameState State { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}