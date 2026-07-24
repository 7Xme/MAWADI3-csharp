namespace Mawadi3Print.Models;

public class Article
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? RawResponse { get; set; }
    public bool IsFallback => RawResponse != null;
}