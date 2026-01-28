using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanObsolete;

namespace DirRX.ProjectPlanner.Server
{
	partial class ProjectPlanObsoleteFunctions
	{	
		[Remote]
		public static DirRX.ProjectPlanning.IProject GetLinkedProejct(IProjectPlanRX pp)
		{
			return DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(pp, x.ProjectPlanDirRX)).FirstOrDefault();
			
		}
		
		[Remote]
		public static IProjectPlanRX GetProjectPlan(long projectId)
		{
			return DirRX.ProjectPlanner.ProjectPlanRXes.Get(projectId);
		}
		
		
		/// <summary>
		/// Возвращает информацию о блокировке проекта.
		/// </summary>
		/// <returns>Информация о блокировке проекта.</returns>
		/// <remarks>
		/// Если проект заблокирован, то возвращается информация о блокировке.
		/// Если же не заблокирован, то пустая строка.
		/// </remarks>
		[Remote]
		public string GetLockInfo()
		{
			var lockInfo = Locks.GetLockInfo(_obj);
			return lockInfo.IsLockedByOther && !string.IsNullOrEmpty(lockInfo.LockedMessage) ? lockInfo.LockedMessage.ToString() : string.Empty;
		}
		
		/// <summary>
		/// Возвращает список проектов c этапами.
		/// </summary>
		/// <returns>Список проектов с этапами.</returns>
		[Remote]
		public static List<IProjectPlanRX> GetProjectsWithActivities()
		{
			var result = new List<IProjectPlanRX>();
			foreach (var item in ProjectPlanRXes.GetAll())
			{
				if (ProjectPlanner.Functions.ProjectActivity.GetActivities(item).Any())
					result.Add(item);
			}
			return result;
		}
		
		/// <summary>
		/// Копирует этапы из одного проекта в другой.
		/// </summary>
		/// <param name="sourceProject">Исходный проект.</param>
		/// <param name="targetProject">Проект, в который копируются этапы.</param>
		/// <param name="newDate">Новая дата начала.</param>
		[Remote]
		public static void CopyActivitiesFormSourceProjectToTargetProject(IProjectPlanRX sourceProject, IProjectPlanRX targetProject, DateTime newDate)
		{
			// Разница времени.
			TimeSpan diff = newDate - sourceProject.StartDate.Value;
			// Соответствие между этапами исходного проекта и этапами проекта, в который копируются этапы.
			var idActivities = new List<Structures.ProjectPlanRX.IDActivities>();
			// Скопировать этапы.
			var sourceActivities = ProjectPlanner.Functions.ProjectActivity.GetActivities(sourceProject);
			// Список скопированных этапов.
			var targetActivities = new List<ProjectPlanner.IProjectActivity>();
			foreach (var activity in sourceActivities)
			{
				var newActivity = ProjectPlanner.ProjectActivities.Copy(activity);
				// Кеш соответствия ID.
				idActivities.Add(Structures.ProjectPlanRX.IDActivities.Create(activity.Id, newActivity));
				newActivity.StartDate = activity.StartDate.Value + diff;
				newActivity.EndDate = activity.EndDate.Value + diff;
				newActivity.ProjectPlan = targetProject;
				// Очистить ведущий этап и проценты выполнения.
				newActivity.LeadingActivity = null;
				newActivity.ExecutionPercent = null;
				// Очистить предшественников.
				newActivity.Predecessors.Clear();
				newActivity.Save();
				targetActivities.Add(newActivity);
			}
			foreach (var sourceActivity in sourceActivities)
			{
				var targetActivity = idActivities.First(x => x.IDSource == sourceActivity.Id).TargetActitvity;
				var isChanged = false;
				// Заполнить ведущий этап для скопированных этапов.
				if (sourceActivity.LeadingActivity != null)
				{
					targetActivity.LeadingActivity = idActivities.First(y => y.IDSource == sourceActivity.LeadingActivity.Id).TargetActitvity;
					isChanged = true;
				}
				// Заполнить предшественников для скопированных этапов.
				foreach (var predecessor in sourceActivity.Predecessors)
				{
					var newPredecessor = targetActivity.Predecessors.AddNew();
					newPredecessor.Activity = idActivities.First(y => y.IDSource == predecessor.Activity.Id).TargetActitvity;
					isChanged = true;
				}
				if (isChanged)
					targetActivity.Save();
			}
		}
		
		/// <summary>
		/// Создать проект.
		/// </summary>
		/// <returns>Проект.</returns>
		[Public, Remote]
		public static IProjectPlanRX CreateProject()
		{
			return ProjectPlanRXes.Create();
		}
		
		/// <summary>
		/// Создать план проекта.
		/// </summary>
		/// <returns>План проекта.</returns>
		[Public, Remote]
		public static IProjectPlanRX CreateProjectPlan()
		{
			return DirRX.ProjectPlanner.ProjectPlanRXes.Create();
		}
	}
}