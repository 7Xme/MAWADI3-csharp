using System.Text.Json;

namespace Mawadi3Print.Services;

public class StorageService
{
    private readonly string _settingsPath;

    public StorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "Mawadi3Print");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    private async Task<Dictionary<string, string>> LoadAllAsync()
    {
        if (!File.Exists(_settingsPath))
            return new Dictionary<string, string>();
        var json = await File.ReadAllTextAsync(_settingsPath);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    private async Task SaveAllAsync(Dictionary<string, string> data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public async Task<string?> GetGeminiKeyAsync()
    {
        var data = await LoadAllAsync();
        return data.TryGetValue("GeminiKey", out var key) ? key : null;
    }

    public async Task SaveGeminiKeyAsync(string apiKey)
    {
        var data = await LoadAllAsync();
        data["GeminiKey"] = apiKey;
        await SaveAllAsync(data);
    }

    public async Task ClearGeminiKeyAsync()
    {
        var data = await LoadAllAsync();
        data.Remove("GeminiKey");
        await SaveAllAsync(data);
    }

    public async Task<bool> HasAnyKeyAsync()
    {
        var data = await LoadAllAsync();
        return data.ContainsKey("GeminiKey") && !string.IsNullOrWhiteSpace(data["GeminiKey"]);
    }
}