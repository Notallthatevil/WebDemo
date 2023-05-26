using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Reflection;
using WebDemo.Server.Models;
using WebDemo.Shared;

namespace WebDemo.Server.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ManagementController : Controller
	{
		private readonly ILogger<ManagementController> _logger;
		private readonly NateDbContext _dbContext;

		public ManagementController(ILogger<ManagementController> logger, NateDbContext dbContext)
		{
			_logger = logger;
			_dbContext = dbContext;
		}

		[Route("[action]")]
		[HttpGet]
		public IEnumerable<DepartmentManager> Managers()
		{
			return _dbContext.Departments.LeftJoin(_dbContext.Managers, d => d.Id, m => m.DepartmentId, (d, m) => new DepartmentManager
			{
				ManagerId = m == null ? 0 : m.ManagerId,
				ManagerName = m == null ? string.Empty : $"{m.ManagerNavigation.FirstName} {m.ManagerNavigation.LastName}",
				DepartmentId = d.Id,
				DepartmentName = d.Name
			});
		}

		[Route("[action]")]
		[HttpGet]
		public IDictionary<int, IEnumerable<Shared.Employee>> Employees()
		{

			return _dbContext.Employees.ToList().GroupBy(e => e.DepartmentId)
				.ToDictionary(g => (int)g!.Key, g => g.Select(e => new Shared.Employee { 
					Id = e.Id, 
					FirstName = e.FirstName, 
					LastName = e.LastName, 
					Title = e.Title, 
					Department = e.DepartmentId == null ? "0" : e.DepartmentId.ToString() 
				}));
		}

		[Route("[action]")]
		[HttpPost]
		public ActionResult AssignManager(Shared.Employee employee)
		{
			try
			{
				if (employee == null)
				{
					return BadRequest("Invalid employee!");
				}

				var departmentId = _dbContext.Employees.SingleOrDefault(e => e.Id == employee.Id)?.DepartmentId;
				if (departmentId != null)
				{
					using var transaction = _dbContext.Database.BeginTransaction();
					{
						var manager = _dbContext.Managers.SingleOrDefault(m => m.DepartmentId == departmentId);
						if (manager != null)
						{
							_dbContext.Managers.Remove(manager);
							_dbContext.SaveChanges();
						}
						_dbContext.Managers.Add(new Manager { ManagerId = employee.Id, DepartmentId = (int)departmentId });

						_dbContext.SaveChanges();
						transaction.Commit();
					}
					return Ok();
				}
				else
				{
					return BadRequest("Department not valid!");
				}
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}

	public static class LeftJoinExtension
	{
		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
			this IQueryable<TOuter> outer,
			IQueryable<TInner> inner,
			Expression<Func<TOuter, TKey>> outerKeySelector,
			Expression<Func<TInner, TKey>> innerKeySelector,
			Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			MethodInfo groupJoin = typeof(Queryable).GetMethods()
													 .Single(m => m.ToString() == "System.Linq.IQueryable`1[TResult] GroupJoin[TOuter,TInner,TKey,TResult](System.Linq.IQueryable`1[TOuter], System.Collections.Generic.IEnumerable`1[TInner], System.Linq.Expressions.Expression`1[System.Func`2[TOuter,TKey]], System.Linq.Expressions.Expression`1[System.Func`2[TInner,TKey]], System.Linq.Expressions.Expression`1[System.Func`3[TOuter,System.Collections.Generic.IEnumerable`1[TInner],TResult]])")
													 .MakeGenericMethod(typeof(TOuter), typeof(TInner), typeof(TKey), typeof(LeftJoinIntermediate<TOuter, TInner>));
			MethodInfo selectMany = typeof(Queryable).GetMethods()
													  .Single(m => m.ToString() == "System.Linq.IQueryable`1[TResult] SelectMany[TSource,TCollection,TResult](System.Linq.IQueryable`1[TSource], System.Linq.Expressions.Expression`1[System.Func`2[TSource,System.Collections.Generic.IEnumerable`1[TCollection]]], System.Linq.Expressions.Expression`1[System.Func`3[TSource,TCollection,TResult]])")
													  .MakeGenericMethod(typeof(LeftJoinIntermediate<TOuter, TInner>), typeof(TInner), typeof(TResult));

			var groupJoinResultSelector = (Expression<Func<TOuter, IEnumerable<TInner>, LeftJoinIntermediate<TOuter, TInner>>>)
										  ((oneOuter, manyInners) => new LeftJoinIntermediate<TOuter, TInner> { OneOuter = oneOuter, ManyInners = manyInners });

			MethodCallExpression exprGroupJoin = Expression.Call(groupJoin, outer.Expression, inner.Expression, outerKeySelector, innerKeySelector, groupJoinResultSelector);

			var selectManyCollectionSelector = (Expression<Func<LeftJoinIntermediate<TOuter, TInner>, IEnumerable<TInner>>>)
											   (t => t.ManyInners.DefaultIfEmpty());

			ParameterExpression paramUser = resultSelector.Parameters.First();

			ParameterExpression paramNew = Expression.Parameter(typeof(LeftJoinIntermediate<TOuter, TInner>), "t");
			MemberExpression propExpr = Expression.Property(paramNew, "OneOuter");

			LambdaExpression selectManyResultSelector = Expression.Lambda(new Replacer(paramUser, propExpr).Visit(resultSelector.Body), paramNew, resultSelector.Parameters.Skip(1).First());

			MethodCallExpression exprSelectMany = Expression.Call(selectMany, exprGroupJoin, selectManyCollectionSelector, selectManyResultSelector);

			return outer.Provider.CreateQuery<TResult>(exprSelectMany);
		}

		private class LeftJoinIntermediate<TOuter, TInner>
		{
			public TOuter OneOuter { get; set; }
			public IEnumerable<TInner> ManyInners { get; set; }
		}

		private class Replacer : ExpressionVisitor
		{
			private readonly ParameterExpression _oldParam;
			private readonly Expression _replacement;

			public Replacer(ParameterExpression oldParam, Expression replacement)
			{
				_oldParam = oldParam;
				_replacement = replacement;
			}

			public override Expression Visit(Expression exp)
			{
				if (exp == _oldParam)
				{
					return _replacement;
				}

				return base.Visit(exp);
			}
		}
	}

}
