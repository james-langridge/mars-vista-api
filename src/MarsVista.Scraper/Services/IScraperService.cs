namespace MarsVista.Scraper.Services;

/// <summary>
/// Interface for rover-specific photo scrapers
/// Implementations handle the details of fetching and parsing photos from NASA APIs
/// </summary>
public interface IScraperService
{
    /// <summary>
    /// Name of the rover this scraper handles
    /// </summary>
    string RoverName { get; }

    /// <summary>
    /// Scrape latest photos for this rover
    /// </summary>
    /// <returns>Number of new photos added</returns>
    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrape photos for a specific sol
    /// </summary>
    /// <param name="sol">Mars sol number to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of new photos added</returns>
    Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default);
}
