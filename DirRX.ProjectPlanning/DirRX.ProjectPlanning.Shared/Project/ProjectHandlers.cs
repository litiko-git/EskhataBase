using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{
  partial class ProjectTeamMembersSharedHandlers
  {

    public override void TeamMembersMemberChanged(Sungero.Projects.Shared.ProjectCoreTeamMembersMemberChangedEventArgs e)
    {
      base.TeamMembersMemberChanged(e);
      
      var thisProject = DirRX.ProjectPlanning.Projects.As(e.Entity);
      
      if (thisProject?.ProjectPlanDirRX != null)
      {
        var diff = DirRX.ProjectPlanner.NotifyDiffs.Create();
      
        diff.ConnectedPlanId = thisProject.ProjectPlanDirRX.Id;
        diff.EventTypeId = DirRX.TeamsCommonAPI.NotifyEventTypes.GetAll(t => t.EventType == DirRX.TeamsCommonAPI.NotifyEventType.EventType.PlanParticipAdd).FirstOrDefault().Id;
        diff.NewValue = e.NewValue.DisplayValue;
        diff.PreviousValue = e.OldValue.DisplayValue;
        diff.Save();
      }
    }
  }

  partial class ProjectSharedHandlers
  {

    public virtual void PlannedWorkloadDirRXChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.PlannedWorkloadDirRX = e.OriginalValue;
      }
    }

    public virtual void ActualWorkloadDirRXChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.ActualWorkloadDirRX = e.OriginalValue;
      }
    }

    public override void ManagerChanged(Sungero.Projects.Shared.ProjectCoreManagerChangedEventArgs e)
    {
      base.ManagerChanged(e);
      if (e.NewValue == null || e.OldValue == e.NewValue || _obj.ProjectPlanDirRX == null)
      {
        return;
      }
      
      var planRespAssignEventType = DirRX.TeamsCommonAPI.NotifyEventTypes.GetAll(t => t.EventType == DirRX.TeamsCommonAPI.NotifyEventType.EventType.PlanRespAssign).FirstOrDefault();
      if (planRespAssignEventType == null)
      {
        return;
      }
      
      var diff = DirRX.ProjectPlanner.NotifyDiffs.Create();
      diff.PreviousValue = e.OldValue?.Id.ToString() ?? string.Empty;
      diff.NewValue = e.NewValue.Id.ToString();
      diff.ConnectedPlanId = _obj.ProjectPlanDirRX.Id;
      diff.EventTypeId = planRespAssignEventType.Id;
      diff.Save();
    }

    public override void ActualFinishDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ActualFinishDateChanged(e);
    }

    public override void ActualStartDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ActualStartDateChanged(e);
    }

    public override void EndDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.EndDateChanged(e);
    }

    public override void StartDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.StartDateChanged(e);
    }

    public override void ShortNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.ShortNameChanged(e);
    }

    public override void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.NameChanged(e);
      if (e.NewValue != e.OldValue)
      {
        if (_obj.ProjectPlanDirRX != null && !_obj.State.IsCopied && !e.NewValue.StartsWith(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan))
        {
          _obj.ProjectPlanDirRX.Name = string.Format(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan, e.NewValue);
        }
      }
    }

    public virtual void ProjectPlanDirRXChanged(DirRX.ProjectPlanning.Shared.ProjectProjectPlanDirRXChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && !e.NewValue.State.IsInserted)
      {
        _obj.StartDate = e.NewValue.StartDate;
        _obj.ActualStartDate = e.NewValue.ActualStartDate;
        _obj.ActualFinishDate = e.NewValue.ActualFinishDate;
        _obj.EndDate = e.NewValue.EndDate;
        _obj.FactualCosts = e.NewValue.FactualCosts;
        _obj.PlannedCosts = e.NewValue.PlannedCosts;
        _obj.ExecutionPercent = e.NewValue.ExecutionPercent;
        _obj.PlannedWorkloadDirRX = e.NewValue.BaselineWork;
        _obj.ActualWorkloadDirRX = e.NewValue.ActualWorkload;
      }
    }

    public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
        _obj.PlannedCosts = e.OriginalValue;
    }

    public virtual void FactualCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
        _obj.FactualCosts = e.OriginalValue;
    }
  }


  
}
