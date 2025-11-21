namespace MarsVista.Api.Models.V2;

/// <summary>
/// Request model for batch photo retrieval
/// </summary>
public class BatchPhotoRequest
{
    /// <summary>
    /// List of photo IDs to retrieve (maximum 100)
    /// </summary>
    public List<int> Ids { get; set; } = new();
}
