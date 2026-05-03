namespace Infrastructure.Language.Interfaces
{
    public interface ILanguageDetector
    {
        string? DetectLanguage(string text);
    }
}