using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.TeamsCommon;
using DirRX.ProjectPlanning;
using DirRX.PortfolioProgram.Portfolio;

namespace DirRX.PortfolioProgram
{
  partial class PortfolioFilteringServerHandler<T>
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

      return query;
    }
  }

  partial class PortfolioServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.AccessRights.CanUpdate())
      {
        e.AddError(Portfolios.Resources.NoRightToUpdatePortfolio);
        return;
      }
      
      if (Equals(_obj.ShortName, Sungero.Projects.Resources.ProjectArhiveFolderName))
        e.AddError(ProjectCores.Resources.PropertyReservedFormat(_obj.Info.Properties.ShortName.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));
      
      if (ProjectCores.GetAll().Any(p => !Equals(p, _obj) && Equals(p.ShortName, _obj.ShortName)))
        e.AddError(ProjectCores.Resources.PropertyAlreadyUsedFormat(_obj.Info.Properties.ShortName.LocalizedName, _obj.ShortName));
      
      // Проверка циклических ссылок в подпроектах.
      if (_obj.State.Properties.LeadingProject.IsChanged && _obj.LeadingProject != null)
      {
        var leadingProject = _obj.LeadingProject;
        
        while (leadingProject != null)
        {
          if (Equals(leadingProject, _obj))
          {
            e.AddError(_obj.Info.Properties.LeadingProject, Portfolios.Resources.LeadingPortfolioCyclicReference, _obj.Info.Properties.LeadingProject);
            return;
          }
          
          leadingProject = leadingProject.LeadingProject;
        }
      }

      if (!e.Params.Contains(Sungero.Projects.Constants.Module.DontUpdateModified))
        _obj.Modified = Calendar.Now;
    }
  }

}