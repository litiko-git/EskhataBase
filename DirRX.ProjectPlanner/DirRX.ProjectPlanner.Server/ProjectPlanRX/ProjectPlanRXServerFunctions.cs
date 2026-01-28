using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Clients;
using DirRX.ProjectPlanner.ProjectPlanRX;
using DirRX.TeamsCommonAPI.NotifyEventType;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;
using DirRX.TeamsCommonAPI;
using PplanMessages;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectPlanRXFunctions
  {

    /// <summary>
    /// Получить модель состояния для плановых данных.
    /// </summary>
    /// <returns>Модель состояния для плановых данных.</returns>
    [Remote(IsPure = true)]
    public StateView TableStatePlanDataFunction()
    {
      var stateView = StateView.Create();
      var data = GetCalculatedPlanDataCached(new List<long>() {_obj.Id})[_obj.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      var header = StateBlockLabelStyle.Create();
      header.FontSize = 13;

      var block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.PlannedCostsName, bold);
      block.AddContent().AddLabel(data.PlanCosts.ToString() + "  ");  // флаг без границы, поэтому справа отступ руками добавил
      block.DockType = DockType.Bottom;
      block.ShowBorder = false;

      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.PlanWorkloadName, bold);
      block.AddContent().AddLabel(data.PlanWorkload.ToString());
      block.DockType = DockType.Bottom;
      block.ShowBorder = true;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent();
      block.AddContent().AddLabel(Resources.ProjectDates, header);
      block.ShowBorder = false;
      block.DockType = DockType.Bottom;
                
      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.PlanStartDateName, bold);
      block.AddContent().AddLabel(data.PlanStartDate?.ToShortDateString() + "  ");  // флаг без границы, поэтому справа отступ руками добавил
      block.ShowBorder = false;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.PlanEndDateName, bold);
      block.AddContent().AddLabel(data.PlanEndDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;

      return stateView;
    }
    
    /// <summary>
    /// Получить модель состояния для фактических данных.
    /// </summary>
    /// <returns>Модель состояния для фактических данных.</returns>
    [Remote(IsPure = true)]
    public StateView TableStateFactDataFunction()
    {
      var data = GetCalculatedPlanDataCached(new List<long>() {_obj.Id})[_obj.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      var heavy = StateBlockLabelStyle.Create();
      heavy.FontSize = 18;
      
      var stateView = StateView.Create();

      var block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.ExecutionPercentageName, bold);
      block.AddContent().AddLabel(data.ExecutionPercent.ToString() + "  ");  // флаг без границы, поэтому справа отступ руками добавил
      block.DockType = DockType.Bottom;
      block.ShowBorder = false;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.FactCostsName, bold);
      block.AddContent().AddLabel(data.FactCosts.ToString());
      block.DockType = DockType.Bottom;
      block.ShowBorder = true;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.FactWorkloadName, bold);
      block.AddContent().AddLabel(data.FactWorkload.ToString());
      block.DockType = DockType.Bottom;
      block.ShowBorder = true;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", heavy);
      block.ShowBorder = false;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.FactStartDateName, bold);
      block.AddContent().AddLabel(data.FactStartDate?.ToShortDateString() + "  ");  // флаг без границы, поэтому справа отступ руками добавил
      block.ShowBorder = false;
      block.DockType = DockType.Bottom;

      block = stateView.AddBlock();
      block.AddContent().AddLabel(Resources.FactEndDateName, bold);
      block.AddContent().AddLabel(data.FactEndDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;

      return stateView;
    }

    /// <summary>
    /// Заблокировать план проекта и связанный проект если он есть.
    /// </summary>
    /// <param name="projectPlan">План проекта.</param>
    /// <returns>Текст с сообщением о блокировке, если на момент вызова план проекта/проект уже заблокмрован другим пользователем.
    /// Возвращается пустая строка если сущности успешно заблокированы.</returns>
    /// <exception cref="Exception">Если неудалось заблокировать план проекта, связанный проект или если нет прав на редактирование.</exception>
    [Remote]
    public static string SetLockOnProjectPlan(IProjectPlanRX projectPlan)
    {
      if (!projectPlan.AccessRights.CanUpdate())
      {
        return DirRX.ProjectPlanner.ProjectPlanRXes.Resources.HaveNoRightsToEditProjectPlanFormat(projectPlan.DisplayValue);
      }
      
      //Без повышения прав не удастся найти сущность за пользователя, у которого нет на нее прав.
      DirRX.ProjectPlanning.IProject relatedProject = null;
      AccessRights.AllowRead(() =>
                             {
                               relatedProject = Functions.ProjectPlanRX.GetLinkedProject(projectPlan);
                             });
      
      if (relatedProject != null && !relatedProject.AccessRights.CanUpdate())
      {
        return DirRX.ProjectPlanner.ProjectPlanRXes.Resources.HasNoRightsOnRelatedProjectFormat(relatedProject.DisplayValue);
      }
      
      try
      {
        var planLockedByMe = Locks.GetLockInfo(projectPlan).IsLockedByMe;
        if (!planLockedByMe)
        {
          Locks.Lock(projectPlan);
        }
      }
      catch(Sungero.Domain.Shared.Exceptions.RepeatedLockException ex)
      {
        return DirRX.ProjectPlanner.Resources.BlockedProjectPlanCardTextFormat(ex.Message);
      }
      catch
      {
        throw;
      }
      
      if (relatedProject == null)
      {
        return string.Empty;
      }
      
      try
      {
        var lockedByMe = Locks.GetLockInfo(relatedProject).IsLockedByMe;
        if (!lockedByMe)
        {
          Locks.Lock(relatedProject);
        }
      }
      catch(Sungero.Domain.Shared.Exceptions.RepeatedLockException ex)
      {
        return DirRX.ProjectPlanner.Resources.BlockedLinkedProjectTextFormat(ex.Message);
      }
      catch
      {
        throw;
      }
      
      return string.Empty;
    }
    
    [Remote(IsPure = true)]
    public static string GetSimpleDocumentTypeGuid()
    {
      return Sungero.Docflow.Server.SimpleDocument.ClassTypeGuid.ToString();
    }
    
    public void NotifyAboutChanges()
    {
      var diffs = NotifyDiffs.GetAll(d => d.ConnectedPlanId == _obj.Id).ToList();
      
      foreach (var diff in diffs.Where(d => d != null))
      {
        var setting = GetSettings(diff.EventTypeId);

        if (setting != null)
        {
          var connectedActivity = ProjectActivities.GetAll(a => a.Id == diff.ConnectedActId).FirstOrDefault();
          setting = ConfigureRecipients(setting, connectedActivity);
          
          DirRX.TeamsCommonAPI.PublicFunctions.Module.RaiseNotify(GetNoticeItem(setting, _obj, connectedActivity, diff));
        }
        NotifyDiffs.Delete(diff);
      }
    }
    
    private DirRX.TeamsCommonAPI.ITeamsNoticesSettings GetSettings(long? eventTypeId)
    {
      var respSetting = _obj.ResponsibleNotices.Where(s => s != null && s.EventType != null & s.EventType.Id == eventTypeId).FirstOrDefault();
      if (respSetting != null)
        return respSetting.LinkedSetting;

      var planSetting = _obj.PlanDateNotices.Where(s => s != null && s.EventType != null & s.EventType.Id == eventTypeId).FirstOrDefault();
      if (planSetting != null)
        return planSetting.LinkedSetting;
      
      var otherSetting = _obj.OtherNotices.Where(s => s != null && s.EventType != null & s.EventType.Id == eventTypeId).FirstOrDefault();
      if (otherSetting != null)
        return otherSetting.LinkedSetting;
      
      return null;
    }
    
    private DirRX.TeamsCommonAPI.ITeamsNoticesSettings ConfigureRecipients(
      DirRX.TeamsCommonAPI.ITeamsNoticesSettings settings,
      DirRX.ProjectPlanner.IProjectActivity activity = null)
    {
      var project = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
      
      foreach (var recipient in settings.Recipients)
      {
        if (recipient.RecipientType == RecipientType.Author)
          recipient.Recipient = _obj.Author != null ? GetRecipientJson(_obj.Author.Id) : null;

        else if (recipient.RecipientType == RecipientType.ProjectLead)
          recipient.Recipient = project?.Manager != null ? GetRecipientJson(project.Manager.Id) : null;

        else if (recipient.RecipientType == RecipientType.ProjectAdmin)
          recipient.Recipient = project?.Administrator != null ? GetRecipientJson(project.Administrator.Id) : null;

        else if (recipient.RecipientType == RecipientType.Participant)
          recipient.Recipient = GetRecipientsByGroup(project, Sungero.Projects.ProjectTeamMembers.Group.Change);

        else if (recipient.RecipientType == RecipientType.Observers)
          recipient.Recipient = GetRecipientsByGroup(project, Sungero.Projects.ProjectTeamMembers.Group.Read);

        else if (recipient.RecipientType == RecipientType.MgmntTeam)
          recipient.Recipient = GetRecipientsByGroup(project, Sungero.Projects.ProjectTeamMembers.Group.Management);

        else if (recipient.RecipientType == RecipientType.CustomerInterna)
          recipient.Recipient = project?.InternalCustomer != null ? GetRecipientJson(project.InternalCustomer.Id) : null;

        else if (recipient.RecipientType == RecipientType.ActivityRep)
          recipient.Recipient = activity?.Responsible != null ? GetRecipientJson(activity.Responsible.Id) : null;

        else if (recipient.RecipientType == RecipientType.RepSection)
          recipient.Recipient = GetSectionRecipient(activity);
      }
      settings.Save();
      return settings;
    }
    
    private string GetRecipientJson(long id)
    {
      return Newtonsoft.Json.JsonConvert.SerializeObject(new List<long>() {id});
    }
    
    private string GetRecipientsByGroup(DirRX.ProjectPlanning.IProject project, Enumeration group)
    {
      if (project?.TeamMembers != null)
      {
        var ids = project.TeamMembers.Where(m => m.Group == group).Select(m => m.Member.Id).ToList();
        if (ids.Count > 0)
        {
          return Newtonsoft.Json.JsonConvert.SerializeObject(ids);
        }
      }
      return null;
    }
    
    private string GetSectionRecipient(DirRX.ProjectPlanner.IProjectActivity activity)
    {
      if (activity == null)
        return null;
      
      var currentActivity = activity;
      //циклически пробегаемся по всем ведущим этапам, ищем первую родительскую секцию (раздел).
      while (currentActivity.TypeActivity != DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section && currentActivity.LeadingActivity != null)
      {
        currentActivity = currentActivity.LeadingActivity;
      }
      
      if (currentActivity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section && currentActivity.Responsible != null)
      {
        return GetRecipientJson(currentActivity.Responsible.Id);
      }
      return null;
    }
    
    private RNDNoticesUtils.Structures.NoticeItem GetNoticeItem(
      DirRX.TeamsCommonAPI.ITeamsNoticesSettings settingItem,
      object projectPlan = null,
      object projectActivity = null,
      DirRX.ProjectPlanner.INotifyDiff diff = null)
    {
      var setting = DirRX.TeamsCommonAPI.TeamsNoticesSettingses.As(settingItem);
      var attachments = new List<Sungero.Domain.Entity>();
      
      Sungero.Domain.Entity mainEntity = null;
      
      string firstTextParam = null;
      string secondaryTextParam = null;
      
      if (projectActivity != null)
      {
        attachments.Add((Sungero.Domain.Entity) projectActivity);
        
        if (mainEntity == null)
        {
          mainEntity = (Sungero.Domain.Entity)projectActivity;
        }
        
        firstTextParam = Hyperlinks.Get((Sungero.Domain.Entity)projectActivity);
      }
      
      if (projectPlan != null)
      {
        attachments.Add((Sungero.Domain.Entity) projectPlan);
        mainEntity = (Sungero.Domain.Entity)projectPlan;
        
        if (firstTextParam == null)
        {
          firstTextParam = Hyperlinks.Get((Sungero.Domain.Entity)projectPlan);
        }
        else
        {
          secondaryTextParam = Hyperlinks.Get((Sungero.Domain.Entity)projectPlan);
        }
      }
      
      var clientWebsite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      
      var noticeItem =  new RNDNoticesUtils.Structures.NoticeItem(setting, attachments, clientWebsite, mainEntity, firstTextParam, secondaryTextParam);
      
      noticeItem.Diffs.Add(new RNDNoticesUtils.Structures.NoticeDiffItem()
                           {
                             newValue = diff.NewValue,
                             oldValue = diff.PreviousValue
                           });
      
      noticeItem.TaskDeadLine = Calendar.Today;
      noticeItem.SolutionGuid = Constants.Module.ProjectPlanGuid;
      
      return noticeItem;
    }
    
    /// <summary>
    /// Удаляет старые настройки уведомлений плана проекта и создает новые с дефолтными значениями,
    /// без сохранения сущности.
    /// </summary>
    /// <param name="projectPlan">План проекта.</param>
    [Remote]
    public static void RecreateDefaultSettingsWithoutSave(IProjectPlanRX projectPlan)
    {
      if (!projectPlan.AccessRights.CanUpdate())
      {
        return;
      }
      
      AccessRights.AllowRead(() =>
                             {
                               projectPlan.OtherNotices.Clear();
                               projectPlan.PlanDateNotices.Clear();
                               projectPlan.ResponsibleNotices.Clear();
                               
                               DirRX.ProjectPlanner.Functions.Module.CreateDefaultNoticeSettings(projectPlan.Id);
                               
                               var settings = DirRX.TeamsCommonAPI.TeamsNoticesSettingses.GetAll(s => s.ConnectedEntityId == projectPlan.Id && s.SolutionIdentifier == DirRX.TeamsCommonAPI.TeamsNoticesSettings.SolutionIdentifier.ProjectPlanner);
                               
                               var respSettings = settings.Where(s => s.EventType.SectionIdentifier == SectionIdentifier.RespAssign);
                               var planSettings = settings.Where(s => s.EventType.SectionIdentifier == SectionIdentifier.PlanDate);
                               var otherSettings = settings.Where(s => s.EventType.SectionIdentifier == SectionIdentifier.Other);
                               
                               foreach (var newSetting in respSettings)
                               {
                                 var newSettingRef = projectPlan.ResponsibleNotices.AddNew();
                                 newSettingRef.LinkedSetting = newSetting;
                               }
                               
                               foreach (var newSetting in planSettings)
                               {
                                 var newSettingRef = projectPlan.PlanDateNotices.AddNew();
                                 newSettingRef.LinkedSetting = newSetting;
                               }
                               
                               foreach (var newSetting in otherSettings)
                               {
                                 var newSettingRef = projectPlan.OtherNotices.AddNew();
                                 newSettingRef.LinkedSetting = newSetting;
                               }
                               
                               projectPlan.EnableReviewAssignmentFlag = true;
                               projectPlan.IsEnableAutoSendingTasks = true;
                             }
                            );
    }
    
    [Remote(IsPure = true)]
    public static IProjectPlanRX GetProjectPlan(long planId)
    {
      return DirRX.ProjectPlanner.ProjectPlanRXes.Get(planId);
    }
    
    /// <summary>
    /// Возвращает информацию о блокировке проекта.
    /// </summary>
    /// <returns>Информация о блокировке проекта.</returns>
    /// <remarks>
    /// Если проект заблокирован, то возвращается информация о блокировке.
    /// Если же не заблокирован, то пустая строка.
    /// </remarks>
    [Remote(IsPure = true)]
    public string GetLockInfo()
    {
      var lockInfo = Locks.GetLockInfo(_obj);
      return lockInfo.IsLockedByOther && !string.IsNullOrEmpty(lockInfo.LockedMessage) ? lockInfo.LockedMessage.ToString() : string.Empty;
    }
    
    /// <summary>
    /// Копирует этапы из одного проекта в другой.
    /// </summary>
    /// <param name="sourceProject">Исходный проект.</param>
    /// <param name="targetProject">Проект, в который копируются этапы.</param>
    /// <param name="newDate">Новая дата начала.</param>
    [Remote]
    public static void CopyActivitiesFormSourceProjectToTargetProject(IProjectPlanRX sourceProject, IProjectPlanRX targetProject, DateTime newDate)
    {
      // Разница времени.
      TimeSpan diff = newDate - sourceProject.StartDate.Value;
      // Соответствие между этапами исходного проекта и этапами проекта, в который копируются этапы.
      var idActivities = new List<Structures.ProjectPlanRX.IDActivities>();
      // Скопировать этапы.
      var sourceActivities = ProjectPlanner.Functions.ProjectActivity.GetActivities(sourceProject);
      // Список скопированных этапов.
      var targetActivities = new List<ProjectPlanner.IProjectActivity>();
      foreach (var activity in sourceActivities)
      {
        var newActivity = ProjectPlanner.ProjectActivities.Copy(activity);
        // Кеш соответствия ID.
        idActivities.Add(Structures.ProjectPlanRX.IDActivities.Create(activity.Id, newActivity));
        newActivity.StartDate = activity.StartDate.Value + diff;
        newActivity.EndDate = activity.EndDate.Value + diff;
        newActivity.ProjectPlan = targetProject;
        // Очистить ведущий этап и проценты выполнения.
        newActivity.LeadingActivity = null;
        newActivity.ExecutionPercent = null;
        // Очистить предшественников.
        newActivity.Predecessors.Clear();
        newActivity.Save();
        targetActivities.Add(newActivity);
      }
      foreach (var sourceActivity in sourceActivities)
      {
        var targetActivity = idActivities.First(x => x.IDSource == sourceActivity.Id).TargetActitvity;
        var isChanged = false;
        // Заполнить ведущий этап для скопированных этапов.
        if (sourceActivity.LeadingActivity != null)
        {
          targetActivity.LeadingActivity = idActivities.First(y => y.IDSource == sourceActivity.LeadingActivity.Id).TargetActitvity;
          isChanged = true;
        }
        // Заполнить предшественников для скопированных этапов.
        foreach (var predecessor in sourceActivity.Predecessors)
        {
          var newPredecessor = targetActivity.Predecessors.AddNew();
          newPredecessor.Activity = idActivities.First(y => y.IDSource == predecessor.Activity.Id).TargetActitvity;
          isChanged = true;
        }
        if (isChanged)
          targetActivity.Save();
      }
    }

    
    /// <summary>
    /// Создать план проекта.
    /// </summary>
    /// <returns>План проекта.</returns>
    [Public, Remote]
    public static IProjectPlanRX CreateProjectPlan()
    {
      return DirRX.ProjectPlanner.ProjectPlanRXes.Create();
    }

    [Remote]
    public static void SendPlanCardClosedMessage(IProjectPlanRX projectPlan)
    {
      var currentClientIds = ClientManager.Instance.GetClientsOfUser(Sungero.CoreEntities.Users.Current.Id);
      PplanMessages.PplanMessageSender.SendProjectPlanCardClosed(currentClientIds, "gantt", projectPlan.Id);
    }

    /// <summary>
    /// Найти последний утвержденый номер версии документа.
    /// </summary>
    /// <returns>Номер версии.</returns>
    /// <remarks>Если у документа отсутствуют подписанные версии, то возвращается Null</remarks>
    [Public]
    public int? GetLatestSignedVersionNumber()
    {
      if (!_obj.HasVersions)
        return null;
      
      if (_obj.LastVersionApproved == true)
        return _obj.LastVersion.Number;
      
      var versions = _obj.Versions.OrderByDescending(v => v.Number);
      foreach(var version in versions)
      {
        if (Signatures.Get(version).Any(s => s.SignatureType == SignatureType.Approval))
          return version.Number;
      }
      
      return null;
    }

    [Remote(IsPure = true)]
    public DirRX.ProjectPlanning.IProject GetLinkedProject()
    {
      return DirRX.ProjectPlanning.Projects.GetAll(x => x.ProjectPlanDirRX != null && _obj.Id == x.ProjectPlanDirRX.Id).FirstOrDefault();
    }
    
    [Remote(IsPure = true)]
    public static bool CanUpdateLinkedProject(DirRX.ProjectPlanning.IProject linkedProject)
    {
      if (linkedProject == null)
      {
        return false;
      }
      if (!linkedProject.AccessRights.CanUpdate())
      {
        Logger.Error(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.NotEnoughRightsToUpdateProjectFormat(linkedProject.Name));
        return false;
      }
      if (Locks.GetLockInfo(linkedProject).IsLockedByOther)
      {
        Logger.Error(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectIsLockedByOtherFormat(linkedProject.Name));
        return false;
      }
      return true;
    }
    
    /// <summary>
    /// Прочитать содержимое версии плана.
    /// </summary>
    /// <param name="planId">ID плана.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Json строку тела документа.</returns>
    public static string ReadPlanBody(long planId, int? versionNumber)
    {
      var plan = ProjectPlanRXes.Get(planId);
      return ReadPlanBody(plan, versionNumber);
    }
    
    /// <summary>
    /// Прочитать содержимое версии плана.
    /// </summary>
    /// <param name="plan">План проекта.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Json строку тела документа.</returns>
    public static string ReadPlanBody(IProjectPlanRX plan, int? versionNumber)
    {
      var docVersion = plan.Versions.First(v => v.Number == versionNumber);
      var docBody = string.Empty;
      using (var reader = new System.IO.StreamReader(docVersion.Body.Read()))
      {
        docBody = reader.ReadToEnd();
      }
      return docBody;
    }
    
    /// <summary>
    /// Получить десериализованное тело плана проекта.
    /// </summary>
    /// <param name="planId">ID плана.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Модель плана проекта.</returns>
    public static DirRX.Planner.Model.IModel GetPlanModel(long planId, int? versionNumber)
    {
      return Newtonsoft.Json.JsonConvert.DeserializeObject<DirRX.Planner.Model.Model>(ReadPlanBody(planId, versionNumber));
    }
    
    /// <summary>
    /// Получить десериализованное тело плана проекта.
    /// </summary>
    /// <param name="planId">План проекта.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Модель плана проекта.</returns>
    public static DirRX.Planner.Model.IModel GetPlanModel(IProjectPlanRX plan, int? versionNumber)
    {
      return Newtonsoft.Json.JsonConvert.DeserializeObject<DirRX.Planner.Model.Model>(ReadPlanBody(plan, versionNumber));
    }
    
    /// <summary>
    /// Получить актуальную модель плана, дозаполненную фактическими значениями из бд.
    /// </summary>
    /// <param name="planId">ID плана.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Модель плана проекта.</returns>
    public static DirRX.Planner.Model.IModel GetPlanModelForGantt(long planId, int versionNumber)
    {
      var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;
      var plan = ProjectPlanRXes.Get(planId);
      var model = GetPlanModel(plan, versionNumber);
      
      // Обновить данные этапов.
      var dynamicActivityData = ProjectActivities.GetAll(a => a.ProjectPlan.Id == planId && a.NumberVersion == versionNumber)
        .Select(a => Structures.ProjectActivity.DynamicActivityData.Create(
          a.Id,
          a.ActualWorkload,
          a.FactualCosts,
          a.ExecutionPercent,
          a.Status
         ))
        .ToDictionary(a => (long?)a.Id);
      
      foreach (var modelActivity in model.Activities)
      {
        var dbActivity = dynamicActivityData[modelActivity.Id];
        modelActivity.ActualWorkload = dbActivity.ActualWorkload;
        modelActivity.FactualCosts = dbActivity.FactualCosts;
        modelActivity.ExecutionPercent = dbActivity.ExecutionPercent;
        modelActivity.Status = new DirRX.Planner.Model.ActivityStatus()
        {
          EnumValue = dbActivity.Status.Value.Value,
          LocalizeValue = ProjectActivity.StatusItems.GetLocalizedValue(dbActivity.Status, cultureInfo)
        };
      }
      
      // Обновить данные плана.
      model.Project.Name = plan.Name;
      model.Project.Note = plan.Note;
      model.Project.ActualWorkload = plan.ActualWorkload;
      model.Project.FactualCosts = plan.FactualCosts;
      model.Project.ExecutionPercent = plan.ExecutionPercent;
      model.Project.LifeCycleState = plan.LifeCycleState?.Value;
      
      return model;
    }
    
    /// <summary>
    /// Получить фактические значения плана проекта по ИД.
    /// </summary>
    /// <param name="projectPlanIds">ИД планов.</param>
    /// <returns>Словарь ИД: коллекция значений.</returns>
    /// <remarks>Тяжелый запрос, рекомендуется использовать кэшированную версию.</remarks>
    public static System.Collections.Generic.IDictionary<long, Structures.ProjectPlanRX.IPlanCalculatedData> GetCalculatedPlanData(System.Collections.Generic.IEnumerable<long> projectPlanIds)
    {
      var result = new Dictionary<long, Structures.ProjectPlanRX.IPlanCalculatedData>();
      
      if (projectPlanIds == null || !projectPlanIds.Any())
      {
        return result;
      }
      
      using (var connection = SQL.CreateConnection())
        using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetPlanCalculatedData;
        SQL.AddArrayParameter(command, "@projectPlanIds", projectPlanIds, System.Data.DbType.Int64);
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var data = Structures.ProjectPlanRX.PlanCalculatedData.Create();
            data.PlanId = (long)reader[0];
            data.ExecutionPercent = Int32.Parse(reader[1].ToString());
            data.PlanCosts = (double?)reader[2];
            data.FactCosts = (double?)reader[3];
            data.PlanWorkload = (double?)reader[4];
            data.FactWorkload = (double?)reader[5];
            data.PlanStartDate = (DateTime?)reader[6];
            data.FactStartDate = (DateTime?)reader[7];
            data.PlanEndDate = (DateTime?)reader[8];
            data.FactEndDate = (DateTime?)reader[9];
            result[data.PlanId] = data;
          }
        }
      }
      return result;
    }
    
    /// <summary>
    /// Получить фактические значения плана проекта по ИД.
    /// </summary>
    /// <param name="projectPlanIds">ИД планов.</param>
    /// <returns>Словарь ИД: коллекция значений.</returns>
    /// <remarks>Кэшированный запрос. Валидация кэша раз в 1 минуту.</remarks>
    [Public]
    public static System.Collections.Generic.IDictionary<long, Structures.ProjectPlanRX.IPlanCalculatedData> GetCalculatedPlanDataCached(System.Collections.Generic.IEnumerable<long> projectPlanIds)
    {
      var result = new Dictionary<long, Structures.ProjectPlanRX.IPlanCalculatedData>();
      var newOrExpiredIds = new List<long>();
      
      foreach (var id in projectPlanIds)
      {
        Structures.ProjectPlanRX.PlanCalculatedData data;
        var key = string.Format(Constants.ProjectPlanRX.DynamicDataCacheKeyFormat, id);
        
        if (Cache.TryGetValue(key, out data))
        {
          result[id] = data;
        }
        else
        {
          newOrExpiredIds.Add(id);
        }
      }
      
      var calculatedData = GetCalculatedPlanData(newOrExpiredIds);
      
      foreach (var id in newOrExpiredIds)
      {
        Structures.ProjectPlanRX.IPlanCalculatedData data;
        
        if (calculatedData.TryGetValue(id, out data))
        {
          var key = string.Format(Constants.ProjectPlanRX.DynamicDataCacheKeyFormat, id);
          Cache.AddOrUpdate(key, data, Calendar.Now.AddMinutes(Constants.ProjectPlanRX.DynamicDataCacheMinutes));
        }
        else
        {
          data = Structures.ProjectPlanRX.PlanCalculatedData.Create();
        }
        
        result[id] = data;
      }
      
      return result;
    }
  }
}
