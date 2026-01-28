using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.ProjectCore;

namespace DirRX.ProjectPlanning
{
  partial class ProjectCoreClientHandlers
  {

    public override void LeadingProjectValueInput(Sungero.Projects.Client.ProjectCoreLeadingProjectValueInputEventArgs e)
    {
      base.LeadingProjectValueInput(e);
      
      if (e.NewValue == null)
      {
        return;
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      _obj.State.Properties.GatesDirRX.IsEnabled = false;
      
      if(_obj.LeadingProject != null)
      {
        
        var startDateLeadingProject = _obj.LeadingProject.StartDate ?? Calendar.SqlMinValue;
        var endDateLeadingProject = _obj.LeadingProject.EndDate ?? Calendar.SqlMaxValue;
        
        if(_obj.StartDate != null && startDateLeadingProject != null && _obj.StartDate < startDateLeadingProject)
        {
          e.AddWarning(ProjectCores.Resources.WarningStartDateEarlierFormat(startDateLeadingProject.ToShortDateString()));
        }
        
        if(_obj.EndDate != null && endDateLeadingProject != null && _obj.EndDate > endDateLeadingProject)
        {
          e.AddWarning(ProjectCores.Resources.WarningEndDateLaterFormat(endDateLeadingProject.ToShortDateString()));
        }
      }
      
      var childProjectCoresIds = Module.Projects.PublicFunctions.Module.GetAllChildProjectIds(_obj.Id);
      var childProjectCoresWithIncorrectDate = ProjectCores.GetAll(x => x.Id != _obj.Id &&
                                                                   childProjectCoresIds.Contains(x.Id) &&
                                                                   x.StartDate != null &&
                                                                   (x.StartDate < _obj.StartDate || x.EndDate > _obj.EndDate)
                                                                  );

      if(childProjectCoresWithIncorrectDate.Any())
      {
        var childsDisplayValue = string.Join(", ", childProjectCoresWithIncorrectDate.Select(x => x.DisplayValue));
        e.AddWarning(ProjectCores.Resources.WarningDateForParentFormat(childsDisplayValue));
      }
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if(!_obj.State.IsInserted && !Projects.Is(_obj))
      {
        _obj.State.Pages.Structure.Activate();
      }
    }
  }

}
