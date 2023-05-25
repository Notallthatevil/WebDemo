using System;
using System.Collections.Generic;

namespace WebDemo.Server.Models;

public partial class Manager
{
    public int ManagerId { get; set; }

    public int DepartmentId { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual Employee ManagerNavigation { get; set; } = null!;
}
