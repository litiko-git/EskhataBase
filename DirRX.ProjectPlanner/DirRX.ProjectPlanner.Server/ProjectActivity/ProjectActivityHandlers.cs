using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(q => q.ProjectPlan != null);
    }
  }


  partial class ProjectActivityServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      var thisTypeGuid = _obj.GetEntityMetadata().NameGuid;
      // TODO Использовать метод GetTeamTasksForActivityIds
      var linkedTasks = TeamsCommonAPI.TeamsTasks
          .GetAll(x => x.AttachmentDetails
                .Any(a => a.AttachmentTypeGuid == thisTypeGuid && a.AttachmentId == _obj.Id));
      
      foreach (var task in linkedTasks)
      {
        TeamsCommonAPI.TeamsTasks.Delete(task);
      }
      
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      //Для того, что бы проинициализировать св-во, но не смешивать с другими активити.
      _obj.NumberVersion = -10;
    }
  }

}