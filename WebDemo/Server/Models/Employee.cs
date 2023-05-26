using System;
using System.Collections.Generic;

namespace WebDemo.Server.Models
{
    public partial class Employee
    {
        public Employee()
        {
            Managers = new HashSet<Manager>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public int? DepartmentId { get; set; }

        public virtual Department? Department { get; set; }
        public virtual ICollection<Manager> Managers { get; set; }
    }
}
