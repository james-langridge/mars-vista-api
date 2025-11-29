namespace MarsVista.Core.Options;

public class ScraperScheduleOptions
{
    public const string SectionName = "ScraperSchedule";

    public int LookbackSols { get; set; } = 14;
    public List<string> ActiveRovers { get; set; } = [];
}
