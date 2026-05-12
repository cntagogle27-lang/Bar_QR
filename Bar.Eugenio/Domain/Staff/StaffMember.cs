using Bar.Eugenio.Domain.Roles;

namespace Bar.Eugenio.Domain.Staff;

public sealed class StaffMember
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public StaffRole Role { get; init; }
    public string? Pin { get; init; }
}
