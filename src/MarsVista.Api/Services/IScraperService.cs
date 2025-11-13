namespace MarsVista.Api.Services;

/// <summary>
/// Interface for NASA API scraper services
/// </summary>
public interface IScraperService
{
    /// <summary>
    /// Scrapes photos from NASA API and stores them in the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for stopping the scrape</param>
    /// <returns>Number of new photos scraped</returns>
    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes photos for a specific sol
    /// </summary>
    /// <param name="sol">Mars sol to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of new photos scraped</returns>
    Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rover name this scraper is for
    /// </summary>
    string RoverName { get; }
}
