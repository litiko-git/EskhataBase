using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectsResource;

namespace DirRX.ProjectPlanner
{
  partial class ProjectsResourceUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
     
      var isProjectsResource = CallContext.CalledFrom(ProjectsResources.Info);

      query = base.Filtering(query, e);
      query = query.Where(x => isProjectsResource);
      return query;
    }
  }

  partial class ProjectsResourceServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Change);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (ProjectsResources.GetAll(x => x.Employee != null).Any(r => Sungero.Company.Employees.Equals(r.Employee,_obj.Employee) && r.Id != _obj.Id))
      {
        e.AddError("Ресурс с этим пользователем уже существует.");
        return;
      }
    }
  }

}