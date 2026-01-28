using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.TeamsCommonAPI.NotifyEventType;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Актуализация сведений о затратах, трудоемкости и ходе работ по планам.
    /// </summary>
    public virtual void SyncProjectPlanData()
    {
      var planProjectIds = Functions.Module.GetModifiedPlanProjectIdsFromDB();
      
      if (!planProjectIds.Any())
      {
        return;
      }
      
      var projectIds = planProjectIds.Values.Where(p => p.HasValue);
      var plans = ProjectPlanRXes.GetAll(p => planProjectIds.Keys.Contains(p.Id)).ToDictionary(p => p.Id);
      var projects = ProjectPlanning.Projects.GetAll(p => projectIds.Contains(p.Id)).ToDictionary(p => p.Id);
      
      var data = ProjectPlanner.Functions.ProjectPlanRX.GetCalculatedPlanData(planProjectIds.Keys);
      var updatedPlanIds = new List<long>();
      
      foreach(var plan in plans)
      {
        try
        {
          Structures.ProjectPlanRX.IPlanCalculatedData planData;
          if (!data.TryGetValue(plan.Key, out planData))
          {
            updatedPlanIds.Add(plan.Key);
            continue;
          }
          
          long? projectId = null;
          ProjectPlanning.IProject project = null;
          planProjectIds.TryGetValue(plan.Key, out projectId);
          
          if (projectId.HasValue)
          {
            projects.TryGetValue(projectId.Value, out project);
          }

          if (TryUpdatePlanProject(plan.Value, project, planData))
          {
            updatedPlanIds.Add(plan.Key);
          }
        }
        catch(Exception ex)
        {
          Logger.Error(Resources.SyncProjectPlanDataErrorFormat(plan.Key), ex);
        }
      }
      
      if (updatedPlanIds.Any())
      {
        Functions.Module.DeleteModifiedPlanIdsFromDB(updatedPlanIds);
      }
    }
    
    private bool TryUpdatePlanProject(IProjectPlanRX plan, ProjectPlanning.IProject project, Structures.ProjectPlanRX.IPlanCalculatedData planData)
    {      
      if ((project != null && Locks.GetLockInfo(project).IsLockedByOther) || Locks.GetLockInfo(plan).IsLockedByOther)
      {
        return false;
      }
      
      Locks.Lock(plan);
      
      if (project != null)
      {
        Locks.Lock(project);
        UpdatePropertiesProject(project, planData);
        
        if (project.State.IsChanged)
        {
          project.Save();
        }
      }
      
      UpdatePropertiesPlan(plan, planData);
      
      if (plan.State.IsChanged)
      {
        plan.Save();
      }
      
      Locks.Unlock(plan);
      
      if (project != null)
      {
        Locks.Unlock(project);
      }
      
      return true;
    }
    
    private static void UpdatePropertiesPlan(IProjectPlanRX plan, Structures.ProjectPlanRX.IPlanCalculatedData planData)
    {
      ChangePropertyIfDifferentValue(plan, "ExecutionPercent", planData.ExecutionPercent);
      ChangePropertyIfDifferentValue(plan, "StartDate", planData.PlanStartDate);
      ChangePropertyIfDifferentValue(plan, "EndDate", planData.PlanEndDate);
      ChangePropertyIfDifferentValue(plan, "ActualStartDate", planData.FactStartDate);
      ChangePropertyIfDifferentValue(plan, "ActualFinishDate", planData.FactEndDate);
      ChangePropertyIfDifferentValue(plan, "PlannedCosts", planData.PlanCosts);
      ChangePropertyIfDifferentValue(plan, "FactualCosts", planData.FactCosts);
      ChangePropertyIfDifferentValue(plan, "BaselineWork", planData.PlanWorkload);
      ChangePropertyIfDifferentValue(plan, "ActualWorkload", planData.FactWorkload);
    }
    
    private static void UpdatePropertiesProject(ProjectPlanning.IProject project, Structures.ProjectPlanRX.IPlanCalculatedData planData)
    { 
      ChangePropertyIfDifferentValue(project, "ExecutionPercent", planData.ExecutionPercent);
      ChangePropertyIfDifferentValue(project, "StartDate", planData.PlanStartDate);
      ChangePropertyIfDifferentValue(project, "EndDate", planData.PlanEndDate);
      ChangePropertyIfDifferentValue(project, "ActualStartDate", planData.FactStartDate);
      ChangePropertyIfDifferentValue(project, "ActualFinishDate", planData.FactEndDate);
      ChangePropertyIfDifferentValue(project, "PlannedCosts", planData.PlanCosts);
      ChangePropertyIfDifferentValue(project, "FactualCosts", planData.FactCosts);
      ChangePropertyIfDifferentValue(project, "PlannedWorkloadDirRX", planData.PlanWorkload);
      ChangePropertyIfDifferentValue(project, "ActualWorkloadDirRX", planData.FactWorkload);
    }
    
    
    private static void ChangePropertyIfDifferentValue(object propertySource, string propertyName, object newValue)
    {
      var previousValue = propertySource.GetType().GetProperty(propertyName).GetValue(propertySource);
      
      if (newValue == previousValue)
      {
        return;
      }
      
      if (newValue is System.DateTime && previousValue is System.DateTime)
      {
        var castedNewValue = newValue as System.Nullable<System.DateTime>;
        var castedPreviousValue = previousValue as System.Nullable<System.DateTime>;
        if (castedNewValue != null && castedPreviousValue != null && castedNewValue.Value.Date == castedPreviousValue.Value.Date)
        {
          return;
        }
      }
      
      if ((previousValue == null && newValue != null) || (newValue == null && previousValue != null) || !previousValue.Equals(newValue))
      {
        propertySource.GetType().GetProperty(propertyName).SetValue(propertySource, newValue);
      }
    }

    /// <summary>
    /// Запуск ФП по отправке задач по этапам.
    /// </summary>
    public virtual void LaunchAutoSendingTasksPP()
    {
      var planVersionPairs = this.GetPlanLastVersionPairFromDB();
      
      foreach (var pair in planVersionPairs)
      {
        var asyncHandler = AsyncHandlers.LaunchTasksByPlanLastVersionAsync.Create();
        asyncHandler.PlanId = pair.Key;
        asyncHandler.LastVersionNumber = pair.Value;
        asyncHandler.ExecuteAsync();
      }
     
    }
    
    /// <summary>
    /// Получить словарь из ид планов и номеров последних версий.
    /// </summary>
    /// <returns>Словарь из ид планов и номера последних версий.</returns>
    private Dictionary<long, int> GetPlanLastVersionPairFromDB()
    {
      var planLastVersionPairs = new Dictionary<long, int>();
      
      using (var connection = SQL.CreateConnection())
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetPlanLastVersionsAutoSendingTasks;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            planLastVersionPairs.Add((long)reader[0], (int)reader[1]);
          }
        }
      }
      
      return planLastVersionPairs;
    }
    
    private RNDNoticesUtils.Structures.NoticeItem GetNoticeItem(DirRX.TeamsCommonAPI.ITeamsNoticesSettings settingItem, DateTime taskDeadline, object projectPlan = null, object projectActivity = null)
    {
      var setting = DirRX.TeamsCommonAPI.TeamsNoticesSettingses.As(settingItem);
      var attachments = new List<Sungero.Domain.Entity>();
      
      Sungero.Domain.Entity mainEntity = null;
      
      string firstTextParam = null;
      string secondaryTextParam = null;
      
      if (projectPlan != null)
      {
        attachments.Add((Sungero.Domain.Entity) projectPlan);
        mainEntity = (Sungero.Domain.Entity) projectPlan;
        
        firstTextParam = Hyperlinks.Get((Sungero.Domain.Entity) projectPlan);
      }
      
      if (projectActivity != null)
      {
        attachments.Add((Sungero.Domain.Entity) projectActivity);
        
        if (mainEntity == null)
        {
          mainEntity = (Sungero.Domain.Entity) projectActivity;
        }
        
        if (firstTextParam == null)
        {
          firstTextParam = Hyperlinks.Get((Sungero.Domain.Entity) projectActivity);
        }
        else
        {
          secondaryTextParam = Hyperlinks.Get((Sungero.Domain.Entity) projectActivity);
        }
      }
      
      var clientWebsite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      
      var noticeItem = new RNDNoticesUtils.Structures.NoticeItem(setting, attachments, clientWebsite, mainEntity, firstTextParam, secondaryTextParam);
      
      noticeItem.TaskDeadLine = taskDeadline;
      noticeItem.SolutionGuid = Constants.Module.ProjectPlanGuid;
      
      return noticeItem;
    }
    
    private IProjectPlanRXPlanDateNotices GetSettingByPlanAndEventType(IProjectPlanRX plan, Sungero.Core.Enumeration eventType)
    {
      var setting = plan?.PlanDateNotices?.Where(s => s.EventType.EventType.HasValue && s.EventType.EventType == eventType).FirstOrDefault();
      if (setting == null)
      {
        Logger.DebugFormat("NoticeTrigger. Не найдена соответствующая eventType {0} настройка для плана с id {1}", eventType.Value, plan.Id);
      }
      
      return setting;
    }
    
    public virtual void NoticeTriggerer()
    {
      this.NotifyAboutPlansWillStart();
      this.NotifyAboutPlansWillEnd();
      this.NotifyAboutOverdatedPlans();
      this.NoyifyAboutActivitiesWillStart();
      this.NotifyAboutActivitiesWillEnd();
      this.NotifyAboutOverdatedActivities();
    }
    
    private void NotifyAboutPlansWillStart()
    {
      var plansWillStart = ProjectPlanRXes.GetAll(p => p.StartDate.HasValue).ToList()
        .Where(p => (p.StartDate.Value - Calendar.Today) <= TimeSpan.FromDays(3) && (p.StartDate.Value - Calendar.Today >= TimeSpan.FromMinutes(1)));
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var plan in plansWillStart)
      {
        setting = GetSettingByPlanAndEventType(plan, EventType.PlanMustStarted);
        if (setting == null)
        {
          continue;
        }
        
        DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, plan.StartDate ?? Calendar.Today, plan));
      }
    }
    
    private void NotifyAboutPlansWillEnd()
    {
      var plansWillEnd = ProjectPlanRXes.GetAll(p => p.EndDate.HasValue).ToList()
        .Where(p => (p.EndDate.Value - Calendar.Today) <= TimeSpan.FromDays(3) && (p.EndDate.Value - Calendar.Today >= TimeSpan.FromMinutes(1)));
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var plan in plansWillEnd)
      {
        setting = GetSettingByPlanAndEventType(plan, EventType.PlanMustEnded);
        if (setting == null)
        {
          continue;
        }
        
        DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, plan.EndDate ?? Calendar.Today, plan));
      }
    }
    
    private void NotifyAboutOverdatedPlans()
    {
      var overdatedPlans = ProjectPlanRXes.GetAll(p => p.EndDate.HasValue && p.Status.HasValue).ToList()
        .Where(p => (p.EndDate.Value - Calendar.Today) <= TimeSpan.FromMinutes(1) && p.Status == DirRX.ProjectPlanner.ProjectPlanRX.Status.Active);
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var plan in overdatedPlans)
      {
        setting = GetSettingByPlanAndEventType(plan, EventType.PlanOverdated);
        if (setting == null)
        {
          continue;
        }
        
        DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, Calendar.Today, plan));
      }
    }
    
    private void NoyifyAboutActivitiesWillStart()
    {
      var activitiesWillStart = ProjectActivities.GetAll(p => p.StartDate.HasValue && p.TypeActivity.HasValue && p.ProjectPlan != null).ToList()
        .Where(p => (p.StartDate.Value - Calendar.Today) <= TimeSpan.FromDays(3) && (p.StartDate.Value - Calendar.Today >= TimeSpan.FromMinutes(1)));
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var activiy in activitiesWillStart)
      {
        if (activiy.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task)
        {
          setting = GetSettingByPlanAndEventType(activiy.ProjectPlan, EventType.ActMustStarted);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, activiy.StartDate ?? Calendar.Today, null, activiy));
          continue;
        }
        
        if (activiy.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section)
        {
          setting = GetSettingByPlanAndEventType(activiy.ProjectPlan, EventType.SectMustStarted);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, activiy.StartDate ?? Calendar.Today, null, activiy));
          continue;
        }
        
        setting = GetSettingByPlanAndEventType(activiy.ProjectPlan, EventType.MilestMustReach);
        if (setting == null)
        {
          continue;
        }
        
        DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, activiy.StartDate ?? Calendar.Today, null, activiy));
      }
    }
    
    private void NotifyAboutActivitiesWillEnd()
    {
      var activitiesWillEnd = ProjectActivities.GetAll(p => p.EndDate.HasValue && p.TypeActivity.HasValue && p.ProjectPlan != null).ToList()
        .Where(p => (p.EndDate.Value - Calendar.Today) <= TimeSpan.FromDays(3) && (p.EndDate.Value - Calendar.Today >= TimeSpan.FromMinutes(1)));
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var activity in activitiesWillEnd)
      {
        if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task)
        {
          setting = GetSettingByPlanAndEventType(activity.ProjectPlan, EventType.ActMustEnded);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, activity.EndDate ?? Calendar.Today, null, activity));
          continue;
        }
        
        if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section)
        {
          setting = GetSettingByPlanAndEventType(activity.ProjectPlan, EventType.SectMustEnded);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, activity.EndDate ?? Calendar.Today, null, activity));
          continue;
        }
      }
    }
    
    private void NotifyAboutOverdatedActivities()
    {
      var overdatedActivities = ProjectActivities.GetAll(p => p.EndDate.HasValue && p.Status.HasValue && p.TypeActivity.HasValue && p.ProjectPlan != null).ToList()
        .Where(p => (p.EndDate.Value - Calendar.Today) <= TimeSpan.FromDays(0) && p.Status == DirRX.ProjectPlanner.ProjectPlanRX.Status.Active);
      IProjectPlanRXPlanDateNotices setting;
      
      foreach (var activity in overdatedActivities)
      {
        if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task)
        {
          setting = GetSettingByPlanAndEventType(activity.ProjectPlan, EventType.ActOverdated);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, Calendar.Today, null, activity));
          continue;
        }
        
        if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section)
        {
          setting = GetSettingByPlanAndEventType(activity.ProjectPlan, EventType.SectOverdated);
          if (setting == null)
          {
            continue;
          }
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, Calendar.Today, null, activity));
        }
        
        setting = GetSettingByPlanAndEventType(activity.ProjectPlan, EventType.MilestOverdated);
        if (setting == null)
        {
          continue;
        }
        
        DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting.LinkedSetting, Calendar.Today, null, activity));
      }
    }
    
    public virtual void SyncAccessRightsProjectPlanAndProject()
    {
      SyncAccessRightsProjectPlanAndProjectHandle();
      SyncAccessRightsProjectAndGatesHandle();
    }
    
    private void SyncAccessRightsProjectPlanAndProjectHandle()
    {
      var needUpdateLastRunDate = true;
      var previousRun = GetLastSyncAccessRightsDate(Constants.Module.LastSyncAccessRightsOfProject);
      var startTimestamp = Calendar.Now;
      
      var projectPlanHistories = Sungero.Content.DocumentHistories.GetAll(x =>
        x.EntityType.ToString() == Constants.Module.ProjectPlanDocKindGuidString &&
        x.HistoryDate.HasValue && x.HistoryDate.Value > previousRun && x.HistoryDate.Value <= startTimestamp &&
        x.Action.HasValue && x.Action.Value == Sungero.CoreEntities.History.Action.Manage
      );
      
      var projects = DirRX.ProjectPlanning.Projects.GetAll(x => x.ProjectPlanDirRX != null &&
        x.Modified >= previousRun && x.Modified <= startTimestamp ||
        //Kiselev_EM Добавляем проекты у которых руками были изменены права в плане проекта.
        projectPlanHistories.Any(h => h.EntityId == x.ProjectPlanDirRX.Id && x.Modified <= h.HistoryDate));
      
      var plansIds = new HashSet<long>();
      foreach (var project in projects.OrderByDescending(x => x.Modified))
      {
        try
        {
          if (plansIds.Contains(project.ProjectPlanDirRX.Id))
          {
            TryDetachProjectPlanLink(project);
            continue;
          }
          
          plansIds.Add(project.ProjectPlanDirRX.Id);
          
          var projectPlanRights = project.ProjectPlanDirRX.AccessRights.Current.ToList();
          var projectRights = project.AccessRights.Current.ToList();
          
          foreach (var rule in projectPlanRights)
          {
            if(!projectRights.Any(pr => pr.AccessRightsType == rule.AccessRightsType && pr.Recipient == rule.Recipient))
            {
              project.ProjectPlanDirRX.AccessRights.Revoke(rule.Recipient, rule.AccessRightsType);
            }
          }
          
          foreach (var rule in projectRights)
          {
            if(!projectPlanRights.Any(pr => pr.AccessRightsType == rule.AccessRightsType && pr.Recipient == rule.Recipient))
            {
              project.ProjectPlanDirRX.AccessRights.Grant(rule.Recipient, rule.AccessRightsType);
            }
          }
          
          project.ProjectPlanDirRX.AccessRights.Save();
        }
        catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException ex)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.SyncAccessRightsProjectErrorFormat(project.Id, project.DisplayValue, ex.Message, "SyncAccessRightsProjectPlanAndProject"));
          needUpdateLastRunDate = false;
        }
        catch (Exception ex)
        {
          Logger.Error(DirRX.ProjectPlanner.Resources.SyncAccessRightsProjectErrorFormat(project.Id, project.DisplayValue, ex.Message, "SyncAccessRightsProjectPlanAndProject"));
          needUpdateLastRunDate = false;
        }
      }
      
      if (needUpdateLastRunDate)
      {
        UpdateLastSyncAccessRightsDate(Constants.Module.LastSyncAccessRightsOfProject, startTimestamp);
      }
    }
    
    private void TryDetachProjectPlanLink(ProjectPlanning.IProject project)
    {
      var planId = project.ProjectPlanDirRX?.Id;

      try
      {
        project.ProjectPlanDirRX = null;
        project.Save();
        Logger.DebugFormat("Job \"{0}\" has found project plan with ID {1} related to two projects. Relation between the plan and project with ID {2} has been deleted.",
          nameof(ProjectPlanner.Jobs.SyncAccessRightsProjectPlanAndProject), planId, project.Id);
      }
      catch(Sungero.Domain.Shared.Exceptions.LockManagementException ex)
      {
        Logger.DebugFormat("An error occurred when running job \"{0}\". Cannot delete relation between plan with ID {1} and project with ID {2}. {3}",
          nameof(ProjectPlanner.Jobs.SyncAccessRightsProjectPlanAndProject), planId, project.Id, ex.Message);
      }
    }

    private void SyncAccessRightsProjectAndGatesHandle()
    {
      var needUpdateLastRunDate = true;
      var previousRun = GetLastSyncAccessRightsDate(Constants.Module.LastSyncAccessRightsOfProjectGates);
      var startTimestamp = Calendar.Now;

      var gatesHistories = Sungero.Content.DocumentHistories.GetAll(x =>
        x.EntityType == Gate.ClassTypeGuid &&
        x.HistoryDate.HasValue && x.HistoryDate.Value > previousRun && x.HistoryDate.Value <= startTimestamp &&
        x.Action.HasValue && x.Action.Value == Sungero.CoreEntities.History.Action.Manage
      );
      
      var projects = DirRX.ProjectPlanning.ProjectCores.GetAll(x => 
        x.Modified > previousRun && x.Modified <= startTimestamp ||
        // yarovikov_gv Добавляем КТ у которых руками были изменены права,
        // чтобы принудительно им восстановить разрешения как у объекта управления
        gatesHistories.Any(h => x.Modified <= h.HistoryDate && x.GatesDirRX.Any(g => g.Id == h.EntityId))
       );
      
      foreach (var project in projects)
      {
        try
        {
          // yarovikov_gv Автоматически выдаем и забираем только на изменение
          // на чтение выданы всем, ограничивающие не трогаем
          var projectRights = project.AccessRights.Current.Where(g =>
                                                                 g.AccessRightsType == Sungero.Core.DefaultAccessRightsTypes.Change ||
                                                                 g.AccessRightsType == Sungero.Core.DefaultAccessRightsTypes.FullAccess
                                                                ).ToList();

          foreach (var gate in project.GatesDirRX.Select(x => x.Gate))
          {
            var gateRights = gate.AccessRights.Current.Where(g => 
                                                             g.AccessRightsType == Sungero.Core.DefaultAccessRightsTypes.Change ||
                                                             g.AccessRightsType == Sungero.Core.DefaultAccessRightsTypes.FullAccess
                                                            ).ToList();
            
            foreach (var rule in gateRights)
            {
              if(!projectRights.Any(pr => pr.AccessRightsType == rule.AccessRightsType && pr.Recipient == rule.Recipient))
              {
                gate.AccessRights.Revoke(rule.Recipient, rule.AccessRightsType);
              }
            }
            
            foreach (var rule in projectRights)
            {
              if(!gateRights.Any(pr => pr.AccessRightsType == rule.AccessRightsType && pr.Recipient == rule.Recipient))
              {
                gate.AccessRights.Grant(rule.Recipient, rule.AccessRightsType);
              }
            }
            
            gate.AccessRights.Save();
          }
        }
        catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException ex)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.SyncAccessRightsProjectErrorFormat(project.Id, project.DisplayValue, ex.Message, "SyncAccessRightsProjectAndGates"));
          needUpdateLastRunDate = false;
        }
        catch (Exception ex)
        {
          Logger.Error(DirRX.ProjectPlanner.Resources.SyncAccessRightsProjectErrorFormat(project.Id, project.DisplayValue, ex.Message, "SyncAccessRightsProjectAndGates"));
          needUpdateLastRunDate = false;
        }
      }
      
      if (needUpdateLastRunDate)
      {
        UpdateLastSyncAccessRightsDate(Constants.Module.LastSyncAccessRightsOfProjectGates, startTimestamp);
      }
    }
    
    /// <summary>
    /// Получить дату последней синхронизации прав.
    /// </summary>
    /// <returns>Дата последней синхронизации прав.</returns>
    private static DateTime GetLastSyncAccessRightsDate(string key)
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        else
          return Calendar.Today;
        
        Logger.DebugFormat("{1} Last sync date in DB is {0} (UTC)", date, key);
        
        DateTime result = Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));
        return result;
      }
      catch (Exception ex)
      {
        Logger.Error("{1} Error while getting last sync date", ex, key);
        return Calendar.Today;
      }
    }

    /// <summary>
    /// Обновить дату последней синхронизации прав.
    /// </summary>
    /// <param name="notificationDate">Дата синхронизации прав.</param>
    private static void UpdateLastSyncAccessRightsDate(string key, DateTime notificationDate)
    {
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("{1} Last sync date is set to {0} (UTC)", newDate, key);
    }
  }
}
