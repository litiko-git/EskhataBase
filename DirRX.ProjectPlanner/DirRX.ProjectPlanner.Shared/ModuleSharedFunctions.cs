using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Shared
{
  public class ModuleFunctions
  {
    [Public]
    public int GetWorkingDays(IProjectActivity activity)
    {
      // HACK уже не помню для чего (то ли оповещения, то ли отправка заданий) было сделано что активити заканчивается в 00:00:00 следующего дня, тут обходим эту проблему
      return WorkingTime.GetDurationInWorkingDays(activity.StartDate.Value, activity.EndDate.Value.AddDays(-1));
    }
  }
}