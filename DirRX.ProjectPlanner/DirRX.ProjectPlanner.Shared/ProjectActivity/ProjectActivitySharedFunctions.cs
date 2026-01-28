using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner.Shared
{
  partial class ProjectActivityFunctions
  {
    /// <summary>
    /// Возвращает длительность этапа.
    /// </summary>
    /// <returns>Количество рабочих дней.</returns>
    public int? GetDuration()
    {
      if (!_obj.StartDate.HasValue || !_obj.EndDate.HasValue)
        return null;
      
      return Functions.Module.GetWorkingDays(_obj);
    }
    
    /// <summary>
    /// Обновляет значение полного номера этапа.
    /// </summary>
    public void UpdateFullNumber()
    {
      _obj.FullNumber = Functions.ProjectActivity.Remote.GenerateFullNumber(_obj);
    }
  }
}