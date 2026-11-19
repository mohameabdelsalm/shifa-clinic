using System.Text.Json.Serialization;

namespace Core.Models.Clinic;

public class ScheduleItemModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}
