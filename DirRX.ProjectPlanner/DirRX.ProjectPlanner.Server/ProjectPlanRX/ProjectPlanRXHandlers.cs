using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;
using DirRX.TeamsCommonAPI.NotifyEventType;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;
using DirRX.TeamsCommonAPI.NotifyConditionItem;

namespace DirRX.ProjectPlanner
{
  partial class ProjectPlanRXPlanDateNoticesObserversPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesObserversFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesMgmntTeamPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesMgmntTeamFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesParticipantPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesParticipantFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesCustomerInternalPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesCustomerInternalFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesProjectAdminPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesProjectAdminFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesActivityRepPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesActivityRepFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesRepSectionPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesRepSectionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXPlanDateNoticesProjectLeadPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PlanDateNoticesProjectLeadFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => 
                         (_obj.EventType.EventType != EventType.ActOverdated 
                          && _obj.EventType.EventType != EventType.MilestOverdated 
                          && _obj.EventType.EventType != EventType.PlanOverdated 
                          && _obj.EventType.EventType != EventType.SectOverdated
                          && _obj.EventType.EventType != EventType.ActCannotStart
                         ) || q.Condition != Condition.Early
                        );
    }
  }

  partial class ProjectPlanRXOtherNoticesObserversPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesObserversFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesMgmntTeamPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesMgmntTeamFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesParticipantPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesParticipantFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesCustomerInternalPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesCustomerInternalFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesProjectAdminPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesProjectAdminFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesActivityRepPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesActivityRepFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesRepSectionPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesRepSectionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXOtherNoticesProjectLeadPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OtherNoticesProjectLeadFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesObserversPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesObserversFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesMgmntTeamPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesMgmntTeamFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesParticipantPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesParticipantFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesCustomerInternalPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesCustomerInternalFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesProjectAdminPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesProjectAdminFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesActivityRepPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesActivityRepFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesRepSectionPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesRepSectionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }

  partial class ProjectPlanRXResponsibleNoticesProjectLeadPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesProjectLeadFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.Condition != Condition.Early);
    }
  }


  partial class ProjectPlanRXResponsibleNoticesEventTypePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResponsibleNoticesEventTypeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(q => q.SectionIdentifier == DirRX.TeamsCommonAPI.NotifyEventType.SolutionIdentifier.ProjectPlanning);
    }
  }

  partial class ProjectPlanRXDocumentKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString());
    }
  }

  partial class ProjectPlanRXFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (_filter.Active || _filter.Draft || _filter.Obsolete)
        query = query.Where(x => (_filter.Active && x.LifeCycleState == LifeCycleState.Active) ||
                            (_filter.Draft && x.LifeCycleState == LifeCycleState.Draft) ||
                            (_filter.Obsolete && x.LifeCycleState == LifeCycleState.Obsolete));

      var today = Calendar.UserToday;
      
      // Фильтр по дате начала проекта.
      var startDateBeginPeriod = _filter.StartDateRangeFrom ?? Calendar.SqlMinValue;
      var startDateEndPeriod = _filter.StartDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.StartPeriodThisMonth)
      {
        startDateBeginPeriod = today.BeginningOfMonth();
        startDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.StartPeriodThisMonth || (_filter.StartDateRangeFrom != null || _filter.StartDateRangeTo != null))
        query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Status, ProjectPlanRX.Status.Closed)) ||
                            (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Status, ProjectPlanRX.Status.Closed)));

      // Фильтр по дате окончания проекта.
      var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
      var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.FinishPeriodThisMonth)
      {
        finishDateBeginPeriod = today.BeginningOfMonth();
        finishDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
        query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Status, ProjectPlanRX.Status.Closed)) ||
                            (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Status, ProjectPlanRX.Status.Closed)));
      
      return query;
    }
  }

  partial class ProjectPlanRXCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ActualStartDate);
      e.Without(_info.Properties.ActualFinishDate);
      e.Without(_info.Properties.ExecutionPercent);
      e.Without(_info.Properties.Note);
      e.Params.Add("IsCopy", true);
      e.Params.Add("CopiedSourceEntityId", _source.Id);
    }
  }

  partial class ProjectPlanRXServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      base.BeforeDelete(e);
    }

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.BodyConverted = true; // чтобы не пытался запустить конвертацию по новым планам
      _obj.Modified = Calendar.Now;
      _obj.StartDate = Calendar.Now;
      _obj.LifeCycleState = ProjectPlanRX.LifeCycleState.Draft;
      _obj.Status = ProjectPlanRX.Status.Active;
      _obj.IsEnableAutoSendingTasks = true;
      
      var defaultOrFirstActiveDocKind = Sungero.Docflow.DocumentKinds
        .GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString() &&
          x.Status == Sungero.Docflow.DocumentKind.Status.Active)
        .OrderByDescending(x => x.IsDefault.HasValue && x.IsDefault.Value)
        .FirstOrDefault();
      
      if (defaultOrFirstActiveDocKind == null)
      {
        throw new ArgumentException(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.CannotFindDocKind);
      }
      
      _obj.DocumentKind = defaultOrFirstActiveDocKind;
      
      var isCopy = false;
      e.Params.TryGetValue("IsCopy", out isCopy);
      _obj.IsCopy = isCopy;
      
      if (isCopy)
      {
        long entityId = 0;
        e.Params.TryGetValue("CopiedSourceEntityId", out entityId);
        
        if (entityId != 0)
        {
          var sourcePlan = ProjectPlanRXes.Get(entityId);
          foreach (var setting in sourcePlan.PlanDateNotices.Select(s => s.LinkedSetting).Concat(sourcePlan.ResponsibleNotices.Select(s => s.LinkedSetting)).Concat(sourcePlan.OtherNotices.Select(s => s.LinkedSetting)))
          {
            var newSetting = TeamsNoticesSettingses.Copy(setting);
            newSetting.ConnectedEntityId = _obj.Id;
            newSetting.Save();
            
          }
        }
      }
      else
      {
        Functions.ProjectPlanRX.RecreateDefaultSettingsWithoutSave(_obj);
      }
      
      _obj.EnableReviewAssignmentFlag = true;
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
      
      var recipient = linkedProject != null ? linkedProject.Administrator : _obj.Author;
      // Изъять права на этапы проекта.
      ProjectPlanner.ProjectActivities.AccessRights.RevokeAll(recipient);
      ProjectPlanner.ProjectActivities.AccessRights.Save();
      _obj.ResponsibleNotices.Clear();
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      if (!e.Params.Contains(Constants.Module.DontUpdateModified) && e.Params.Contains(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument))
      {
        Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
        e.Params.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument);
      }
      
      if (!e.Params.Contains(Constants.Module.DontUpdateModified))
        Sungero.Projects.Jobs.GrantAccessRightsToProjectFolders.Enqueue();
      
      Functions.ProjectPlanRX.NotifyAboutChanges(_obj);
    }

    private void UpdateLinkedProject()
    {
      //TODO Urmanov Нагруженная часть, можно отказаться при отказе от связи с проектом
      var linkedProject = Functions.ProjectPlanRX.GetLinkedProject(_obj);
      if (!Functions.ProjectPlanRX.CanUpdateLinkedProject(linkedProject))
      {
        return;
      }
      if (_obj.State.Properties.StartDate.IsChanged)
      {
        linkedProject.StartDate = _obj.StartDate;
      }
      
      if (_obj.State.Properties.EndDate.IsChanged)
      {
        linkedProject.EndDate = _obj.EndDate;
      }
      
      if (_obj.State.Properties.ActualStartDate.IsChanged)
      {
        linkedProject.ActualStartDate = _obj.ActualStartDate;
      }
      if (_obj.State.Properties.ActualFinishDate.IsChanged)
      {
        linkedProject.ActualFinishDate = _obj.ActualFinishDate;
      }
      
      if (_obj.State.Properties.ExecutionPercent.IsChanged)
      {
        linkedProject.ExecutionPercent = _obj.ExecutionPercent;
      }
      
      if (_obj.State.Properties.FactualCosts.IsChanged)
      {
        linkedProject.FactualCosts = _obj.FactualCosts;
      }

      if (_obj.State.Properties.PlannedCosts.IsChanged)
      {
        linkedProject.PlannedCosts = _obj.PlannedCosts;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!_obj.AccessRights.CanUpdate())
      {
        e.AddError(ProjectPlanRXes.Resources.NoRightToUpdateProject);
        return;
      }
      
      UpdateLinkedProject();
      
      // TODO Zamerov: сравнивать надо с ресурсом в локали тенанта. BUG: 35010
      if (Equals(_obj.Name, Sungero.Projects.Resources.ProjectArhiveFolderName))
        e.AddError(ProjectPlanRXes.Resources.PropertyReservedFormat(_obj.Info.Properties.Name.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));

      if (!e.Params.Contains(Constants.Module.DontUpdateModified))
        _obj.Modified = Calendar.Now; 
    }
  }
  
  partial class ProjectPlanRXTeamMembersMemberPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> TeamMembersMemberFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.Administrators &&
                         x.Sid != Sungero.Domain.Shared.SystemRoleSid.Auditors &&
                         x.Sid != Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers &&
                         x.Sid != Sungero.Domain.Shared.SystemRoleSid.ServiceUsers &&
                         x.Sid != Sungero.Domain.Shared.SystemRoleSid.SoloUsers &&
                         x.Sid != Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid);
    }
  }

}
