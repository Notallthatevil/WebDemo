using Microsoft.AspNetCore.Mvc;
using WebDemo.Server.Models;

namespace WebDemo.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly NateDbContext _dbContext;

        public EmployeeController(ILogger<EmployeeController> logger, NateDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }


        [HttpGet]
        public IEnumerable<Shared.Employee> Get()
        {
            return _dbContext.Employees.Select(emp => new Shared.Employee { FirstName = emp.FirstName, LastName = emp.LastName, Title = emp.Title, Department = emp.Department == null ? "<UNKNOWN>" : emp.Department.Name });
        }

       
    }
}
