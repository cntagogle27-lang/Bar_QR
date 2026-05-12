using Bar.Eugenio.Domain.Roles;
using Bar.Eugenio.Domain.Staff;

namespace Bar.Eugenio.Application;

public sealed class StaffSession
{
    public bool IntranetUnlocked { get; private set; }
    public StaffMember? CurrentStaff { get; private set; }
    public StaffRole CurrentRole => CurrentStaff?.Role ?? StaffRole.Cliente;

    public void UnlockIntranet()
    {
        IntranetUnlocked = true;
    }

    public void SignIn(StaffMember staff)
    {
        CurrentStaff = staff;
    }

    public void SignOut()
    {
        CurrentStaff = null;
    }
}
