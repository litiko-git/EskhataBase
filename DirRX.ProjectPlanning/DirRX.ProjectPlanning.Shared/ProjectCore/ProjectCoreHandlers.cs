using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.ProjectCore;

namespace DirRX.ProjectPlanning
{

  partial class ProjectCoreGatesDirRXSharedHandlers
  {

    public virtual void GatesDirRXGateChanged(DirRX.ProjectPlanning.Shared.ProjectCoreGatesDirRXGateChangedEventArgs e)
    {
      _obj.Level = e.NewValue?.Level;
      _obj.PlanDate = e.NewValue?.PlanDate;
      _obj.ActualDate = e.NewValue?.ActualDate;
      _obj.IsPassed = e.NewValue?.IsPassed;
      _obj.Assessment = e.NewValue?.Assessment;
      _obj.Performer = e.NewValue?.Performer;
    }
  }
}