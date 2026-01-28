using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.ProjectCore;

namespace DirRX.ProjectPlanning
{
  partial class ProjectCoreCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.GatesDirRX);
    }
  }

  partial class ProjectCoreFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if(_filter == null)
        return query;
      
      query = DirRX.ProjectPlanning.PublicFunctions.Module.BaseFilter(query,
                                                                      _filter.Active,
                                                                      _filter.Closed,
                                                                      _filter.Closing,
                                                                      _filter.Initiation,
                                                                      _filter.Planning,
                                                                      _filter.ProjectManager,
                                                                      _filter.LeadingProject,
                                                                      _filter.InternalCustomer,
                                                                      _filter.ExternalCustomer,
                                                                      _filter.StartDateRangeFrom,
                                                                      _filter.StartDateRangeTo,
                                                                      _filter.FinishDateRangeFrom,
                                                                      _filter.FinishDateRangeTo,
                                                                      _filter.NeedAssistance,
                                                                      _filter.UnderControl,
                                                                      _filter.Me,
                                                                      _filter.MySubordinates,
                                                                      _filter.EmployeeSelect
                                                                     ).Cast<T>();
      
      query = DirRX.ProjectPlanning.PublicFunctions.Module.FilterProjectCoreByType(query, _filter.Project, _filter.Program, _filter.Portfolio)
                                                                                   .Cast<T>();
      
      return query;
    }
  }

  partial class ProjectCoreServerHandlers
  {

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      base.Deleting(e);
      
      var gates = _obj.GatesDirRX.Select(x => x.Gate);
      
      foreach(var gate in gates)
      {
        DirRX.ProjectPlanner.Gates.Delete(gate);
      }
      
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.StatusIssues = ProjectCore.StatusIssues.NotSpecified;
    }
  }

}