using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivityTask;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityTaskFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(t => t.MainTaskId.HasValue && t.MainTaskId == t.Id);
    }
  }

  partial class ProjectActivityTaskServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      if (_obj.ProjectPlan != null)
      {
        Functions.Module.AddModifiedPlanIdInDB(_obj.ProjectPlan.Id);
      }
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      var relatedActivity = Functions.ProjectActivity.GetLatestActivityByPlanAndRefId(_obj.ProjectPlan, _obj.ActivityRefId);
      if (relatedActivity == null)
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.StageRelatedToTaskNotFound);
        return;
      }
    }
    
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      Functions.ProjectActivityTask.SendTaskStatusChanged(_obj, DirRX.ProjectPlanner.ProjectActivity.Status.Active.Value, null);
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (_obj.Responsible == null)
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.ResponsibleRequired);
        return;
      }
      
      var relatedActivity = Functions.ProjectActivity.GetLatestActivityByPlanAndRefId(_obj.ProjectPlan, _obj.ActivityRefId);
      if (relatedActivity == null)
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.StageRelatedToTaskNotFound);
        return;
      }
      
      Functions.ProjectActivityTask.SendTaskStatusChanged(_obj, DirRX.ProjectPlanner.ProjectActivity.Status.InWork.Value, null);
    }

  }


}