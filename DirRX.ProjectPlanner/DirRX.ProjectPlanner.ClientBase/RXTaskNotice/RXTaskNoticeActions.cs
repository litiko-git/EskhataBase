using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.RXTaskNotice;

namespace DirRX.ProjectPlanner.Client
{
  partial class RXTaskNoticeActions
  {
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

  }

}
