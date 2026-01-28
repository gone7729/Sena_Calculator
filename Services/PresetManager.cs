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
        
        public PresetManager()
        {
            // exe가 있는 경로에 preset 폴더 생성
            var presetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preset");
            
            // 폴더가 없으면 생성
            if (!Directory.Exists(presetFolder))
            {
                Directory.CreateDirectory(presetFolder);
            }
            
            _presetPath = Path.Combine(presetFolder, "presets.json");
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
