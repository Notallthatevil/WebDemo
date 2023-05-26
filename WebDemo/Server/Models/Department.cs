using System;
using System.Collections.Generic;

namespace WebDemo.Server.Models
{
    public partial class Department
    {
        public Department()
        {
            Employees = new HashSet<Employee>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public virtual Manager? Manager { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
