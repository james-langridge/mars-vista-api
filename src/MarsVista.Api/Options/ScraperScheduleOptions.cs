namespace MarsVista.Api.Options;

public class ScraperScheduleOptions
{
    public const string SectionName = "ScraperSchedule";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public int RunAtUtcHour { get; set; } = 2;  // 2 AM UTC
    public int LookbackSols { get; set; } = 7;
    public List<string> ActiveRovers { get; set; } = new() { "curiosity", "perseverance" };
}
