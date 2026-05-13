namespace PaperClaw.Search;

public interface ISearchService
{
    Task<string> SearchAsync(string question);
}
