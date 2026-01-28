using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.TeamsCommonAPI.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// 
    /// </summary>
    public virtual void ConsolidatedTasksMonitor()
    {
      var collectiveCondition = NotifyConditionItems.GetAll(c => c.Condition.HasValue && c.Condition.Value == DirRX.TeamsCommonAPI.NotifyConditionItem.Condition.Consolidated).FirstOrDefault();
      
      if (collectiveCondition == null)
      {
        return;
      }
      
      var allEarlyTasks = TeamsTasks.GetAll(t => t.Status.Value == DirRX.TeamsCommonAPI.TeamsTask.Status.Draft &&
                                          t.Condition == collectiveCondition &&
                                          t.IsDelayed.HasValue &&
                                          t.IsDelayed.Value &&
                                          t.IsRootTaskForConsolidated.HasValue &&
                                          t.IsRootTaskForConsolidated.Value &&
                                          t.PlannedNotifyData.HasValue &&
                                          Calendar.Today >= t.PlannedNotifyData
                                         );

      foreach (var task in allEarlyTasks)
      {
        task.Start();
      }
    }

  }
}