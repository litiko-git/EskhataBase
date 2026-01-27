using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ReviewAssignment;

namespace DirRX.ProjectPlanner
{
  partial class ReviewAssignmentServerHandlers
  {

    public override void BeforeAccept(Sungero.Workflow.Server.BeforeAcceptEventArgs e)
    {
      var rootTask = ProjectActivityTasks.As(_obj.Task);
      if (rootTask == null)
      {
        throw new Exception(DirRX.ProjectPlanner.ReviewAssignments.Resources.BadAttemptGetRootTaskExceptionTextFormat(_obj.Task.Id, _obj.Id));
      }
      
      var asyncHandler = AsyncHandlers.UpdateProjectAssignmentFactData.Create();
      asyncHandler.TaskId = rootTask.Id;
      asyncHandler.ExecutionPercent = 100;
      asyncHandler.ExecuteAsync();
      
      Functions.ProjectActivityTask.OnTaskEnded(rootTask);
    }
  }

}