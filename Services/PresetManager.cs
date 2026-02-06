using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services
{
    public class PresetManager
    {
        private readonly string _presetPath;
        public List<Preset> Presets { get; private set; } = new List<Preset>();
        
        public PresetManager(string fileName)
        {
            var presetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preset");

            if (!Directory.Exists(presetFolder))
            {
                Directory.CreateDirectory(presetFolder);
            }

            // 기존 presets.json → presets_1.json 마이그레이션
            var oldPath = Path.Combine(presetFolder, "presets.json");
            _presetPath = Path.Combine(presetFolder, fileName);
            if (File.Exists(oldPath) && !File.Exists(_presetPath) && fileName == "presets_1.json")
            {
                File.Copy(oldPath, _presetPath);
            }

            Load();
        }
        
        public void Load()
        {
            try
            {
                if (File.Exists(_presetPath))
                {
                    string json = File.ReadAllText(_presetPath);
                    Presets = JsonSerializer.Deserialize<List<Preset>>(json) ?? new List<Preset>();
                }
            }
            catch (Exception)
            {
                Presets = new List<Preset>();
            }
        }
        
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Presets, options);
                File.WriteAllText(_presetPath, json);
            }
            catch (Exception)
            {
                // 저장 실패 시 무시
            }
        }
        
        public void AddPreset(Preset preset)
        {
            preset.CreatedAt = DateTime.Now;
            Presets.Add(preset);
            Save();
        }
        
        public void UpdatePreset(int index, Preset preset)
        {
            if (index >= 0 && index < Presets.Count)
            {
                preset.CreatedAt = Presets[index].CreatedAt;
                Presets[index] = preset;
                Save();
            }
        }
        
        public void DeletePreset(int index)
        {
            if (index >= 0 && index < Presets.Count)
            {
                Presets.RemoveAt(index);
                Save();
            }
        }
        
        public Preset GetPreset(int index)
        {
            if (index >= 0 && index < Presets.Count)
            {
                return Presets[index];
            }
            return null;
        }
        
        public List<string> GetPresetNames()
        {
            var names = new List<string>();
            foreach (var preset in Presets)
            {
                names.Add(preset.Name);
            }
            return names;
        }
    }
}
