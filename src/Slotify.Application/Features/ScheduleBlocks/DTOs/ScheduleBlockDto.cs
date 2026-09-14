namespace Slotify.Application.Features.ScheduleBlocks.DTOs;

public class ScheduleBlockDto
{
    public long Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }
}
