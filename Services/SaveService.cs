using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public string FileVersion { get; set; } = "1.0";
    }

    public class SaveService
    {
        private const int MaxSlots = 5;
        private const int CurrentVersion = 1;
        private const string SavesFolderName = "Saves";

        private readonly string _savesFolder;
        private readonly JsonSerializerOptions _jsonOptions;

        public SaveService()
        {

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _savesFolder = Path.Combine(baseDir, SavesFolderName);

            if (!Directory.Exists(_savesFolder))
                Directory.CreateDirectory(_savesFolder);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        private string SlotPath(int index) => Path.Combine(_savesFolder, $"save_{index}.json");

        public bool SaveToSlot(GameState state, int slotIndex)
        {
            if (state == null)
            {
                Log("SaveToSlot: state == null");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= MaxSlots)
            {
                Log($"SaveToSlot: неверный слот {slotIndex}");
                return false;
            }

            try
            {
                var data = new SaveData
                {
                    State = state,
                    SavedAt = DateTime.UtcNow,
                    Version = CurrentVersion
                };

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var path = SlotPath(slotIndex);

                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(tempPath, path);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Ошибка сохранения в слот {slotIndex}: {ex.Message}");
                return false;
            }
        }

        public GameState LoadFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return null;

            var path = SlotPath(slotIndex);
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                if (data?.State == null)
                {
                    Log($"LoadFromSlot: сейв {slotIndex} пустой");
                    return null;
                }

 
                if (data.Version > CurrentVersion)
                {
                    Log($"LoadFromSlot: сейв {slotIndex} новее текущей версии игры");
                    return null;
                }

                return data.State;
            }
            catch (JsonException ex)
            {
                Log($"LoadFromSlot: сейв {slotIndex} повреждён (JSON): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log($"LoadFromSlot: ошибка загрузки {slotIndex}: {ex.Message}");
                return null;
            }
        }

        public bool DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;

            var path = SlotPath(slotIndex);
            if (!File.Exists(path)) return false;

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Log($"DeleteSlot {slotIndex}: {ex.Message}");
                return false;
            }
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
                    var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);

                    if (data?.State?.Player != null)
                    {
                        list.Add(new SaveSlotInfo
                        {
                            SlotIndex = i,
                            PlayerName = data.State.Player.Name ?? "Игрок",
                            Chapter = data.State.Player.Chapter,
                            Money = data.State.Player.Money,
                            SavedAt = data.SavedAt.ToLocalTime()
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log($"GetSaveSlots: слот {i} повреждён: {ex.Message}");
                }
            }

            return list;
        }


        public int AutoSave(GameState state)
        {

            for (int i = 0; i < MaxSlots; i++)
            {
                if (!File.Exists(SlotPath(i)))
                {
                    if (SaveToSlot(state, i)) return i;
                }
            }


            int last = MaxSlots - 1;
            if (SaveToSlot(state, last)) return last;
            return -1;
        }


        private void Log(string message)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SaveService] {message}");
#endif
        }


        private class SaveData
        {
            public int Version { get; set; } = 1;
            public GameState State { get; set; }
            public DateTime SavedAt { get; set; }
        }
    }
}
