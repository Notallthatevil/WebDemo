using Microsoft.AspNetCore.Mvc;
using WebDemo.Server.Models;
using WebDemo.Shared;

namespace WebDemo.Server.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ManagementController : Controller
	{
		private readonly ILogger<EmployeeController> _logger;
		private readonly NateDbContext _dbContext;

		public ManagementController(ILogger<EmployeeController> logger, NateDbContext dbContext)
		{
			_logger = logger;
			_dbContext = dbContext;
		}

		[Route("[action]")]
		[HttpGet]
		public IEnumerable<DepartmentManager> Managers()
		{

			return _dbContext.Departments.GroupJoin(_dbContext.Managers, d => d.Id, m => m.DepartmentId, (d, m) => new DepartmentManager
			{
				ManagerId = m.SingleOrDefault() == null ? 0 : m.Single().ManagerId,
				ManagerName = m.SingleOrDefault() == null ? string.Empty : $"{m.Single().ManagerNavigation.FirstName} {m.Single().ManagerNavigation.LastName}",
				DepartmentId = d.Id,
				DepartmentName = d.Name
			});
		}

		[Route("[action]")]
		[HttpGet]
		public IDictionary<int, IEnumerable<Shared.Employee>> Employees()
		{
			return _dbContext.Employees.Where(e => e.DepartmentId != null).GroupBy(e => e.DepartmentId)
				.ToDictionary(g => (int)g!.Key, g => g.Select(e => new Shared.Employee { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, Title = e.Title, Department = e.DepartmentId == null ? "0" : e.DepartmentId.ToString() }));
		}

		[Route("[action]")]
		[HttpPost]
		public ActionResult AssignManager(Shared.Employee employee)
		{
			try
			{
				using (var transaction = _dbContext.Database.BeginTransaction())
				{
					var departmentId = _dbContext.Employees.SingleOrDefault(e => e.Id == employee.Id)?.DepartmentId;
					if (departmentId != null)
					{
						var manager = _dbContext.Managers.SingleOrDefault(m => m.DepartmentId == departmentId);
						if (manager != null)
						{
							_dbContext.Remove(manager);
							_dbContext.SaveChanges();
						}
						_dbContext.Managers.Add(new Manager { ManagerId = employee.Id, DepartmentId = (int)departmentId });
					}

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
