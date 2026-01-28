using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.Assignment;

namespace DirRX.ProjectPlanner
{
  partial class AssignmentServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      var task = ProjectActivityTasks.As(_obj.Task);
      
      if (task == null)
      {
        throw new Exception($"задача, в рамках которого создано задание c Id = {_obj.Id}, не является ProjectActivityTask");
      }
      
      // Может возникнуть ситация, что задача прекращена, а задание нет, потому что заблокировано.
      // Изменять фактические значения задачи из задания можно только из последнего задания. 
      var lastId = Assignments.GetAll(x => x.Task.Id == task.Id).Select(x => x.Id).Max();
      
      if ( _obj.Id != lastId ||
          task.Status != Sungero.Workflow.Task.Status.InProcess ||
          (task.ExecutionPercent == _obj.ExecutionPercent &&
           task.FactualCosts == _obj.FactualCosts &&
           task.ActualWorkload == _obj.ActualWorkload))
      {
        return;
      }
      
      var asyncHandler = AsyncHandlers.UpdateProjectTaskFactData.Create();
      asyncHandler.MainTaskId = _obj.Task.Id;
      asyncHandler.ProjectPlanId = _obj.ProjectPlan.Id;
      asyncHandler.ActivityRefId = _obj.ActivityRefId ?? 0;
      if (_obj.ExecutionPercent.HasValue)
      {
        asyncHandler.HasExecutionPercent = true;
        asyncHandler.ExecutionPercent = _obj.ExecutionPercent.Value;
      }
      if (_obj.FactualCosts.HasValue)
      {
        asyncHandler.HasFactualCosts = true;
        asyncHandler.FactualCosts = _obj.FactualCosts.Value;
      }
      if (_obj.ActualWorkload.HasValue)
      {
        asyncHandler.HasActualWorkload = true;
        asyncHandler.ActualWorkload = _obj.ActualWorkload.Value;
      }

      asyncHandler.ExecuteAsync();
      
      base.AfterSave(e);
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      base.BeforeComplete(e);
      
      if (!_obj.ProjectPlan.EnableReviewAssignmentFlag.HasValue || !_obj.ProjectPlan.EnableReviewAssignmentFlag.Value)
      {
        _obj.ExecutionPercent = 100;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.ExecutionPercent.HasValue && (_obj.ExecutionPercent.Value < 0 || _obj.ExecutionPercent.Value > 100))
      {
        e.AddError(_obj.Info.Properties.ExecutionPercent, DirRX.ProjectPlanner.Assignments.Resources.ProjectAssignmentExecutionPercentInvalid);
        return;
      }
      
      if (_obj.ActualWorkload.HasValue && (_obj.ActualWorkload.Value < 0))
      {
        e.AddError(
          _obj.Info.Properties.ActualWorkload,
          DirRX.ProjectPlanner.Assignments.Resources.TaskFactualValueShouldBeGreaterThanZeroFormat(_obj.Info.Properties.ActualWorkload.LocalizedName));
        return;
      }
      
      if (_obj.FactualCosts.HasValue && (_obj.FactualCosts.Value < 0))
      {
        e.AddError(
          _obj.Info.Properties.FactualCosts,
          DirRX.ProjectPlanner.Assignments.Resources.TaskFactualValueShouldBeGreaterThanZeroFormat(_obj.Info.Properties.FactualCosts.LocalizedName));
        return;
      }
      
      base.BeforeSave(e);
    }
  }

}