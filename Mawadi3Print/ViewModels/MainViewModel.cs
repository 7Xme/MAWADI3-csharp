using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Mawadi3Print.Models;
using Mawadi3Print.Services;

namespace Mawadi3Print.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService = new();
    private readonly StorageService _storageService = new();
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _topic = "فوائد المشي يومياً وتاريخه";

    [ObservableProperty]
    private string _selectedLanguage = "fr";

    [ObservableProperty]
    private ComboItem _selectedModel = new("gemini-2.5-flash", "Gemini 2.5 Flash");

    [ObservableProperty]
    private string? _apiKeyStatus;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Article? _currentArticle;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private bool _isWordCountGood;

    [ObservableProperty]
    private string? _snackbarMessage;

    [ObservableProperty]
    private bool _isSnackbarVisible;

    public ObservableCollection<ComboItem> Models { get; } =
    [
        new("gemini-3-pro", "Gemini 3 Pro"),
        new("gemini-3-flash", "Gemini 3 Flash"),
        new("gemini-2.5-pro", "Gemini 2.5 Pro (الأقوى)"),
        new("gemini-2.5-flash", "Gemini 2.5 Flash"),
        new("gemini-2.0-flash", "Gemini 2.0 Flash"),
        new("gemini-2.0-flash-lite", "Gemini 2.0 Flash Lite"),
        new("gemini-1.5-pro", "Gemini 1.5 Pro"),
        new("gemini-1.5-flash", "Gemini 1.5 Flash"),
        new("gemini-1.5-flash-8b", "Gemini 1.5 Flash 8B (خفيف)"),
        new("gemma-4-26b-a4b-it", "Gemma 4 26B (IT)"),
        new("gemma-4-31b-it", "Gemma 4 31B (IT)"),
        new("gemini-flash-latest", "Gemini Flash Latest"),
        new("gemini-pro-latest", "Gemini Pro Latest")
    ];

    public MainViewModel()
    {
    }

    public async Task InitializeAsync()
    {
        HasApiKey = await _storageService.HasAnyKeyAsync();
        UpdateApiKeyStatus();
    }

    private void UpdateApiKeyStatus()
    {
        ApiKeyStatus = HasApiKey ? "المفتاح: موجود" : "ماكاينش مفتاح";
    }

    [RelayCommand]
    private async Task GenerateArticleAsync()
    {
        if (string.IsNullOrWhiteSpace(Topic))
        {
            ErrorMessage = "الرجاء إدخال الموضوع";
            HasError = true;
            return;
        }

        if (!HasApiKey)
        {
            ErrorMessage = "ماكاينش مفتاح API. افتح الإعدادات (الترس) ودخل مفتاح Gemini.";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var key = await _storageService.GetGeminiKeyAsync();
            if (string.IsNullOrEmpty(key))
            {
                ErrorMessage = "ماكاينش مفتاح API. افتح الإعدادات (الترس) ودخل مفتاح Gemini.";
                HasError = true;
                return;
            }

            var article = await _apiService.GenerateArticleAsync(
                key, Topic, SelectedLanguage, SelectedModel.Value, _cts.Token);

            CurrentArticle = article;
            WordCount = article.WordCount;
            IsWordCountGood = article.WordCount >= 300 && article.WordCount <= 500;

            if (article.WordCount < 150 && article.WordCount > 0)
            {
                ShowSnackbar($"لمقال فيه غير {article.WordCount} كلمة (المفروض مابين 350 و 400)");
            }
            else if (article.WordCount >= 150 && article.WordCount < 300)
            {
                ShowSnackbar($"لمقال فيه {article.WordCount} كلمة (قريب من المطلوب)");
            }
            else if (article.WordCount > 500)
            {
                ShowSnackbar($"لمقال فيه {article.WordCount} كلمة (شويا زايد).");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CopyArticleAsync()
    {
        if (CurrentArticle == null) return;
        try
        {
            var text = $"{CurrentArticle.Title}\n\n{CurrentArticle.Content}";
            System.Windows.Clipboard.SetText(text);
            ShowSnackbar("تنسخ للحافظة");
        }
        catch (Exception ex)
        {
            ShowSnackbar($"النسخ فشل: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task PrintArticleAsync()
    {
        if (CurrentArticle == null) return;
        try
        {
            await PrintService.PrintArticleAsync(
                CurrentArticle.Title, CurrentArticle.Content, SelectedLanguage);
        }
        catch (Exception ex)
        {
            ShowSnackbar($"الطابع فشل: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SavePdfAsync()
    {
        if (CurrentArticle == null) return;
        try
        {
            await PrintService.SaveArticleAsPdfAsync(
                CurrentArticle.Title, CurrentArticle.Content, SelectedLanguage);
            ShowSnackbar("تَحْفَظ الـ PDF بنجاح");
        }
        catch (OperationCanceledException)
        {
            // User cancelled, nothing to show
        }
        catch (Exception ex)
        {
            ShowSnackbar($"الحفظ فشل: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var viewModel = new SettingsViewModel(_storageService);
        var dialog = new Views.SettingsDialog { DataContext = viewModel };

        viewModel.OnSaved += () =>
        {
            HasApiKey = true;
            UpdateApiKeyStatus();
        };

        viewModel.OnCleared += () =>
        {
            HasApiKey = false;
            UpdateApiKeyStatus();
        };

        viewModel.RequestClose = () =>
        {
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        };

        await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
    }

    private void ShowSnackbar(string message)
    {
        SnackbarMessage = message;
        IsSnackbarVisible = true;
    }

    partial void OnTopicChanged(string value)
    {
        HasError = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _cts?.Cancel();
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}

public class ComboItem
{
    public string Value { get; }
    public string Display { get; }

    public ComboItem(string value, string display)
    {
        Value = value;
        Display = display;
    }

    public override string ToString() => Display;
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly StorageService _storage;

    [ObservableProperty]
    private string? _existingKeyPreview;

    [ObservableProperty]
    private bool _hasExistingKey;

    [ObservableProperty]
    private string? _newKey;

    [ObservableProperty]
    private string? _errorMessage;

    public event Action? OnSaved;
    public event Action? OnCleared;
    public Action? RequestClose { get; set; }

    public SettingsViewModel(StorageService storage)
    {
        _storage = storage;
        _ = LoadExistingKeyAsync();
    }

    private async Task LoadExistingKeyAsync()
    {
        var key = await _storage.GetGeminiKeyAsync();
        if (!string.IsNullOrEmpty(key))
        {
            HasExistingKey = true;
            ExistingKeyPreview = $"مفتاح Gemini محفوظ: ...{key[^6..]}";
        }
    }

    [RelayCommand]
    private async Task SaveKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKey))
        {
            ErrorMessage = "المفتاح ما يقدرش يكون فارغ";
            return;
        }

        ErrorMessage = null;
        await _storage.SaveGeminiKeyAsync(NewKey.Trim());
        HasExistingKey = true;
        ExistingKeyPreview = $"مفتاح Gemini محفوظ: ...{NewKey.Trim()[^6..]}";
        NewKey = null;
        OnSaved?.Invoke();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private async Task ClearKeyAsync()
    {
        await _storage.ClearGeminiKeyAsync();
        HasExistingKey = false;
        ExistingKeyPreview = null;
        OnCleared?.Invoke();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private async Task OpenLinkAsync()
    {
        var url = "https://aistudio.google.com/app/apikey";
        try
        {
            System.Windows.Clipboard.SetText(url);
        }
        catch { }
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
        await Task.CompletedTask;
    }
}