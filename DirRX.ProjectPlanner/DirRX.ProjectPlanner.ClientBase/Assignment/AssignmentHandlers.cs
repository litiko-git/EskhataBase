using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.Assignment;

namespace DirRX.ProjectPlanner
{
  partial class AssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Кэшируем актуальный id этапа при загрузке карточки, чтобы больше в бд не ходить.
      Functions.Module.WriteActivityIdAndVersionToRefreshActionParams(e, _obj.ProjectPlan, _obj.ActivityRefId.HasValue ? _obj.ActivityRefId.Value : 0);

      var task = _obj.Task;
      var isTaskInProcess = task.Status.Equals(Sungero.Workflow.Task.Status.InProcess);
      
      _obj.State.Properties.ExecutionPercent.IsEnabled = isTaskInProcess;
      _obj.State.Properties.FactualCosts.IsEnabled = isTaskInProcess;
      _obj.State.Properties.ActualWorkload.IsEnabled = isTaskInProcess;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      _obj.State.Properties.ProjectPlan.IsEnabled = false;
    }

  }
}
