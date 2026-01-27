using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{
  partial class ProjectProjectKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> ProjectKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query = base.ProjectKindFiltering(query, e)
        .Where(p => !(p.IsHiddenDirRX.HasValue && p.IsHiddenDirRX.Value));
    }
  }

  partial class ProjectCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.ProjectPlanDirRX);
    }
  }

  partial class ProjectFilteringServerHandler<T>
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
      
      
      if(_filter.ProjectKind != null)
      {
        query = DirRX.ProjectPlanning.PublicFunctions.Module.FilterProjectByKind(query, _filter.ProjectKind).Cast<T>();
      }
      
      return query;
    }
  }

  partial class ProjectServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.AccessRights.CanUpdate())
      {
        e.AddError(ProjectCores.Resources.NoRightToUpdateProject);
        return;
      }
      
      if (Equals(_obj.ShortName, Sungero.Projects.Resources.ProjectArhiveFolderName))
      {
        e.AddError(ProjectCores.Resources.PropertyReservedFormat(_obj.Info.Properties.ShortName.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));
        return;
      }
      
      if (ProjectCores.GetAll().Any(p => !Equals(p, _obj) && Equals(p.ShortName, _obj.ShortName)))
      {
        e.AddError(ProjectCores.Resources.PropertyAlreadyUsedFormat(_obj.Info.Properties.ShortName.LocalizedName, _obj.ShortName));
        return;
      }
      
      if (_obj.ProjectPlanDirRX != null)
      {
        var lockInfo = Locks.GetLockInfo(_obj.ProjectPlanDirRX);
        if (lockInfo != null && lockInfo.IsLockedByOther)
        {
          e.AddError(string.Format(DirRX.ProjectPlanning.Projects.Resources.CloseProjectErrorT, lockInfo.OwnerName));
          return;
        }
      }
      
      var projectsFolder = Folders.GetAll().SingleOrDefault(f => f.Uid == Sungero.Projects.Constants.Module.ProjectFolders.ProjectFolderUid);
      if (projectsFolder == null)
      {
        e.AddWarning(DirRX.ProjectPlanning.Projects.Resources.CannotPerformSavePlan);
      }

      // Проверка циклических ссылок в подпроектах.
      if (_obj.State.Properties.LeadingProject.IsChanged && _obj.LeadingProject != null)
      {
        var leadingProject = _obj.LeadingProject;
        
        while (leadingProject != null)
        {
          if (Equals(leadingProject, _obj))
          {
            e.AddError(_obj.Info.Properties.LeadingProject, Projects.Resources.LeadingProjectCyclicReference, _obj.Info.Properties.LeadingProject);
            break;
          }
          
          leadingProject = leadingProject.LeadingProject;
        }
      }
      _obj.Modified = Calendar.Now;
      
      Functions.Project.ApplyNewProjectPlan(_obj);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      var projectsFolder = Folders.GetAll().SingleOrDefault(f => f.Uid == Sungero.Projects.Constants.Module.ProjectFolders.ProjectFolderUid);
      
      if (projectsFolder != null)
      {
        base.Saving(e);
      }
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);
    }

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      // Изъять права на этапы проекта.
      ProjectPlanner.ProjectActivities.AccessRights.RevokeAll(_obj.Administrator);
      ProjectPlanner.ProjectActivities.AccessRights.Save();
      
      base.Deleting(e);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.StartDate = Calendar.Now;
    }
  }

}