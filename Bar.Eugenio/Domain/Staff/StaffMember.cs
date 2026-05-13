using Bar.Eugenio.Domain.Roles;

namespace Bar.Eugenio.Domain.Staff;

public sealed class StaffMember
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public StaffRole Role { get; set; }
    public string? Pin { get; set; }
    public string? PhotoUrl { get; set; }
}
