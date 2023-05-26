using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using System.Data;
using WebDemo.Server.Controllers;
using WebDemo.Server.Models;
using WebDemo.Shared;

namespace ServerTests
{
	internal class Common
	{
		public static List<WebDemo.Server.Models.Employee> GetFakeEmployeeList()
		{
			return new List<WebDemo.Server.Models.Employee>()
			{
				new WebDemo.Server.Models.Employee{ Id = 1, FirstName = "Adam", LastName = "Johnson", Title = "CEO", DepartmentId = 5 },
				new WebDemo.Server.Models.Employee{ Id = 2, FirstName = "Betty", LastName = "Hudson", Title = "CFO", DepartmentId = 5 },
				new WebDemo.Server.Models.Employee{ Id = 3, FirstName = "Dwight", LastName = "Rodgers", Title = "Directory of Marketing", DepartmentId = 4 },
				new WebDemo.Server.Models.Employee{ Id = 4, FirstName = "Cody", LastName = "Ferguson", Title = "Senior Marketing", DepartmentId = 4 },
				new WebDemo.Server.Models.Employee{ Id = 5, FirstName = "Loretta", LastName = "Rios", Title = "HR Rep", DepartmentId = 3 },
				new WebDemo.Server.Models.Employee{ Id = 6, FirstName = "Wilber", LastName = "Ramsey", Title = "Director of HR", DepartmentId = 3 },
				new WebDemo.Server.Models.Employee{ Id = 7, FirstName = "Jessica", LastName = "Jenkins", Title = "Senior Architect", DepartmentId = 2 },
				new WebDemo.Server.Models.Employee{ Id = 8, FirstName = "Martin", LastName = "Morris", Title = "Senior Software Engineer", DepartmentId = 2 },
				new WebDemo.Server.Models.Employee{ Id = 9, FirstName = "Ashley", LastName = "Gray", Title = "Directory of Accounting", DepartmentId = 1 },
				new WebDemo.Server.Models.Employee{ Id = 10, FirstName = "Lisa", LastName = "Foster", Title = "Senior Accountant", DepartmentId = 1 }
			};
		}

		public static List<Manager> GetFakeManagerList()
		{
			return new List<Manager>()
			{
				new Manager{ ManagerId = 9, DepartmentId = 1},
				new Manager{ ManagerId = 7, DepartmentId = 2},
				new Manager{ ManagerId = 6, DepartmentId = 3},
				new Manager{ ManagerId = 3, DepartmentId = 4},
				new Manager{ ManagerId = 1, DepartmentId = 5 }
			};
		}

		public static List<Department> GetFakeDepartmentsList()
		{
			return new List<Department>()
			{
				new Department{ Id = 1, Name = "Accounting" },
				new Department{ Id = 2, Name = "SD" },
				new Department{ Id = 3, Name = "HR" },
				new Department{ Id = 4, Name = "Marketing" },
				new Department{ Id = 5, Name = "CSuite" }
			};
		}
	}

	public class EmployeeControllerTest
	{
		[Fact]
		public void Get_ReturnsIEnumerableOfEmployee()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);

			var sut = new EmployeeController(new NullLogger<EmployeeController>(), contextMoq.Object);
			var result = sut.Get();

			var expected = Common.GetFakeEmployeeList()
				.Select(emp => new WebDemo.Shared.Employee { Id = emp.Id, FirstName = emp.FirstName, LastName = emp.LastName, Title = emp.Title, Department = emp.Department == null ? "<UNKNOWN>" : emp.Department.Name });

			Assert.Equivalent(expected.ToList(), result.ToList());
		}

		//public ActionResult AddEmployee(Shared.Employee employee)

		[Fact]
		public void AddEmployee_ProvideNull_ReturnBadRequest()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);

			var sut = new EmployeeController(new NullLogger<EmployeeController>(), contextMoq.Object);
			var result = sut.AddEmployee(null);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void AddEmployee_Valid_DepartmentDoesntExist_DepAdded_EmpAdded()
		{
			var empList = Common.GetFakeEmployeeList();
			var mockEmp = empList.BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var depList = Common.GetFakeDepartmentsList();
			var mockDep = depList.BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			var databaseMoq = new Mock<DatabaseFacade>(contextMoq.Object);
			var transactionMoq = new Mock<IDbContextTransaction>();

			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);
			contextMoq.Setup(db => db.Database).Returns(databaseMoq.Object);
			databaseMoq.Setup(t => t.BeginTransaction()).Returns(transactionMoq.Object);

			var sut = new EmployeeController(new NullLogger<EmployeeController>(), contextMoq.Object);

			var newDepartment = new Department { Id = 100, Name = "NewDep" };
			var newSharedEmployee = new WebDemo.Shared.Employee { Id = 100, FirstName = "First", LastName = "Last", Department = newDepartment.Name, Title = "Title" };
			var newEmployee = new WebDemo.Server.Models.Employee { Id = newSharedEmployee.Id, FirstName = newSharedEmployee.FirstName, LastName = newSharedEmployee.LastName, DepartmentId = newDepartment.Id, Title = newSharedEmployee.Title };

			mockEmp.Setup(m => m.Add(It.IsAny<WebDemo.Server.Models.Employee>())).Callback<WebDemo.Server.Models.Employee>(e => empList.Add(newEmployee));
			mockDep.Setup(m => m.Add(It.IsAny<Department>())).Callback<Department>(d => depList.Add(newDepartment));

			var result = sut.AddEmployee(newSharedEmployee);

			Assert.IsType<OkResult>(result);

			Assert.NotNull(depList.FirstOrDefault(d => d.Name == "NewDep"));
			Assert.NotNull(empList.FirstOrDefault(d => d.Id == 100));
		}

		[Fact]
		public void AddEmployee_Valid_DepartmentDoesExist_EmpAdded()
		{
			var empList = Common.GetFakeEmployeeList();
			var mockEmp = empList.BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var depList = Common.GetFakeDepartmentsList();
			var mockDep = depList.BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			var databaseMoq = new Mock<DatabaseFacade>(contextMoq.Object);
			var transactionMoq = new Mock<IDbContextTransaction>();

			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);
			contextMoq.Setup(db => db.Database).Returns(databaseMoq.Object);
			databaseMoq.Setup(t => t.BeginTransaction()).Returns(transactionMoq.Object);

			mockEmp.Setup(m => m.Add(It.IsAny<WebDemo.Server.Models.Employee>())).Callback<WebDemo.Server.Models.Employee>(e => empList.Add(e));
			mockDep.Setup(m => m.Add(It.IsAny<Department>())).Callback<Department>(d => depList.Add(d));

			var sut = new EmployeeController(new NullLogger<EmployeeController>(), contextMoq.Object);

			var newSharedEmployee = new WebDemo.Shared.Employee { Id = 100, FirstName = "First", LastName = "Last", Department = "CSuite", Title = "Title" };
			var newEmployee = new WebDemo.Server.Models.Employee { Id = newSharedEmployee.Id, FirstName = newSharedEmployee.FirstName, LastName = newSharedEmployee.LastName, DepartmentId = 5, Title = newSharedEmployee.Title };

			mockEmp.Setup(m => m.Add(It.IsAny<WebDemo.Server.Models.Employee>())).Callback<WebDemo.Server.Models.Employee>(e => empList.Add(newEmployee));

			var depListSizeBefore = depList.Count();
			var result = sut.AddEmployee(new WebDemo.Shared.Employee { Id = 100, FirstName = "First", LastName = "Last", Department = "CSuite", Title = "Title" });

			Assert.IsType<OkResult>(result);

			Assert.Equal(depListSizeBefore, depList.Count());
			Assert.NotNull(empList.FirstOrDefault(d => d.Id == 100));
		}

		//public ActionResult UpdateEmployee(Shared.Employee employee)
		//public ActionResult GetById([FromBody] int id)
		//public ActionResult DeleteEmployee(Shared.Employee employee)
	}

	public class ManagementControllerTests
	{

		[Fact]
		public void Employees_ReturnsIDictionaryOfDepartmentIdsAndListOfEmployees()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);

			var sut = new ManagementController(new NullLogger<ManagementController>(), contextMoq.Object);
			var result = sut.Employees();

			var expected = Common.GetFakeEmployeeList().Where(e => e.DepartmentId != null).GroupBy(e => e.DepartmentId)
				.ToDictionary(g => (int)g!.Key, g => g.Select(e => new WebDemo.Shared.Employee
				{
					Id = e.Id,
					FirstName = e.FirstName,
					LastName = e.LastName,
					Title = e.Title,
					Department = e.DepartmentId == null ? "0" : e.DepartmentId.ToString()
				}));


			Assert.Equivalent(expected.ToList(), result.ToList());
		}

		[Fact]
		public void AssignManager_NullEmployee_BadRequestReturned()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);

			var sut = new ManagementController(new NullLogger<ManagementController>(), contextMoq.Object);
			var result = sut.AssignManager(null);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void AssignManager_BadDepartmentId_BadRequestReturned()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var mockMan = Common.GetFakeManagerList().BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);

			var sut = new ManagementController(new NullLogger<ManagementController>(), contextMoq.Object);
			var result = sut.AssignManager(new WebDemo.Shared.Employee());

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void AssignManager_ValidEmployeeAssigned()
		{
			var mockEmp = Common.GetFakeEmployeeList().BuildMock().BuildMockDbSet();
			var manList = Common.GetFakeManagerList();
			var mockMan = manList.BuildMock().BuildMockDbSet();
			var mockDep = Common.GetFakeDepartmentsList().BuildMock().BuildMockDbSet();

			var contextMoq = new Mock<NateDbContext>();
			var databaseMoq = new Mock<DatabaseFacade>(contextMoq.Object);
			var transactionMoq = new Mock<IDbContextTransaction>();

			contextMoq.Setup(db => db.Employees).Returns(mockEmp.Object);
			contextMoq.Setup(db => db.Managers).Returns(mockMan.Object);
			contextMoq.Setup(db => db.Departments).Returns(mockDep.Object);
			contextMoq.Setup(db => db.Database).Returns(databaseMoq.Object);
			databaseMoq.Setup(t => t.BeginTransaction()).Returns(transactionMoq.Object);

			mockMan.Setup(m => m.Add(It.IsAny<Manager>())).Callback<Manager>(m => manList.Add(m));
			mockMan.Setup(m => m.Remove(It.IsAny<Manager>())).Callback<Manager>(m => manList.Remove(m));

			var sut = new ManagementController(new NullLogger<ManagementController>(), contextMoq.Object);
			var result = sut.AssignManager(new WebDemo.Shared.Employee { Id = 2, Department = "CSuite" });

			Assert.IsType<OkResult>(result);
			var managerId = mockMan.Object.Single(m => m.DepartmentId == 5).ManagerId;

			Assert.Equal(2, managerId);
		}

	}
}