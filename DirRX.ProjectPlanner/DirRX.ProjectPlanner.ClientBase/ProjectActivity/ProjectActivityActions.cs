using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectActivityCollectionActions
  {
    public override void ShowSelectedEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var projectActivity = ProjectActivities.As(e.Entity);
      
      if (projectActivity == null)
      {
        Logger.Debug(DirRX.ProjectPlanner.ProjectActivities.Resources.CastActivityError);
        return;
      }
      
      var projectPlan = projectActivity.ProjectPlan;
      
      if (projectPlan == null)
      {
        //Не требуется никаких действий, прав на активити нет.
        return;
      }
      
      if(projectPlan != null && !projectActivity.ProjectPlan.AccessRights.CanRead())
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanner.ProjectActivities.Resources.ProjectActivityAccessRightErrorFormat(ProjectActivities.As(e.Entity).Name));
        return;
      }
      
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(projectPlan.Id, projectActivity.NumberVersion.Value, projectActivity.Id, Users.Current.Id, !projectPlan.AccessRights.CanUpdate());
    }

    public override bool CanShowSelectedEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowSelectedEntity(e);
    }

  }


  partial class ProjectActivityActions
  {

    public virtual void ShowActivity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.ProjectPlan.Id, _obj.NumberVersion.Value, _obj.Id, Users.Current.Id, !_obj.AccessRights.CanUpdate());
    }

    public virtual bool CanShowActivity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }


    public override void SetAccessRights(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate("NeedOpenWeb", false);
      base.SetAccessRights(e);
    }

    public override bool CanSetAccessRights(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSetAccessRights(e);
    }

		public virtual void ShowActivities(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var activities = Functions.ProjectActivity.Remote.GetChildActivities(_obj);
			activities.Show();
		}

		public virtual bool CanShowActivities(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

		public virtual void SendToPerformers(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			
		}

		public virtual bool CanSendToPerformers(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

	}

}