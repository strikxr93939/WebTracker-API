using Microsoft.EntityFrameworkCore;

public class DayRecord
{
    public int Id { get; set; }
    public string Date { get; set; }
    public int HoursPlayed { get; set; }
    public bool DidSomethingUseful { get; set; }
    public string Feeling {  get; set; }
}

public class TrackerContext : DbContext
{
    public DbSet<DayRecord> Records { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=tracker.db");
    }
}