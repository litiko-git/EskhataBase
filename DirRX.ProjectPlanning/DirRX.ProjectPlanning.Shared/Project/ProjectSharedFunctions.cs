using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning.Shared
{
  partial class ProjectFunctions
  {
    /// <summary>
    /// Установить доступность свойств в зависимости от наличия этапов по проекту.
    /// </summary>
    public void SetPropertiesAvailability()
    {
      var isPrepareStage = _obj.State.IsInserted ||
        _obj.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Planning ||
        _obj.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Initiation;
      var haventProjectPlan = _obj.ProjectPlanDirRX == null;
      _obj.State.Properties.StartDate.IsEnabled = haventProjectPlan;
      _obj.State.Properties.EndDate.IsEnabled = haventProjectPlan;
      _obj.State.Properties.ActualStartDate.IsEnabled = haventProjectPlan && !isPrepareStage;
      _obj.State.Properties.ActualFinishDate.IsEnabled = haventProjectPlan && !isPrepareStage;
      _obj.State.Properties.PlannedWorkloadDirRX.IsEnabled = haventProjectPlan;
      _obj.State.Properties.ActualWorkloadDirRX.IsEnabled = haventProjectPlan;
      _obj.State.Properties.ExecutionPercent.IsEnabled = haventProjectPlan;
      _obj.State.Properties.FactualCosts.IsEnabled = haventProjectPlan;
      _obj.State.Properties.PlannedCosts.IsEnabled = haventProjectPlan;
      
      if (_obj.State.IsInserted)
      {
        _obj.State.Properties.ProjectPlanDirRX.IsEnabled = false;
      }
    }
    
    public void TogglePlanFields()
    {
      var hasPlan = _obj.ProjectPlanDirRX != null;
      
      _obj.State.Properties.EndDate.IsVisible = !hasPlan;
      _obj.State.Properties.StartDate.IsVisible = !hasPlan;
      _obj.State.Properties.ActualFinishDate.IsVisible = !hasPlan;
      _obj.State.Properties.ActualStartDate.IsVisible = !hasPlan;
      _obj.State.Properties.ActualWorkloadDirRX.IsVisible = !hasPlan;
      _obj.State.Properties.PlannedWorkloadDirRX.IsVisible = !hasPlan;
      _obj.State.Properties.PlannedCosts.IsVisible = !hasPlan;
      _obj.State.Properties.FactualCosts.IsVisible = !hasPlan;
      _obj.State.Properties.ExecutionPercent.IsVisible = !hasPlan;
      
      _obj.State.Controls.DynamicFactCosts.IsVisible = hasPlan;
      _obj.State.Controls.DynamicPlanCosts.IsVisible = hasPlan;
      _obj.State.Controls.DynamicFactDates.IsVisible = hasPlan;
      _obj.State.Controls.DynamicPlanDates.IsVisible = hasPlan;
    }
  }
}