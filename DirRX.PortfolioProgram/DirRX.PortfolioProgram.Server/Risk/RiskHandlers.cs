using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram
{

  partial class RiskRelatedActivityRefPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RelatedActivityRefFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var project = ProjectPlanning.Projects.As(_obj.RelatedProjectCore);
      if (project != null && project.ProjectPlanDirRX != null)
      {
        var stages = Functions.Risk.GetStagesLastVersionApproved(project.ProjectPlanDirRX);
        return query.Where(x => stages.Contains(x));
      }
      else
      {
        return query.Where(x => false);
      }
    }
  }

  partial class RiskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (CallContext.CalledFrom(Sungero.Projects.ProjectCores.Info))
      {
        var projectId = CallContext.GetCallerEntityId(Sungero.Projects.ProjectCores.Info);
        var project = Sungero.Projects.ProjectCores.Get(projectId);
        _obj.RelatedProjectCore = project;
      }
      _obj.IsImplement = false;
    }
  }

}