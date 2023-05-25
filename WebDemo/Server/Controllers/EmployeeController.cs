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

		[HttpPost]
		public ActionResult Post([FromBody]Shared.Employee employee)
		{
			try
			{
				using (var transaction = _dbContext.Database.BeginTransaction())
				{
					var dep = _dbContext.Departments.SingleOrDefault(d => d.Name.ToLower() == employee.Department.ToLower());
					if (dep == null)
					{
						dep = new Models.Department { Name = employee.Department };
						var result = _dbContext.Departments.Add(dep);
						_dbContext.SaveChanges();
					}

					_dbContext.Employees.Add(new Employee { FirstName = employee.FirstName, LastName = employee.LastName, Title = employee.Title, DepartmentId = dep.Id });
					_dbContext.SaveChanges();
					transaction.Commit();
				}

				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

		}
	}
}
