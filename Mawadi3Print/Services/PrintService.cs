using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Markup;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mawadi3Print.Services;

public static class PrintService
{
    private static FontFamily? _arabicFont;
    private static bool _arabicFontAttempted;

    private static FontFamily GetArabicFont()
    {
        if (_arabicFont != null) return _arabicFont;
        if (_arabicFontAttempted) return new FontFamily("Segoe UI");

        _arabicFontAttempted = true;
        try
        {
            var uri = new Uri("pack://application:,,,/Mawadi3Print;component/Assets/fonts/NotoSansArabic-Regular.ttf");
            _arabicFont = new FontFamily(uri, "./#Noto Sans Arabic");
            return _arabicFont;
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine("Warning: Arabic font not available, using system default.");
            return new FontFamily("Segoe UI");
        }
    }

    public static async Task PrintArticleAsync(string title, string content, string language)
    {
        await Task.CompletedTask; // Ensure method is async-compatible

        var rtl = language == "ar";
        var arabicFont = GetArabicFont();

        var flowDoc = new FlowDocument
        {
            PageSize = new Size(96 * 8.27, 96 * 11.69), // A4
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            FontFamily = rtl ? arabicFont : new FontFamily("Segoe UI"),
            ColumnWidth = double.MaxValue,
            PagePadding = new Thickness(96 * 0.79) // ~2cm
        };

        // Title
        var titleParagraph = new Paragraph(new Run(title))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = rtl ? TextAlignment.Right : TextAlignment.Left,
            FontFamily = rtl ? arabicFont : new FontFamily("Segoe UI")
        };
        flowDoc.Blocks.Add(titleParagraph);

        // Divider
        flowDoc.Blocks.Add(new BlockUIContainer(new Separator
        {
            Margin = new Thickness(0, 4, 0, 8),
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(128, 128, 128))
        }));

        // Content paragraphs
        var paragraphs = content.Split(["\n\n"], StringSplitOptions.None);
        foreach (var para in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(para)) continue;
            var paraElement = new Paragraph(new Run(para.Trim()))
            {
                FontSize = 11,
                LineHeight = 16.5,
                TextAlignment = rtl ? TextAlignment.Right : TextAlignment.Left,
                FontFamily = rtl ? arabicFont : new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 6)
            };
            flowDoc.Blocks.Add(paraElement);
        }

        // Footer
        var footerText = $"توليد بواسطة مكتبة العلوم • {DateTime.Now.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        var footerParagraph = new Paragraph(new Run(footerText))
        {
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
            TextAlignment = TextAlignment.Center,
            FontFamily = rtl ? arabicFont : new FontFamily("Segoe UI"),
            Margin = new Thickness(0, 20, 0, 0)
        };
        flowDoc.Blocks.Add(footerParagraph);

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            try
            {
                printDialog.PrintDocument(
                    ((IDocumentPaginatorSource)flowDoc).DocumentPaginator,
                    $"مَواضيع_print - {title}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"الطابع فشل: {ex.Message}");
            }
        }
    }

    public static async Task SaveArticleAsPdfAsync(string title, string content, string language)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "حفظ كـ PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(title) + ".pdf"
        };

        if (saveDialog.ShowDialog() != true)
            throw new OperationCanceledException("المستخدم غلغى الحفظ");

        var rtl = language == "ar";
        var arabicFontName = "NotoSansArabic";
        var fontPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Assets", "fonts", "NotoSansArabic-Regular.ttf");

        if (!File.Exists(fontPath))
        {
            // Try to extract from embedded resource
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("NotoSansArabic-Regular.ttf"));
                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        var dir = Path.GetDirectoryName(fontPath)!;
                        Directory.CreateDirectory(dir);
                        using var fileStream = File.Create(fontPath);
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                {
                    if (rtl && File.Exists(fontPath))
                        x = x.FontFamily(arabicFontName);
                    return x;
                });

                if (rtl)
                    page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Title
                    col.Item().Text(title)
                        .FontSize(20).Bold()
                        .AlignLeft();

                    // Divider
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey);

                    // Content paragraphs
                    var paragraphs = content.Split(["\n\n"], StringSplitOptions.None);
                    foreach (var para in paragraphs)
                    {
                        if (string.IsNullOrWhiteSpace(para)) continue;
                        col.Item().Text(para.Trim())
                            .FontSize(11)
                            .LineHeight(1.5f);
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(
                    $"توليد بواسطة مكتبة العلوم • {DateTime.Now.ToLocalTime():yyyy-MM-dd HH:mm:ss}")
                    .FontSize(9).FontColor(Colors.Grey);
            });
        }).GeneratePdf(saveDialog.FileName);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}