using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.Assignment;

namespace DirRX.ProjectPlanner.Client
{
  partial class AssignmentActions
  {
    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Sungero.Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Sungero.Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate();
    }

    public virtual void ShowActivityOnProjectPlan(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.ShowActivityOnProjectPlanFromActionParams(e, _obj.ProjectPlan);
    }

    public virtual bool CanShowActivityOnProjectPlan(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      long actualActivityId;
      e.Params.TryGetValue(Constants.ProjectActivityTask.ActualActivityIdParamName, out actualActivityId);
      return _obj.ProjectPlan != null &&
        _obj.ActivityRefId != null &&
        actualActivityId > 0 &&
        _obj.ProjectPlan.AccessRights.CanRead(Users.Current);
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}
