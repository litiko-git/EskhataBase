using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.OpensProjectPlansFromCard;

namespace DirRX.ProjectPlanner.Server
{
	partial class OpensProjectPlansFromCardFunctions
	{
		[Remote]
		public static void CreateEntry(IProjectPlanRX projectPlan)
		{
			if (!OpensProjectPlansFromCards.GetAll(x => ProjectPlanRXes.Equals(x.PrjectPlan, projectPlan)).Any())
			{
				var entry = OpensProjectPlansFromCards.Create();
				entry.PrjectPlan = projectPlan;
				entry.User = Users.Current;
				entry.Save();
			}
		}
		
		[Remote]
		public static void DeleteEntry(IProjectPlanRX projectPlan)
		{
			var entry = OpensProjectPlansFromCards.GetAll(x => ProjectPlanRXes.Equals(x.PrjectPlan, projectPlan) && Users.Equals(x.User, Users.Current)).FirstOrDefault();
			if (entry != null)
				OpensProjectPlansFromCards.Delete(entry);
		}
		
		[Public, Remote]
		public static void CreateEntry(DirRX.ProjectPlanning.IProject project)
		{
			if (!OpensProjectPlansFromCards.GetAll(x => DirRX.ProjectPlanning.Projects.Equals(x.Project, project)).Any())
			{
				var entry = OpensProjectPlansFromCards.Create();
				entry.Project = project;
				entry.User = Users.Current;
				entry.Save();
			}
		}
		
		[Public, Remote]
		public static void DeleteEntry(DirRX.ProjectPlanning.IProject project)
		{
			var entry = OpensProjectPlansFromCards.GetAll(x => DirRX.ProjectPlanning.Projects.Equals(x.Project, project) && Users.Equals(x.User, Users.Current)).FirstOrDefault();
			if (entry != null)
				OpensProjectPlansFromCards.Delete(entry);
		}
	}
}