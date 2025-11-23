using System.IO;
using System.Text.Json;
using WslTamer.UI.Models;

namespace WslTamer.UI.Services;

public class ProfileManager
{
    private readonly string _configPath;
    private AppConfig _config;

    public ProfileManager()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WslTamer");
        Directory.CreateDirectory(appData);
        _configPath = Path.Combine(appData, "config.json");
        _config = LoadConfig();
    }

    public List<WslProfile> GetProfiles() => _config.Profiles;
    public List<AutomationRule> GetRules() => _config.Rules;

    public void AddProfile(WslProfile profile)
    {
        _config.Profiles.Add(profile);
        SaveConfig();
    }

    public void UpdateProfile(WslProfile profile)
    {
        var index = _config.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index != -1)
        {
            _config.Profiles[index] = profile;
            SaveConfig();
        }
    }

    public void RemoveProfile(Guid id)
    {
        _config.Profiles.RemoveAll(p => p.Id == id);
        // Also remove rules associated with this profile
        _config.Rules.RemoveAll(r => r.TargetProfileId == id);
        SaveConfig();
    }

    public void AddRule(AutomationRule rule)
    {
        _config.Rules.Add(rule);
        SaveConfig();
    }

    public void RemoveRule(Guid id)
    {
        _config.Rules.RemoveAll(r => r.Id == id);
        SaveConfig();
    }
    
    public void UpdateRule(AutomationRule rule)
    {
        var index = _config.Rules.FindIndex(r => r.Id == rule.Id);
        if (index != -1)
        {
            _config.Rules[index] = rule;
            SaveConfig();
        }
    }

    public void ReorderProfiles(WslProfile source, WslProfile target)
    {
        var oldIndex = _config.Profiles.IndexOf(source);
        var newIndex = _config.Profiles.IndexOf(target);

        if (oldIndex != -1 && newIndex != -1)
        {
            _config.Profiles.RemoveAt(oldIndex);
            _config.Profiles.Insert(newIndex, source);
            SaveConfig();
        }
    }

    public WslProfile? GetProfile(Guid id)
    {
        return _config.Profiles.FirstOrDefault(p => p.Id == id);
    }

    private AppConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? CreateDefaultConfig();
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    private AppConfig CreateDefaultConfig()
    {
        var defaults = new AppConfig();
        
        defaults.Profiles.Add(new WslProfile 
        { 
            Name = "Eco Mode", 
            Memory = "4GB", 
            Processors = 2,
            Swap = "0"
        });
        
        defaults.Profiles.Add(new WslProfile 
        { 
            Name = "Balanced", 
            Memory = "8GB", 
            Processors = 4,
            Swap = "2GB"
        });
        
        defaults.Profiles.Add(new WslProfile 
        { 
            Name = "Unleashed", 
            Memory = "32GB", 
            Processors = 16,
            Swap = "8GB"
        });

        return defaults;
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
