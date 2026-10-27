namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Total event distance in kilometres.</summary>
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    public string EventType { get; set; } = "Run";

    /// <summary>Public Azure Blob Storage URL of the event banner image.</summary>
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
