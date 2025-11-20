namespace MarsVista.Api.Options;

public class ScraperScheduleOptions
{
    public const string SectionName = "ScraperSchedule";

    public int LookbackSols { get; set; } = 7;
    public List<string> ActiveRovers { get; set; } = new() { "curiosity", "perseverance" };
}
