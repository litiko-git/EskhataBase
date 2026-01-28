using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // все редактирование только через гант
      _obj.State.IsEnabled = false;
      var currentUser = Sungero.Company.Employees.Current ?? Users.Current;
      if (_obj.ProjectPlan != null && !_obj.ProjectPlan.AccessRights.CanRead(currentUser))
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivities.Resources.NoAccessRightsToViewActivity);
        foreach (var property in _obj.State.Properties)
        {
          property.IsVisible = false;
        }
      }
      
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.ProjectPlan.Id, _obj.NumberVersion.Value, _obj.Id, Users.Current.Id, !_obj.AccessRights.CanUpdate());
      if (Locks.GetLockInfo(_obj).IsLockedByMe)
      {
        Locks.Unlock(_obj);
      }
    }

    public virtual void NumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue.Value < 1)
        e.AddError(ProjectActivities.Resources.NumberMustBePositive);
    }

    public virtual void ExecutionPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && (e.NewValue.Value < 0 || e.NewValue.Value > 100))
        e.AddError(Sungero.Projects.Projects.Resources.IncorrectPercent);
    }

    public virtual void BaselineWorkValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue.Value < 0)
        e.AddError(DirRX.ProjectPlanning.Projects.Resources.IncorrectBaselineWork);
    }

	}
}