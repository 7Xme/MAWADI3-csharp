# Mawadi3Print (مَواضيع_print)

A Windows desktop article generator that uses the Google Gemini API to create 350–400 word articles in French or Arabic, with native Windows print and PDF export support.

## Tech Stack

- **Framework**: .NET 8 WPF
- **UI**: MaterialDesignInXamlToolkit
- **MVVM**: CommunityToolkit.Mvvm
- **PDF**: QuestPDF
- **Printing**: WPF Native PrintDialog + FlowDocument
- **Settings**: JSON file in `%LocalAppData%\Mawadi3Print\settings.json`

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Google Gemini API key (get one free at https://aistudio.google.com/app/apikey)

## Build & Run

```bash
cd Mawadi3Print
dotnet restore
dotnet build
dotnet run
```

## Project Structure

```
Mawadi3Print/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Models/
│   └── Article.cs
├── Services/
│   ├── ApiService.cs       # Gemini API integration
│   ├── StorageService.cs   # Settings persistence
│   └── PrintService.cs     # Print & PDF export
├── ViewModels/
│   └── MainViewModel.cs    # Main window logic + Settings VM
├── Views/
│   └── SettingsDialog.xaml / .cs
├── Assets/
│   ├── logo.png
│   └── fonts/
│       ├── Cairo-Regular.ttf
│       └── NotoSansArabic-Regular.ttf
└── Mawadi3Print.csproj
```

## Usage

1. Launch the app — if no API key is configured, the settings dialog opens automatically.
2. Enter a topic, select language (French or Arabic), and choose a Gemini model.
3. Click **وِلّد المقال** (Generate Article).
4. Preview the article, then **طبع** (Print) or **حفظ كـ PDF** (Save as PDF).