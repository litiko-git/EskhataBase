using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ProjectPlanner.ProjectActivityTask;
using Sungero.Domain.Clients;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityTaskRouteHandlers
  {

    public virtual void StartReviewAssignment2(DirRX.ProjectPlanner.IReviewAssignment reviewAssignment)
    {
      reviewAssignment.ProjectPlan = _obj.ProjectPlan;
      reviewAssignment.ActivityRefId = _obj.ActivityRefId;
    }

    public virtual void Script25Execute()
    {
      _obj.NeedsReview = _obj.ProjectPlan.EnableReviewAssignmentFlag.HasValue ? _obj.ProjectPlan.EnableReviewAssignmentFlag.Value : false;
      _obj.Save();
    }

    public virtual bool Decision24Result()
    {
      return _obj.ProjectPlan.EnableReviewAssignmentFlag.HasValue ? _obj.ProjectPlan.EnableReviewAssignmentFlag.Value : false;
    }

    public virtual bool Decision21Result()
    {
      return _obj.Subtasks.FirstOrDefault().Status == Status.Completed;
    }

    public virtual void StartBlock10(DirRX.ProjectPlanner.Server.RXTaskNoticeArguments e)
    {
      var latestVersionActivity = Functions.ProjectActivity.GetLatestActivityByPlanAndRefId(_obj.ProjectPlan, _obj.ActivityRefId);
      e.Block.Performers.Add(_obj.Author);
      e.Block.Subject = DirRX.ProjectPlanner.ProjectActivityTasks.Resources.FinishedWorksOnStageFormat(latestVersionActivity.Name);
      e.Block.ProjectPlan = _obj.ProjectPlan;
      e.Block.ActivityRefId = _obj.ActivityRefId;
      
      Functions.ProjectActivityTask.OnTaskEnded(_obj);
    }

    public virtual void StartBlock11(DirRX.ProjectPlanner.Server.AssignmentArguments e)
    {
      e.Block.Performers.Add(_obj.Responsible);
      e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
      e.Block.Subject = _obj.Subject;
      e.Block.ActivityRefId = _obj.ActivityRefId;
      e.Block.ProjectPlan = _obj.ProjectPlan;
      e.Block.ExecutionPercent = _obj.ExecutionPercent;
      e.Block.ActualWorkload = _obj.ActualWorkload;
      e.Block.FactualCosts = _obj.FactualCosts;
    }

  }
}