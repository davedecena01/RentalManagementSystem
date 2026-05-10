namespace RentalManagementApi.DTOs.Reminders;

public class ReminderSettingDto
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public bool IsEnabled { get; set; }
    public int DaysBeforeDue { get; set; }
    public int DaysAfterDue { get; set; }
}

public class UpdateReminderSettingRequest
{
    public bool IsEnabled { get; set; }
    public int DaysBeforeDue { get; set; }
    public int DaysAfterDue { get; set; }
}

public class TriggerRemindersResult
{
    public int Sent { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}
