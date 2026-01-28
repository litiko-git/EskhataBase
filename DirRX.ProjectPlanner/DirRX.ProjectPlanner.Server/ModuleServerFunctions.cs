using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.Company;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner;
using TypeData = DirRX.ProjectPlanner.Constants.Module.TypeData;
using DateScale = DirRX.ProjectPlanner.Constants.Module.DateScale;
using DirRX.ProjectPlanning;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.NotifyConditionItem;
using DirRX.TeamsCommonAPI.NotifyEventType;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Получение информации об объектах управления в виде иерархии.
    /// </summary>
    /// <param name="projectCoreId">Идентификаторы объектов управления.</param>
    /// <returns>Информация об объектах управления в виде иерархии</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string GetSubProjectsInfo(List<long> projectCoreIds)
    {
      if (!DirRX.PortfolioProgram.PublicFunctions.Module.PortfolioProgramModuleHasLicense())
      {
        throw new Exception(DirRX.PortfolioProgram.Resources.RoadmapIsNotAvailableWitoutPortfolioProgramLicense);
      }
      
      var hierarchy = CreateDefaultHierarchy();

      if (projectCoreIds == null || !projectCoreIds.Any())
      {
        var accessableProjects = ProjectCores.GetAll().Select(p => new { Id = p.Id, ParentId = p.LeadingProject != null ? (long?)p.LeadingProject.Id : null }).ToList();
        // Верхние элементы структур, в случае если родитель есть, но на него нет прав
        var highLevelProjects = accessableProjects.Where(t => t.ParentId != null).Select(t => t.ParentId.Value).Except(accessableProjects.Select(t => t.Id));
        var rootElements = accessableProjects.Where(t => t.ParentId == null).Select(t => t.Id);  // Корневые элементы, у которых нет родителя
        projectCoreIds = rootElements.Concat(highLevelProjects).ToList();
      }
      else
      {
        if (projectCoreIds.Count != 1)
        {
          throw new NotImplementedException("Для GetSubProjectsInfo не реализована обработка нескольих projectCoreIds.");
        }
        
        var singleProjectCoreId = projectCoreIds.Single();
        hierarchy.Title = GetRoadMapTitleByProjectType(ProjectCores.Get(singleProjectCoreId));
      }

      var planIdProjectIdPairs = new Dictionary<long, long>();
      FillProjectHierarchy(hierarchy, projectCoreIds, planIdProjectIdPairs);
      
      FillOuterGateIds(hierarchy, planIdProjectIdPairs);
      var milestones = FillProjectMilestones(hierarchy, planIdProjectIdPairs);
      if (projectCoreIds.Count == 1)  // строим для конкретного ОУ
      {
        var outerMilestones = milestones.Where(m => !planIdProjectIdPairs.Keys.Contains(m.ProjectPlan.Id));  // Вехи проектов, которые не входят в структуру.
        FillOuterProjects(hierarchy, outerMilestones);
      }
      FillGates(hierarchy);
      FillLevelStatuses(hierarchy);
      return Newtonsoft.Json.JsonConvert.SerializeObject(hierarchy);
    }

    private static DirRX.Planner.Model.ProjectHierarchy CreateDefaultHierarchy()
    {
      var projectHierarchy = new DirRX.Planner.Model.ProjectHierarchy();
      projectHierarchy.Title = DirRX.ProjectPlanner.Resources.DefaultRoadMapTitle;
      projectHierarchy.Hierarchy = new List<DirRX.Planner.Model.HierarchyItem>();
      projectHierarchy.ProjectCores = new Dictionary<long, DirRX.Planner.Model.ProjectCoreDto>();
      return projectHierarchy;
    }

    private static string GetRoadMapTitleByProjectType(IProjectCore projectCore)
    {
      var title = string.Empty;
      if (PortfolioProgram.Portfolios.Is(projectCore))
      {
        title = DirRX.ProjectPlanner.Resources.RoadMapPortfolioTitleFormat(projectCore.DisplayValue);
      }
      
      if (PortfolioProgram.Programs.Is(projectCore))
      {
        title = DirRX.ProjectPlanner.Resources.RoadMapProgramTitleFormat(projectCore.DisplayValue);
      }
      
      if (Projects.Is(projectCore))
      {
        title = DirRX.ProjectPlanner.Resources.RoadMapProjectTitleFormat(projectCore.DisplayValue);
      }
      
      return title;
    }
    
    private static void FillProjectHierarchy(DirRX.Planner.Model.ProjectHierarchy hierarchy, IEnumerable<long> projectCoreIds, Dictionary<long, long> planIdProjectIdPairs)
    {
      var allSubProjectCoreIds = DirRX.ProjectPlanning.Module.Projects.PublicFunctions.Module.GetAllChildProjectIds(projectCoreIds);
      var accessibleProjectCoreId = new HashSet<long>(ProjectCores.GetAll(p => allSubProjectCoreIds.Contains(p.Id)).Select(p => p.Id));
      List<Structures.Module.IRoadmapProjectDatabaseItem> subProjectCores = null;
      
      AccessRights.AllowRead(() =>
      {
         subProjectCores = ProjectCores.GetAll(p => allSubProjectCoreIds.Contains(p.Id))
           .Select(p => (Structures.Module.IRoadmapProjectDatabaseItem)
                   new Structures.Module.RoadmapProjectDatabaseItem {
                     Id = p.Id,
                     ParentId = p.LeadingProject != null ? (long?)p.LeadingProject.Id : null,
                     Project = p,
                     ManagerId = p.Manager != null ? (long?)p.Manager.Id : null,
                     ManagerName = p.Manager != null ? p.Manager.Name : null,
                   })
           .ToList();
       });

      foreach(var subProjectCore in subProjectCores)
      {
        var subProjectCoreAsProject = Projects.As(subProjectCore.Project);
        bool isProject = subProjectCoreAsProject != null;
        var subProjectCoreDto = CreateProjectCoreDto(subProjectCore, true, accessibleProjectCoreId, isProject);
        hierarchy.ProjectCores[subProjectCore.Id] = subProjectCoreDto;
        
        //Kiselev_EM Если проект - запоминаем его план проекта.
        if (isProject && subProjectCoreAsProject.ProjectPlanDirRX != null)
        {
          planIdProjectIdPairs[subProjectCoreAsProject.ProjectPlanDirRX.Id] = subProjectCore.Id;
        }
      }

      foreach (var rootId in projectCoreIds)
      {
        int maxDepth;
        var projectCoreHierarchy = GetChildHierarchyItem(subProjectCores, rootId, 1, out maxDepth);
        hierarchy.Hierarchy.Add(projectCoreHierarchy);
      }
      hierarchy.Hierarchy = hierarchy.Hierarchy.OrderByDescending(h => h.Depth).ToList();
      ClearHierarchyDuplicates(hierarchy);
    }
    
    private static void ClearHierarchyDuplicates(DirRX.Planner.Model.ProjectHierarchy hierarchy)
    {
      var uniqueIds = new HashSet<long>();
      for (var i = 0; i < hierarchy.Hierarchy.Count; ++i)
      {
        hierarchy.Hierarchy[i] = ClearHierarchyDuplicatesReq(hierarchy.Hierarchy[i], uniqueIds);
      }
      hierarchy.Hierarchy = hierarchy.Hierarchy.Where(h => h != null).ToList();
    }
    
    private static DirRX.Planner.Model.HierarchyItem ClearHierarchyDuplicatesReq(DirRX.Planner.Model.HierarchyItem item, HashSet<long> uniqueIds)
    {
      if (uniqueIds.Contains(item.Id))
      {
        return null;
      }
      else
      {
        uniqueIds.Add(item.Id);
      }
      for (var i = 0; i < item.ChildItems.Count; ++i)
      {
        item.ChildItems[i] = ClearHierarchyDuplicatesReq(item.ChildItems[i], uniqueIds);
      }
      item.ChildItems = item.ChildItems.Where(c => c != null).ToList();
      return item;
    }
    
    /// <summary>
    /// Наполняет иерархию информацией о вехах
    /// </summary>
    /// <param name="projectHierarchy">Иерархия</param>
    /// <param name="planIdProjectIdPairs">Словарь сопоставляющий id плана с id проекта</param>
    /// <returns>Список вех, которые связаны с КТ иерархии</returns>
    private static IEnumerable<IProjectActivity> FillProjectMilestones(DirRX.Planner.Model.ProjectHierarchy hierarchy, Dictionary<long, long> planIdProjectIdPairs)
    {
      var allHierarchyGateIds = hierarchy.ProjectCores.Where(p => p.Value.GateIds != null && p.Value.GateIds.Count > 0)
        .SelectMany(x => x.Value.GateIds)
        .Concat(hierarchy.OtherHierarchy.GateIds);
      
      var milestones = Functions.ProjectActivity.GetMilestonesForGates(allHierarchyGateIds);
      FillMilestones(hierarchy, milestones);
      
      var projectMilestones = milestones.GroupBy(x => GetProjectIdByPlanId(x.ProjectPlan.Id, planIdProjectIdPairs)).ToDictionary(x => x.Key);

      foreach (var project in hierarchy.ProjectCores)
      {
        if (projectMilestones.ContainsKey(project.Key))
        {
          project.Value.MilestoneIds = projectMilestones[project.Key].Select(m => m.Id).ToList();
        }
      }
      return milestones;
    }
    
    /// <summary>
    /// Заполнить в структуре данные о "Прочих" проектах.
    /// </summary>
    /// <param name="projectHierarchy">Текущая структура.</param>
    /// <param name="milestones">Вехи проектов, которые не входят в структуру.</param>
    private static void FillOuterProjects(DirRX.Planner.Model.ProjectHierarchy hierarchy, IEnumerable<IProjectActivity> milestones)
    {
      var planMilestones = milestones.GroupBy(m => m.ProjectPlan.Id).ToDictionary(g => g.Key);
      var outerProjects = Projects.GetAll(p => p.ProjectPlanDirRX != null && planMilestones.Keys.Contains(p.ProjectPlanDirRX.Id))
        .Select(p => 
                   new Structures.Module.RoadmapProjectDatabaseItem {
                     Id = p.Id,
                     ParentId = p.LeadingProject != null ? (long?)p.LeadingProject.Id : null,
                     Project = p,
                     ManagerId = p.Manager != null ? (long?)p.Manager.Id : null,
                     ManagerName = p.Manager != null ? p.Manager.Name : null,
                     ProjectPlanId = (long?)p.ProjectPlanDirRX.Id,
                   });
      foreach (var project in outerProjects)
      {
        hierarchy.OtherHierarchy.ProjectIds.Add(project.Id);
        var projectDto = CreateProjectCoreDto(project, false, null, true);
        projectDto.MilestoneIds = planMilestones[project.ProjectPlanId.Value].Select(m => m.Id).ToList();
        hierarchy.ProjectCores[projectDto.Id] = projectDto;
      }
      
      var outerProjectCoresForGates = ProjectCores.GetAll(p => p.GatesDirRX.Any(g => hierarchy.OtherHierarchy.GateIds.Contains(g.Gate.Id)))
        .Select(p => 
                   new Structures.Module.RoadmapProjectDatabaseItem {
                     Id = p.Id,
                     ParentId = p.LeadingProject != null ? (long?)p.LeadingProject.Id : null,
                     Project = p,
                     ManagerId = p.Manager != null ? (long?)p.Manager.Id : null,
                     ManagerName = p.Manager != null ? p.Manager.Name : null,
                   });
      foreach (var project in outerProjectCoresForGates)
      {
        bool isProject = Projects.Is(project.Project);
        var projectDto = CreateProjectCoreDto(project, false, null, isProject);
        hierarchy.ProjectCores[projectDto.Id] = projectDto;
      }
    }
      
    /// <summary>
    /// Выбор КТ вне иерархии, но на которые ссылаются вехи внутри иерархии
    /// </summary>
    private static void FillOuterGateIds(DirRX.Planner.Model.ProjectHierarchy hierarchy, Dictionary<long, long> planIdProjectIdPairs)
    {
      var allHierarchyGateIds = hierarchy.ProjectCores.Values.Where(p => p.GateIds != null && p.GateIds.Count > 0).SelectMany(x => x.GateIds);
      var gateIds = Functions.ProjectActivity.GetOuterMilestonesForGates(allHierarchyGateIds, planIdProjectIdPairs.Keys).Select(m => m.GateId.Value);
      hierarchy.OtherHierarchy.GateIds = gateIds.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить значение по ключу из словаря
    /// </summary>
    /// <param name="planId">Ключ</param>
    /// <param name="planIdProjectIdPairs">Словарь</param>
    /// <returns>Значение по ключу. Если не найден, то -1</returns>
    private static long GetProjectIdByPlanId(long planId, Dictionary<long, long> planIdProjectIdPairs)
    {
      if (planIdProjectIdPairs.ContainsKey(planId))
      {
        return planIdProjectIdPairs[planId];
      }
      return -1;
    }
    
    private static void FillLevelStatuses(DirRX.Planner.Model.ProjectHierarchy projectHierarchy)
    {
      var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;
      
      foreach (var enumValue in DirRX.ProjectPlanner.Server.Gate.LevelItems.ItemsMergedWithDescendants)
      {
        projectHierarchy.LevelStatuses.Add(new DirRX.Planner.Model.LevelStatus(
          enumValue.Value,
          DirRX.ProjectPlanner.Server.Gate.LevelItems.GetLocalizedValue(enumValue, cultureInfo)
         ));
      }
    }

    /// <summary>
    /// Возвращает номер последней версии плана проекта подписанной утверждающей подписью. Если версий с утверждающей
    /// подписью нет, вернет номер последней версии. Согласующая подпись не учитывается.
    /// </summary>
    /// <param name="projectPlanId">ИД плана проекта.</param>
    /// <returns>Номер версии.</returns>
    [Remote]
    public int GetActualProjectPlanVersionNumber(long projectPlanId)
    {
      int actualProjectPlanVersionNumber = 0;
      using (var connection = CreateDBConnection())
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetActualProjectPlanVersionNumber;
        SQL.AddParameter(command, "@projectPlanId", projectPlanId, System.Data.DbType.Int64);
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            actualProjectPlanVersionNumber = (int)reader[0];
          }
        }
      }
      
      return actualProjectPlanVersionNumber;
    }
    
    /// <summary>
    /// Получить ссылку на тикет в отчете.
    /// </summary>
    /// <param name="boardId">Ид доски.</param>
    /// <param name="ticketId">Ид тикета.</param>
    /// <returns>Ссылка на тикет.</returns>
    [Public]
    public string GetLinkTicket(long boardId, long ticketId)
    {
      string result = null;
      
      try
      {
        var getter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
        var uriAgile = getter.GetContentFullAddress(new Uri(Constants.Module.AgileClientAddress, UriKind.Relative));
        var builder = new UriBuilder(uriAgile)
        {
          Query = string.Format("boardId={0}&ticketId={1}", boardId, ticketId)
        };
        
        result = builder.Uri.ToString();
      }
      catch(System.UriFormatException ex)
      {
        Logger.Error(Resources.ErrorUriTicketFormat(ticketId, boardId), ex);
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить ссылку на этап в отчете.
    /// </summary>
    /// <param name="planId">Ид плана.</param>
    /// <param name="activityId">Ид этапа.</param>
    /// <param name="activityId">Номер версии плана.</param>
    /// <returns>Ссылка на этап.</returns>
    [Public]
    public string GetLinkActivity(long planId, long activityId, int numberVersion)
    {
      string result = null;
      
      try
      {
        var webSite = Functions.Module.GetWebSite();
        var getter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
        var uriProjectPlan = getter.GetContentFullAddress(new Uri(Constants.Module.DefaultClientAddress, UriKind.Relative));
        var builder = new UriBuilder(uriProjectPlan)
        {
          Query = string.Format("projectId={0}&readonly={1}&numberVersion={2}&activityId={3}", planId, true, numberVersion, activityId)
        };
        
        result = builder.Uri.ToString();
      }
      catch(System.UriFormatException ex)
      {
        Logger.Error(Resources.ErrorUriActivityFormat(activityId, planId), ex);
      }
      
      return result;
    }
    

    private static void AddNewRecipientTypeToSettings(ITeamsNoticesSettings rule, Enumeration recipientType, INotifyConditionItem notifyCondition)
    {
       var newRecipient = rule.Recipients.AddNew();
       newRecipient.RecipientType = recipientType;
       newRecipient.NotifyCondition = notifyCondition;
    }
    
    /// <summary>
    /// Создать правило уведомлений по-умолчанию.
    /// </summary>
    /// <param name="eventType">Тип уведомления.</param>
    /// <param name="name">Отображаемое имя уведомления.</param>
    /// <param name="projectLead">Тип уведомления для руководителя проекта/плана.</param>
    /// <param name="repSection">Тип уведомления для ответственного за раздел.</param>
    /// <param name="activityRep">Тип уведомления для ответственного за этап/веху.</param>
    /// <param name="projectAdmin">Тип уведомления для админа проекта.</param>
    /// <param name="customerInternal">Тип уведомления для внутреннего заказчика.</param>
    /// <param name="participant">Тип уведомления для участников проекта.</param>
    /// <param name="mgmntTeam">Тип уведомления для команды управления проекта.</param>
    /// <param name="observers">Тип уведомления для наблюдателей.</param>
    private static void SetDefaultRule(long entityId, INotifyEventType type, string name, INotifyConditionItem projectLead, INotifyConditionItem repSection, INotifyConditionItem activityRep, INotifyConditionItem projectAdmin, INotifyConditionItem customerInternal, INotifyConditionItem participant, INotifyConditionItem mgmntTeam, INotifyConditionItem observers)
    {
      ITeamsNoticesSettings settings = null;
      
      var existingRule = TeamsNoticesSettingses.GetAll(s => s.ConnectedEntityId == entityId && s.EventType == type).FirstOrDefault();
      
      if (existingRule != null)
      {
        settings = existingRule;
      }
      else
      {
        settings = TeamsNoticesSettingses.Create();
      }
      
      settings.Recipients.Clear();
      
      settings.Name = name;
     
      AddNewRecipientTypeToSettings(settings, RecipientType.ProjectLead, projectLead);
      AddNewRecipientTypeToSettings(settings, RecipientType.RepSection, repSection);
      AddNewRecipientTypeToSettings(settings, RecipientType.ActivityRep, activityRep);
      AddNewRecipientTypeToSettings(settings, RecipientType.ProjectAdmin, projectAdmin);
      AddNewRecipientTypeToSettings(settings, RecipientType.CustomerInterna, customerInternal);
      AddNewRecipientTypeToSettings(settings, RecipientType.Participant, participant);
      AddNewRecipientTypeToSettings(settings, RecipientType.MgmntTeam, mgmntTeam);
      AddNewRecipientTypeToSettings(settings, RecipientType.Observers, observers);
      settings.EventType = type;
      settings.ConnectedEntityId = entityId;
      settings.SolutionIdentifier = DirRX.TeamsCommonAPI.TeamsNoticesSettings.SolutionIdentifier.ProjectPlanner;
      settings.Save();
    }
    
    public static void CreateDefaultNoticeSettings(long entityId)
    {
      var existsSettings = TeamsNoticesSettingses.GetAll(s => s.ConnectedEntityId == entityId).ToList();
      
      var notifyConditions = NotifyConditionItems.GetAll().ToList();
      var never = notifyConditions.Where(c => c.Condition == Condition.Never).FirstOrDefault();
      var standart = notifyConditions.Where(c => c.Condition == Condition.StandartNotice).FirstOrDefault();
      var task = notifyConditions.Where(c => c.Condition == Condition.Task).FirstOrDefault();
      var early = notifyConditions.Where(c => c.Condition == Condition.Early).FirstOrDefault();
      var collective = notifyConditions.Where(c => c.Condition == Condition.Consolidated).FirstOrDefault();

      
      //Перевызов не страшен, так как идут проверки, что условия еще не созданы
      //Однако этот вызов гарантирует, что все нужные справочники будут созданы при нарушении порядка инициализации
      DirRX.TeamsCommonAPI.PublicInitializationFunctions.Module.CreateDefaultEventTypes();
      var eventTypes = NotifyEventTypes.GetAll().ToList();
      
      #region Уведомления о правах доступа и зонах ответственности
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.PlanRespAssign),
        DirRX.ProjectPlanner.Resources.PplanRespAssigned,
        projectLead:collective,
        repSection:never,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.SectRespAssign),
        DirRX.ProjectPlanner.Resources.PplanSectionRespAssigned,
        projectLead:never,
        repSection:collective,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActRespAssign),
        DirRX.ProjectPlanner.Resources.PplanActivityRespAssigned,
        projectLead:never,
        repSection:never,
        activityRep:collective,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.MilesRespAssign),
        DirRX.ProjectPlanner.Resources.PplanMilestoneRespAssigned,
        projectLead:never,
        repSection:never,
        activityRep:collective,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.PlanParticipAdd),
        DirRX.ProjectPlanner.Resources.PlanParticipAdd,
        projectLead:never,
        repSection:never,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      #endregion
      #region Уведомления по плану проекта
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.PlanMustStarted),
        DirRX.ProjectPlanner.Resources.PlanMustStarted,
        projectLead:collective,
        repSection:never,
        activityRep:never,
        projectAdmin:collective,
        customerInternal:collective,
        participant:never,
        mgmntTeam:collective,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.PlanMustEnded),
        DirRX.ProjectPlanner.Resources.PlanMustEnded,
        projectLead:collective,
        repSection:never,
        activityRep:never,
        projectAdmin:collective,
        customerInternal:collective,
        participant:never,
        mgmntTeam:collective,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.PlanOverdated),
        DirRX.ProjectPlanner.Resources.PlanOverdated,
        projectLead:standart,
        repSection:never,
        activityRep:never,
        projectAdmin:standart,
        customerInternal:standart,
        participant:never,
        mgmntTeam:standart,
        observers:never
       );
      
      #endregion
      #region Уведомления по разделам плана проекта
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.SectMustStarted),
        DirRX.ProjectPlanner.Resources.SectMustStarted,
        projectLead:collective,
        repSection:collective,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.SectMustEnded),
        DirRX.ProjectPlanner.Resources.SectMustEnded,
        projectLead:collective,
        repSection:collective,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.SectOverdated),
        DirRX.ProjectPlanner.Resources.SectOverdated,
        projectLead:standart,
        repSection:standart,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      #endregion
      #region Уведомления по этапам и вехам плана проекта
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActMustStarted),
        "Этап плана проекта должен быть начат",
        projectLead:collective,
        repSection:collective,
        activityRep:collective,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActCannotStart),
        DirRX.ProjectPlanner.Resources.ActCannotStart,
        projectLead:collective,
        repSection:collective,
        activityRep:collective,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActMustEnded),
        DirRX.ProjectPlanner.Resources.ActMustEnded,
        projectLead:early,
        repSection:early,
        activityRep:early,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActOverdated),
        DirRX.ProjectPlanner.Resources.ActOverdated,
        projectLead:standart,
        repSection:standart,
        activityRep:standart,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.MilestMustReach),
        DirRX.ProjectPlanner.Resources.MilestMustReach,
        projectLead:early,
        repSection:early,
        activityRep:early,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.MilestOverdated),
        DirRX.ProjectPlanner.Resources.MilestOverdated,
        projectLead:standart,
        repSection:standart,
        activityRep:standart,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.ActStatusChangd),
        DirRX.ProjectPlanner.Resources.ActStatusChangd,
        projectLead:collective,
        repSection:collective,
        activityRep:never,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      SetDefaultRule(
        entityId,
        eventTypes.FirstOrDefault(t => t.EventType == EventType.StartedActChd),
        DirRX.ProjectPlanner.Resources.StartedActChd,
        projectLead:never,
        repSection:collective,
        activityRep:collective,
        projectAdmin:never,
        customerInternal:never,
        participant:never,
        mgmntTeam:never,
        observers:never
       );
      
      #endregion
    }
    

    public void UpdateLeadActivity(DirRX.ProjectPlanner.Structures.Module.IActivityDto sourceActivity,
                                   List<DirRX.ProjectPlanner.Structures.Module.IdActivityMapper> updatedActivityIds,
      long lastActivityId)
    {
      if (sourceActivity.LeadActivityId.HasValue && sourceActivity.LeadActivityId.Value > lastActivityId)
      {
        var leadActivityId = updatedActivityIds.FirstOrDefault(x => x.ModelActivityId == sourceActivity.LeadActivityId.Value);
        if (leadActivityId == null)
        {
          throw new Exception(DirRX.ProjectPlanner.Resources.FailToFindUpdatedIdExceptionTextFormat(sourceActivity.LeadActivityId.Value));
        }
        
        sourceActivity.LeadActivityId = new Nullable<long>(leadActivityId.Id);
      }
    }
    
    /// <summary>
    /// Проверяет успешное прохождение инициализации.
    /// </summary>
    /// <returns>Ссылка на веб-клиент.</returns>
    /// <exception cref="Exception">Если проверка не пройдена.</exception>
    [Public(WebApiRequestType = RequestType.Get)]
    public string Check()
    {
      try
      {
        this.ThrowExceptionIfResourceLinksTableNotExist();
        
        return DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      }
      catch (Exception ex)
      {
        throw new Exception(Resources.HealthCheckExceptionText, ex);
      }
    }
    
    private void ThrowExceptionIfResourceLinksTableNotExist()
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.CheckResourceLinksTable;
        if ((int)command.ExecuteScalar() == 0)
        {
          throw new Exception(Resources.ResourceLinksTableNotFoundExceptionText);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public static Structures.Module.IProjectPlanDto UpdatePlanData(long projectPlanId, int numberVersion)
    {
      try
      {
        var projectPlanDto = new Structures.Module.ProjectPlanDto();
        
        AccessRights.AllowRead(() =>
          {
            projectPlanDto.ResourceData = HandleUpdateResourcesDataDto(projectPlanId, numberVersion);
            projectPlanDto.Tasks = GetRxTasks(projectPlanId, numberVersion);
          });
        
        projectPlanDto.ActivityGates = GetGatesForPlan(projectPlanId, numberVersion);
        projectPlanDto.LevelStatuses = GetGateLevelStatusesForPlan();
        projectPlanDto.ActivityStatuses = GetActivityStatusesForPlan();
        return projectPlanDto;
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.UpdatePlanDataExceptionFormat(projectPlanId, numberVersion), ex);
      }
    }
    
    /// <summary>
    /// Проверяет существует ли целевая версия в плане.
    /// </summary>
    /// <param name="projectPlanId">ИД плана проекта.</param>
    /// <param name="numberVersion">Номер версии.</param>
    /// <returns>True если план проекта и версия существуют. Иначе False.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public bool CheckPlanExists(long projectPlanId, int numberVersion)
    {
      var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
      
      try
      {
        return projectPlan != null && projectPlan.Versions.Any(x => x.Number.HasValue ? x.Number.Value == numberVersion : false);
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.CheckPlanExistsExeptionFormat(projectPlanId, numberVersion), ex);
      }
    }
    
    /// <summary>
    /// Устанавливает блокировки на карточку плана проекта и связанный проект.
    /// </summary>
    /// <param name="projectPlanId">ИД плана проекта.</param>
    /// <returns>Список сообщений об установленных блокировках.</returns>
    /// <exception cref="Exception">Если не удалось заблокировать план проекта и связанный проект.</exception>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.Module.IProjectPlanLockInfo TryLockProjectPlanAndLinkedEntities(long projectPlanId)
    {
      try
      {
        var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
        if (projectPlan == null)
        {
          throw new Exception(DirRX.ProjectPlanner.Resources.ProjectPlanNotFoundExceptionTextFormat(projectPlanId));
        }
        
        var projectPlanLockInfo = Structures.Module.ProjectPlanLockInfo.Create();
        projectPlanLockInfo.lockMessages = DetectProjectPlanLocks(projectPlan);
        
        if (projectPlanLockInfo.lockMessages.Count > 0)
        {
          return projectPlanLockInfo;
        }
        
        var lockMessagesDuringLocking = Functions.ProjectPlanRX.SetLockOnProjectPlan(projectPlan);
        if (!string.IsNullOrEmpty(lockMessagesDuringLocking))
        {
          projectPlanLockInfo.lockMessages.Add(lockMessagesDuringLocking);
        }
        
        return projectPlanLockInfo;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("{0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
        throw new Exception(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, ex);
      }
    }
    
    [Remote]
    public static List<string> DetectProjectPlanLocks(IProjectPlanRX projectPlan)
    {
      var lockMessages = new List<string>();
      
      var versionLockMessages = DetectLockOnVersions(projectPlan.Versions);
      if (!string.IsNullOrEmpty(versionLockMessages))
      {
        lockMessages.Add(versionLockMessages);
      }
      
      var projectLockMessage = DetectLockOnLinkedProjectFromPlan(projectPlan);
      if (!string.IsNullOrEmpty(projectLockMessage))
      {
        lockMessages.Add(projectLockMessage + '\n');
      }
      
      var projectPlanCardLockMessage = DetectLockOnProjectPlanCard(projectPlan);
      if (!string.IsNullOrEmpty(projectPlanCardLockMessage))
      {
        lockMessages.Add(projectPlanCardLockMessage + '\n');
      }
      
      return lockMessages;
    }
    
    private static string DetectLockOnVersions(IChildEntityCollection<Sungero.Content.IElectronicDocumentVersions> versions)
    {
      var collectiveVersionLocksMessage = string.Empty;
      LockInfo binaryLockInfo;
      
      foreach (var version in versions)
      {
        binaryLockInfo = Locks.GetLockInfo(version.Body);
        if (binaryLockInfo.IsLockedByOther)
        {
          collectiveVersionLocksMessage += $"{binaryLockInfo.LockedMessage}\n";
        }
      }
      
      if (string.IsNullOrEmpty(collectiveVersionLocksMessage))
      {
        return collectiveVersionLocksMessage;
      }
      
      return DirRX.ProjectPlanner.Resources.BlockedVersionsTextFormat(collectiveVersionLocksMessage);
    }
    
    private static string DetectLockOnLinkedProject(IProject linkedProject)
    {
      if (linkedProject == null)
      {
        return string.Empty;
      }
      
      var linkProjectLockInfo = Locks.GetLockInfo(linkedProject);
      if (!linkProjectLockInfo.IsLockedByOther)
      {
        return string.Empty;
      }
      
      return DirRX.ProjectPlanner.Resources.BlockedLinkedProjectTextFormat(linkProjectLockInfo.LockedMessage);
    }
    
    private static string DetectLockOnLinkedProjectFromPlan(IProjectPlanRX projectPlan)
    {
      var linkedProject = Projects.GetAll(x => Equals(x.ProjectPlanDirRX, projectPlan)).FirstOrDefault();
                                          
      return DetectLockOnLinkedProject(linkedProject);
    }
    
    private static string DetectLockOnProjectPlanCard(IProjectPlanRX projectPlan)
    {
      var lockInfo = Locks.GetLockInfo(projectPlan);
      if (!lockInfo.IsLockedByOther)
      {
        return string.Empty;
      }
      
      return DirRX.ProjectPlanner.Resources.BlockedProjectPlanCardTextFormat(lockInfo.LockedMessage);
    }

    /// <summary>
    /// 
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public Structures.Module.ICapacityResponseDto GetCapacityDto(List<long> resourceIds, double startDate, double endDate, long planId, int planVersion)
    {
      try
      {
        var startDateFromTimeStamp = GetDateTimeFromJsDateSeconds(startDate);
        var endDateFromTimeStamp = GetDateTimeFromJsDateSeconds(endDate);
        return new Structures.Module.CapacityResponseDto()
        {
          Capacity = GetCapacity(resourceIds, startDateFromTimeStamp, endDateFromTimeStamp, planId, planVersion),
          WorkingTimeCalendar = GetWorkingTimeCalendars(resourceIds, startDateFromTimeStamp, endDateFromTimeStamp)
        };
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.GetCapacityExceptionFormat(resourceIds, startDate, endDate, ex));
      }
    }
    
    /// <summary>
    /// Найти или создать ресурс сотрудника.
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.ProjectsResource.IProjectResourceDto GetOrCreateResource(long employeeId)
    {
      try
      {
        return this.HandleGetOrCreateResource(employeeId);
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.ExecutionRequestExeptionFormat(ex, employeeId));
      }
    }

    /// <summary>
    /// 
    /// </summary>
    private static void AddEmployeeResources(List<long> projectPlanResourceIds, Dictionary<long, Structures.Module.IUser> users, Dictionary<long, Structures.Module.IResource> resources)
    {
      var employeeResources = ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.Users && projectPlanResourceIds.Contains(x.Id));
      foreach (var employeeResource in employeeResources)
      {
        if (!users.ContainsKey(employeeResource.Employee.Id))
        {
          users.Add(employeeResource.Employee.Id, CreateUser(employeeResource.Employee));
        }
        
        if (!resources.ContainsKey(employeeResource.Id))
        {
          resources.Add(
          employeeResource.Id,
          new Structures.Module.Resource()
                      {
                        Id = employeeResource.Id,
                        EntityTypeId = employeeResource.Type.Id,
                        EntityId = employeeResource.Employee.Id,
                        UnitLabel = employeeResource.Type.MeasureUnit
                      }
         );
        }
      }
      
    }
    
    private static Structures.Module.IUser CreateUser(IEmployee employee)
    {
      var user = new Structures.Module.User()
      {
        Id = employee.Id,
        //HACK Kiselev_EM Баги платформы 4.2 с условными операторами null. Проверяем явным образом.
        Name = (employee.Person == null) ? employee.Name : employee.Person.Name,
        Department = (employee.Department == null) ? string.Empty : employee.Department.DisplayValue,
        JobTitle = (employee.JobTitle == null) ? string.Empty : employee.JobTitle.DisplayValue,
        IsActive = employee.Status == Sungero.Company.Employee.Status.Active
      };
      
      return user;
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static void FillUsersFromResponsibles(List<DirRX.ProjectPlanner.IProjectActivity> activities, Dictionary<long, Structures.Module.IUser> users)
    {
      foreach(var activity in activities.Where(a => a.Responsible != null))
      {
        if (users.ContainsKey(activity.Responsible.Id))
        {
          continue;
        }
        
        users.Add(activity.Responsible.Id, CreateUser(activity.Responsible));
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static List<Structures.Module.IMaterialResource> GetMaterialResources()
    {
      //HACK В данной версии материальные ресурсы не используются.
      return new List<Structures.Module.IMaterialResource>();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// Может ли быть пустой???
    private static List<Structures.Module.IResourceTypes> GetResourcesTypes()
    {
      var resourceTypes = new List<Structures.Module.IResourceTypes>();
      foreach(var type in ProjectResourceTypes.GetAll())
      {
        resourceTypes.Add(new Structures.Module.ResourceTypes()
                          {
                            Id = type.Id,
                            Name = type.Name,
                            SectionName = type.ServiceName
                          });
      }
      return resourceTypes;
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static Structures.Module.IUser TryGetProjectManager(DirRX.ProjectPlanner.IProjectPlanRX project)
    {
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
      if (linkedProject != null && linkedProject.Manager != null)
      {
        return CreateUser(linkedProject.Manager);
      }
      return null;
    }
    
    private Structures.ProjectsResource.IProjectResourceDto HandleGetOrCreateResource(long employeeId)
    {
      //Если ресурс есть, то просто возвращаем его
      var resourceByEmployeeId = DirRX.ProjectPlanner.ProjectsResources.GetAll(x => x.Employee != null && x.Employee.Id == employeeId).FirstOrDefault();
      if (resourceByEmployeeId != null)
      {
        return new Structures.ProjectsResource.ProjectResourceDto()
        {
          ResourceId = resourceByEmployeeId.Id,
          ResourceName = resourceByEmployeeId.Name
        };
      }
      //Если ресурса нет, то создаем новый
      //получаем сотрудника
      var employee = Employees.GetAll(x => x.Id == employeeId).FirstOrDefault();
      if (employee == null){
        throw new Exception(DirRX.ProjectPlanner.Resources.EmployeeIdNotFoundExeptionFormat(employeeId));
      }
      //получаем тип ресурса
      var userResourceType = DirRX.ProjectPlanner.ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.Users).FirstOrDefault();
      if (userResourceType == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.NullReferenceResourceTypeExceptionFormat(Constants.Module.ResourceTypes.Users));
      }
      //Создаем ресурс
      var newResource = DirRX.ProjectPlanner.ProjectsResources.Create();
      newResource.Type = userResourceType;
      newResource.Employee = employee;
      newResource.Name = newResource.Employee.Name;
      newResource.Save();
      //Возвращаем новый ресурс
      return new Structures.ProjectsResource.ProjectResourceDto()
      {
        ResourceId = newResource.Id,
        ResourceName = newResource.Name
      };
    }
    
    private static List<long> GetPlanOldTasksQuery(long activityId)
    {
      var oldTaskIds = new List<long>();
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        try
        {
          Logger.Debug("Execute get activity old tasks query");
          command.CommandText = string.Format(Queries.Module.GetPlanOldTasks, activityId);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              try 
              {
                oldTaskIds.Add((long)reader[0]);
              }
              catch (Exception ex) 
              {
                Logger.ErrorFormat("Не найдена задача с ID = \"{0}\", Exception: {1} Trace: {2}", reader[0], ex.ToString(), ex.StackTrace.ToString());
              }
            }
          }
        }
        catch (Exception ex) 
        {
          Logger.ErrorFormat("Ошибка выполнения запроса. Exception: {0}, Trace: {1}", ex.ToString(), ex.StackTrace.ToString());
        }
      }
      return oldTaskIds;
    }
    
    /// <summary>
    /// Получить список задач по всем этапам указанной версии плана.
    /// </summary>
    private static List<Structures.Module.ITask> GetRxTasks(long projectPlanId, int numVersion)
    {
      var projectActivities = ProjectActivities.GetAll(x => x.ProjectPlan.Id == projectPlanId && x.NumberVersion == numVersion);
      var refIds = projectActivities.ToDictionary(x => x.RefId, x => x.Id);
      var activityTasks = ProjectActivityTaskFunctions.GetTasksByRefId(refIds.Keys, projectPlanId);
      var rootTaskInfo = GetRootTaskInfo(activityTasks);
      var actionTasks = Sungero.RecordManagement.ActionItemExecutionTasks.GetAll(t => t.AttachmentDetails.Any(e => e.EntityTypeGuid == ProjectActivity.ClassTypeGuid &&
                                             projectActivities.Select(pa => pa.Id).Contains(e.EntityId.Value)));

      var result = GetTasksDto(activityTasks, refIds, projectPlanId, rootTaskInfo);
      result.AddRange(GetTasksDto(actionTasks, refIds, projectPlanId, rootTaskInfo));
      return result;
    }
    
    private static Structures.Module.IRootTaskInfo GetRootTaskInfo(IEnumerable<DirRX.ProjectPlanner.IProjectActivityTask> tasks)
    {
      var activityExistedTasks = tasks
        .GroupBy(t => t.ActivityRefId)
        .Where(x => x.Count() > 1)
        .ToDictionary(k => k.Key, t => t);
      
      var rootTaskInfo = Structures.Module.RootTaskInfo.Create();
        
      rootTaskInfo.RootTaskIds = activityExistedTasks
        .Select(x => x.Value.Where(t => ProjectActivityTasks.Is(t) && t.Status != Sungero.Workflow.Task.Status.Aborted).Min(y => y.Id))
        .ToList();
      
      rootTaskInfo.NotRootTaskIds = activityExistedTasks
        .SelectMany(x => x.Value
          .Where(t => !rootTaskInfo.RootTaskIds.Contains(t.Id))
          .Select(t => t.Id))
        .ToList();
      
      //Kiselev_EM Одиночные черновики.
      rootTaskInfo.RootTaskIds.AddRange(tasks
        .Where(t => t.Status == Sungero.Workflow.Task.Status.Draft)
        .GroupBy(g => g.ActivityRefId)
        .Where(g => g.Count() == 1)
        .Select(g => g.First().Id)
        .Where(i => !rootTaskInfo.RootTaskIds.Contains(i))
        .ToList());
      
      //Kiselev_EM Одиночные задачи которые могут быть рутовыми.
      rootTaskInfo.RootTaskIds.AddRange(tasks
        .Where(t => t.Status != Sungero.Workflow.Task.Status.Draft &&
          t.Status != Sungero.Workflow.Task.Status.Aborted)
        .GroupBy(g => g.ActivityRefId)
        .Where(g => g.Count() == 1)
        .Select(g => g.First().Id)
        .Where(i => !rootTaskInfo.NotRootTaskIds.Contains(i))
        .ToList());
      
      rootTaskInfo.NotRootTaskIds.AddRange(tasks
        .Where(t => !rootTaskInfo.RootTaskIds.Contains(t.Id))
        .Select(t => t.Id)
        .ToList());
        
      return rootTaskInfo;
    }
    
    private static List<Structures.Module.ITask> GetTasksDto(IEnumerable<Sungero.Workflow.ITask> tasks,
      Dictionary<long?, long> refIds,
      long projectPlanId,
      Structures.Module.IRootTaskInfo rootTaskInfo)
    {
      var rxTasks = new List<Structures.Module.ITask>();
      
      foreach (var task in tasks)
      {
        // Zheleznov_AV HACK клиент сейчас не умеет обрабатывать отмененные задачи.
        // Надо синхронизировать модель статусов на киленте и на сервере.
        if (task.Status == Sungero.Workflow.Task.Status.Aborted)
        {
          continue;
        }
        
        var taskStatus = Constants.Module.TaskDtoStatuses.Unfinished;
        if (task.Status == Sungero.Workflow.Task.Status.Completed)
        {
          taskStatus = Constants.Module.TaskDtoStatuses.Submitted;
        }
        
        foreach (var refId in GetTaskRefIds(task, projectPlanId))
        {
          var newTaskItem = new Structures.Module.Task()
            {
              ActivityId = refIds[refId],
              Deadline = task.MaxDeadline,
              HyperLink = Hyperlinks.Get(task),
              Id = task.Id,
              TaskStatus = taskStatus,
              DisplayValue = task.DisplayValue,
              //Kiselev_EM У черновиков нет Started.
              StartDate = task.Started.HasValue ? task.Started.Value : task.Created.Value,
              //Kiselev_EM Последнее что происходит с задачей - обновляется статус на Completed, соответственно и Modified.
              EndDate = task.Status == Sungero.Workflow.Task.Status.Completed ? task.Modified : null,
              //Kiselev_EM Новые задачи по этапу будут ходить по новой схеме.
              IsRoot = IsRootTask(task, rootTaskInfo),
              ActivityStatus = MapTaskStatusToActivityStatus(task.Status)
            };
          
          var projectActivityTask = ProjectActivityTasks.As(task);
          if (projectActivityTask != null && newTaskItem.IsRoot)
          {
            newTaskItem.ExecutionPercent = projectActivityTask.ExecutionPercent;
            newTaskItem.ActualWorkload = projectActivityTask.ActualWorkload;
            newTaskItem.FactualCosts = projectActivityTask.FactualCosts;
          }
          
          rxTasks.Add(newTaskItem);
        }
      }
      
      return rxTasks;
    }
    
    /// <summary>
    /// Транслирует статус задачи в статус этапа.
    /// </summary>
    /// <param name="status">Статус задачи.</param>
    /// <returns>Dto статуса этапа.</returns>
    private static string MapTaskStatusToActivityStatus(Enumeration? status)
    {
      var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;
      if (status == Sungero.Workflow.Task.Status.Completed)
      {
        return DirRX.ProjectPlanner.ProjectActivity.Status.Closed.Value;
      }
      if (status == Sungero.Workflow.Task.Status.Draft || status == Sungero.Workflow.Task.Status.Aborted)
      {
        return DirRX.ProjectPlanner.ProjectActivity.Status.Active.Value;
      }
      if (status == Sungero.Workflow.Task.Status.InProcess || status == Sungero.Workflow.Task.Status.UnderReview || status == Sungero.Workflow.Task.Status.Suspended)
      {
        return DirRX.ProjectPlanner.ProjectActivity.Status.InWork.Value;
      }
      // Досюда дойти не должно.
      Logger.Error(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.NotSupportedStatus);
      throw new ArgumentException(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.NotSupportedStatus);
    }
    
    private static bool IsRootTask(Sungero.Workflow.ITask task, Structures.Module.IRootTaskInfo rootTaskInfo)
    {
      //Kiselev_EM Из "На исполнение поручением" не делаем рутовые таски.
      if (!DirRX.ProjectPlanner.ProjectActivityTasks.Is(task) ||
         (rootTaskInfo.NotRootTaskIds.Contains(task.Id)))
      {
        return false;
      }
      
      return rootTaskInfo.RootTaskIds.Contains(task.Id) ||
        (task.SchemeVersion >= Constants.ProjectActivityTask.ScheduledTaskSchemeNumber &&
         !rootTaskInfo.NotRootTaskIds.Contains(task.Id));
    }
    
    private static IEnumerable<long?> GetTaskRefIds(Sungero.Workflow.ITask task, long projectPlanId)
    {
      if (ProjectActivityTasks.Is(task))
      {
        return new long?[] {ProjectActivityTasks.As(task).ActivityRefId};
      }
      if (Sungero.RecordManagement.ActionItemExecutionTasks.Is(task))
      {
        var actionItemTask = Sungero.RecordManagement.ActionItemExecutionTasks.As(task);
        var activities = actionItemTask.OtherGroup.All.Where(e => ProjectActivities.Is(e)).Select(e => ProjectActivities.As(e));
        return activities.Where(a => a.ProjectPlan.Id == projectPlanId).Select(a => a.RefId);
      }
      Logger.Error("Неподдерживаетый тип Задачи");
      return new long?[]{};
    }
    
    /// <summary>
    /// Получение открытых из карточки планов.
    /// </summary>
    [Remote]
    public static List<IOpensProjectPlansFromCard> GetOpensProjectPlansFromCard(IProjectPlanRX pp, DirRX.ProjectPlanning.IProject linkedProject)
    {
      return OpensProjectPlansFromCards.GetAll(x => ProjectPlanRXes.Equals(x.PrjectPlan, pp) || (linkedProject != null && DirRX.ProjectPlanning.Projects.Equals(x.Project, linkedProject))).ToList();
    }
    
    
    [Remote]
    public static List<IOpensProjectPlansFromCard> GetOpensProjectsFromCard( DirRX.ProjectPlanning.IProject project)
    {
      return project == null ?
        new List<IOpensProjectPlansFromCard>(0) :
        OpensProjectPlansFromCards.GetAll(x => x.Project != null && DirRX.ProjectPlanning.Projects.Equals(x.Project, project)).ToList();
    }
    
    /// <summary>
    /// Удаление всех этапов для указанной версии плана.
    /// </summary>
    /// <param name="projectPlanId">Id плана проекта</param>
    /// <param name="numVersion">Номер версии плана</param>
    [Remote]
    public static void DeleteProjectActiviesByNumberVersion(long projectPlanId, int numVersion)
    {
      var prActs = ProjectActivities.GetAll(x => x.NumberVersion.Value == numVersion && x.ProjectPlan.Id == projectPlanId).ToList();
      DeleteActivities(prActs);
    }
    
    /// <summary>
    /// Удаление всех этапов плана проекта.
    /// </summary>
    [Remote]
    public static void DeleteAllActivities(IProjectPlanRX projectPlan)
    {
      var linkedProject = Functions.ProjectPlanRX.GetLinkedProject(projectPlan);
      
      if (!DirRX.ProjectPlanning.ProjectDocuments.GetAll(x => x.Project.Id == (linkedProject != null ? linkedProject.Id : 0)).Any() || linkedProject == null)
      {
        var prActs = ProjectActivities.GetAll(x => x.ProjectPlan.Id == projectPlan.Id).ToList();
        DeleteActivities(prActs);
      }
    }
    
    private static void DeleteActivities(IEnumerable<IProjectActivity> prActs)
    {
      AbortProjectActivityTasks(prActs);
      StopTeamTasksForActivities(prActs.Select(x => x.Id).ToList());
      
      foreach (var act in prActs)
      {
        if (act.Predecessors != null)
        {
          act.Predecessors.Clear();
        }
        act.LeadingActivity = null;
      }
      
      BatchSave(prActs);
      
      foreach (var act in prActs)
      {
        try
        {
          ProjectActivities.Delete(act);
        }
        catch(Exception ex)
        {
          var errorMessage = string.Format("Невозможно удалить активити с id {0}, Error: {1}", act.Id, ex.Message + Environment.NewLine + ex.StackTrace);
          Logger.Error(errorMessage);
          throw new Exception(errorMessage);
        }
      }
    }
    
    /// <summary>
    /// Создание копии версии плана проекта.
    /// </summary>
    /// <param name="projectPlan">План проекта</param>
    /// <param name="sourceNumVersion">Номер исходной версии</param>
    [Remote]
    public static void CreateCopyVersion(IProjectPlanRX projectPlan, int sourceNumVersion)
    {
      var newVersion = projectPlan.Versions.AddNew();
      newVersion.AssociatedApplication = Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").First();
      projectPlan.Save();
      
      var model = string.Empty;
      
      using (var reader = new System.IO.StreamReader(projectPlan.Versions.First(x => x.Number.Value == sourceNumVersion).Body.Read()))
      {
        model = reader.ReadToEnd();
      }
      
      SaveModelFromVersionString(model, projectPlan, projectPlan.LastVersion.Number.Value, true, false);
    }
    
    /// <summary>
    /// Создание копии проекта
    /// </summary>
    [Remote]
    public static void CreateCopyProject(IProjectPlanRX projectPlan, int numVersion)
    {
      var model = string.Empty;
      
      using (var reader = new System.IO.StreamReader(projectPlan.Versions.First(x => x.Number.Value == numVersion).Body.Read()))
      {
        model = reader.ReadToEnd();
      }
      
      SaveModelFromVersionString(model, projectPlan, projectPlan.LastVersion.Number.Value, false, true);
    }
    
    [Public(WebApiRequestType = RequestType.Post), Remote]
    public static void WriteJsonBodyToProjectVersion(long projectPlanId, int numVersion, bool writeToPublicBody)
    {
      var projectPlan = ProjectPlanRXes.Get(projectPlanId);
      WriteJsonBodyToProjectVersion(projectPlan, numVersion, writeToPublicBody);
    }
    
    /// <summary>
    /// Записать тело Json-модели в свойство проекта "ModelBody".
    /// </summary>
    /// <param name="projectPlan">Ссылка на план проекта.</param>
    /// <param name="numVersion">Порядковый номер версии.</param>
    /// <param name="writeToPublicBody">Записать в Read-only тело.</param>
    [Public, Remote]
    public static void WriteJsonBodyToProjectVersion(IProjectPlanRX projectPlan, int numVersion, bool writeToPublicBody)
    {
      if (projectPlan != null)
      {
        try
        {
          string jsonBody = GetModel(projectPlan, numVersion);
          
          using (var stream = new System.IO.MemoryStream())
          {
            var bytes = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(jsonBody);
            stream.Write(bytes, 0, bytes.Length);
            
            if (numVersion == 0)
            {
              if (projectPlan.HasVersions)
              {
                var lastVersion = projectPlan.LastVersion;
                if (writeToPublicBody)
                {
                  lastVersion.PublicBody.Write(stream);
                }
                else
                {
                  lastVersion.Body.Write(stream);
                }
              }
              else
              {
                projectPlan.CreateVersionFrom(stream, "rxpp");
              }
              
            }
            else
            {
              var version = projectPlan.Versions.FirstOrDefault(x => x.Number.Value == numVersion);
              if (version != null)
              {
                if (writeToPublicBody)
                {
                  version.PublicBody.Write(stream);
                }
                else
                {
                  version.Body.Write(stream);
                }
              }
              ///HACK Urmanov_AR: Фикс ошибки с ресурсами при создании плана из файла.
              else
              {
                projectPlan.CreateVersionFrom(stream, "rxpp");
              }
            }
            projectPlan.IsCopy = false;
            projectPlan.Save();
            //HACK несмотря на using в коробке принято явно закрывать стрим
            stream.Close();
          }
          
        }
        catch(Exception ex)
        {
          Logger.DebugFormat("При сохранении плана проекта ID {0} произошла ошибка. {1}", projectPlan.Id, ex.Message+ex.StackTrace);
        }
      }
    }
    
    /// <summary>
    /// Запросить модель из сервиса хранилищ по ссылке.
    /// </summary>
    /// <param name="uriModel">Ссылка</param>
    [Public, Remote]
    public static string GetJsonStringByUri(string uriModel)
    {
      var client = new System.Net.WebClient();
      var bytesModel = client.DownloadData(uriModel);
      var stringModel = System.Text.Encoding.UTF8.GetString(bytesModel);
      
      return stringModel;
    }
    
    /// <summary>
    /// Создаёт активити из модели.
    /// </summary>
    /// <param name="activityModel">Модель активити.</param>
    /// <param name="projectPlan">План проекта.</param>
    /// <param name="numVersion">Номер версии.</param>
    /// <returns></returns>
    private static ProjectPlanner.IProjectActivity CreateActivityFromModelString(
      DirRX.Planner.Model.Activity activityModel,
      long lastActivityId,
      IProjectPlanRX projectPlan,
      DirRX.ProjectPlanning.IProject linkedProject,
      Nullable<int> numVersion)
    {
      var newActivity = ProjectPlanner.ProjectActivities.Create();

      newActivity.Name = activityModel.Name;
      if (string.IsNullOrWhiteSpace(newActivity.Name))
      {
        return null;
      }
      
      newActivity.Number = activityModel.CurrentNumber;
      newActivity.StartDate = activityModel.StartDate;
      newActivity.EndDate = activityModel.EndDate;
      if (!newActivity.StartDate.HasValue || !newActivity.EndDate.HasValue)
      {
        return null;
      }
      newActivity.ProjectPlan = projectPlan;
      
      newActivity.Duration = ProjectPlanner.Functions.Module.GetWorkingDays(newActivity).ToString();
      newActivity.BaselineWork = activityModel.BaselineWork;
      newActivity.ActualWorkload = activityModel.ActualWorkload;
      newActivity.ExecutionPercent = activityModel.ExecutionPercent;
      newActivity.Note = activityModel.Note;
      newActivity.SortIndex = activityModel.SortIndex;
      newActivity.Priority = activityModel.Priority;
      newActivity.FactualCosts = activityModel.FactualCosts;
      newActivity.PlannedCosts = activityModel.PlannedCosts;
      newActivity.NumberVersion = numVersion;
      newActivity.RefId = GetRefId(newActivity, activityModel, lastActivityId);
      
      if (activityModel.Status != null)
      {
        newActivity.Status = GetActualActivityStatus(activityModel.Status.EnumValue);
      }
      
      newActivity.TypeActivity = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task;
      
      var type = newActivity.TypeActivityAllowedItems.Where(t => t != null && t.Value == activityModel.TypeActivity).FirstOrDefault();
      
      if (type != null && type.Value != null)
      {
        newActivity.TypeActivity = type;
      }
      
      if (activityModel.ResponsibleId.HasValue)
      {
        var responsible = Sungero.Company.Employees.GetAll(x => x.Id == activityModel.ResponsibleId.Value).FirstOrDefault();
      
        if (responsible == null)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.EmployeeForActivityNotFoundFormat(activityModel.ResponsibleId.Value, activityModel.Name));
        }
        else
        {
          newActivity.Responsible = responsible;
        
          if (!projectPlan.TeamMembers.Any(x => Recipients.Equals(x.Member, newActivity.Responsible)))
          {
            var newMember = projectPlan.TeamMembers.AddNew();
            newMember.Member = newActivity.Responsible;
            newMember.Group = DirRX.ProjectPlanner.ProjectPlanRXTeamMembers.Group.Change;
          }
          
          if (linkedProject != null)
          {
            if (!linkedProject.TeamMembers.Any(x => x.Member.Id == newActivity.Responsible.Id))
            {
              var newMember = linkedProject.TeamMembers.AddNew();
              newMember.Member = newActivity.Responsible;
              newMember.Group = DirRX.ProjectPlanner.ProjectPlanRXTeamMembers.Group.Change;
            }
          }
          else
          {
            if (!projectPlan.AccessRights.CanRead(newActivity.Responsible))
            {
              projectPlan.AccessRights.Grant(newActivity.Responsible, DefaultAccessRightsTypes.Read);
            }
          }
        }
      }
      
      if (activityModel.Attachments != null && activityModel.Attachments.Count > 0)
      {
        foreach (var attachment in activityModel.Attachments)
        {
          if (!attachment.AttachmentId.HasValue)
          {
            continue;
          }
          
          var activityAttachment = newActivity.Attachments.AddNew();
          activityAttachment.AttachmentId = attachment.AttachmentId;
          activityAttachment.Name = attachment.Name;
          activityAttachment.Url = attachment.Url;
        }
      }
      
      return newActivity;
    }
    
    private static long? GetRefId(ProjectPlanner.IProjectActivity newActivity, DirRX.Planner.Model.Activity activityModel, long lastActivityId)
    {
      var newRefId = newActivity.RefId ?? activityModel.RefId;
      if ((newRefId ?? 0) == 0)
      {
        var useActivityModelId = lastActivityId > 0 && activityModel?.Id != null && lastActivityId >= activityModel.Id.Value;
        newRefId = useActivityModelId ? activityModel.Id : newActivity.Id;
      }
      return newRefId;
    }
    
    /// <summary>
    /// Заполнить предшественников в новой версии.
    /// </summary>
    /// <param name="modelActivities">Модели активити.</param>
    /// <param name="idActivityMapperList">Соответствие ИД новых этапов из модели и базы.</param>
    /// <param name="originalCopyActivities">Справочник, содержащий соотношение старых этапов с новыми.</param>
    private static void UpdatePredecessorsNewVersion(List<DirRX.Planner.Model.Activity> modelActivities,
                                                     List<Structures.Module.IdActivityMapper> idActivityMapperList,
                                                     IDictionary<long, KeyValuePair<IProjectActivity, IProjectActivity>> originalCopyActivities)
        {
        
      foreach (var modelActivity in modelActivities.Where(a => a.Predecessors != null && a.Predecessors.Any()))
        {
        var idActivityMapper = idActivityMapperList.First(a => a.ModelActivityId == modelActivity.Id);
        var activity = originalCopyActivities[idActivityMapper.ModelActivityId].Value;
        
        foreach (var modelPredecessor in modelActivity.Predecessors)
          {
          var idPredecessorMapper = idActivityMapperList.First(a => a.ModelActivityId == modelPredecessor.Id);
          var predecessor = originalCopyActivities[idPredecessorMapper.ModelActivityId].Value;
          
          var predecessorRef  = activity.Predecessors.AddNew();
          predecessorRef.Activity = predecessor;
          predecessorRef.LinkType = modelPredecessor.LinkType;
          predecessorRef.Lag = modelPredecessor.Lag;
        }
      }
    }
    
    /// <summary>
    /// Заполнить ведущие этапы в новой версии.
    /// </summary>
    /// <param name="modelActivities">Модели активити.</param>
    /// <param name="idActivityMapperList">Соответствие ИД новых этапов из модели и базы.</param>
    /// <param name="originalCopyActivities">Справочник, содержащий соотношение старых этапов с новыми.</param>
    private static void SetLeadActivitiesNewVersion(List<DirRX.Planner.Model.Activity> modelActivities,
                                          List<Structures.Module.IdActivityMapper> idActivityMapperList,
                                          IDictionary<long, KeyValuePair<IProjectActivity, IProjectActivity>> originalCopyActivities)
      {
      
      foreach (var modelActivity in modelActivities.Where(a => a.LeadActivityId.HasValue))
        {
        var idActivityMapper = idActivityMapperList.First(a => a.ModelActivityId == modelActivity.Id);
        var activity = originalCopyActivities[idActivityMapper.ModelActivityId].Value;
        
        var idLeadActivityMapper = idActivityMapperList.First(a => a.ModelActivityId == modelActivity.LeadActivityId);
        var leadActivity = originalCopyActivities[idLeadActivityMapper.ModelActivityId].Value;
        
        activity.LeadingActivity = leadActivity;
      }
    }
    
    /// <summary>
    /// Создать копии этапов с модели. Не подходит для сохранения текущей версии.
    /// </summary>
    /// <param name="activities"></param>
    /// <param name="lastActivityId"></param>
    /// <param name="numVersion"></param>
    /// <param name="linkedProject"></param>
    /// <param name="projectPlan"></param>
    /// <param name="originalCopyActivities"></param>
    /// <param name="activityGateLinks"></param>
    /// <returns></returns>
    private static System.Func<System.Threading.Tasks.Task> UpdateActivitiesNewVersion(
      System.Collections.Generic.List<DirRX.Planner.Model.Activity> activities,
      long lastActivityId,
      int numVersion,
      DirRX.ProjectPlanning.IProject linkedProject,
      IProjectPlanRX projectPlan,
      IDictionary<long, KeyValuePair<IProjectActivity, IProjectActivity>> originalCopyActivities,
      List<Structures.Module.IdActivityMapper> idActivityMapperList,
      IDictionary<long, long?> activityGateLinks = null)
    {
      var oldIds = activities.Where(ma => ma.Id != null).Select(ma => ma.Id.Value).ToList();
      var oldActivities = ProjectActivities.GetAll(pa => oldIds.Contains(pa.Id)).ToDictionary(pa => pa.Id);
      var gateLinksPresent = activityGateLinks != null;
      
      foreach (var activityModel in activities)
      {
        // TODO: нужно целиком пересмотреть метод. Выглядит неоптимальным, происходит выдача прав, а также выполняется логика, которая возможно уже не нужна
        var newActivity = CreateActivityFromModelString(activityModel, lastActivityId, projectPlan, linkedProject, numVersion);

        if (newActivity == null)
        {
          continue;
        }
        
        idActivityMapperList.Add(Structures.Module.IdActivityMapper.Create(activityModel.Id.Value, newActivity.Id));
        
        if (gateLinksPresent)
        {
          long? activityGateId;
          var gateIds = ActualizeGateIds(activityGateLinks.Where(x => x.Value.HasValue).Select(x => x.Value.Value));
          activityGateLinks.TryGetValue(activityModel.Id.Value, out activityGateId);
          
          if (activityGateId.HasValue && gateIds.Contains(activityGateId.Value))
          {
            newActivity.GateId = activityGateId;
          }
        }
        
        RefreshCapacityInfo(activityModel.Resources, newActivity);
        
        if (activityModel.Attachments != null && activityModel.Attachments.Count > 0)
        {
          ConvertAndUpdateActivityAttachments(newActivity, activityModel.Attachments);
        }
        
        IProjectActivity oldActivity = null;
        oldActivities.TryGetValue(activityModel.Id.Value, out oldActivity);
        originalCopyActivities.Add(activityModel.Id.Value, new KeyValuePair<IProjectActivity, IProjectActivity>(oldActivity ?? newActivity, newActivity));
      }
      return null;
    }
    
    private static void ConvertAndUpdateActivityAttachments(IProjectActivity activity,
      List<DirRX.Planner.Model.Attachment> attachments)
    {
      //HACK Kiselev_EM Из-за не единообразия моделей конвертирую вложения, что-бы не дублировать код обработки.
      var convertedAttachments = attachments.Select(x =>
        DirRX.ProjectPlanner.Structures.Module.AttachmentDto.Create(x.AttachmentId, x.Url, x.Name)).ToList();
      
      UpdateActivityAttachments(activity, convertedAttachments);
    }
    
    /// <summary>
    /// Разбор json модели плана проекта. Для импорта из файла.
    /// </summary>
    [Public, Remote]
    public static void SaveModelFromString(string modelJson, IProjectPlanRX projectPlan, int numVersion)
    {
      var model = GetProjectPlanModelFromString(modelJson);
      SaveModel(model, projectPlan, numVersion, false, false, null);
    }
    
    /// <summary>
    /// Получение модели плана проекта из JSON-строки.
    /// </summary>
    /// <param name="modelJson">Модель плана проекта в JSON.</param>
    /// <returns>Модель плана проекта.</returns>
    private static DirRX.Planner.Model.Model GetProjectPlanModelFromString(string modelJson)
    {
      return Newtonsoft.Json.JsonConvert.DeserializeObject<DirRX.Planner.Model.Model>(modelJson);
    }
    
    /// <summary>
    /// Разбор json модели плана проекта. Копирование версии из карточки.
    /// </summary>
    private static void SaveModelFromVersionString(string modelJson, IProjectPlanRX projectPlan, int numVersion, bool isCopyVersion, bool isCopyProject)
    {
      var model = Newtonsoft.Json.JsonConvert.DeserializeObject<DirRX.Planner.Model.Model>(modelJson);
      var ids = model.Activities.Where(a => a.Id != null).Select(a => a.Id.Value).ToList();
      var activityGateLinks = DirRX.ProjectPlanner.ProjectActivities.GetAll(pa => ids.Contains(pa.Id)).ToDictionary(a => a.Id, a => a.GateId);
      SaveModel(model, projectPlan, numVersion, isCopyVersion, isCopyProject, activityGateLinks);
    }
    
    /// <summary>
    /// Сохранение плана проекта из модели.
    /// </summary>
    public static void SaveModel(
      DirRX.Planner.Model.Model model,
      IProjectPlanRX projectPlan,
      int numVersion,
      bool isCopyVersion,
      bool isCopyProject,
      System.Collections.Generic.Dictionary<long, long?> activityGateLinks)
    {
      using (var connection = CreateDBConnection())
      {
        if (!(isCopyVersion || isCopyProject))
        {
          DeleteProjectActiviesByNumberVersion(projectPlan.Id, numVersion);
        }
        
        // Если сохраняем в новый документ (не новую версию), то сбрасываем refId. RefId имеют значение только в границах одного документа.
        if (!isCopyVersion)
        {
          model.LastActivityId = 0;
          model.Activities.ForEach(a => a.RefId = null);
        }
        
        var originalCopyActivities = new Dictionary<long, KeyValuePair<IProjectActivity, IProjectActivity>>();
        var linkedProject = DirRX.ProjectPlanner.Functions.ProjectPlanRX.GetLinkedProject(projectPlan);
        var idActivityMapperList = new List<Structures.Module.IdActivityMapper>();
  
        Sungero.Domain.ParallelHelper.TaskRun(
          UpdateActivitiesNewVersion(
                model.Activities,
                model.LastActivityId,
                numVersion,
                linkedProject,
                projectPlan,
            originalCopyActivities,
            idActivityMapperList,
                activityGateLinks)
             );
        
        foreach (var activityModel in model.Activities)
        {
          var newactivityId = originalCopyActivities[activityModel.Id.Value].Value.Id;
          SaveResources(activityModel.Resources, newactivityId, activityModel.StartDate.Value, activityModel.EndDate.Value, connection);
        }
        
        UpdatePredecessorsNewVersion(model.Activities, idActivityMapperList, originalCopyActivities);
        SetLeadActivitiesNewVersion(model.Activities, idActivityMapperList, originalCopyActivities);
        BatchSave(originalCopyActivities.Values.Select(p => p.Value)); // сохраняем один раз этапы
        
        WriteJsonBodyToProjectVersion(projectPlan, numVersion, false);
        
        if (isCopyVersion)
        {
          FindVersionsActivityDiff(originalCopyActivities, projectPlan, numVersion);
        }
      }
    }
    
    /// <summary>
    /// Найти отличия между версиями и выполнить связанную логику.
    /// </summary>
    /// <param name="originalCopyActivities">Словарь оригинальный этап - копия этапа.</param>
    /// <param name="versionNumber">Номер версии</param>
    private static void FindVersionsActivityDiff(IDictionary<long, KeyValuePair<IProjectActivity, IProjectActivity>> originalCopyActivities, IProjectPlanRX plan, int versionNumber)
    {
      var allUpdatedActivitiesForTaskRefIds = new List<long>();
      var changedResponsibleProjectActivityRefIds = new List<long>();
      var changedStartDateActivityRefIds = new List<long>();
      
      foreach (var item in originalCopyActivities)
      {
        var original = item.Value.Key;
        var copy = item.Value.Value;
      
        if (original == null || copy == null)
        {
          continue;
        }
      
        if (original.Responsible?.Id != copy.Responsible?.Id)
        {
          Functions.ProjectActivity.CreateResponsibleDiff(copy, copy.Responsible?.Id, original.Responsible?.Id);
        }
        
        if (!copy.RefId.HasValue)
        {
          continue;
        }
        
        if (original.StartDate != copy.StartDate || original.EndDate != copy.EndDate || original.TypeActivity != copy.TypeActivity || original.Responsible?.Id != copy.Responsible?.Id)
        {
          allUpdatedActivitiesForTaskRefIds.Add(copy.RefId.Value);
      }
        if (original.StartDate != copy.StartDate)
        {
          changedStartDateActivityRefIds.Add(copy.RefId.Value);
        }
        if (original.Responsible?.Id != copy.Responsible?.Id)
        {
          changedResponsibleProjectActivityRefIds.Add(copy.RefId.Value);
        }
      }
      // Обновить автозапускаемые задачи.
      UpdateTasks(allUpdatedActivitiesForTaskRefIds, changedResponsibleProjectActivityRefIds, changedStartDateActivityRefIds, plan.Id, versionNumber);
      // Уведомить об изменении ответственного.
      Functions.ProjectPlanRX.NotifyAboutChanges(plan);
    }
    
    /// <summary>
    /// Создать план проекта из шаблона.
    /// </summary>
    /// <param name="template">Шаблон.</param>
    /// <param name="projectPlan">План проекта.</param>
    [Public, Remote]
    public static void SaveProjectPlanFromTemplate(DirRX.ProjectPlanning.IDocumentTemplate template, IProjectPlanRX projectPlan)
    {
      using (var stream = new System.IO.MemoryStream())
      {
        template.LastVersion.Body.Read().CopyTo(stream);
        var templateBody = System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

        var planModel = GetProjectPlanModelFromString(templateBody);
        
        
        if (planModel.Activities != null && planModel.Activities.Count > 0)
        {
          var activities = planModel.Activities;
          if (projectPlan.StartDate.HasValue)
          {
            var startDate = projectPlan.StartDate.Value;
            
            var minActivityStartDate = activities.Where(a => a.StartDate.HasValue).Select(a => a.StartDate.Value).Min<DateTime>();
            var dateDifference = startDate - minActivityStartDate;
            
            foreach (var activity in activities)
            {
              if (activity.EndDate.HasValue)
              {
                activity.EndDate += dateDifference;
              }
              
              if (activity.StartDate.HasValue)
              {
                activity.StartDate += dateDifference;
              }
            }
          }
        }

        SaveModel(planModel, projectPlan, 1, false, false, null);
      }
    }

    private static void InsertResourceLink(long activityId, long resourceId, double busy, System.Data.IDbConnection connection)
    {
      using (var command = connection.CreateCommand())
        {
          command.CommandText = "insert into ResourceLinks values(@project_activity_id, @resource_id, @average_busy)";
          SQL.AddParameter(command, "@project_activity_id", activityId, System.Data.DbType.Int64);
          SQL.AddParameter(command, "@resource_id", resourceId, System.Data.DbType.Int64);
          SQL.AddParameter(command, "@average_busy", busy, System.Data.DbType.Double);
          command.ExecuteScalar();
        }
    }
    
    [Public]
    public static System.Data.IDbConnection CreateDBConnectionPublic()
    {
      return CreateDBConnection();
    }
    
    [Public]
    public static void SaveResources(long resourceId, long activityId, int busy, System.Data.IDbConnection connection)
    {
      InsertResourceLink(activityId, resourceId, busy, connection);
    }
    
    /// <summary>
    /// Сохранение изменений в ресурсах проекта.
    /// </summary>
    public static void SaveResources(List<Planner.Model.ResourcesWorkload> resourcesWorkload, long activityId, DateTime startDate, DateTime endDate, System.Data.IDbConnection connection)
    {
      foreach(var resourceWorkload in resourcesWorkload)
      {
        var resource = ProjectsResources.GetAll(x => x.Id == resourceWorkload.ResourceId).SingleOrDefault();
        if (resource == null)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.ResourceNotFoundFormat(resourceWorkload.ResourceId));
          continue;
        }
        
        //TODO Urmanov: вот тут была важная проверка на то что ресурсов не найдено, надо сделать с новыми ресурсами
        var activityLengthInDays = WorkingTime.GetDurationInWorkingDays(startDate, endDate.AddDays(-1), resource.Employee);
        var busy = resourceWorkload.Value;
        
        if (activityLengthInDays > 1)
          busy = resourceWorkload.Value / activityLengthInDays;
        
        InsertResourceLink(activityId, resourceWorkload.ResourceId, busy, connection);
      }
    }
    
    private static System.Data.IDbConnection CreateDBConnection()
    {
      System.Data.IDbConnection connection;
      
      connection = SQL.CreateConnection();
      
      if (connection.State != System.Data.ConnectionState.Open)
      {
        connection.Open();
      }
      
      return connection;
    }
    
    
    /// <summary>
    /// Удаление упоминаний активити в ресурсах.
    /// </summary>
    public static void RemoveActivityResources(System.Collections.Generic.List<long> activityIds, System.Data.IDbConnection connection)
    {
      using (var command = connection.CreateCommand())
      {
        var activityIdsParameter = activityIds.Count == 0 ? "0,0" : string.Join(",", activityIds.ToArray());
        //HACK: через AddArrayParameter работает нестабильно, пришлось сделать replace в запросе.
        //HACK: для того чтобы параметр IN(@resourceIds) в результате не привел к строке IN()
        command.CommandText = "delete from ResourceLinks where project_activity_id  IN(@project_activity_ids)"
          .Replace("@project_activity_ids", activityIdsParameter);
        
        command.ExecuteScalar();
      }
    }
    
    /// <summary>
    /// Удаление упоминаний активити в ресурсах.
    /// </summary>
    public static void RemoveActivityResources(long activityId, System.Data.IDbConnection connection)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = "delete from ResourceLinks where project_activity_id = @project_activity_id";
        SQL.AddParameter(command, "@project_activity_id", activityId, System.Data.DbType.Int64);
        command.ExecuteScalar();
      }
      
    }
    
    /// <summary>
    /// Обновление информации о трудоемкости.
    /// </summary>
    public static void RefreshCapacityInfo(List<DirRX.Planner.Model.ResourcesWorkload> resources, IProjectActivity activity)
    {
      var deletingCapasities = new List<IProjectActivityResourcesCapacity>();
      foreach(var capacity in activity.ResourcesCapacity)
      {
        if(!resources.Any(x => x.ResourceId == capacity.ResourceId))
        {
          deletingCapasities.Add(capacity);
        }
      }

      foreach(var deletingCapacity in deletingCapasities)
        activity.ResourcesCapacity.Remove(deletingCapacity);
      
      foreach(var resourceCapacity in resources)
      {
        IProjectActivityResourcesCapacity capacity = new ProjectActivityResourcesCapacity();
        capacity = activity.ResourcesCapacity.Where(x => x.ResourceId == resourceCapacity.ResourceId).FirstOrDefault();
        if (capacity == null)
        {
          capacity = activity.ResourcesCapacity.AddNew();
        }
        ChangePropertyIfDifferentValue(capacity, "ResourceId", resourceCapacity.ResourceId);
        ChangePropertyIfDifferentValue(capacity, "Capacity", resourceCapacity.Value);
      }
    }
    
    private void CheckProjectAndPlanLocksOrThrowException(DirRX.ProjectPlanner.IProjectPlanRX plan, DirRX.ProjectPlanning.IProject linkedProject)
    {
      var planLockMessages = DetectLockOnProjectPlanCard(plan);
      if (!string.IsNullOrEmpty(planLockMessages))
      {
        throw AppliedCodeException.Create(planLockMessages);
      }
      
      var linkedProjectLockMessages = DetectLockOnLinkedProject(linkedProject);
      if (!string.IsNullOrEmpty(linkedProjectLockMessages))
      {
        throw AppliedCodeException.Create(linkedProjectLockMessages);
      }
    }
    
    private void UpdatePlanFromGanttService(DirRX.ProjectPlanner.Structures.Module.IProjectDto projectModel, DirRX.ProjectPlanner.IProjectPlanRX plan)
    {
      if (!plan.AccessRights.CanUpdate())
      {
        throw new Sungero.Domain.Shared.Exceptions.SecuritySystemException(false, Resources.ExceprionMessage);
      }
      plan.Name = projectModel.Name;
      plan.ExecutionPercent = projectModel.ExecutionPercent;
      plan.Note = projectModel.Note;
      plan.FactualCosts = projectModel.FactualCosts;
      plan.ActualWorkload = projectModel.ActualWorkload;
      plan.StartDate = projectModel.StartDate;
      plan.EndDate = projectModel.EndDate;
      plan.BaselineWork = projectModel.BaselineWork;
      plan.PlannedCosts = projectModel.PlannedCosts;
      
       // Если есть запущенные (выполенные) задачи, то дату не меняем. Факт взятия в работу случился.
      if (!Functions.ProjectActivityTask.HasStartedTasks(plan))
      {
        plan.ActualStartDate = plan.StartDate;
      }
      var maxActivityDate = Functions.ProjectActivityTask.MaxActivityDate(plan);
      var maxTaskDate = Functions.ProjectActivityTask.MaxTaskDate(plan);
      plan.ActualFinishDate = MaxDate(maxActivityDate, maxTaskDate) ?? plan.EndDate;
    }
    
    public static DateTime? MaxDate(DateTime? first, DateTime? second)
    {
      if (first.HasValue)
      {
        if (second.HasValue)
        {
          return first > second ? first : second;
        }
        return first;
      }
      return second;
    }
    
    private void RestoreProjectCard(DirRX.ProjectPlanner.Structures.ProjectPlanRX.PlanCardInfoBackup fieldsToRestore, DirRX.ProjectPlanner.IProjectPlanRX plan, int versionNumber)
      {
      // Восстанавливаем значения полей карточки из сохраненного объекта, если редактируемая версия плана не последняя.
      // Это значения из последней версии
      if (versionNumber != plan.LastVersion.Number)
      {
        plan.ExecutionPercent = fieldsToRestore.ExecutionPercent;
        plan.FactualCosts = fieldsToRestore.FactualCosts;
        plan.ActualWorkload = fieldsToRestore.ActualWorkload;
      }
      
      // Восстанавливаем значения полей карточки из сохраненного объекта, eсли есть подписанная версия.
      // Если подписанной версии нет, восстанавливаем из последней
      var lastSignedVersion = PublicFunctions.ProjectPlanRX.GetLatestSignedVersionNumber(plan);
      if (lastSignedVersion != null || versionNumber != plan.LastVersion.Number)
      {
        plan.StartDate = fieldsToRestore.StartDate;
        plan.EndDate = fieldsToRestore.EndDate;
        plan.BaselineWork = fieldsToRestore.BaselineWork;
        plan.PlannedCosts = fieldsToRestore.PlannedCosts;
      }
      plan.Save();
    }
    
    private void DeleteActivitiesFromGanttService(
      DirRX.ProjectPlanner.IProjectPlanRX plan,
      IEnumerable<DirRX.ProjectPlanner.Structures.Module.IActivityDto> modelActivities,
      long lastId,
      int numberVersion
     )
    {
      var appActivityIds = modelActivities.Where(i => i.Id <= lastId).Select(i => i.Id).ToList();
      var activitiesToDelete = ProjectPlanner.ProjectActivities.GetAll()
        .Where(a => a.Id <= lastId && !appActivityIds.Contains(a.Id) && plan == a.ProjectPlan && a.NumberVersion == numberVersion);
      
      DeleteActivities(activitiesToDelete);
    }

    /// <summary>
    /// Прекратить задачи по этапам.
    /// </summary>
    /// <param name="activities">Этапы на удаление.</param>
    private static void AbortProjectActivityTasks(IEnumerable<DirRX.ProjectPlanner.IProjectActivity> activities)
    {
      if (!activities.Any())
      {
        return;
      }
      
      var refIds = activities.Select(x => x.RefId).ToList();
      
      var uniqueRefIds = ProjectActivities.GetAll(pa => refIds.Contains(pa.RefId))
        .GroupBy(pa => pa.RefId)
        .Where(g => g.Count() == 1)
        .Select(g => g.Key)
        .ToList();
      
      var mainTasks = ProjectActivityTasks.GetAll(task => uniqueRefIds.Contains(task.ActivityRefId) &&
                                                  task.Status == ProjectPlanner.ProjectActivityTask.Status.InProcess ||
                                                  task.Status == ProjectPlanner.ProjectActivityTask.Status.UnderReview);
      
      var allAbortTasks = Sungero.Workflow.Tasks.GetAll(task => mainTasks.Contains(task.MainTask) &&
                                                        task.Status != Sungero.Workflow.Task.Status.Aborted &&
                                                        task.Status != Sungero.Workflow.Task.Status.Completed);
      
      
      foreach (var task in allAbortTasks)
      {
        try
        {
          task.Abort();
        }
        catch(Exception ex)
        {
          Logger.Error(Resources.CouldNotStopProjectActivityTaskErrorFormat(task.Id, ex.Message));
        }
      }
      
    }
    
    private static void StopTeamTasksForActivities(List<long> activityIds)
    {
      try
      {
        var projectActivityGuid = ProjectActivity.ClassTypeGuid;
        var tasks =  TeamsCommonAPI.TeamsTasks
          .GetAll(x => x.AttachmentDetails.Any(a => projectActivityGuid == a.AttachmentTypeGuid && activityIds.Contains(a.AttachmentId ?? -1)));
        
        foreach (var task in tasks)
        {
          task.Abort();
        }
      }
      catch(Exception ex)
      {
        var errorMessage = string.Format("Невозможно остановить TeamTasks. Error: {0}", ex.Message + Environment.NewLine + ex.StackTrace);
        Logger.Error(errorMessage);
        throw new Exception(errorMessage, ex);
      }
    }
    
    private void UpdateActivityDataFromGanttService(
      int planNumberVersion,
      DirRX.ProjectPlanner.IProjectActivity activity,
      DirRX.ProjectPlanner.Structures.Module.IActivityDto activityModel,
      List<long> updatedActivitiesForTaskRefIds,
      List<long> changedStartDateActivityRefIds,
      HashSet<long> gateIds)
    {
      ChangePropertyIfDifferentValue(activity, "Name", activityModel.Name);
      ChangePropertyIfDifferentValue(activity, "Number", activityModel.CurrentNumber);
      
      var isChangedStartDate = ChangePropertyIfDifferentValue(activity, "StartDate", activityModel.StartDate);
      var isChangedEndDate = ChangePropertyIfDifferentValue(activity, "EndDate", activityModel.EndDate);
      if ((isChangedStartDate || isChangedEndDate) && activity.RefId.HasValue)
      {
        if (isChangedStartDate)
        {
          changedStartDateActivityRefIds.Add(activity.RefId.Value);
        }
        updatedActivitiesForTaskRefIds.Add(activity.RefId.Value);
      }
      
      // Длительность в рабочих днях.
      ChangePropertyIfDifferentValue(activity, "Duration", ProjectPlanner.Functions.Module.GetWorkingDays(activity).ToString());
      ChangePropertyIfDifferentValue(activity, "BaselineWork", activityModel.BaselineWork);
      ChangePropertyIfDifferentValue(activity, "ActualWorkload", activityModel.ActualWorkload);
      ChangePropertyIfDifferentValue(activity, "ExecutionPercent", activityModel.ExecutionPercent);
      ChangePropertyIfDifferentValue(activity, "Note", activityModel.Note);
      ChangePropertyIfDifferentValue(activity, "SortIndex", activityModel.SortIndex);
      ChangePropertyIfDifferentValue(activity, "Priority", activityModel.Priority);
      ChangePropertyIfDifferentValue(activity, "FactualCosts", activityModel.FactualCosts);
      ChangePropertyIfDifferentValue(activity, "PlannedCosts", activityModel.PlannedCosts);
      ChangePropertyIfDifferentValue(activity, "NumberVersion", planNumberVersion);
      if (!activity.RefId.HasValue || activity.RefId == 0)
      {
        activity.RefId = activity.Id;
        ChangePropertyIfDifferentValue(activity, "RefId", activity.Id);
      }
      
      if (!activityModel.GateId.HasValue || !gateIds.Contains(activityModel.GateId.Value))
      {
        activityModel.GateId = null;
        ChangePropertyIfDifferentValue(activity, "GateId", null);
      }
    }
    
    private static bool ChangePropertyIfDifferentValue(object propertySource, string propertyName, object newValue)
    {
      var isValueChanged = false;
       var previousValue = propertySource.GetType().GetProperty(propertyName).GetValue(propertySource);
       
       if (newValue == previousValue)
       {
        return isValueChanged;
       }
       
       if (newValue is System.DateTime && previousValue is System.DateTime)
       {
         var castedNewValue = newValue as System.Nullable<System.DateTime>;
         var castedPreviousValue = previousValue as System.Nullable<System.DateTime>;
         if (castedNewValue != null && castedPreviousValue != null && castedNewValue.Value.Date == castedPreviousValue.Value.Date)
         {
          return isValueChanged;
         }
       }
       
       if ((previousValue == null && newValue != null) || (newValue == null && previousValue != null) || !previousValue.Equals(newValue))
       {
         propertySource.GetType().GetProperty(propertyName).SetValue(propertySource, newValue);
        isValueChanged = true;
       }
      
      return isValueChanged;
    }
    
    /// <summary>
    /// Обновить записи этапов в бд с модели Гантта.
    /// </summary>
    /// <param name="activities">Этапы с модели.</param>
    /// <param name="lastId">Последний достоверный id этапа в структуре.</param>
    /// <param name="planNumberVersion">Версия редактируемого документа.</param>
    /// <param name="plan">Редактируемый план.</param>
    /// <param name="updatedActivityIds">Пустая коллекция, которую наполняем в этом методе. Id этапов, которые мы изменили.</param>
    /// <param name="leadActivities">Пустая коллекция, которую наполняем в этом методе. Созданные ведущие этапы.</param>
    /// <returns>Task</returns>
    private System.Func<System.Threading.Tasks.Task> UpdateActivitiesFromGanttService(
      DirRX.ProjectPlanner.Structures.Module.ActivityDto[] activities,
      long lastId,
      int planNumberVersion,
      DirRX.ProjectPlanner.IProjectPlanRX plan,
      List<DirRX.ProjectPlanner.Structures.Module.IdActivityMapper> updatedActivityIds,
      List<DirRX.ProjectPlanner.Structures.Module.LeadActivities> leadActivities,
      List<long> updatedActivitiesForTaskRefIds,
      List<long> changedResponsibleProjectActivityRefIds,
      List<long> changedStartDateActivityRefIds,
      List<IEmployee> changedResponsibles
     )
    {
      var activitiesForBatchSaving = new List<DirRX.ProjectPlanner.IProjectActivity>(activities.Length);
      var activityResourcesForDeleting = new List<long>();
      var activityModelDict = new Dictionary<DirRX.ProjectPlanner.Structures.Module.ActivityDto, long>();
      var notNullActivities = activities.Where(a => a != null).ToList();
      
      var gateIds = notNullActivities.Where(a => a.GateId.HasValue && a.GateId > 0).Select(a => a.GateId.Value);
      ValidateGatesDistinctOrThrowException(gateIds);
      using (var connection = CreateDBConnection())
      {
        var notNullActivitiesIds = notNullActivities.Select(act => act.Id).ToList();
        var existsActivities = ProjectPlanner.ProjectActivities.GetAll(a => notNullActivitiesIds.Contains(a.Id)).ToList();
        foreach (var activityModel in notNullActivities)
        {
          DirRX.ProjectPlanner.IProjectActivity activity;
          //TODO URMANOV_AR: Get запрос в foreach. - надо исправить в будущем.
          var responsible = activityModel.ResponsibleId.HasValue ? Employees.Get(activityModel.ResponsibleId.Value) : null;
          
          // Создать новый этап.
          if (activityModel.Id > lastId)
          {
            activity = ProjectPlanner.ProjectActivities.Create();
            activity.ProjectPlan = plan;
            
            //HACK Kiselev_EM Синхронизируем значения с клиента у новых этапов, иначе UpdateProjectActivityTasksAsync
            //подхватывает их как измененные и выдаем ответственному права на план.
            activity.StartDate = activityModel.StartDate;
            activity.Responsible = responsible;
            
            if (responsible != null)
            {
              changedResponsibles.Add(responsible);
            }
            
            var idActivity = Structures.Module.IdActivityMapper.Create(activityModel.Id, activity.Id);
            
            updatedActivityIds.Add(idActivity);
            activityModel.Id = idActivity.Id;
            
            activity.TypeActivity = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task;
          }
          else
          {
            activity = existsActivities.FirstOrDefault(a => a.Id == activityModel.Id);
          }
          
          UpdateActivityDataFromGanttService(planNumberVersion, activity, activityModel, updatedActivitiesForTaskRefIds, changedStartDateActivityRefIds, ActualizeGateIds(gateIds));
          if (activityModel.Status != null)
          {
            ChangePropertyIfDifferentValue(activity, "Status", GetActualActivityStatus(activityModel.Status.EnumValue));
          }
          //TODO URMANOV_AR: Поиск статуса через foreach - надо исправить в будущем.
          foreach (var type in activity.TypeActivityAllowedItems.Where(t => t.ToString() == activityModel.TypeActivity))
          {
            var isChangedTypeActivity = ChangePropertyIfDifferentValue(activity, "TypeActivity", type);
            
            if (isChangedTypeActivity)
              updatedActivitiesForTaskRefIds.Add(activity.RefId.Value);
            
            if (type.Equals(DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone))
            {
              ChangePropertyIfDifferentValue(activity, "GateId", activityModel.GateId);
            }
          }
          
          if (ChangePropertyIfDifferentValue(activity, "Responsible", responsible))
          {
            changedResponsibleProjectActivityRefIds.Add(activity.RefId.Value);
            updatedActivitiesForTaskRefIds.Add(activity.RefId.Value);
            changedResponsibles.Add(responsible);
          }
          // Подобрать ведущий этап.
          if (activityModel.LeadActivityId.HasValue)
          {
            if (activityModel.LeadActivityId.Value <= lastId)
            {
              ChangePropertyIfDifferentValue(activity, "LeadingActivity", ProjectPlanner.ProjectActivities.Get(activityModel.LeadActivityId.Value));
            }
            else
            {
              var leadActivity = Structures.Module.LeadActivities.Create(activity, activityModel.LeadActivityId.Value);
              leadActivities.Add(leadActivity);
            }
          }
          else
          {
            // Очистить ведущий этап.
            ChangePropertyIfDifferentValue(activity, "LeadingActivity", null);
          }
          RefreshCapacityInfo(activityModel.Resources.Select(r => new DirRX.Planner.Model.ResourcesWorkload() 
                                                           {
                                                             ResourceId = r.ResourceId,
                                                             Value = r.Value
                                                           }).ToList(), activity);
          
          UpdateActivityAttachments(activity, activityModel.Attachments);
          if (activity.State.IsChanged || activity.State.IsInserted)
          {
            activitiesForBatchSaving.Add(activity);
            activityResourcesForDeleting.Add(activity.Id);
            activityModelDict.Add(activityModel, activity.Id);
          }
        }
        
        BatchSave(activitiesForBatchSaving.Where(a => a.State.IsChanged));
        RemoveActivityResources(activityResourcesForDeleting, connection);
        foreach (var activityModel in activityModelDict.Keys)
        {
          var newactivityId = activityModelDict[activityModel as DirRX.ProjectPlanner.Structures.Module.ActivityDto];
          SaveResources(
            activityModel.Resources.Select(r => new DirRX.Planner.Model.ResourcesWorkload()
                                           {
                                             ResourceId = r.ResourceId,
                                             Value = r.Value
                                           }).ToList(),
            newactivityId,
            activityModel.StartDate,
            activityModel.EndDate.Value,
            connection);
        }
      }
      return null;
    }
    private static void UpdateTasks(List<long> allUpdatedActivitiesForTaskRefIds,
                                    List<long> changedResponsibleActivityRefIds,
                                    List<long> changedStartDateActivityRefIds,
                                    long projectPlanId,
                                    int numberVersion)
    {
      var asyncHandler = DirRX.ProjectPlanner.AsyncHandlers.UpdateProjectActivityTasksAsync.Create();
      asyncHandler.ListAllUpdatedActivityRefIds = string.Join(";", allUpdatedActivitiesForTaskRefIds);
      asyncHandler.ListChangedResponsibleActivityRefIds = string.Join(";", changedResponsibleActivityRefIds);
      asyncHandler.ListChangedStartDateActivityRefIds = string.Join(";", changedStartDateActivityRefIds);
      asyncHandler.NumberVersion = numberVersion;
      asyncHandler.ProjectPlanId = projectPlanId;
      asyncHandler.PlanSaveTime = Calendar.Now;
      asyncHandler.ExecuteAsync();
    }
    
    private static Enumeration GetActualActivityStatus(string oldStatus)
    {
      if (oldStatus == DirRX.ProjectPlanner.ProjectActivity.Status.Active.Value)
        return DirRX.ProjectPlanner.ProjectActivity.Status.Active;
      
      if (oldStatus == DirRX.ProjectPlanner.ProjectActivity.Status.InWork.Value)
        return DirRX.ProjectPlanner.ProjectActivity.Status.InWork;
      
      return DirRX.ProjectPlanner.ProjectActivity.Status.Closed;
    }
    
    private static HashSet<long> ActualizeGateIds(IEnumerable<long> gateIds)
    {
      var actualizedGateIds = new HashSet<long>(Gates.GetAll(g => gateIds.Contains(g.Id)).Select(t => t.Id));
      actualizedGateIds.IntersectWith(gateIds);
      
      return actualizedGateIds;
    }
    
    private static void UpdateActivityAttachments(IProjectActivity activity,
      List<DirRX.ProjectPlanner.Structures.Module.IAttachmentDto> sourceAttachments)
    {
      DeleteRemovedAttachments(activity, sourceAttachments);
      AddNewAttachments(activity, sourceAttachments);
    }
    
    private static void DeleteRemovedAttachments(IProjectActivity activity,
      List<DirRX.ProjectPlanner.Structures.Module.IAttachmentDto> sourceAttachments)
    {
      // Нет смысла использовать другие структуры данных, потому что в наборе вряд ли будет больше 7-10 элементов,
      // а вероятнее 0 или 2-3. Такое перебором может быть быстрее, чем HashSet
      var sourceAttachmentIds = sourceAttachments.Where(x => x.AttachmentId.HasValue)
        .Select(x => x.AttachmentId.Value);
      var activityAttachmentsForRemove = activity.Attachments.Where(x => x.AttachmentId.HasValue && !sourceAttachmentIds.Contains(x.AttachmentId.Value)).ToList();
      foreach (var attachment in activityAttachmentsForRemove)
      {
        activity.Attachments.Remove(attachment);
      }
    }
    
    private static void AddNewAttachments(IProjectActivity activity,
      List<DirRX.ProjectPlanner.Structures.Module.IAttachmentDto> sourceAttachments)
    {
      foreach (var sourceAttachment in sourceAttachments)
      {
        if (!sourceAttachment.AttachmentId.HasValue)
        {
          var newActivityAttachment = activity.Attachments.AddNew();
          ChangePropertyIfDifferentValue(newActivityAttachment, "AttachmentId", newActivityAttachment.Id);
          ChangePropertyIfDifferentValue(newActivityAttachment, "Url", sourceAttachment.Url);
          ChangePropertyIfDifferentValue(newActivityAttachment, "Name", sourceAttachment.Name);
          
          continue;
        }
        
        var targetAttachment = activity.Attachments.Where(x => x.AttachmentId == sourceAttachment.AttachmentId.Value).FirstOrDefault();
        if (targetAttachment == null)
        {
          continue;
        }
        
        ChangePropertyIfDifferentValue(targetAttachment, "Url", sourceAttachment.Url);
        ChangePropertyIfDifferentValue(targetAttachment, "Name", sourceAttachment.Name);
      }
    }
    
    /// <summary>
    /// Получить модель ПП из модели Dto.
    /// </summary>
    /// <returns>Модель ПП.</returns>
    public DirRX.Planner.Model.Model GetModelFromGanttDto(DirRX.ProjectPlanner.Structures.Module.IGanttProjectPlanDto ganttModelDto)
    {
      var model = new DirRX.Planner.Model.Model();
      
      model.Activities = ganttModelDto.Activities.Select(x => new DirRX.Planner.Model.Activity()
                                                         {
                                                           Id = x.Id,
                                                           RefId = x.RefId,
                                                           Name = x.Name,
                                                           CurrentNumber = x.CurrentNumber,
                                                           Note = x.Note,
                                                           LeadActivityId = x.LeadActivityId,
                                                           ResponsibleId = x.ResponsibleId,
                                                           StartDate = x.StartDate,
                                                           EndDate = x.EndDate,
                                                           BaselineWork = x.BaselineWork,
                                                           ActualWorkload = x.ActualWorkload,
                                                           ExecutionPercent = x.ExecutionPercent,
                                                           Predecessors = x.Predecessors.Select(pr => new DirRX.Planner.Model.Predecessor()
                                                                                                {
                                                                                                  Id = pr.Id,
                                                                                                  LinkType = pr.LinkType,
                                                                                                  Lag = pr.Lag
                                                                                                }
                                                                                               ).ToList(),
                                                           SubmittedTasks = x.SubmittedTasks.Select(st => new DirRX.Planner.Model.Task()
                                                                                                    {
                                                                                                      Id = st.Id,
                                                                                                      DisplayValue = st.DisplayValue,
                                                                                                      HyperLink = st.HyperLink,
                                                                                                      Deadline = st.Deadline
                                                                                                    }
                                                                                                   ).ToList(),
                                                           UnfinishedTasks = x.UnfinishedTasks.Select(ut => new DirRX.Planner.Model.Task()
                                                                                                      {
                                                                                                        Id = ut.Id,
                                                                                                        DisplayValue = ut.DisplayValue,
                                                                                                        HyperLink = ut.HyperLink,
                                                                                                        Deadline = ut.Deadline
                                                                                                      }
                                                                                                     ).ToList(),
                                                           SortIndex = x.SortIndex,
                                                           TypeActivity = x.TypeActivity,
                                                           PlannedCosts = x.PlannedCosts,
                                                           FactualCosts = x.FactualCosts,
                                                           Priority = x.Priority,
                                                           Status = new DirRX.Planner.Model.ActivityStatus()
                                                                                           {
                                                                                             EnumValue = x.Status.EnumValue,
                                                                                             LocalizeValue = x.Status.LocalizeValue
                                                                                           },
                                                           Resources = x.Resources.Select(r => new DirRX.Planner.Model.ResourcesWorkload()
                                                                                          {
                                                                                            ResourceId = r.ResourceId,
                                                                                            Value = r.Value
                                                                                          }).ToList(),
                                                           Attachments = x.Attachments.Select(attachmentDto => new DirRX.Planner.Model.Attachment()
                                                                                              {
                                                                                                AttachmentId = attachmentDto.AttachmentId,
                                                                                                Name = attachmentDto.Name,
                                                                                                Url = attachmentDto.Url
                                                                                              }).ToList()
                                                         }
                                                        ).ToList();
      
      model.LastActivityId = ganttModelDto.LastActivityId;
      model.NumberVersion = ganttModelDto.NumberVersion;
      
      if(ganttModelDto.Project != null)
      {
        model.Project = new DirRX.Planner.Model.Project()
                            {
                              Id = ganttModelDto.Project.Id,
                              Name = ganttModelDto.Project.Name,
                              ManagerId = ganttModelDto.Project.ManagerId,
                              StartDate = ganttModelDto.Project.StartDate,
                              EndDate = ganttModelDto.Project.EndDate,
                              BaselineWork = ganttModelDto.Project.BaselineWork,
                              ExecutionPercent = ganttModelDto.Project.ExecutionPercent,
                              Note = ganttModelDto.Project.Note,
                              PlannedCosts = ganttModelDto.Project.PlannedCosts,
                              FactualCosts = ganttModelDto.Project.FactualCosts
                            };
      }
      
      if(ganttModelDto.ResourcesData != null)
      {
        model.ResourcesData = new DirRX.Planner.Model.ResourcesData()
        {
          ResourceTypes = ganttModelDto.ResourcesData.ResourceTypes.Select(rt => new DirRX.Planner.Model.ResourceTypes()
                                                                           {
                                                                             Id = rt.Id,
                                                                             Name = rt.Name,
                                                                             SectionName = rt.SectionName
                                                                           }).ToList(),
          
          Users = ganttModelDto.ResourcesData.Users.Select(u => new DirRX.Planner.Model.Users()
                                                           {
                                                             Id = u.Id,
                                                             Name = u.Name,
                                                             Surname = u.Surname,
                                                             Position = u.Position
                                                           }).ToList(),
          
          MaterialResources = ganttModelDto.ResourcesData.MaterialResources.Select(mr => new DirRX.Planner.Model.MaterialResources()
                                                                                   {
                                                                                     Id = mr.Id,
                                                                                     Name = mr.Name
                                                                                   }).ToList(),
          
          Resources = ganttModelDto.ResourcesData.Resources.Select(r => new DirRX.Planner.Model.Resources()
                                                                   {
                                                                     Id = r.Id,
                                                                     EntityTypeId = r.EntityTypeId,
                                                                     EntityId = r.EntityId,
                                                                     UnitLabel = r.UnitLabel
                                                                   }).ToList(),
          
          Capacity = ganttModelDto.ResourcesData.Capacity.Select(c => new DirRX.Planner.Model.Capacity()
                                                                 {
                                                                   ResourceId = c.ResourceId,
                                                                   Values = c.Values.Select(v => new DirRX.Planner.Model.Value()
                                                                                            {
                                                                                              Date = v.Date,
                                                                                              Busy = v.Busy
                                                                                            }).ToList()
                                                                 }).ToList(),
          
          WorkingTimeCalendars = ganttModelDto.ResourcesData.WorkingTimeCalendars.Select(wtc => new DirRX.Planner.Model.WorkingTimeCalendar()
                                                                                         {
                                                                                           ResourcesIds = wtc.ResourcesIds,
                                                                                           FreeDays = wtc.FreeDays,
                                                                                           WorkDays = wtc.WorkDays.Select(wd => new DirRX.Planner.Model.WorkDays()
                                                                                                                          {
                                                                                                                            Date = wd.Date,
                                                                                                                            Duration = wd.Duration
                                                                                                                          }).ToList()
                                                                                         }).ToList(),
          
        };
      }
      
      return model;
    }
    
    /// <summary>
    /// Сохраняет изменения по проекту и этапам в новую версию.
    /// </summary>
    /// <param name="model">Dto - модель плана проекта.</param>
    /// <returns>Номер новой версии</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public int SaveModelFromGanttServiceNewVersion(DirRX.ProjectPlanner.Structures.Module.IGanttProjectPlanDto model)
    {
      try
      {
        var plan = ProjectPlanRXes.GetAll(x => x.Id == model.ProjectPlanId).SingleOrDefault();
        if(plan == null)
        {
          throw new Exception($"не удалось получить план проекта с id={model.ProjectPlanId}");
        }
        
        ValidateActivityRefIdOrThrowException(model.Activities);
        
        var activityGateLinks = model.Activities.ToDictionary(a => a.Id, a => a.GateId);
        ValidateActivityGateLinksOrThrowException(activityGateLinks);
        
        var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(plan, x.ProjectPlanDirRX)).FirstOrDefault();
        CheckProjectAndPlanLocksOrThrowException(plan, linkedProject);
        
        var newVersion = plan.Versions.AddNew();
        newVersion.AssociatedApplication = Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").First();
        plan.Save();
  
        var newVersionNumber = newVersion.Number.Value;
        var projectModel = model.Project;
        
        var cardDataBu = Structures.ProjectPlanRX.PlanCardInfoBackup.Create(
          plan.ExecutionPercent,
          plan.FactualCosts,
          plan.ActualWorkload,
          plan.BaselineWork,
          plan.PlannedCosts,
          plan.StartDate,
          plan.EndDate
         );
  
        var ganttModel = GetModelFromGanttDto(model);
        SaveModel(ganttModel, plan, newVersionNumber, true, false, activityGateLinks);
        UpdatePlanFromGanttService(projectModel, plan);
        AddModifiedPlanIdInDB(plan.Id);
        RestoreProjectCard(cardDataBu, plan, newVersionNumber);
        return newVersionNumber;
      }
      catch (Sungero.Core.AppliedCodeException)
      {
        throw;
      }
      catch (Exception e)
      {
        Logger.Error(e.Message, e);
        throw Sungero.Core.AppliedCodeException.Create(string.Empty);
      }
    }
    
    /// <summary>
    /// Сохраняет изменения по проекту и этапам.
    /// </summary>
    /// <param name="model">Dto - модель плана проекта.</param>
    [Remote, Public(WebApiRequestType = RequestType.Post)]
    public void SaveModelFromGanttService(DirRX.ProjectPlanner.Structures.Module.IGanttProjectPlanDto model)
    {
      try
      {
        var allUpdatedActivitiesForTaskRefIds = new List<long>();
        var changedResponsibleProjectActivityRefIds = new List<long>();
        var changedStartDateActivityRefIds = new List<long>();
        var changedResponsibles = new List<IEmployee>();
        
        //1.найти проект и засинхронить с тем что пришло с веба.
        var projectModel = model.Project;
        var plan = ProjectPlanRXes.Get(model.ProjectPlanId);
        
        var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(plan, x.ProjectPlanDirRX)).FirstOrDefault();
        
        ValidateActivityRefIdOrThrowException(model.Activities);
        CheckProjectAndPlanLocksOrThrowException(plan, linkedProject);

        var cardDataBu = Structures.ProjectPlanRX.PlanCardInfoBackup.Create(
          plan.ExecutionPercent,
          plan.FactualCosts,
          plan.ActualWorkload,
          plan.BaselineWork,
          plan.PlannedCosts,
          plan.StartDate,
          plan.EndDate
         );

        var updatedActivityIds = new List<Structures.Module.IdActivityMapper>();
        var leadActivities = new List<Structures.Module.LeadActivities>();
        
        //Zheleznov_AV надо обновить все этапы. Операция обновления одного этапа стоит дорого. Поэтому будем сохранять все этапы за раз.
        var activitiesForBatchSaving = new List<DirRX.ProjectPlanner.IProjectActivity>(model.Activities.Count);;
        Sungero.Domain.ParallelHelper.TaskRun(
          UpdateActivitiesFromGanttService(
            model.Activities.Select(a => (DirRX.ProjectPlanner.Structures.Module.ActivityDto)a).ToArray(),
            model.LastActivityId,
            model.NumberVersion,
            plan,
            updatedActivityIds,
            leadActivities,
            allUpdatedActivitiesForTaskRefIds,
            changedResponsibleProjectActivityRefIds,
            changedStartDateActivityRefIds,
            changedResponsibles
           )
         );
        
        // Дозаполнить ведущие этапы.
        foreach (var actStructure in leadActivities)
        {
          var updatedIdItem = updatedActivityIds.FirstOrDefault(a => a.ModelActivityId == actStructure.LeadActivityId);
          if (updatedIdItem == null)
          {
            throw new Exception(string.Format("не удалось найти обновленный id для активити с id={0}", actStructure.LeadActivityId));
          }
          ChangePropertyIfDifferentValue(actStructure.Activity, "LeadingActivity", ProjectPlanner.ProjectActivities.Get(updatedIdItem.Id));
          if (actStructure.Activity.State.IsChanged)
          {
            activitiesForBatchSaving.Add(actStructure.Activity);
          }
        }

        BatchSave(activitiesForBatchSaving.Where(a => a.State.IsChanged));
        activitiesForBatchSaving.Clear();

        // Обновить предшественников для этапов и ведущие этапы для сохраняемой в Storage модели.
        this.UpdateActivityRelation(model.Activities, updatedActivityIds, model.LastActivityId);
        DeleteActivitiesFromGanttService(plan, model.Activities, model.LastActivityId, model.NumberVersion);
        
        UpdatePlanFromGanttService(projectModel, plan);
        // Если у плана есть связанный проект, то добавляем ответсвенного в участники. 
        // Затем ФП выдает права на план и проект участникам. Если проекта нет, то напрямую выдем права на план.
        // TODO: Выдача прав считается долгой операцией, поэтому нужно перенести ее в ФП или АО.
        GrantRightsPlanOrUpdateTeamMembers(plan, linkedProject, changedResponsibles);
        plan.Save();
        
        WriteJsonBodyToProjectVersion(plan, model.NumberVersion, false);
        
        //После заполнения тела документа вернуть в карточку актуальные значения.
        AddModifiedPlanIdInDB(plan.Id);
        RestoreProjectCard(cardDataBu, plan, model.NumberVersion);
        
        if (model.NumberVersion == plan.LastVersion.Number)
        {
          UpdateTasks(allUpdatedActivitiesForTaskRefIds, changedResponsibleProjectActivityRefIds, changedStartDateActivityRefIds, plan.Id, plan.LastVersion.Number.Value);
        }
      }
      catch (Sungero.Core.AppliedCodeException)
      {
        throw;
      }
      catch (Exception e)
      {
        Logger.Error(e.Message, e);
        throw Sungero.Core.AppliedCodeException.Create(string.Empty);
      }
    }

    private void ValidateActivityRefIdOrThrowException(List<Structures.Module.IActivityDto> activities)
    {
      var uniqueRefIds = new HashSet<long>();
      
      foreach (var activity in activities)
      {
        if (activity.RefId.HasValue && activity.RefId.Value != 0 && !uniqueRefIds.Add(activity.RefId.Value))
        {
          throw new Exception($"Duplicates with RefId = {activity.RefId.Value} have been found for the stage with Id = {activity.Id}. " +
            "Each RefId in the collection must be unique.");
        }
      }
    }
    
    
    #region Методы по работе с таблицей DirRX_PP_Modified_Plans 
    public void AddModifiedPlanIdInDB(long planId)
    {
      using (var connection = CreateDBConnection())
        using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.InsertPlanIdInModifiedPlans;
        SQL.AddParameter(command, "@planId", planId, System.Data.DbType.Int64);
        command.ExecuteNonQuery();
      }
    }
    
    public System.Collections.Generic.IDictionary<long, long?> GetModifiedPlanProjectIdsFromDB()
    {
      var planProjectIds = new Dictionary<long, long?>();
      
      using (var connection = CreateDBConnection())
        using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetModifiedPlanProjectIds;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var planId = reader.GetInt64(0);
            var projectId = !reader.IsDBNull(1) ? (Nullable<long>)reader.GetInt64(1) : null;
            if (!planProjectIds.ContainsKey(planId))
            {
              planProjectIds.Add(planId, projectId);
            }
          }
        }
      }
      
      return planProjectIds;
    }
    
    public void DeleteModifiedPlanIdsFromDB(System.Collections.Generic.IEnumerable<long> planIds)
    {
      if (!planIds.Any())
      {
        return;
      }
      
      using (var connection = CreateDBConnection())
        using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.DeleteModifiedPlanIds;
        SQL.AddArrayParameter(command, "@planIds", planIds, System.Data.DbType.Int64);
        command.ExecuteNonQuery();
      }
    }
    #endregion
    
    /// <summary>
    /// Выдать права на план или обновить участников связанного проекта.
    /// </summary>
    /// <param name="plan">План.</param>
    /// <param name="project">Связанный проект.</param>
    /// <param name="responsibles">Ответсвенные.</param>
    /// <remarks>Сейчас есть ФП, который выдет права участникам проекта</remarks>
    private void GrantRightsPlanOrUpdateTeamMembers(IProjectPlanRX plan, IProject project, List<IEmployee> responsibles)
    {
      var distictChangedResponsibles = responsibles.Distinct();
      
      if (project == null)
      {
        GrantRightsPlan(plan, distictChangedResponsibles);
      }
      else
      {
        UpdateProjectTeamMembers(project, distictChangedResponsibles);
      }
    }
    
    private void GrantRightsPlan(IProjectPlanRX plan, IEnumerable<IEmployee> responsibles)
    {
      foreach (var responsible in responsibles.Where(r => r != null))
      {
        if (!plan.AccessRights.CanRead(responsible))
        {
          plan.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Read);
        }
      }
    }
    
    private void UpdateProjectTeamMembers(IProject project, IEnumerable<IEmployee> responsibles)
    {
      var teamMemberSet = new HashSet<long>(project.TeamMembers.Where(tm => tm.Member != null).Select(x => x.Member.Id));
      
      foreach (var responsible in responsibles.Where(r => r != null))
      {
        if (!teamMemberSet.Contains(responsible.Id))
        {
          var teamMemberRef = project.TeamMembers.AddNew();
          teamMemberRef.Member = responsible;
          teamMemberRef.Group = DirRX.ProjectPlanning.ProjectCoreTeamMembers.Group.Change;
        }
      }
      
      project.Save();
    }
    
    private void UpdateActivityRelation(List<DirRX.ProjectPlanner.Structures.Module.IActivityDto> sourceActivities,
      List<DirRX.ProjectPlanner.Structures.Module.IdActivityMapper> updatedActivityIds,
      long lastActivityId)
    {
      var activitiesForBatchSaving = new List<DirRX.ProjectPlanner.IProjectActivity>(sourceActivities.Count);
      
      foreach (var sourceActivity in sourceActivities)
      {
        var targetActivity = ProjectPlanner.ProjectActivities.Get(sourceActivity.Id);
        
        this.UpdatePredecessor(sourceActivity, targetActivity, updatedActivityIds, lastActivityId);
        this.UpdateLeadActivity(sourceActivity, updatedActivityIds, lastActivityId);
        
        activitiesForBatchSaving.Add(targetActivity);
      }
      
      BatchSave(activitiesForBatchSaving.Where(a => a.State.IsChanged));
    }
    
    private void UpdatePredecessor(DirRX.ProjectPlanner.Structures.Module.IActivityDto sourceActivity,
      DirRX.ProjectPlanner.IProjectActivity targetActivity,
      List<DirRX.ProjectPlanner.Structures.Module.IdActivityMapper> updatedActivityIds,
      long lastActivityId)
    {
      if (sourceActivity.Predecessors != null && sourceActivity.Predecessors.Count > 0)
      {
        targetActivity.Predecessors.Clear();
        
        foreach (var predecessor in sourceActivity.Predecessors)
        {
          long predecessorId = predecessor.Id.Value;
          
          if (predecessor.Id.Value > lastActivityId)
          {
            var updatedPredeccessorId = updatedActivityIds.FirstOrDefault(x => x.ModelActivityId == predecessor.Id);
            if (updatedPredeccessorId == null)
            {
              throw new Exception(DirRX.ProjectPlanner.Resources.FailToFindUpdatedIdExceptionTextFormat(predecessor.Id));
            }
            predecessorId = updatedPredeccessorId.Id;
          }
          
          var activityPredecessor  = targetActivity.Predecessors.AddNew();
          activityPredecessor.Activity = ProjectPlanner.ProjectActivities.Get(predecessorId);
          activityPredecessor.LinkType = predecessor.LinkType;
          activityPredecessor.Lag = predecessor.Lag;
          predecessor.Id = targetActivity.Id;
        }
      }
      else
      {
        targetActivity.Predecessors.Clear();
      }
    }
    
    /// <summary>
    /// Массовое сохранение сущностей в отдельной сессии БД.
    /// </summary>
    /// <param name="activities">Сущности для сохранения.</param>
    private static void BatchSave(System.Collections.Generic.IEnumerable<object> entities)
    {
      if (!entities.Any())
        return;
      
      using (var session = Sungero.Domain.Session.CreateIndependentSession())
      {
        foreach (var entity in entities)
        {
          session.Update(entity);
        }
        
        session.SubmitChanges();
      }
    }
    
    private static List<long> GetProjectResources(long projectPlanId, int numberVersion)
    {
      using (var connection = CreateDBConnection())
      {
        var resourcesList = new List<long>();
         
        using (var command = connection.CreateCommand())
        {
          //получить ресурсы по активити
          command.CommandText = "select distinct resource_id from ResourceLinks rl "+
                                "join DirRX_Projec1_PrjctActivity activities on rl.project_activity_id = activities.id " +
                                "where (activities.ProjectPlan = @projectPlanId AND activities.NumberVersion = @VersionNum)";
          SQL.AddParameter(command, "@projectPlanId", projectPlanId, System.Data.DbType.Int64);
          SQL.AddParameter(command, "@VersionNum", numberVersion, System.Data.DbType.Int32);
          using (var reader = command.ExecuteReader())
          while (reader.Read())
          {
            long val;
            if (long.TryParse(reader[0].ToString(), out val))
            {
              resourcesList.Add(val);            
            }
          }
        }
        return resourcesList;
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static Structures.Module.IResourcesData HandleUpdateResourcesDataDto(long projectPlanId, int numberVersion)
    {
      var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
      if (projectPlan == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.NonExistentPlanIdExeptionFormat(projectPlanId));
      }
      
      var planActivities = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, projectPlan) && a.NumberVersion.Value == numberVersion).ToList();
      var planDates = GetProjectPlanDates(projectPlan, planActivities);
      
      //users и resources в dictionary
      
      var users = new Dictionary<long, Structures.Module.IUser>();
      var projectManager = TryGetProjectManager(projectPlan);
      if (projectManager != null)
      {
        users.Add(projectManager.Id, projectManager);
      }
      
      var resources = new Dictionary<long, Structures.Module.IResource>();
      var resourceIdList = GetProjectResources(projectPlanId, numberVersion);
      
      FillUsersFromResponsibles(planActivities, users);
      AddEmployeeResources(resourceIdList, users, resources);
      return new Structures.Module.ResourcesData
      {
        Users = users.Values.ToList(),
        Resources = resources.Values.ToList(),
        Capacity = GetCapacity(resourceIdList, planDates.Start, planDates.End, projectPlanId, numberVersion),
        MaterialResources = GetMaterialResources(),
        ResourceTypes = GetResourcesTypes(),
        WorkingTimeCalendars = GetWorkingTimeCalendars(resourceIdList, planDates.Start, planDates.End)
      };
    }
    
    /// <summary>
    /// Выбирает актуальные даты начала и окончания плана проекта. В карточке плана находятся даты для последней версии плана.
    /// Если надо открыть не последнюю версию плана, то актуальные даты могут отличаться. Поэтому дополнительно ищем даты в списке активити.
    /// </summary>
    /// <param name="projectPlan">План проекта</param>
    /// <param name="activities">Список активити</param>
    /// <returns>Структура с актуальными датами начала и окончания плана</returns>
    private static Structures.Module.ProjectPlanDates GetProjectPlanDates(DirRX.ProjectPlanner.IProjectPlanRX projectPlan, List<DirRX.ProjectPlanner.IProjectActivity> activities)
    {
      var startDateFromCard = Convert.ToDateTime(projectPlan.StartDate);
      //Kiselev HACK в карточке, для удобства отображения, endDate уменьшается на день. Добавляем к endDate день.
      var endDateFromCard = Convert.ToDateTime(projectPlan.EndDate).AddDays(1);
      
      if (startDateFromCard >= endDateFromCard)
      {
        endDateFromCard = startDateFromCard.AddDays(1);
      }
      
      var startDates = new List<DateTime>{startDateFromCard};
      var endDates = new List<DateTime>{endDateFromCard};
      
      startDates.AddRange(activities.Where(a => a.StartDate.HasValue).Select(b => b.StartDate.Value));
      endDates.AddRange(activities.Where(a => a.EndDate.HasValue).Select(b => b.EndDate.Value));
      
      return new Structures.Module.ProjectPlanDates
      {
        Start = startDates.Min(),
        End = endDates.Max()
      };
    }
    
    /// <summary>
    /// Получаем выходные дни - исключения (выходные в ПН-ПТ).
    /// </summary>
    /// <param name="calendar">Календарь рабочего времени.</param>
    /// <returns>Дополнительные выходные дни.</returns>
    private static List<DateTime> GetExtraFreeDays(IEnumerable<Sungero.CoreEntities.IWorkingTimeCalendar> calendars)
    {
      //Zheleznov_AV HACK предварительная оптимизация работы с календарями. При получении дат нам надо проверять является ли день рабочим,
      //с учетом частного календаря. Раньше мы использовали метод `d.Day.IsWorkingDay(employee)`. Но это было медленно. Сейчас мы смотрим на d.Duration .
      //
      //Скорее всего есть способ дальнейшей оптимизации. Вроде того, что бы сразу из БД получать нужные данные, a не фильтровать их на уровни прикладной.
      //
      //Данный hack используется еще в методах GetExtraWorkingDays и GetCapacity.
      return calendars.SelectMany(c => c.Day).Where(d =>
                                (d.Day.DayOfWeek != DayOfWeek.Saturday && d.Day.DayOfWeek != DayOfWeek.Sunday)
                                && d.Duration == 0)
        .Select(d => d.Day)
        .ToList();
    }
    
    /// <summary>
    /// Получаем рабочие дни - исключения (рабочие в СБ-ВС).
    /// Если суббота или воскресенье становится рабочим днем, то считаем что продолжительность для будет 8 часов.
    /// </summary>
    /// <param name="calendar">Календарь рабочего времени.</param>
    /// <returns>Дополнительные рабочие дни.</returns>
    private static List<Structures.Module.IWorkDay> GetExtraWorkingDays(IEnumerable<Sungero.CoreEntities.IWorkingTimeCalendar> calendars)
    {
      const int defaultWorkDayDuration = 8;
      return calendars.SelectMany(c => c.Day).Where(d => (d.Day.DayOfWeek == DayOfWeek.Saturday || d.Day.DayOfWeek == DayOfWeek.Sunday) && d.Duration > 0)
        .Select(d => new Structures.Module.WorkDay
                {
                  Date = d.Day,
                  Duration = defaultWorkDayDuration
                } as Structures.Module.IWorkDay)
        .ToList();
    }
    
    #region методы, вынесенные из GetModel()
    
    /// <summary>
    /// Создает модель проекта
    /// </summary>
    /// <param name="projectPlan">Проект</param>
    /// <returns>Модель</returns>
    private static DirRX.Planner.Model.Project CreateProjectAppModel(IProjectPlanRX projectPlan)
    {
      var projectApp = new DirRX.Planner.Model.Project();
      projectApp.Id = projectPlan.Id;
      projectApp.Name = projectPlan.Name;
      projectApp.StartDate = projectPlan.StartDate;
      projectApp.EndDate = projectPlan.EndDate;
      projectApp.BaselineWork = projectPlan.BaselineWork;
      projectApp.ExecutionPercent = projectPlan.ExecutionPercent;
      projectApp.FactualCosts = projectPlan.FactualCosts;
      projectApp.PlannedCosts = projectPlan.PlannedCosts;
      projectApp.Note = projectPlan.Note;
     
      return projectApp;
    }
    
    /// <summary>
    /// Добавить менеджера проекта.
    /// </summary>
    /// <param name="projectApp">Проект.</param>
    /// <param name="users">Список пользователей.</param>
    private static void AddManager(DirRX.Planner.Model.Project projectApp, List<DirRX.Planner.Model.Users> users, DirRX.ProjectPlanning.IProject linkedProject)
    {
     if (projectApp.ManagerId == null)
     {
       return;
     }
     
     var managerInfo = new DirRX.Planner.Model.Users();
     managerInfo.Id = linkedProject.Manager.Id;
     managerInfo.Name = linkedProject.Manager.Person.FirstName;
     managerInfo.Surname = linkedProject.Manager.Person.LastName;
     users.Add(managerInfo);
    }
    
    /// <summary>
    /// Создать модель активити.
    /// </summary>
    /// <returns>Модель.</returns>
    private static DirRX.Planner.Model.Activity CreateActivity(IProjectActivity activity, List<DirRX.Planner.Model.Users> users)
    {
      var item = new DirRX.Planner.Model.Activity();
      item.BaselineWork = activity.BaselineWork;
      item.ActualWorkload = activity.ActualWorkload;
      item.LeadActivityId = activity.LeadingActivity != null ? activity.LeadingActivity.Id : (long?)null;
      item.Id = activity.Id;
      item.StartDate = activity.StartDate;
      item.EndDate = activity.EndDate;
      item.Name = activity.Name;
      item.ExecutionPercent = activity.ExecutionPercent;
      item.Note = activity.Note;
      item.SortIndex = activity.SortIndex.HasValue ? activity.SortIndex.Value : 1;

      item.Predecessors = activity.Predecessors.Where(x => x.Activity != null).Select(x => 
                                                                                      new DirRX.Planner.Model.Predecessor 
                                                                                      {
                                                                                        Id = x.Activity.Id,
                                                                                        LinkType = x.LinkType,
                                                                                        Lag = x.Lag ?? 0
                                                                                      }).ToList();

      item.Priority = activity.Priority;
      item.PlannedCosts = activity.PlannedCosts;
      item.FactualCosts = activity.FactualCosts;
      item.TypeActivity = activity.TypeActivity.ToString();
      item.RefId = activity.RefId;
      item.Status = new DirRX.Planner.Model.ActivityStatus 
      {
        EnumValue = activity.Status.ToString(),
        LocalizeValue = activity.Info.Properties.Status.GetLocalizedValue(activity.Status)
      };
     
      FillResources(activity, item);
      
      AddResponsible(activity, users, item);
      
      FillAttachments(activity.Attachments, item);
      
      return item;
    }
    
    /// <summary>
    /// Заполнить активити ресурсами.
    /// </summary>
    /// <param name="activity">Активити.</param>
    /// <param name="item">Модель активити.</param>
    private static void FillResources(IProjectActivity activity, DirRX.Planner.Model.Activity item)
    {
      foreach(var resource in activity.ResourcesCapacity)
      {
        var resourceData = new DirRX.Planner.Model.ResourcesWorkload();
        resourceData.ResourceId = resource.ResourceId.Value;
        resourceData.Value = resource.Capacity.Value;
        item.Resources.Add(resourceData);
      }
    }
    /// <summary>
    /// Добавить ответственных.
    /// </summary>
    /// <param name="activity">Активити.</param>
    /// <param name="users">Список ответственных.</param>
    private static void AddResponsible(IProjectActivity activity, List<DirRX.Planner.Model.Users> users, DirRX.Planner.Model.Activity item)
    {
      var user = new DirRX.Planner.Model.Users();
      if (activity.Responsible != null)
      {
        item.ResponsibleId = activity.Responsible.Id;
        
        user.Id = activity.Responsible.Id;
        user.Name = activity.Responsible.Person.FirstName;
        user.Surname = activity.Responsible.Person.LastName;
        
        if (!users.Any(x => x.Id == user.Id))
          users.Add(user);
      }
    }
    
    private static void FillAttachments(IChildEntityCollection<IProjectActivityAttachments> sourceAttachments, DirRX.Planner.Model.Activity targetActivity)
    {
      foreach (var sourceAttachment in sourceAttachments)
      {
        var targetAttachment = new DirRX.Planner.Model.Attachment()
        {
          AttachmentId = sourceAttachment.AttachmentId,
          Name = sourceAttachment.Name,
          Url = sourceAttachment.Url
        };
        
        targetActivity.Attachments.Add(targetAttachment);
      }
    }
    
    /// <summary>
    /// Заполнить типы ресурсов.
    /// </summary>
    /// <returns>Типы ресурсов.</returns>
    private static List<DirRX.Planner.Model.ResourceTypes> FillResourceTypes()
    {
      var resourceTypes = new List<DirRX.Planner.Model.ResourceTypes>();
      foreach(var type in ProjectResourceTypes.GetAll())
      {
        var resourceType = new DirRX.Planner.Model.ResourceTypes();
        resourceType.Id = type.Id;
        resourceType.Name = type.Name;
        resourceType.SectionName = type.ServiceName;
        resourceTypes.Add(resourceType);
      }
      return resourceTypes;
    }
    
    /// <summary>
    /// Найти человекоресурсы. наверное объединять с следующим методом надо.
    /// </summary>
    /// <param name="projActs">Активити.</param>
    /// <param name="users">Пользователи проекта.</param>
    /// <param name="resources">Ресурсы.</param>
    private static void FindEmployeeResources(List<IProjectActivity> projActs, List<DirRX.Planner.Model.Users> users, List<DirRX.Planner.Model.Resources> resources)
    {
      Logger.Debug("Find employee Resource.");
      
      var firstAct = projActs.FirstOrDefault();
      if (firstAct == null)
      {
        return;
      }
      
      var resourcesList = GetProjectResources(firstAct.ProjectPlan.Id, firstAct.NumberVersion.Value);
      //TODO URMANOV: тут такой же запрос как в UpdateActivityResources на получение ид ресурсов в проекте
      foreach (var employeeResource in ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.Users
                                                                && resourcesList.Contains(x.Id)))
      {
        Logger.Debug("Finded employee Resource.");
        var user = new DirRX.Planner.Model.Users();
        var resourceLink = new DirRX.Planner.Model.Resources();
        
        user.Id = employeeResource.Employee.Id;
        user.Name = employeeResource.Employee.Person != null ? employeeResource.Employee.Person.FirstName : employeeResource.Employee.Name;
        user.Surname = employeeResource.Employee.Person != null ? employeeResource.Employee.Person.LastName : string.Empty;
        resourceLink.Id = employeeResource.Id;
        resourceLink.EntityTypeId = employeeResource.Type.Id;
        resourceLink.EntityId = employeeResource.Employee.Id;
        resourceLink.UnitLabel = employeeResource.Type.MeasureUnit;
        
        if(!users.Any(x => x.Id == user.Id))
          users.Add(user);
        
        resources.Add(resourceLink);
        Logger.DebugFormat("employee {0} Resource {1} added.", user.Id, resourceLink.Id);
      }
    }
    
    /// <summary>
    /// Найти материальные ресурсы.
    /// </summary>
    /// <param name="projActs">Активити.</param>
    /// <param name="resources">Ресурсы.</param>
    /// <returns></returns>
    private static List<DirRX.Planner.Model.MaterialResources> FindMaterialResources(List<IProjectActivity> projActs, List<DirRX.Planner.Model.Resources> resources)
    {
      var firstAct = projActs.FirstOrDefault();
      if (firstAct == null)
      {
        return null;
      }
      
      var resourcesList = GetProjectResources(firstAct.ProjectPlan.Id, firstAct.NumberVersion.Value);
      
      var materialResources = new List<DirRX.Planner.Model.MaterialResources>();
      Logger.Debug("Find material Resource.");
      foreach (var resource in ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.MaterialResources
                                                        && resourcesList.Contains(x.Id)))
      {
        Logger.Debug("Finded material Resource.");
        var materialResource = new DirRX.Planner.Model.MaterialResources();
        var resourceLink = new DirRX.Planner.Model.Resources();
        
        materialResource.Id = resource.Id;
        materialResource.Name = resource.Name;
        
        resourceLink.Id = resource.Id;
        resourceLink.EntityTypeId = resource.Type.Id;
        resourceLink.EntityId = resource.Id;
        resourceLink.UnitLabel = resource.Type.MeasureUnit;
        
        materialResources.Add(materialResource);
        resources.Add(resourceLink);
        Logger.DebugFormat("resource {0} Resource {1} added.", materialResource.Id, resourceLink.Id);
      }
      return materialResources;
    }
    #endregion
    
    private static long GetLastActivityId(List<IProjectActivity> projActs)
    {
      //Zheleznov_AV HACK изначально тут был метод, получающий lastActivtyId через анализ истории сущностей.
      //Он работал медленно. В рамках оптимизации изменил метод на "однострочник". Не делаю inline-ing данного метода,
      //что бы не менять текущий API.
      return projActs.Any() ? projActs.Max(x => x.Id) : 0;
    }
    
    public static string UpdateModel(IProjectPlanRX project, int numberVersion, DirRX.Planner.Model.Model model)
    {
      var prActs = ProjectActivities.Create();
      var listStatuses = new List<DirRX.Planner.Model.ActivityStatus>();
      foreach (Enumeration i in prActs.StatusAllowedItems)
      {
        listStatuses.Add(new DirRX.Planner.Model.ActivityStatus {EnumValue = i.ToString(), LocalizeValue = prActs.Info.Properties.Status.GetLocalizedValue(i)});
      }
     
      model.ActivityStatuses = listStatuses;
      
      var projActs = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, project) && a.NumberVersion.Value == numberVersion).ToList();
      model.LastActivityId = GetLastActivityId(projActs);

      var modelJson = Newtonsoft.Json.JsonConvert.SerializeObject(model);
      
      return modelJson;
    }
    
    [Remote, Public(WebApiRequestType = RequestType.Get)]
    public static string GetModel(long projectPlanId, int numberVersion)
    {
      var projectPlan = ProjectPlanRXes.Get(projectPlanId);
      return GetModel(projectPlan, numberVersion);
    }
    
    /// <summary>
    /// Передает данные по проекту в модель Json.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <returns>Сериализованная строка с данными по проекту.</returns>
    [Remote, Public]
    public static string GetModel(IProjectPlanRX project, int numberVersion)
    {
      var model = new DirRX.Planner.Model.Model();
      var projActs = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, project) && a.NumberVersion.Value == numberVersion).ToList();
      
      var activitiesApp = new List<DirRX.Planner.Model.Activity>();
      var resources = new List<DirRX.Planner.Model.Resources>();
      var users = new List<DirRX.Planner.Model.Users>();
      
      AccessRights.AllowRead(() =>
                             {
                               var projectApp = CreateProjectAppModel(project);
                               projectApp.Note = project.Note;
                               
                               foreach (var activity in projActs)
                               {
                                 var item = CreateActivity(activity, users);
                                 
                                 activitiesApp.Add(item);
                               }
                               var resourceTypes = FillResourceTypes();
                              
                               var linkedProject = Functions.ProjectPlanRX.GetLinkedProject(project);
                               
                               if (linkedProject != null)
                               {
                                 projectApp.ManagerId = linkedProject.Manager.Id;
                               }
                               
                               AddManager(projectApp, users, linkedProject);
                               
                               FindEmployeeResources(projActs, users, resources);
                               
                               var materialResources = FindMaterialResources(projActs, resources);
                               
                               List<DirRX.Planner.Model.Capacity> capacities = new List<DirRX.Planner.Model.Capacity>();
                               
                               model.LastActivityId = GetLastActivityId(projActs);
                               
                               var prAct = ProjectActivities.Create();
                               var listStatuses = new List<DirRX.Planner.Model.ActivityStatus>();
                               foreach (Enumeration i in prAct.StatusAllowedItems)
                               {
                                 listStatuses.Add(new DirRX.Planner.Model.ActivityStatus {EnumValue = i.ToString(), LocalizeValue = prAct.Info.Properties.Status.GetLocalizedValue(i)});
                               }
                               
                               model.ActivityStatuses = listStatuses;
                               model.Project = projectApp;
                               model.Activities = activitiesApp.ToList();
                               Logger.Debug("Filling resourcesData.");
                               var resourcesData = new DirRX.Planner.Model.ResourcesData();
                               resourcesData.MaterialResources = materialResources;
                               resourcesData.Resources = resources;
                               resourcesData.ResourceTypes = resourceTypes;
                               resourcesData.Users = users;
                               model.ResourcesData = resourcesData;
                             });
      
      var modelJson = Newtonsoft.Json.JsonConvert.SerializeObject(model);
      
      return modelJson;
    }
    
    /// <summary>
    /// Получить актуальную модель плана, дозаполненную фактическими значениями из бд в формате Json строки.
    /// </summary>
    /// <param name="planId">ID плана.</param>
    /// <param name="versionNumber">Номер версии документа.</param>
    /// <returns>Модель плана проекта.</returns>
    /// <remarks>Нельзя возвращать кастомный интерфейс из библиотеки в сервисе интеграции, поэтому строкой.</remarks>
    [Public(WebApiRequestType = RequestType.Get)]
    public static string GetPlanModelForGantt(long projectPlanId, int numberVersion)
    {
      var model = Functions.ProjectPlanRX.GetPlanModelForGantt(projectPlanId, numberVersion);
      return Newtonsoft.Json.JsonConvert.SerializeObject(model);
    }
    
    /// <summary>
    /// Создать и отправить корневую задачу этапа проекта.
    /// </summary>
    /// <param name="activityId">Id этапа.</param>
    /// <returns>Запущеную задачу.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public Structures.Module.ITask StartRootTask(long activityId, int? executionPercent, double? actualWorkload, double? factualCosts)
    {
      var activity = ProjectActivities.Get(activityId);
      var task = ProjectActivityTaskFunctions.GetTasksByRefId(activity)
        .OrderBy(t => t.Id)
        .FirstOrDefault();
      
      if (task != null)
      {
        if (task.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.Aborted)
        {
          var isTaskValid = Functions.ProjectActivityTask.UpdateProjectActivityTaskFields(task, true);
          
          if (isTaskValid)
          {
            task.Restart();
            task.Start();
          }
        }
        else
        {
          throw new Exception(Resources.RootTaskAlreadyExistMessageFormat(activity.Id));
        }
      }
      else
      {
        task = CreateProjectActivityTask(activity, executionPercent, actualWorkload, factualCosts, true);
        task.Start();
      }
      
      return new Structures.Module.Task()
      {
        ActivityId = activityId,
        Deadline = task.MaxDeadline.Value,
        HyperLink = Hyperlinks.Get(task),
        Id = task.Id,
        TaskStatus = Constants.Module.TaskDtoStatuses.Unfinished,
        DisplayValue = task.DisplayValue,
        StartDate = task.Started.Value,
        IsRoot = true,
        ActivityStatus = DirRX.ProjectPlanner.ProjectActivity.Status.InWork.Value
      };
    }
    
    /// <summary>
    /// Создание автозапускаемой задачи по этапу.
    /// </summary>
    /// <param name="activity">Этап.</param>
    /// <returns>Задача по этапу или null, если не удалось определить исполнителей.</returns>
    public static DirRX.ProjectPlanner.IProjectActivityTask CreateProjectActivityTask(
      IProjectActivity activity,
      int? executionPercent,
      double? actualWorkload,
      double? factualCosts,
      bool isManual)
    {
      var projectActivityTask = ProjectActivityTasks.Create();
      projectActivityTask.ActivityRefId = activity.RefId;
      projectActivityTask.ProjectPlan = activity.ProjectPlan;
      
      var isTaskValid = Functions.ProjectActivityTask.UpdateProjectActivityTaskFields(projectActivityTask, isManual);
      
      if (!isTaskValid)
      {
        return null;
      }
      
      projectActivityTask.MaxDeadline = CalculateActivityEndDateForTasks(activity);
      projectActivityTask.ExecutionPercent = executionPercent;
      projectActivityTask.ActualWorkload = actualWorkload;
      projectActivityTask.FactualCosts = factualCosts;
      projectActivityTask.Save();

      Functions.ProjectActivityTask.SendTaskAdded(projectActivityTask);

      return projectActivityTask;
    }
    
    public static DateTime CalculateActivityEndDateForTasks(IProjectActivity activity)
    {
      //Отнимаем один календарный день, потому что дата окончания активити в коде равна 00:00 следующего дня, 
      //что не соответствует визуальному отображению в клиенте.
      var activityEndDate = (activity.EndDate ?? activity.StartDate ?? Calendar.Today).AddDays(-1);
      //Для того чтобы точно быть уверенными, что мы берем дату без времени - берем свойство Date у активити,
      //не смотря на то, что в нормальных ситуациях там будет DateTime с временем 00:00.
      return activityEndDate.Date;
    }
    
    /// <summary>
    /// Возвращает список задач по их идентификаторам.
    /// </summary>
    /// <param name="tasksIds">Список ИД задач.</param>
    /// <returns>Список задач.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<Sungero.Workflow.ISimpleTask> GetTasksByIds(List<long> tasksIds)
    {
      return Sungero.Workflow.SimpleTasks.GetAll().Where(c => tasksIds.Contains(c.Id));
    }
    
    [Remote(IsPure = true)]
    public static bool VersionApproved(Sungero.Content.IElectronicDocument project, int numVersion)
    {
      return Signatures.Get(project.Versions.First(x => x.Number.Value == numVersion)).Where(s => s.SignatureType == SignatureType.Approval).Any();
    }
    
    [Remote, Public]
    public static string GetWebSite()
    {
      var result = string.Empty;
      AccessRights.AllowRead(() =>
                             {
                               try
                               {
                                 var webAdressGetter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
                                 result = webAdressGetter.GetContentFullAddress(new Uri(Constants.Module.DefaultClientAddress, UriKind.Relative)).ToString();
                               }
                               catch (ConfigWrapperV1_0_0.Exceptions.SungeroConfigSettingsException ex)
                               {
                                 throw new Exception(DirRX.ProjectPlanner.Resources.ErrorWhileTryOpenWebClient, ex);
                               }
                               
                             });
      return result;
    }
    
    
    /// <summary>
    /// Проверить наличие у участника прав на сущность.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="member">Участник.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    /// <returns>True - если права есть, иначе - false.</returns>
    public static bool CheckGrantedRights(Sungero.Domain.Shared.IEntity entity, IRecipient member, Guid accessRightsType)
    {
      if (accessRightsType == DefaultAccessRightsTypes.Change)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
      
      if (accessRightsType == Constants.Module.ChangeContent)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
      
      if (accessRightsType == DefaultAccessRightsTypes.Read)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
          entity.AccessRights.IsGrantedDirectly(Constants.Module.ChangeContent, member);
      
      return entity.AccessRights.IsGrantedDirectly(accessRightsType, member);
    }
    
    /// <summary>
    /// 
    /// </summary>
    public static List<Structures.Module.ICapacity> GetCapacity(List<long> resourceIds, DateTime startDate, DateTime endDate, long planId, int planVersion)
    {
      var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resourceIds);
      
      var capacities = new Dictionary<long, List<DirRX.ProjectPlanner.Structures.Module.ICapacityValue>>();
      using (var connection = CreateDBConnection())
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = Queries.Module.GetCapacity;
          SQL.AddParameter(command, "@startDate", startDate, System.Data.DbType.Date);
          //HACK: -1 день т.к. активити на самом деле длятся минимум 2 дня, отображаются на 1 день меньше.
          SQL.AddParameter(command, "@endDate", endDate, System.Data.DbType.Date);
          SQL.AddParameter(command, "@projectplan_id", planId, System.Data.DbType.Int64);
          SQL.AddParameter(command, "@projectplan_version", planVersion, System.Data.DbType.Int32);
          
          //HACK: через AddArrayParameter работает нестабильно, пришлось сделать replace в запросе.
          //HACK: для того чтобы параметр IN(@resourceIds) в результате не привел к строке IN()
          var resourceIdsParameter = resourceIds.Count == 0 ? "0,0" : string.Join(",", resourceIds.ToArray());
          command.CommandText = Queries.Module.GetCapacity.Replace(
                                                          "@resource_ids", 
                                                          resourceIdsParameter);
          
          using (var reader = command.ExecuteReader())
          {
            var calendarErrors = new Dictionary<long, HashSet<int>>();
            while (reader.Read())
            {
              var resourceId = (long)reader[0];
              var date = (DateTime)reader[2];
              var Busy = double.Parse(reader[1].ToString());
              
              var calendar = calendars[resourceId].FirstOrDefault(c => c.Year == date.Year);
              if (calendar == null) 
              {
                if (!calendarErrors.ContainsKey(resourceId))
                {
                  calendarErrors.Add(resourceId, new HashSet<int>() {date.Year});
                }
                else
                {
                  if (!calendarErrors[resourceId].Contains(date.Year))
                  {
                    calendarErrors[resourceId].Add(date.Year);
                  }
                }
                continue;
              }
              
              if (!capacities.ContainsKey(resourceId)) {
                capacities.Add(resourceId, new List<DirRX.ProjectPlanner.Structures.Module.ICapacityValue>());
              }
              
              var calendarDay = calendar.Day.FirstOrDefault(d => ((DateTime)reader[2]).Date == d.Day.Date);
              
              if (calendarDay != null && calendarDay.Kind == null)
              {
                capacities[resourceId].Add(new DirRX.ProjectPlanner.Structures.Module.CapacityValue() 
                                  {
                                    Date = (DateTime)reader[2],
                                    Busy = double.Parse(reader[1].ToString())
                                   });
              }
              
            }
            
            foreach (var resId in calendarErrors)
            {
              foreach (var year in resId.Value)
                Logger.Error(DirRX.ProjectPlanner.Resources.ColdNotFindCalendarWithResourceIdFormat(year, resId.Key));
            }
            
          }
        }
      }
      var result = new List<Structures.Module.ICapacity>();
      
      result.AddRange(
        capacities.Select(x => 
                          new DirRX.ProjectPlanner.Structures.Module.Capacity
                          {
                            ResourceId = x.Key,
                            Values = x.Value
                          }
                         )
       );
      
      return result;
    }
    
    private static double GetDailyAverageBusy(IEnumerable<Structures.Module.AverageBusy> busyes, long resourceId, DateTime date)
    {
      return busyes.Where(b => b.EndDate > date && b.StartDate <= date).Sum(b => b.AvgBusy);
    }
    
    private static List<Structures.Module.AverageBusy> GetResourceAverageBusy(long planId, int numberVersion, DateTime startDate, DateTime endDate, System.Data.IDbConnection connection)
    {
      List<Structures.Module.AverageBusy> result = new List<DirRX.ProjectPlanner.Structures.Module.AverageBusy>();
      
      //получить список активити (дата начала, конца, загрузка) в выбранный период для ресурса
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetResourceAverageBusy;
        SQL.AddParameter(command, "@projectPlanId", planId, System.Data.DbType.Int64);
        SQL.AddParameter(command, "@VersionNum", numberVersion, System.Data.DbType.Int32);
        SQL.AddParameter(command, "@StartDate", startDate, System.Data.DbType.DateTime);
        SQL.AddParameter(command, "@EndDate", (endDate == default) ? startDate : endDate , System.Data.DbType.DateTime);
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var avgBusy = new Structures.Module.AverageBusy();
            avgBusy.AvgBusy = (float)reader[0];
            avgBusy.ResourceId = (long)reader[3];
            avgBusy.StartDate = (DateTime)reader[1];
            avgBusy.EndDate = (DateTime)reader[2];
            result.Add(avgBusy);
          }
        }
      }
      return result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    public static List<Structures.Module.IWorkingTimeCalendar> GetWorkingTimeCalendars(List<long> resourceIds, DateTime startDate, DateTime endDate)
    {
      var workingTimeCalendar = new List<Structures.Module.IWorkingTimeCalendar>();
      var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resourceIds);
      
      var calendarsDto = resourceIds.Select(resId => 
                                          new Structures.Module.WorkingTimeCalendar() 
                                          {
                                            ResourcesIds = new List<long>(){resId},
                                            FreeDays = GetExtraFreeDays(calendars[resId]),
                                            WorkDays = GetExtraWorkingDays(calendars[resId])
                                          });
      workingTimeCalendar.AddRange(calendarsDto);
      return workingTimeCalendar;
    }
    
    /// <summary>
    /// Получить календарь
    /// </summary>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата окончания.</param>
    /// <param name="resourceId">id ресурса</param>
    /// <returns>Календарь рабочего времени.</returns>
    public static System.Collections.Generic.Dictionary<long, List<Sungero.CoreEntities.IWorkingTimeCalendar>> GetPrivateOrPublicCalendars(DateTime startDate, DateTime endDate, List<long> resourceIds)
    {
      var resources = ProjectsResources.GetAll(r => resourceIds.Contains(r.Id)).ToArray();
      var calendars = new Dictionary<long, List<Sungero.CoreEntities.IWorkingTimeCalendar>>();
      foreach (var resource in resources)
      {
        if (!calendars.ContainsKey(resource.Id)) {
                calendars.Add(resource.Id, new List<Sungero.CoreEntities.IWorkingTimeCalendar>());
              }
        
        Sungero.CoreEntities.Shared.CalendarCache.Restrict(resource.Employee, startDate.Year, endDate.Year);
        
        var calendarsCached = Sungero.CoreEntities.Shared.CalendarCache.GetCached();
        calendars[resource.Id].AddRange(
          calendarsCached
          .Where(c => 
                 PrivateWorkingTimeCalendars.Is(c) && (PrivateWorkingTimeCalendars.As(c).Recipients.Select(r => r.Recipient.Id)
                                                                                                           .Contains(resource.Employee.Id)
                                                       || (resource.Employee.Department != null 
                                                           && PrivateWorkingTimeCalendars.As(c).Recipients.Select(r => r.Recipient.Id)
                                                                                                           .Contains(resource.Employee.Department.Id))
                                                       || PrivateWorkingTimeCalendars.As(c).Recipients.Any(r => 
                                                                                                           Groups.Is(r.Recipient) 
                                                                                                           && resource.Employee.IncludedIn(Groups.As(r.Recipient)))
                                                      )));
          
          calendars[resource.Id].AddRange(
          calendarsCached
          .Where(c => 
                 !PrivateWorkingTimeCalendars.Is(c)
                 && !calendars[resource.Id]
                              .Any(cc => cc.Year == c.Year)
                             ));
          
      }
      return calendars;
    }
    
    /// <summary>
    /// Получить календарь
    /// </summary>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата окончания.</param>
    /// <param name="resourceId">id ресурса</param>
    /// <returns>Календарь рабочего времени.</returns>
    public static List<IWorkingTimeCalendar> GetPrivateOrPublicCalendars(DateTime startDate, DateTime endDate, long resourceId)
    {
      var resource = ProjectsResources.Get(resourceId);
      Sungero.CoreEntities.Shared.CalendarCache.Restrict(resource.Employee, startDate.Year, endDate.Year);
      return Sungero.CoreEntities.Shared.CalendarCache.GetCached(true).ToList();
    }
    
    
    /// <summary>
    /// [Для демо - генератора] Добавление ресурса. 
    /// </summary>
    /// <param name="activityId">Идентификатор этапа. </param>
    /// <param name="resourceId">Идентификатор ресурса. </param>
    /// <param name="workload">Трудоемкость. </param>
    /// <param name="projectPlanId">Идентификатор плана. </param>
    [Public(WebApiRequestType = RequestType.Post), Remote]
    public void AddResourcesDemo(long activityId, long resourceId, int workload, long projectPlanId)
    {
      var activity = ProjectActivities.GetAll(a => a.Id == activityId).FirstOrDefault();
      var resource = ProjectsResources.GetAll(r => r.Id == resourceId).FirstOrDefault();
      var projectPlan = ProjectPlanRXes.GetAll(p => p.Id == projectPlanId).FirstOrDefault();
      
      if (resource == null || activity == null)
      {
        return;
      }
      
      var res1 = activity.ResourcesCapacity.AddNew();
      res1.Capacity = workload;
      res1.ResourceId = resource.Id;
      
      using (var connection = CreateDBConnection())
      {
        var act = new DirRX.Planner.Model.Activity()
        {
          Resources = new List<DirRX.Planner.Model.ResourcesWorkload>(),
          StartDate = activity.StartDate,
          EndDate = activity.EndDate,
          Id = activity.Id
        };
        act.Resources.Add(new DirRX.Planner.Model.ResourcesWorkload()
                          {
                            ResourceId = resource.Id,
                            Value = workload
                          });
        SaveResources(act.Resources, act.Id.Value, act.StartDate.Value, act.EndDate.Value, connection);
        activity.Save();
        connection.Close();
      }
      DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(projectPlan, projectPlan.LastVersion.Number.Value, false);
    }
    
    [Public(WebApiRequestType = RequestType.Get)]
    public List<Structures.Module.ISignatureDto> GetSignaturesInfo(long planId)
    {
      var plan = GetPlanOrThrowExceptionIfNull(planId);
      
      var signatures = Signatures.Get(plan).OfType<IInternalSignature>();
      var result = new List<Structures.Module.ISignatureDto>();
      
      foreach (var signature in signatures)
      {
        // проверяем, что подписанное свойство сущности действительно тело документа
        var signedProperty = signature.SignedEntityProperties.SingleOrDefault(prop => prop.SignedPropertyGuid == Constants.Module.BodyVersionGuid);

        if (signedProperty != null && signedProperty.ChildEntityId != null)
        {
          var versionId = signedProperty.ChildEntityId;
          var isApproval = signature.SignatureType == SignatureType.Approval;
          var isEndorsing = signature.SignatureType == SignatureType.Endorsing;
          var signingDate = signature.SigningDate;
          var signatoryFullName = signature.SignatoryFullName;
          var signatoryId = signature.Signatory.Id;
          
          result.Add(Structures.Module.SignatureDto.Create(versionId.Value,
                                                           isApproval,
                                                           isEndorsing,
                                                           signingDate,
                                                           signatoryFullName,
                                                           signatoryId)
                    );
        }
      }
      return result;
    }

    [Public(WebApiRequestType = RequestType.Get)]
    public bool CanUpdatePlan(long planId)
    {
      var plan = GetPlanOrThrowExceptionIfNull(planId);
      
      return plan.AccessRights.CanUpdate();
    }
    
    private DirRX.ProjectPlanner.IProjectPlanRX GetPlanOrThrowExceptionIfNull(long planId)
    {
      var plan = ProjectPlanRXes.GetAll(x => x.Id == planId).SingleOrDefault();
      
      if(plan == null)
      {
        throw new Exception($"не удалось получить план проекта с id={planId}");
      }
      
      return plan;
    }
    
    #region Сводный отчет
    
    #region Загрузка ресурсов по объектам управления
    
    /// <summary>
    /// Получение метаданных для сводного отчета по объектам управления.
    /// </summary>
    /// <param name="projectCoreId">Ид проекта.</param>
    /// <param name="dateRange">Период.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <returns>Метаданные для отчета.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public string ResourcesReportMetadata(long projectCoreId, string dateRange, string dateScaleKey)
    {
      // проверка прав на проект
      var projectCore = DirRX.ProjectPlanning.ProjectCores.Get(projectCoreId);
      
      var path = Constants.Module.PathResourcesReport;
      var colsDefault = new List<string>();
      
      var dateScaleDictionary = new Dictionary<string, string>()
      {
        { Constants.Module.DateScaleKey.TimelineDays, DateScale.TimelineDays },
        { Constants.Module.DateScaleKey.TimelineWeeks, DateScale.TimelineWeeks },
        { Constants.Module.DateScaleKey.TimelineMonths, DateScale.TimelineMonths },
        { Constants.Module.DateScaleKey.TimelineQuarters, DateScale.TimelineQuarters },
        { Constants.Module.DateScaleKey.TimelineYears, DateScale.TimelineYears }
      };
      
      if (String.IsNullOrEmpty(dateScaleKey) || !dateScaleDictionary.ContainsKey(dateScaleKey))
      {
        throw new Exception($"не удалось получить детализацию по ключу {dateScaleKey ?? "null"}");
      }
      
      var answerMetadata = new DirRX.Planner.Model.Olap.Model.AnswerMetadata();
      answerMetadata.AddTitle(Resources.ResourceReportTitleFormat(projectCore.Name));
      answerMetadata.AddLocalePath(Constants.Module.PathLocaleResourcesReport);
      
      answerMetadata.AddPath(path);
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.ProjectCoreId, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.Number },
                                { Constants.Module.LocalizationKeys.Default, projectCoreId },
                                { Constants.Module.LocalizationKeys.NumberFormat, Constants.Module.LocalizationKeys.Integer },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.ProjectCoreId },
                                { Constants.Module.LocalizationKeys.IsImmutable, true }
                              }
                             );
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.DateRange, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.DateRange },
                                { Constants.Module.LocalizationKeys.Default, dateRange },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.DateRange },
                                { Constants.Module.LocalizationKeys.MinValue, null },
                                { Constants.Module.LocalizationKeys.MaxValue, null },
                                { Constants.Module.LocalizationKeys.IsImmutable, false }
                              }
                             );
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.DateScaleKey, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.DateScale },
                                { Constants.Module.LocalizationKeys.Default, dateScaleKey },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.DateScale },
                                { Constants.Module.LocalizationKeys.IsImmutable, false }
                              }
                             );
      

      answerMetadata.AddType(Constants.Module.LocalizationKeys.DateScalesParam, new Dictionary<string, object>()
                              {
                                { Constants.Module.DateScaleKey.TimelineDays, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineDays }} },
                                { Constants.Module.DateScaleKey.TimelineWeeks, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineWeeks }} },
                                { Constants.Module.DateScaleKey.TimelineMonths, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineMonths }} },
                                { Constants.Module.DateScaleKey.TimelineQuarters, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineQuarters }} },
                                { Constants.Module.DateScaleKey.TimelineYears, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineYears }} }
                              }
                             );
 
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>
                                         ( Constants.Module.LocalizationKeys.Rows, new string[]
                                          {
                                            Constants.Module.LocalizationKeys.EmployeeName,
                                            Constants.Module.LocalizationKeys.ProjectIncludedIn,
                                            Constants.Module.LocalizationKeys.ProjectShortName
                                          }
                                         )
                                        );
      
      
      switch (dateScaleKey)
      {
        case Constants.Module.DateScaleKey.TimelineYears:
          colsDefault.Add(Constants.Module.LocalizationKeys.Year);
          break;
        case Constants.Module.DateScaleKey.TimelineQuarters:
          colsDefault.Add(Constants.Module.LocalizationKeys.Quarter);
          goto case Constants.Module.DateScaleKey.TimelineYears;
        case Constants.Module.DateScaleKey.TimelineMonths:
          colsDefault.Add(Constants.Module.LocalizationKeys.Month);
          goto case Constants.Module.DateScaleKey.TimelineYears;
        case Constants.Module.DateScaleKey.TimelineWeeks:
          colsDefault.Add(Constants.Module.LocalizationKeys.Week);
          goto case Constants.Module.DateScaleKey.TimelineMonths;
        case Constants.Module.DateScaleKey.TimelineDays:
          colsDefault.Add(Constants.Module.LocalizationKeys.Date);
          goto case Constants.Module.DateScaleKey.TimelineWeeks;
        default:
          goto case Constants.Module.DateScaleKey.TimelineDays;
      }
      
      colsDefault.Reverse();
      
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>( Constants.Module.LocalizationKeys.Cols, colsDefault ));
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>(Constants.Module.LocalizationKeys.AggregateType, Constants.Module.AggregatorsKeys.Sum));
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>(Constants.Module.LocalizationKeys.AggregateCol, Constants.Module.LocalizationKeys.Busy));
      
      return Newtonsoft.Json.JsonConvert.SerializeObject(answerMetadata.GetObjectForSerialization());
    }
    
    /// <summary>
    /// Получение данных для сводного отчета по объектам управления.
    /// </summary>
    /// <param name="projectCoreId">Ид проекта.</param>
    /// <param name="dateRange">Диапазон даты.</param>
    /// <param name="dateScaleKey">Ключ детализации</param>
    /// <returns>Данные для отчета в json.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public string ResourcesReport(long projectCoreId, string dateRange, string dateScaleKey)
    {
      //проверка прав на проект
      var project = DirRX.ProjectPlanning.ProjectCores.Get(projectCoreId);
      
      var allProjectIds = Sungero.Projects.ProjectCores.GetAll().Select(x => x.Id);
      var allPlanIds = DirRX.ProjectPlanner.ProjectPlanRXes.GetAll().Select(x => x.Id);
      var answer = new DirRX.Planner.Model.Olap.Model.Answer();
      var records = new List<Dictionary<string, object>>();
      var recordsWorkingDays = new List<Structures.Module.IResourceReportRow>();
      var workingDaysEmployee = new Dictionary<long, Dictionary<DateTime, bool>>();
      var reportData = new List<Structures.Module.IResourceReportRow>();
      var culture = System.Globalization.CultureInfo.CurrentUICulture;
      DateTime startDate;
      DateTime endDate;
      
      try
      {
        string[] dateStrings = dateRange.Split('/');
        startDate = DateTime.ParseExact(dateStrings[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(dateStrings[1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
      }
      catch(Exception ex)
      {
        throw new Exception($"неверный формат даты в параметре dateRange={dateRange}", ex);
      }
      
      this.AddTypes(answer, dateScaleKey);
      
      using (var connection = CreateDBConnection())
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = Queries.Module.GetOlapPlanReportData;
          
          // HACK: AddArrayParameter работает нестабильно, если коллекция allPlanIds пустая.
          // HACK: Поэтому добавил replace с условным 0
          if (allPlanIds.Any())
            SQL.AddArrayParameter(command, "@allPlans", allPlanIds, System.Data.DbType.Int64);
          else
            command.CommandText = Queries.Module.GetOlapPlanReportData.Replace("@allPlans", "0");
          
          SQL.AddParameter(command, "@project_id", projectCoreId, System.Data.DbType.Int64);
          SQL.AddParameter(command, "@startDatePeriod", startDate, System.Data.DbType.DateTime);
          SQL.AddParameter(command, "@endDatePeriod", endDate, System.Data.DbType.DateTime);
          SQL.AddArrayParameter(command, "@all_projects_with_access", allProjectIds, System.Data.DbType.Int64);

          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              var olapReportDataRow = Structures.Module.ResourceReportRow.Create(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetFloat(2),
                reader.GetInt64(3),
                !reader.IsDBNull(4) ? reader.GetString(4) : null,
                reader.GetInt64(5),
                reader.GetInt64(6),
                !reader.IsDBNull(7) ? reader.GetString(7) : null,
                reader.GetDateTime(8)
               );
              reportData.Add(olapReportDataRow);
            }
          }
        }
      }

      var employeeIdsCache = new HashSet<long>();
      var projectCoreIdsCache = new HashSet<long>();
      
      var allEmployeeIdsForReport = new HashSet<long>(reportData.Select(x => x.PerformerId));
      var allProjectCoreIdsForReport = new HashSet<long>(reportData.Where(x => x.ProjectCoreId != null).Select(x => x.ProjectCoreId.Value));
      Dictionary<long, IEmployee> employeesCache = null;
      
      AccessRights.AllowRead(() =>
                             {
                               employeesCache = Sungero.Company.Employees.GetAll(emp => allEmployeeIdsForReport.Contains(emp.Id)).ToDictionary(x => x.Id);
                             });
      
      var projectCoresCache = Sungero.Projects.ProjectCores.GetAll(p => allProjectCoreIdsForReport.Contains(p.Id)).ToDictionary(x => x.Id);
      
      foreach (var reportDataRow in reportData)
      {
        // проверка рабочих дней
        if (!workingDaysEmployee.ContainsKey(reportDataRow.PerformerId))
        {
          var isWorkingDay = Calendar.IsWorkingDay(reportDataRow.Date, employeesCache[reportDataRow.PerformerId]);
          workingDaysEmployee.Add(reportDataRow.PerformerId, new Dictionary<DateTime, bool>()
                                  {
                                    { reportDataRow.Date, isWorkingDay }
                                  });
        }
        else if (!workingDaysEmployee[reportDataRow.PerformerId].ContainsKey(reportDataRow.Date))
        {
          var isWorkingDay = Calendar.IsWorkingDay(reportDataRow.Date, employeesCache[reportDataRow.PerformerId]);
          workingDaysEmployee[reportDataRow.PerformerId].Add(reportDataRow.Date, isWorkingDay);
        }
        
        if (!workingDaysEmployee[reportDataRow.PerformerId][reportDataRow.Date])
        {
          continue;
        }
        
        var performerId = employeesCache.ContainsKey(reportDataRow.PerformerId) ? reportDataRow.PerformerId : -1;
        
        if (!employeeIdsCache.Contains(performerId))
        {
          var performer = employeesCache.ContainsKey(performerId) ? employeesCache[performerId] : null;
          this.AddTypeRecordEmployee(answer, performer);
          employeeIdsCache.Add(performerId);
        }
        
        if (!projectCoreIdsCache.Contains(reportDataRow.ProjectCoreId.Value))
        {
          var projectCore = reportDataRow.ProjectCoreId != -1 ? projectCoresCache[reportDataRow.ProjectCoreId.Value] : null;
          this.AddTypeRecordProject(answer, projectCore);
        }
        
        recordsWorkingDays.Add(Structures.Module.ResourceReportRow.Create(reportDataRow.ResourceId,
                                                                          reportDataRow.PerformerId,
                                                                          reportDataRow.Busy,
                                                                          reportDataRow.ActivityId,
                                                                          reportDataRow.ActivityName,
                                                                          reportDataRow.ProjectPlanId,
                                                                          reportDataRow.ProjectCoreId,
                                                                          reportDataRow.ManagerName,
                                                                          reportDataRow.Date));
      }
      
      answer.AddRecords(this.GetRecordsByTimeScale(recordsWorkingDays, dateScaleKey, culture));
      
      return Newtonsoft.Json.JsonConvert.SerializeObject(answer.GetObjectForSerialization());
    }
    
    /// <summary>
    /// Получение сгруппированных строк для отчета по объектам управления
    /// </summary>
    /// <param name="recordsWorkingDays">Строки отчета.</param>
    /// <param name="dateScalKey">Ключ детализации.</param>
    /// <param name="culture">Локализация.</param>
    /// <returns>Сгруппированные строки.</returns>
    private List<Dictionary<string, object>> GetRecordsByTimeScale(List<Structures.Module.IResourceReportRow> recordsWorkingDays, string dateScalKey, System.Globalization.CultureInfo culture)
    {
      var records = new List<Dictionary<string, object>>();
      switch (dateScalKey)
      {
        case Constants.Module.DateScaleKey.TimelineWeeks:
          records = recordsWorkingDays.GroupBy(x => new
                                               {
                                                 x.ProjectCoreId,
                                                 x.ProjectPlanId,
                                                 x.ResourceId,
                                                 x.PerformerId,
                                                 x.ActivityId,
                                                 x.ActivityName,
                                                 Year = x.Date.Year,
                                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                                 Month = x.Date.Month,
                                                 Week = this.GetWeekString(x.Date, culture)
                                               }
                                              )
            .Select(x => new Dictionary<string, object>()
                    {
                      { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                      { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                      { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                      { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                      { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                      { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                      { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage },
                      { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                    } ).ToList();
          break;
        case Constants.Module.DateScaleKey.TimelineMonths:
          records = recordsWorkingDays.GroupBy(x => new
                                               {
                                                 x.ProjectCoreId,
                                                 x.ProjectPlanId,
                                                 x.ResourceId,
                                                 x.PerformerId,
                                                 x.ActivityId,
                                                 x.ActivityName,
                                                 Year = x.Date.Year,
                                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                                 Month = x.Date.Month
                                               }
                                              )
            .Select(x => new Dictionary<string, object>()
                    {
                      { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                      { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                      { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter},
                      { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                      { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                      { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage },
                      { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                    } ).ToList();
          break;
        case Constants.Module.DateScaleKey.TimelineQuarters:
          records = recordsWorkingDays.GroupBy(x => new
                                               {
                                                 x.ProjectCoreId,
                                                 x.ProjectPlanId,
                                                 x.ResourceId,
                                                 x.PerformerId,
                                                 x.ActivityId,
                                                 x.ActivityName,
                                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                                 Year = x.Date.Year,
                                               }
                                              )
            .Select(x => new Dictionary<string, object>()
                    {
                      { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                      { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                      { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                      { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                      { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage },
                      { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                    } ).ToList();
          break;
        case Constants.Module.DateScaleKey.TimelineYears:
          records = recordsWorkingDays.GroupBy(x => new
                                               {
                                                 x.ProjectCoreId,
                                                 x.ProjectPlanId,
                                                 x.ResourceId,
                                                 x.PerformerId,
                                                 x.ActivityId,
                                                 x.ActivityName,
                                                 Year = x.Date.Year
                                               }
                                              )
            .Select(x => new Dictionary<string, object>()
                    {
                      { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                      { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                      { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                      { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage },
                      { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                    } ).ToList();
          break;
        default:
          records = recordsWorkingDays.Select(x => new Dictionary<string, object>()
                                              {
                                                { Constants.Module.LocalizationKeys.Performer, x.PerformerId },
                                                { Constants.Module.LocalizationKeys.Date, x.Date },
                                                { Constants.Module.LocalizationKeys.Week, this.GetWeekString(x.Date, culture) },
                                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Date.Month, culture) },
                                                { Constants.Module.LocalizationKeys.Quarter, this.GetQuaerterString(x.Date.Month) },
                                                { Constants.Module.LocalizationKeys.Year, x.Date.Year },
                                                { Constants.Module.LocalizationKeys.Busy, x.Busy },
                                                { Constants.Module.LocalizationKeys.ActivityName, x.ActivityName ?? Resources.OtherStage },
                                                { Constants.Module.LocalizationKeys.Project, x.ProjectCoreId }
                                              } ).ToList();
          break;
          
      }
      return records;
    }
    
    /// <summary>
    /// Получение Uri для формирования сводного отчета по объектам управления.
    /// </summary>
    /// <param name="projectCoreId">Ид проекта.</param>
    /// <param name="dateRange">Диапазон даты.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <returns>Uri.</returns>
    [Public, Remote]
    public Uri GetUriResourceReport(long projectCoreId, string dateRange, string dateScaleKey)
    {
      var defaultPivotTableAddress = Constants.Module.DefaultPivotTableAddress;
      var getter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
      var uriCompReport = getter.GetContentFullAddress(new Uri(defaultPivotTableAddress, UriKind.Relative));
      var builder = new UriBuilder(uriCompReport)
      {
        Query = string.Format(Constants.Module.PathResourcesReportMetadata,
                              string.Format("&projectCoreId={0}&dateRange={1}&dateScaleKey={2}", projectCoreId, System.Net.WebUtility.UrlEncode(dateRange), dateScaleKey))
      };
      
      return builder.Uri;
    }
    
    #endregion
    
    #region Загрузка ресурсов по оргструктуре
    
    /// <summary>
    /// Получение метаданных для сводного отчета по оргструктуре.
    /// </summary>
    /// <param name="businessUnitIds">Ид организаций.</param>
    /// <param name="departmentIds">Ид подразделений.</param>
    /// <param name="dateRange">Период.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <returns>Метаданные для отчета.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public string ResourcesReportMetadataOrg(string businessUnitIds, string departmentIds, string dateRange, string dateScaleKey)
    { 
      var path = Constants.Module.PathResourcesReportOrg;
      var colsDefault = new List<string>();
      
      var dateScaleDictionary = new Dictionary<string, string>()
      {
        { Constants.Module.DateScaleKey.TimelineDays, DateScale.TimelineDays },
        { Constants.Module.DateScaleKey.TimelineWeeks, DateScale.TimelineWeeks },
        { Constants.Module.DateScaleKey.TimelineMonths, DateScale.TimelineMonths },
        { Constants.Module.DateScaleKey.TimelineQuarters, DateScale.TimelineQuarters },
        { Constants.Module.DateScaleKey.TimelineYears, DateScale.TimelineYears }
      };
      
      if (String.IsNullOrEmpty(dateScaleKey) || !dateScaleDictionary.ContainsKey(dateScaleKey))
      {
        throw new Exception($"не удалось получить детализацию по ключу {dateScaleKey ?? "null"}");
      }
      
      var answerMetadata = new DirRX.Planner.Model.Olap.Model.AnswerMetadata();
      answerMetadata.AddTitle(Resources.ResourceReportOrgTitle);
      answerMetadata.AddLocalePath(Constants.Module.PathLocaleResourcesReport);
      
      answerMetadata.AddPath(path);
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.BusinessUnitIds, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.String },
                                { Constants.Module.LocalizationKeys.Default, businessUnitIds },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.BusinessUnitIds },
                                { Constants.Module.LocalizationKeys.IsImmutable, true }
                              }
                             );
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.DepartmentIds, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.String },
                                { Constants.Module.LocalizationKeys.Default, departmentIds },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.DepartmentIds },
                                { Constants.Module.LocalizationKeys.IsImmutable, true }
                              }
                             );
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.DateRange, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.DateRange },
                                { Constants.Module.LocalizationKeys.Default, dateRange },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.DateRange },
                                { Constants.Module.LocalizationKeys.MinValue, null },
                                { Constants.Module.LocalizationKeys.MaxValue, null },
                                { Constants.Module.LocalizationKeys.IsImmutable, false }
                              }
                             );
      
      answerMetadata.AddParam(Constants.Module.LocalizationKeys.DateScaleKey, new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Type, TypeData.DateScale },
                                { Constants.Module.LocalizationKeys.Default, dateScaleKey },
                                { Constants.Module.LocalizationKeys.Name, Constants.Module.LocalizationKeys.DateScale },
                                { Constants.Module.LocalizationKeys.IsImmutable, false }
                              }
                             );
      

      answerMetadata.AddType(Constants.Module.LocalizationKeys.DateScalesParam, new Dictionary<string, object>()
                             {
                               { Constants.Module.DateScaleKey.TimelineDays, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineDays }} },
                               { Constants.Module.DateScaleKey.TimelineWeeks, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineWeeks }} },
                               { Constants.Module.DateScaleKey.TimelineMonths, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineMonths }} },
                               { Constants.Module.DateScaleKey.TimelineQuarters, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineQuarters }} },
                               { Constants.Module.DateScaleKey.TimelineYears, new Dictionary<string, object>() {{ Constants.Module.LocalizationKeys.Name, DateScale.TimelineYears }} }
                             }
                            );
      
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>
                                         ( Constants.Module.LocalizationKeys.Rows, new string[]
                                          {
                                            Constants.Module.LocalizationKeys.AvailabilityStatus,
                                            Constants.Module.LocalizationKeys.EmployeeBusinessUnit,
                                            Constants.Module.LocalizationKeys.EmployeeDepartment,
                                            Constants.Module.LocalizationKeys.EmployeeJobTitle,
                                            Constants.Module.LocalizationKeys.EmployeeName,
                                            Constants.Module.LocalizationKeys.ProjectShortName
                                          }
                                         )
                                        );
          
      switch (dateScaleKey)
      {
        case Constants.Module.DateScaleKey.TimelineYears:
          colsDefault.Add(Constants.Module.LocalizationKeys.Year);
          break;
        case Constants.Module.DateScaleKey.TimelineQuarters:
          colsDefault.Add(Constants.Module.LocalizationKeys.Quarter);
          goto case Constants.Module.DateScaleKey.TimelineYears;
        case Constants.Module.DateScaleKey.TimelineMonths:
          colsDefault.Add(Constants.Module.LocalizationKeys.Month);
          goto case Constants.Module.DateScaleKey.TimelineYears;
        case Constants.Module.DateScaleKey.TimelineWeeks:
          colsDefault.Add(Constants.Module.LocalizationKeys.Week);
          goto case Constants.Module.DateScaleKey.TimelineMonths;
        case Constants.Module.DateScaleKey.TimelineDays:
          colsDefault.Add(Constants.Module.LocalizationKeys.Date);
          goto case Constants.Module.DateScaleKey.TimelineWeeks;
        default:
          goto case Constants.Module.DateScaleKey.TimelineDays;
      }
      
      colsDefault.Reverse();
      
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>(Constants.Module.LocalizationKeys.Cols, colsDefault ));
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>(Constants.Module.LocalizationKeys.AggregateType, Constants.Module.AggregatorsKeys.Sum));
      answerMetadata.AddDefaultViewParam(new KeyValuePair<string, object>(Constants.Module.LocalizationKeys.AggregateCol, Constants.Module.LocalizationKeys.Busy));
      
      return Newtonsoft.Json.JsonConvert.SerializeObject(answerMetadata.GetObjectForSerialization());
    }
    
    /// <summary>
    /// Получение данных для сводного отчета по оргструктуре.
    /// </summary>
    /// <param name="businessUnitIds">Ид организаций.</param>
    /// <param name="departmentIds">Ид подразделений.</param>
    /// <param name="dateRange">Период.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <returns>Данные для отчета в json.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public string ResourcesReportOrg(string businessUnitIds, string departmentIds, string dateRange, string dateScaleKey)
    {
      var allProjectIds = Sungero.Projects.ProjectCores.GetAll().Select(x => x.Id);
      var allPlanIds = DirRX.ProjectPlanner.ProjectPlanRXes.GetAll().Select(x => x.Id);
      var answer = new DirRX.Planner.Model.Olap.Model.Answer();
      var records = new List<Dictionary<string, object>>();
      var recordsWorkingDays = new List<Structures.Module.IResourceReportOrgRow>();
      var workingDaysEmployee = new Dictionary<long, Dictionary<DateTime, bool>>();
      var reportData = new List<Structures.Module.IResourceReportRow>();
      var culture = System.Globalization.CultureInfo.CurrentUICulture;
      DateTime startDate;
      DateTime endDate;
      List<long> departmentIntIds = null;
      List<long> allSubDepartmentIds = null;
      
      if (String.IsNullOrEmpty(departmentIds))
      {
        var businessUnitIdsInt = this.GetNumbersFromString(businessUnitIds);
        departmentIntIds = Sungero.Company.Departments.GetAll(d => d.BusinessUnit != null && businessUnitIdsInt.Contains(d.BusinessUnit.Id)).Select(d => d.Id).ToList();
      }
      else
      {
        departmentIntIds = this.GetNumbersFromString(departmentIds).ToList();
      }
      
      if (departmentIntIds == null || !departmentIntIds.Any())
      {
        return Newtonsoft.Json.JsonConvert.SerializeObject(answer.GetObjectForSerialization());
      }
      
      try
      {
        string[] dateStrings = dateRange.Split('/');
        startDate = DateTime.ParseExact(dateStrings[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        endDate = DateTime.ParseExact(dateStrings[1], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
      }
      catch(Exception ex)
      {
        throw new Exception($"неверный формат даты в параметре dateRange={dateRange}", ex);
      }
      
      this.AddTypes(answer, dateScaleKey);
      
      using (var connection = CreateDBConnection())
      {
        allSubDepartmentIds = this.GetSubDepartmentsFromDb(connection, departmentIntIds);
        reportData = this.GetReportDataOrgFromDb(connection, allPlanIds, startDate, endDate, allProjectIds, allSubDepartmentIds);
      }
      
      var employeeIdsCache = new HashSet<long>();
      var projectCoreIdsCache = new HashSet<long>();
      
      var allProjectCoreIdsForReport = new HashSet<long>(reportData.Where(x => x.ProjectCoreId != null).Select(x => x.ProjectCoreId.Value));
      Dictionary<long, IEmployee> employeesCache = null;
      
      AccessRights.AllowRead(() =>
                             {
                               employeesCache = Sungero.Company.Employees.GetAll(emp => allSubDepartmentIds.Contains(emp.Department.Id)).ToDictionary(x => x.Id);
                             });
      
      var projectCoresCache = Sungero.Projects.ProjectCores.GetAll(p => allProjectCoreIdsForReport.Contains(p.Id)).ToDictionary(x => x.Id);
      
      employeesCache.Values.ToList().ForEach(emp => this.AddTypeRecordEmployee(answer, emp));
      
      foreach (var reportDataRow in reportData)
      {
        // проверка рабочих дней
        if (!workingDaysEmployee.ContainsKey(reportDataRow.PerformerId))
        {
          var isWorkingDay = Calendar.IsWorkingDay(reportDataRow.Date, employeesCache[reportDataRow.PerformerId]);
          workingDaysEmployee.Add(reportDataRow.PerformerId, new Dictionary<DateTime, bool>()
                                  {
                                    { reportDataRow.Date, isWorkingDay }
                                  });
        }
        else if (!workingDaysEmployee[reportDataRow.PerformerId].ContainsKey(reportDataRow.Date))
        {
          var isWorkingDay = Calendar.IsWorkingDay(reportDataRow.Date, employeesCache[reportDataRow.PerformerId]);
          workingDaysEmployee[reportDataRow.PerformerId].Add(reportDataRow.Date, isWorkingDay);
        }
        
        if (!workingDaysEmployee[reportDataRow.PerformerId][reportDataRow.Date])
        {
          continue;
        }
        
        if (reportDataRow.ProjectCoreId != null && !projectCoreIdsCache.Contains(reportDataRow.ProjectCoreId.Value))
        {
          var projectCore = reportDataRow.ProjectCoreId != -1 ? projectCoresCache[reportDataRow.ProjectCoreId.Value] : null;
          this.AddTypeRecordProject(answer, projectCore);
        }
        
        recordsWorkingDays.Add(Structures.Module.ResourceReportOrgRow.Create(reportDataRow.ResourceId,
                                                                          reportDataRow.PerformerId,
                                                                          reportDataRow.Busy,
                                                                          reportDataRow.ActivityId,
                                                                          reportDataRow.ActivityName,
                                                                          reportDataRow.ProjectPlanId,
                                                                          reportDataRow.ProjectCoreId,
                                                                          reportDataRow.ManagerName,
                                                                          reportDataRow.Date,
                                                                          Resources.Workload));
      }
      
      answer.AddRecords(this.GetRecordsOrgByTimeScale(recordsWorkingDays, dateScaleKey, culture, employeesCache, startDate, endDate));
      
      return Newtonsoft.Json.JsonConvert.SerializeObject(answer.GetObjectForSerialization());
    }
    
    /// <summary>
    /// Получить дочерние подразделения.
    /// </summary>
    /// <param name="connection">Подключение к БД.</param>
    /// <param name="departmentIntIds">Ид подразделений.</param>
    /// <returns>Дочерние подразделения, включая указанные подразделения.</returns>
    private List<long> GetSubDepartmentsFromDb(System.Data.IDbConnection connection, List<long> departmentIntIds)
    {
      var allSubDepartments = new List<long>();
      
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetSubDepartments.Replace("@departmentIds", string.Join(", ", departmentIntIds));
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            allSubDepartments.Add(reader.GetInt64(0));
          }
        }
      }
      
      return allSubDepartments;
    }
    
    /// <summary>
    /// Получить данные для сводного отчета по оргструктуре из БД.
    /// </summary>
    /// <param name="connection">Подключение к БД.</param>
    /// <param name="allPlanIds">Ид доступных планов.</param>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата окончания.</param>
    /// <param name="allProjectIds">Ид доступных проектов.</param>
    /// <param name="allSubDepartmentIds">Ид подразделений.</param>
    /// <returns>Данные для сводного отчета по оргструктуре из БД</returns>
    private List<Structures.Module.IResourceReportRow> GetReportDataOrgFromDb(System.Data.IDbConnection connection,
                                                                              IQueryable<long> allPlanIds,
                                                                              DateTime startDate,
                                                                              DateTime endDate,
                                                                              IQueryable<long> allProjectIds,
                                                                              List<long> allSubDepartmentIds)
    {
      var reportData = new List<Structures.Module.IResourceReportRow>();
      
      using (var command = connection.CreateCommand())
      {
        command.CommandText = Queries.Module.GetOlapPlanReportDataOrg;
        
        // HACK: AddArrayParameter работает нестабильно, если коллекция allPlanIds пустая.
        // HACK: Поэтому добавил replace с условным 0
        if (allPlanIds.Any())
          SQL.AddArrayParameter(command, "@allPlans", allPlanIds, System.Data.DbType.Int64);
        else
          command.CommandText = command.CommandText.Replace("@allPlans", "0");
        
        if (allProjectIds.Any())
          SQL.AddArrayParameter(command, "@all_projects_with_access", allProjectIds, System.Data.DbType.Int64);
        else
          command.CommandText = command.CommandText.Replace("@all_projects_with_access", "0");
        
        if (allSubDepartmentIds.Any())
          SQL.AddArrayParameter(command, "@allSubDepartmentIds", allSubDepartmentIds, System.Data.DbType.Int64);
        else
          command.CommandText = command.CommandText.Replace("@allSubDepartmentIds", "0");
        
        SQL.AddParameter(command, "@startDatePeriod", startDate, System.Data.DbType.DateTime);
        SQL.AddParameter(command, "@endDatePeriod", endDate, System.Data.DbType.DateTime);
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var olapReportDataRow = Structures.Module.ResourceReportRow.Create(
              reader.GetInt64(0),
              reader.GetInt64(1),
              reader.GetFloat(2),
              reader.GetInt64(3),
              !reader.IsDBNull(4) ? reader.GetString(4) : null,
              reader.GetInt64(5),
              !reader.IsDBNull(6) ? (Nullable<long>)reader.GetInt64(6) : null,
              !reader.IsDBNull(7) ? reader.GetString(7) : null,
              reader.GetDateTime(8)
             );
            
            reportData.Add(olapReportDataRow);
          }
        }
      }
      
      return reportData;
    }
    
    /// <summary>
    /// Получение сгруппированных строк для отчета по оргструктуре.
    /// </summary>
    /// <param name="recordsWorkingDays">Строки отчета.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <param name="culture">Локализация.</param>
    /// <param name="employeesCache">Кэш сотрудников.</param>
    /// <returns>Сгруппированные строки.</returns>
    private List<Dictionary<string, object>> GetRecordsOrgByTimeScale(List<Structures.Module.IResourceReportOrgRow> recordsWorkingDays,
                                                                      string dateScaleKey,
                                                                      System.Globalization.CultureInfo culture,
                                                                      Dictionary<long, IEmployee> employeesCache,
                                                                      DateTime startDate,
                                                                      DateTime endDate)
    {
      var records = new List<Dictionary<string, object>>();
      var performerIds = employeesCache.Keys;
      var allDays = Enumerable.Range(0, (endDate - startDate).Days + 1).Select(offset => startDate.AddDays(offset));
      var performerDates = performerIds.SelectMany(performerId => allDays.Select(day => new { PerformerId = performerId, Date = day })).ToList();
      
      switch (dateScaleKey)
      {
        case Constants.Module.DateScaleKey.TimelineWeeks:
          
          // Загрузка
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.ProjectCoreId,
                                                   x.ProjectPlanId,
                                                   x.ResourceId,
                                                   x.PerformerId,
                                                   x.ActivityId,
                                                   x.ActivityName,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month),
                                                   Month = x.Date.Month,
                                                   Week = this.GetWeekString(x.Date, culture)
                                                 }
                                                )
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Workload.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage.ToString() },
                                { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                              } ));
          
          // Свободное время по ресурсам
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.PerformerId,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month),
                                                   Month = x.Date.Month,
                                                   Week = this.GetWeekString(x.Date, culture),
                                                   WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(this.GetBeginningWeekOrMonth(x.Date),
                                                                                                                            this.GetEndWeekOrBeginningMonth(x.Date),
                                                                                                                            employeesCache[x.PerformerId])
                                                 })
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime - x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }
                             ));
          
          // Свободное время по отсутсвующим ресурсам
          records.Add(performerDates
                      .Where(x => !records.Any(record => record[Constants.Module.LocalizationKeys.Week].ToString() == this.GetWeekString(x.Date, culture) &&
                                               (int)record[Constants.Module.LocalizationKeys.Year] == x.Date.Year))
                      .GroupBy(x => new
                               {
                                 x.PerformerId,
                                 Year = x.Date.Year,
                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                 Month = x.Date.Month,
                                 Week = this.GetWeekString(x.Date, culture),
                                 WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(this.GetBeginningWeekOrMonth(x.Date),
                                                                                                          this.GetEndWeekOrBeginningMonth(x.Date),
                                                                                                          employeesCache[x.PerformerId])
                               })
                      .Where(x => x.Key.WorkingTime != 0)
                      .Select(x => new Dictionary<string, object>
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }));
          break;
        case Constants.Module.DateScaleKey.TimelineMonths:

          // Загрузка
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.ProjectCoreId,
                                                   x.ProjectPlanId,
                                                   x.ResourceId,
                                                   x.PerformerId,
                                                   x.ActivityId,
                                                   x.ActivityName,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month),
                                                   Month = x.Date.Month
                                                 }
                                                )
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Workload.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage.ToString() },
                                { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                              } ));
          
          // Свободное время по ресурсам
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.PerformerId,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month),
                                                   Month = x.Date.Month,
                                                   WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date.BeginningOfMonth(),
                                                                                                                            x.Date.EndOfMonth(),
                                                                                                                            employeesCache[x.PerformerId])
                                                 })
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime - x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }
                             ));
          
          // Свободное время по отсутсвующим ресурсам
          records.Add(performerDates
                      .Where(x => !records.Any(record => record[Constants.Module.LocalizationKeys.Month].ToString() == this.GetMonthString(x.Date.Month, culture) &&
                                               (int)record[Constants.Module.LocalizationKeys.Year] == x.Date.Year))
                      .GroupBy(x => new
                               {
                                 x.PerformerId,
                                 Year = x.Date.Year,
                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                 Month = x.Date.Month,
                                 WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date.BeginningOfMonth(),
                                                                                                          x.Date.EndOfMonth(),
                                                                                                          employeesCache[x.PerformerId])
                               })
                      .Where(x => x.Key.WorkingTime != 0)
                      .Select(x => new Dictionary<string, object>
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }));
          break;
        case Constants.Module.DateScaleKey.TimelineQuarters:
          
          // Загрузка
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.ProjectCoreId,
                                                   x.ProjectPlanId,
                                                   x.ResourceId,
                                                   x.PerformerId,
                                                   x.ActivityId,
                                                   x.ActivityName,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month)
                                                 }
                                                )
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Workload.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage.ToString() },
                                { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                              } ));
          
          // Свободное время по ресурсам
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.PerformerId,
                                                   Year = x.Date.Year,
                                                   Quarter = this.GetQuaerterString(x.Date.Month),
                                                   WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(this.GetBeginningQuarter(x.Date),
                                                                                                                            this.GetEndQuarter(x.Date),
                                                                                                                            employeesCache[x.PerformerId])
                                                 })
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime - x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }
                             ));
          
          // Свободное время по отсутсвующим ресурсам
          records.Add(performerDates
                      .Where(x => !records.Any(record => record[Constants.Module.LocalizationKeys.Quarter].ToString() == this.GetQuaerterString(x.Date.Month) &&
                                               (int)record[Constants.Module.LocalizationKeys.Year] == x.Date.Year))
                      .GroupBy(x => new
                               {
                                 x.PerformerId,
                                 Year = x.Date.Year,
                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                 WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(this.GetBeginningQuarter(x.Date),
                                                                                                          this.GetEndQuarter(x.Date),
                                                                                                          employeesCache[x.PerformerId])
                               })
                      .Where(x => x.Key.WorkingTime != 0)
                      .Select(x => new Dictionary<string, object>
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }));
          
          break;
        case Constants.Module.DateScaleKey.TimelineYears:
          
          // Загрузка
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.ProjectCoreId,
                                                   x.ProjectPlanId,
                                                   x.ResourceId,
                                                   x.PerformerId,
                                                   x.ActivityId,
                                                   x.ActivityName,
                                                   Year = x.Date.Year
                                                 }
                                                )
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Workload.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, x.Key.ActivityName ?? Resources.OtherStage.ToString() },
                                { Constants.Module.LocalizationKeys.Project, x.Key.ProjectCoreId }
                              } ));
          
          // Свободное время по ресурсам
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                 {
                                                   x.PerformerId,
                                                   Year = x.Date.Year,
                                                   WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date.BeginningOfYear(),
                                                                                                                            x.Date.EndOfYear(),
                                                                                                                            employeesCache[x.PerformerId])
                                                 })
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime - x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }
                             ));
          
          // Свободное время по отсутсвующим ресурсам
          records.Add(performerDates
                      .Where(x => !records.Any(record => (int)record[Constants.Module.LocalizationKeys.Year] == x.Date.Year))
                      .GroupBy(x => new
                               {
                                 x.PerformerId,
                                 Year = x.Date.Year,
                                 WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date.BeginningOfYear(),
                                                                                                          x.Date.EndOfYear(),
                                                                                                          employeesCache[x.PerformerId])
                               })
                      .Where(x => x.Key.WorkingTime != 0)
                      .Select(x => new Dictionary<string, object>
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime.ToString() },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }));
          break;
        default:
          
          // Загрузка
          records.Add(recordsWorkingDays.Select(x => new Dictionary<string, object>()
                                                {
                                                  { Constants.Module.LocalizationKeys.Performer, x.PerformerId },
                                                  { Constants.Module.LocalizationKeys.Date, x.Date },
                                                  { Constants.Module.LocalizationKeys.Week, this.GetWeekString(x.Date, culture) },
                                                  { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Date.Month, culture) },
                                                  { Constants.Module.LocalizationKeys.Quarter, this.GetQuaerterString(x.Date.Month) },
                                                  { Constants.Module.LocalizationKeys.Year, x.Date.Year },
                                                  { Constants.Module.LocalizationKeys.Busy, x.Busy },
                                                  { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Workload.ToString() },
                                                  { Constants.Module.LocalizationKeys.ActivityName, x.ActivityName ?? Resources.OtherStage },
                                                  { Constants.Module.LocalizationKeys.Project, x.ProjectCoreId }
                                                } ));
          
          // Свободное время по ресурсам
          records.Add(recordsWorkingDays.GroupBy(x => new
                                                  {
                                                    x.PerformerId,
                                                    Year = x.Date.Year,
                                                    Quarter = this.GetQuaerterString(x.Date.Month),
                                                    Month = x.Date.Month,
                                                    Week = this.GetWeekString(x.Date, culture),
                                                    Date = x.Date,
                                                    WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date,
                                                                                                                             x.Date,
                                                                                                                             employeesCache[x.PerformerId])
                                                  })
                      .Select(x => new Dictionary<string, object>()
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Date, x.Key.Date },
                                { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime - x.Sum(r => r.Busy) },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }
                             ));
          
          // Свободное время по отсутсвующим ресурсам
          records.Add(performerDates
                      .Where(x => !records.Any(record => record[Constants.Module.LocalizationKeys.Date].ToString() == x.Date.ToString()))
                      .GroupBy(x => new
                               {
                                 x.PerformerId,
                                 Year = x.Date.Year,
                                 Quarter = this.GetQuaerterString(x.Date.Month),
                                 Month = x.Date.Month,
                                 Week = this.GetWeekString(x.Date, culture),
                                 Date = x.Date,
                                 WorkingTime = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(x.Date,
                                                                                                          x.Date,
                                                                                                          employeesCache[x.PerformerId])
                               })
                      .Where(x => x.Key.WorkingTime != 0)
                      .Select(x => new Dictionary<string, object>
                              {
                                { Constants.Module.LocalizationKeys.Performer, x.Key.PerformerId },
                                { Constants.Module.LocalizationKeys.Date, x.Key.Date },
                                { Constants.Module.LocalizationKeys.Week, x.Key.Week },
                                { Constants.Module.LocalizationKeys.Month, this.GetMonthString(x.Key.Month, culture) },
                                { Constants.Module.LocalizationKeys.Quarter, x.Key.Quarter },
                                { Constants.Module.LocalizationKeys.Year, x.Key.Year },
                                { Constants.Module.LocalizationKeys.Busy, x.Key.WorkingTime },
                                { Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.Available.ToString() },
                                { Constants.Module.LocalizationKeys.ActivityName, null },
                                { Constants.Module.LocalizationKeys.Project, null }
                              }));
          
          break;
          
      }
      
      return records;
    }
    
    /// <summary>
    /// Получение Uri для формирования сводного отчета по оргструктуре.
    /// </summary>
    /// <param name="businessUnitId">Ид организации.</param>
    /// <param name="departmentId">Ид подразделения.</param>
    /// <param name="dateRange">Диапазон даты.</param>
    /// <param name="dateScaleKey">Ключ детализации.</param>
    /// <returns>Uri.</returns>
    [Public, Remote]
    public Uri GetUriResourceReportOrg(List<long> businessUnitIds, List<long> departmentIds, string dateRange, string dateScaleKey)
    {
      var defaultPivotTableAddress = Constants.Module.DefaultPivotTableAddress;
      var getter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
      var uriCompReport = getter.GetContentFullAddress(new Uri(defaultPivotTableAddress, UriKind.Relative));
      
      var businessUnitIdsString = businessUnitIds.Any() ? "[" + string.Join(",", businessUnitIds) + "]" : "null";
      var departmentIdsString = departmentIds.Any() ? "[" + string.Join(",", departmentIds) + "]" : "null";

      var builder = new UriBuilder(uriCompReport)
      {
        Query = string.Format(Constants.Module.PathResourcesReportMetadataOrg,
                              string.Format("&businessUnitIds={0}&departmentIds={1}&dateRange={2}&dateScaleKey={3}",
                                            businessUnitIdsString,
                                            departmentIdsString,
                                            System.Net.WebUtility.UrlEncode(dateRange),
                                            dateScaleKey))
      };
      
      return builder.Uri;
    }
    #endregion
    
    #region Общие методы для отчета
    /// <summary>
    /// Получить числа из параметра запроса, разделенных ","
    /// </summary>
    /// <param name="queryString">Параметр запроса.</param>
    /// <returns>Хэш чисел.</returns>
    private HashSet<long> GetNumbersFromString(string queryString)
    {
      var intNumbers = new HashSet<long>();
      
      try
      {
        intNumbers = new HashSet<long>(queryString.Trim('[', ']').Split(',').Select(long.Parse));
      }
      catch(Exception ex)
      {
        throw new Exception($"не удалось получить id организаций или подразделений из строки {queryString}", ex);
      }
      
      return intNumbers;
    }
    
    /// <summary>
    /// Добавить кастомные типы данных в модель.
    /// </summary>
    /// <param name="resourceReport">Данные для отчета.</param>
    private void AddTypes(DirRX.Planner.Model.Olap.Model.Answer answer, string dateScale)
    {
      var recordTypes = new Dictionary<string, string>()
      {
        { Constants.Module.LocalizationKeys.Performer, TypeData.Employee },
        { Constants.Module.LocalizationKeys.Year, TypeData.Number },
        { Constants.Module.LocalizationKeys.Busy, TypeData.Number },
        { Constants.Module.LocalizationKeys.ActivityName, TypeData.String },
        { Constants.Module.LocalizationKeys.AvailabilityStatus, TypeData.String },
        { Constants.Module.LocalizationKeys.Project, Constants.Module.LocalizationKeys.Project }
      };
      
      switch (dateScale)
      {
        case Constants.Module.DateScaleKey.TimelineQuarters:
          recordTypes.Add(Constants.Module.LocalizationKeys.Quarter, TypeData.String);
          break;
        case Constants.Module.DateScaleKey.TimelineMonths:
          recordTypes.Add(Constants.Module.LocalizationKeys.Month, TypeData.String);
          goto case Constants.Module.DateScaleKey.TimelineQuarters;
        case Constants.Module.DateScaleKey.TimelineWeeks:
          recordTypes.Add(Constants.Module.LocalizationKeys.Week, TypeData.String);
          goto case Constants.Module.DateScaleKey.TimelineMonths;
        case Constants.Module.DateScaleKey.TimelineDays:
          recordTypes.Add(Constants.Module.LocalizationKeys.Date, TypeData.Date);
          goto case Constants.Module.DateScaleKey.TimelineMonths;
        default:
          goto case Constants.Module.DateScaleKey.TimelineDays;
      }
      
      answer.AddType("records", recordTypes);
      
      answer.AddType(Constants.Module.LocalizationKeys.Project, new Dictionary<string, string>()
                     {
                       { Constants.Module.LocalizationKeys.ShortName, TypeData.String },
                       { Constants.Module.LocalizationKeys.State, TypeData.String },
                       { Constants.Module.LocalizationKeys.Kind, TypeData.String },
                       { Constants.Module.LocalizationKeys.IncludedIn, TypeData.String },
                       { Constants.Module.LocalizationKeys.Manager, TypeData.String }
                     }
                    );
      
      answer.AddType(TypeData.Employee, new Dictionary<string, string>()
                     {
                       { Constants.Module.LocalizationKeys.Name, TypeData.String },
                       { Constants.Module.LocalizationKeys.Department, TypeData.String },
                       { Constants.Module.LocalizationKeys.JobTitle, TypeData.String },
                       { Constants.Module.LocalizationKeys.BusinessUnit, TypeData.String }
                     }
                    );
    }
    
    /// <summary>
    /// Добавить информацию о сотруднике в отчет.
    /// </summary>
    /// <param name="resourceReport">Данные для отчета.</param>
    /// <param name="employee">Сотрудник.</param>
    private void AddTypeRecordEmployee(DirRX.Planner.Model.Olap.Model.Answer answer, IEmployee employee)
    {
      string employeeName = Resources.NoAccessRightsName;
      string departmentName = Resources.NoAccessRightsName;
      string jobTitleName = Resources.NoAccessRightsName;
      string businessUnit = Resources.NoAccessRightsName;
      long employeeId = -1;
      
      if (employee != null)
      {
        employeeName = employee.Name;
        departmentName = employee.Department?.DisplayValue;
        businessUnit = employee.Department?.BusinessUnit?.DisplayValue;
        jobTitleName =  employee.JobTitle?.DisplayValue;
        employeeId = employee.Id;
      }
      
      answer.AddTypeRecord(TypeData.Employee, employeeId.ToString(), new Dictionary<string, object>()
                           {
                             { Constants.Module.LocalizationKeys.Name, employeeName },
                             { Constants.Module.LocalizationKeys.Department, departmentName },
                             { Constants.Module.LocalizationKeys.JobTitle, jobTitleName },
                             { Constants.Module.LocalizationKeys.BusinessUnit, businessUnit }
                           }
                          );
    }
    
    /// <summary>
    /// Добавить информацию о проекте в отчет.
    /// </summary>
    /// <param name="resourceReport">Данные для отчета.</param>
    /// <param name="projectCore">Проект.</param>
    private void AddTypeRecordProject(DirRX.Planner.Model.Olap.Model.Answer answer, Sungero.Projects.IProjectCore projectCore)
    {
      string shortName = Resources.OtherProject;
      string state = Resources.OtherState;
      string kind = Resources.OtherKind;
      string includedIn = Resources.OtherLeadingProject;
      string managerName = Resources.OtherManager;
      long projectCoreId = -1;
      
      if (projectCore != null)
      {
        shortName = projectCore.ShortName;
        state = Sungero.Projects.ProjectCores.Info.Properties.Stage.GetLocalizedValue(projectCore.Stage);
        kind = projectCore.ProjectKind?.DisplayValue;
        includedIn = projectCore.LeadingProject?.Name;
        projectCoreId = projectCore.Id;
        managerName = projectCore.Manager?.Name;
      }
      
      answer.AddTypeRecord(Constants.Module.LocalizationKeys.Project, projectCoreId.ToString(), new Dictionary<string, object>()
                           {
                             { Constants.Module.LocalizationKeys.ShortName, shortName },
                             { Constants.Module.LocalizationKeys.State, state },
                             { Constants.Module.LocalizationKeys.Kind, kind },
                             { Constants.Module.LocalizationKeys.IncludedIn, includedIn },
                             { Constants.Module.LocalizationKeys.Manager, managerName }
                           }
                          );
    }
    
    /// <summary>
    /// Получить строку для отображения недели.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="culture">Локаль.</param>
    /// <returns>Отображение недели.</returns>
    private string GetWeekString(DateTime date, System.Globalization.CultureInfo culture)
    {
      var diff = date.DayOfWeek - DayOfWeek.Monday;
      var weekYear = culture.Calendar.GetWeekOfYear(date,
                                                    culture.DateTimeFormat.CalendarWeekRule,
                                                    culture.DateTimeFormat.FirstDayOfWeek);
      if (diff < 0)
      {
        diff += 7;
      }
      
      var dateStartWeek =  date.AddDays(-diff);
      string dateFormat = null;
      
      if (Equals(culture, System.Globalization.CultureInfo.GetCultureInfo("en-US")))
      {
        dateFormat = "MM/dd";
      }
      else if (Equals(culture, System.Globalization.CultureInfo.GetCultureInfo("ru-RU")))
      {
        dateFormat = "dd.MM";
      }
      else
      {
        dateFormat = "dd/MM";
      }
      
      return string.Format("#{0}: {1}", weekYear, dateStartWeek.ToString(dateFormat));
    }
    
    /// <summary>
    /// Получить строку для отображения месяца.
    /// </summary>
    /// <param name="monthNumber">Номер месяца.</param>
    /// <param name="culture">Локаль.</param>
    /// <returns>Отображение месяца.</returns>
    private string GetMonthString(int monthNumber, System.Globalization.CultureInfo culture)
    {
      var monthName = culture.TextInfo.ToTitleCase(culture.DateTimeFormat.GetMonthName(monthNumber));
      var monthNumberFormat = monthNumber.ToString("D2");
      
      return string.Format("{0}-{1}", monthNumberFormat, monthName);
    }
    
    /// <summary>
    /// Получить номер квартала.
    /// </summary>
    /// <param name="monthNumber">Номер месяца.</param>
    /// <returns>Отображение квартала.</returns>
    private string GetQuaerterString(int monthNumber)
    {
      var quarter = (monthNumber - 1) / 3 + 1;
      return Resources.QuarterStringFormat(quarter);
    }
    
    /// <summary>
    /// Получить начало недели в зависимости от месяца.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Начало недели или начало месяца.</returns>
    private DateTime GetBeginningWeekOrMonth(DateTime date)
    {
      var month = date.Month;
      var beginningDate = date.BeginningOfWeek();
      
      if (beginningDate.Month != month)
      {
        beginningDate = date.BeginningOfMonth();
      }
      
      return beginningDate;
    }
    
    /// <summary>
    /// Получить конец недели в зависимости от месяца.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Конец недели или начало месяца.</returns>
    private DateTime GetEndWeekOrBeginningMonth(DateTime date)
    {
      var month = date.Month;
      var endDate = date.EndOfWeek();
      
      if (endDate.Month != month)
      {
        endDate = date.EndOfMonth();
      }
      
      return endDate;
    }
    
    /// <summary>
    /// Получить начало квартала даты.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Дата начала квартала.</returns>
    private DateTime GetBeginningQuarter(DateTime date)
    {
      var year = date.Year;
      var month = date.Month;
      var beginningQuarter = new DateTime(year, 3 * ((month - 1) / 3 + 1) -2, 1);

      return beginningQuarter;
    }
    
    /// <summary>
    /// Получить дату окончания квартала.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Дата окончания квартала.</returns>
    private DateTime GetEndQuarter(DateTime date)
    {
      var year = date.Year;
      var month = date.Month;
      var beginningQuarter = new DateTime(year, 3 * ((month - 1) / 3 + 1) -2, 1);
      var endQuarter = beginningQuarter.AddMonths(3).AddDays(-1);

      return endQuarter;
    }
    
    /// <summary>
    /// Сформировать диапазон дат из даты начала и даты окончания.
    /// </summary>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата окончания.</param>
    /// <returns>Диапазон дат.</returns>
    [Public, Remote]
    public string GetDateRange(DateTime startDate, DateTime endDate)
    {
      var dateRangeModel = new DirRX.Planner.Model.Olap.Model.DateRange(startDate, endDate);
      return dateRangeModel.GetDateRange();
    }
    
    /// <summary>
    /// Получение локализации для сводного отчета.
    /// </summary>
    /// <returns>Локализация.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string GetLocaleResourcesReport()
    {
      var localeResources = new Dictionary<string, string>();
      var culture = System.Globalization.CultureInfo.CurrentUICulture;
      
      using (Sungero.Core.CultureInfoExtensions.SwitchTo(culture))
      {
        // проект
        localeResources.Add(Constants.Module.LocalizationKeys.ProjectShortName, Resources.Project);
        localeResources.Add(Constants.Module.LocalizationKeys.ProjectIncludedIn, ProjectCores.Info.Properties.LeadingProject.LocalizedName);
        localeResources.Add(Constants.Module.LocalizationKeys.ProjectKind, ProjectCores.Info.Properties.ProjectKind.LocalizedName);
        localeResources.Add(Constants.Module.LocalizationKeys.ProjectState, ProjectCores.Info.Properties.Stage.LocalizedName);
        localeResources.Add(Constants.Module.LocalizationKeys.ProjectCoreId, Resources.ProjectCoreId);
        
        // сотрудник
        localeResources.Add(Constants.Module.LocalizationKeys.EmployeeName, Employees.Info.LocalizedName);
        localeResources.Add(Constants.Module.LocalizationKeys.EmployeeDepartment, Employees.Info.Properties.Department.LocalizedName);
        localeResources.Add(Constants.Module.LocalizationKeys.EmployeeBusinessUnit, Resources.BusinessUnit);
        localeResources.Add(Constants.Module.LocalizationKeys.Busy, Resources.Workload);
        localeResources.Add(Constants.Module.LocalizationKeys.AvailabilityStatus, Resources.AvailabilityStatus);
        localeResources.Add(Constants.Module.LocalizationKeys.EmployeeJobTitle, Resources.JobTitle);
        localeResources.Add(Constants.Module.LocalizationKeys.ActivityName, Resources.ActivityName);
        
        // детализация
        localeResources.Add(Constants.Module.LocalizationKeys.DateRange, Resources.DateRange);
        localeResources.Add(Constants.Module.LocalizationKeys.Year, Resources.Year);
        localeResources.Add(Constants.Module.LocalizationKeys.Quarter, Resources.Quarter);
        localeResources.Add(Constants.Module.LocalizationKeys.Month, Resources.Month);
        localeResources.Add(Constants.Module.LocalizationKeys.Date, Resources.Date);
        localeResources.Add(Constants.Module.LocalizationKeys.DateScale, Resources.WorkloadAnalysisTimeline);
        
        // выбор детализации
        localeResources.Add(DateScale.TimelineDays, Resources.DaysTimeline);
        localeResources.Add(DateScale.TimelineWeeks, Resources.WeeksTimeline);
        localeResources.Add(DateScale.TimelineMonths, Resources.MonthsTimeline);
        localeResources.Add(DateScale.TimelineQuarters, Resources.QuartersTimeline);
        localeResources.Add(DateScale.TimelineYears, Resources.YearsTimeline);
        
        // руководитель проекта
        localeResources.Add(Constants.Module.LocalizationKeys.ManagerProjectName, Resources.ManagerName);
      }
      
      return Newtonsoft.Json.JsonConvert.SerializeObject(localeResources);
    }
    #endregion
    
    #endregion
    
    
    /// <summary>
    /// Получает самые актуальные контрольные точки на дату.
    /// </summary>
    /// <param name="model">Список контрольных точек.</param>
    [Public(WebApiRequestType = RequestType.Get)]
    public List<DirRX.ProjectPlanner.Structures.Module.IGateDto> GetActualGates(double dateFilter)
    {
      var startDate = GetDateTimeFromJsDateSeconds(dateFilter);
      
      return DirRX.ProjectPlanner.Gates
        .GetAll(c => c.PlanDate.HasValue && c.PlanDate.Value >= startDate)
        .OrderBy(c => c.PlanDate)
        .Take(Constants.Module.ActualGatesSearchLimit)
        .ToList()
        .Select(c => CastGateToDto(c))
        .ToList();
    }
    
    /// <summary>
    /// Получает список контрольных точек с фильтром по имени.
    /// </summary>
    /// <param name="model">Список контрольных точек.</param>
    [Public(WebApiRequestType = RequestType.Get)]
    public List<DirRX.ProjectPlanner.Structures.Module.IGateDto> SearchGatesByName(string nameFilter)
    {
      if (string.IsNullOrWhiteSpace(nameFilter))
      {
        return new List<DirRX.ProjectPlanner.Structures.Module.IGateDto>();
      }
      
      nameFilter = nameFilter.ToUpper();
      
      return DirRX.ProjectPlanner.Gates
        .GetAll(c => c.Name.ToUpper().Contains(nameFilter))
        .OrderBy(c => c.Name)
        .ThenBy(c => c.PlanDate)
        .Take(Constants.Module.GatesSearchLimit)
        .ToList()
        .Select(c => CastGateToDto(c))
        .ToList();
    }
    
    /// <summary>
    /// Получает контрольные точки для вех из версии плана проекта.
    /// </summary>
    /// <param name="model">Список связанных с вехами контрольных точек.</param>
    private static List<DirRX.ProjectPlanner.Structures.Module.IActivityGateDto> GetGatesForPlan(long projectPlanId, int versionNumber)
    {
      var projectActivities = ProjectActivities
        .GetAll(a => a.NumberVersion.Value == versionNumber
                && a.ProjectPlan.Id == projectPlanId
                && a.TypeActivity.Equals(DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone))
        .ToList();
      
      if (projectActivities.Count == 0)
      {
        return new List<DirRX.ProjectPlanner.Structures.Module.IActivityGateDto>();
      }
      
      var allGateIds = projectActivities.Where(a => a.GateId.HasValue && a.GateId.Value > 0).Select(a => a.GateId.Value);
      var gates = DirRX.ProjectPlanner.Gates.GetAll();
      
      var activityGates = projectActivities
        .Join(gates,
              activity => activity.GateId,
              gate => gate.Id,
              (a, g) => DirRX.ProjectPlanner.Structures.Module.ActivityGateDto.Create(a.Id, CastGateToDto(g)))
        .ToList();
      
      var deletedGatesIds = allGateIds.Where(gateId => activityGates.All(existingGate => existingGate.Gate.Id != gateId));  
      
      foreach (var deletedGateId in deletedGatesIds)
      {
        var relatedActivity = projectActivities.First(a => a.GateId.HasValue && a.GateId.Value == deletedGateId);
        
        var deletedGate = DirRX.ProjectPlanner.Structures.Module.GateDto.Create();
        deletedGate.Id = deletedGateId;
        deletedGate.Name = null;
        
        var activityDeletedGate = DirRX.ProjectPlanner.Structures.Module.ActivityGateDto.Create(relatedActivity.Id, deletedGate);
          
        activityGates.Add(activityDeletedGate);
      }
      
      return activityGates;
    }
    
    private static DirRX.ProjectPlanner.Structures.Module.IGateDto CastGateToDto(DirRX.ProjectPlanner.IGate gate)
    {
      return DirRX.ProjectPlanner.Structures.Module.GateDto.Create(
        gate.Id,
        gate.Name,
        gate.DisplayValue,
        gate.Level?.Value,
        gate.IsPassed,
        gate.PlanDate,
        gate.ActualDate);
    }
    
    private static List<DirRX.ProjectPlanner.Structures.Module.ILocalizedEnum> GetGateLevelStatusesForPlan()
    {
      var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;
      
      return DirRX.ProjectPlanner.Server.Gate.LevelItems.ItemsMergedWithDescendants.Select(enumValue => 
        DirRX.ProjectPlanner.Structures.Module.LocalizedEnum.Create(enumValue.Value, DirRX.ProjectPlanner.Server.Gate.LevelItems.GetLocalizedValue(enumValue, cultureInfo))
       ).ToList();
    }
    
    private static List<DirRX.ProjectPlanner.Structures.Module.ILocalizedEnum> GetActivityStatusesForPlan()
    {
      var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;
      
      return DirRX.ProjectPlanner.Server.ProjectActivity.StatusItems.ItemsMergedWithDescendants.Select(enumValue => 
        DirRX.ProjectPlanner.Structures.Module.LocalizedEnum.Create(
          enumValue.Value,
          DirRX.ProjectPlanner.Server.ProjectActivity.StatusItems.GetLocalizedValue(enumValue, cultureInfo))
               ).ToList();
    }
    
    private void ValidateActivityGateLinksOrThrowException(System.Collections.Generic.IDictionary<long, long?> activityGateLinks)
    {
      var gateIds = activityGateLinks.Where(a => a.Value.HasValue && a.Value.Value > 0).Select(a => a.Value.Value);
      ValidateGatesDistinctOrThrowException(gateIds);
    }
    
    private void ValidateGatesDistinctOrThrowException(System.Collections.Generic.IEnumerable<long> gateIds)
    {
      var gateIdCount = gateIds.Count();
      var uniqueGateIdCount = gateIds.Distinct().Count();
      
      if (gateIdCount != uniqueGateIdCount)
      {
        throw AppliedCodeException.Create(DirRX.ProjectPlanner.Resources.SingleGateToMilestoneWarning);
      }
    }
    
    private DateTime GetDateTimeFromJsDateSeconds(double jsDateSeconds)
    {
      return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(jsDateSeconds);
    }
    
    /// <summary>
    /// Получение Uri для открытия Дорожной карты.
    /// </summary>
    /// <returns>Uri.</returns>
    [Public, Remote]
    public Uri GetUriRoadmap()
    {
      var defaultRoadmapAddress = Constants.Module.RoadmapClientAddress;
      var getter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
      var uriRoadmap = getter.GetContentFullAddress(new Uri(defaultRoadmapAddress, UriKind.Relative));
      return uriRoadmap;
    }
    
    /// <summary>
    /// Получение Uri для открытия Дорожной карты.
    /// </summary>
    /// <param name="entityId">Ид проекта, портфеля, отчета.</param>
    /// <returns>Uri.</returns>
    [Public, Remote]
    public Uri GetUriRoadmap(long entityId)
    {
      var uriRoadmap = GetUriRoadmap();
      var builder = new UriBuilder(uriRoadmap)
      {
        Query = string.Format("id={0}", entityId)
      };
      return builder.Uri;
    }

    /// <summary>
    /// Создать Dto объекта управления для roadmap.
    /// </summary>
    /// <param name="projectCore">Объект управления.</param>
    /// <param name="accessibleProjectCoreIds">Ид доступных проектов управления.</param>
    /// <returns>Dto объекта управления.</returns>
    private static DirRX.Planner.Model.ProjectCoreDto CreateProjectCoreDto(Structures.Module.IRoadmapProjectDatabaseItem projectCore, bool checkAccess, HashSet<long> accessibleProjectCoreIds, bool isProject)
    {
      var typeGuid = isProject ? Constants.Module.ProjectBaseGuidString : projectCore.Project.GetEntityMetadata().NameGuid.ToString();
      
      string name = null;
      string shortName = null;
      string statusIssues = null;
      long? managerId = null;
      string managerName = null;
      DateTime? startDatePlan = null;
      DateTime? endDatePlan = null;
      DateTime? startDateActual = null;
      DateTime? endDateActual = null;
      bool? isCompleted = null;
      bool isAccessible = false;
      long? projectPlanId = null;
      int? projectPlanLastVersion = null;
      
      if (!checkAccess || accessibleProjectCoreIds.Contains(projectCore.Id))
      {
        name = projectCore.Project.Name;
        shortName = projectCore.Project.ShortName;
        statusIssues = projectCore.Project.StatusIssues != null ? projectCore.Project.StatusIssues.Value.Value : null;
        managerId = projectCore.ManagerId;
        managerName = projectCore.ManagerName;
        startDatePlan = projectCore.Project.StartDate;
        endDatePlan = projectCore.Project.EndDate;
        startDateActual = projectCore.Project.ActualStartDate;
        endDateActual = projectCore.Project.ActualFinishDate;
        isCompleted = projectCore.Project.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Completed;
        isAccessible = true;
        
        if (isProject)
        { 
          // Yarovikov_GV : это для каждого плана будет по 2 запроса в бд, прямо долго и много.
          // Может вообще это переписать, чтобы формировать ссылку в гант на сервере по id проекта
          var plan = DirRX.ProjectPlanning.Projects.As(projectCore.Project).ProjectPlanDirRX;
          projectPlanId = plan?.Id;
          projectPlanLastVersion = plan?.LastVersion?.Number;
        }
      }
      else
      {
        name = Resources.NoAccess;
        shortName = Resources.NoAccess;
        statusIssues = Resources.NoAccess;
        managerName = Resources.NoAccess;
      }
      
      return new DirRX.Planner.Model.ProjectCoreDto(projectCore.Id,
        typeGuid,
        name,
        shortName,
        statusIssues,
        managerId,
        managerName,
        startDatePlan,
        endDatePlan,
        startDateActual,
        endDateActual,
        isCompleted,
        isAccessible,
        null,
        projectCore.Project.GatesDirRX.Select(g => g.Gate.Id).ToList(),
        projectPlanId,
        projectPlanLastVersion
      );
    }
    
    private static void FillGates(DirRX.Planner.Model.ProjectHierarchy projectHierarchy)
    {
      var allGateIds = projectHierarchy.ProjectCores.SelectMany(x => x.Value.GateIds).Concat(projectHierarchy.OtherHierarchy.GateIds);
      
      var allGatesData = Gates.GetAll(x => allGateIds.Contains(x.Id))
        .Select(g => 
                new {
                  Id = g.Id,
                  Gate = g,
                  Owner = g.Owner != null ? new { Id = g.Owner.Id, Name = g.Owner.DisplayValue } : null,
                  Performer = g.Performer != null ? new { Id = g.Performer.Id, Name = g.Performer.DisplayValue } : null,
                  Supervisor = g.Supervisor != null ? new { Id = g.Supervisor.Id, Name = g.Supervisor.DisplayValue } : null,
                });
      foreach (var gate in allGatesData)
      {
        projectHierarchy.Gates.Add(
          gate.Id,
          new DirRX.Planner.Model.Gate
          (
            gate.Id,
            "4f931c4f-1331-4736-962a-0e6d1eb58abe",
            gate.Gate.Status?.Value?.ToString(),
            gate.Gate.DisplayValue,
            gate.Gate.Description,
            gate.Gate.Level?.Value?.ToString(),
            gate.Gate.Assessment?.Value,
            gate.Gate.IsPassed,
            gate.Owner == null ? null : new DirRX.Planner.Model.Performer(gate.Owner.Id, gate.Owner.Name),
            gate.Performer == null ? null : new DirRX.Planner.Model.Performer(gate.Performer.Id, gate.Performer.Name),
            gate.Supervisor == null ? null : new DirRX.Planner.Model.Performer(gate.Supervisor.Id, gate.Supervisor.Name),
            gate.Gate.PlanDate,
            gate.Gate.ActualDate,
            gate.Gate.Note,
            projectHierarchy.ProjectCores.Values.FirstOrDefault(pc => pc.GateIds.Contains(gate.Id))?.Id
           ));
      }
    }
    
    private static void FillMilestones(DirRX.Planner.Model.ProjectHierarchy projectHierarchy, IEnumerable<IProjectActivity> milestones)
    {
      foreach (var subProjectMilestone in milestones)
      {
        projectHierarchy.Milestones.Add(
          subProjectMilestone.Id,
          new DirRX.Planner.Model.Milestone
          (
            subProjectMilestone.Id,
            "647a920e-ebf5-47b3-8aa6-bd9f2831515c",
            subProjectMilestone.DisplayValue,
            subProjectMilestone.StartDate,
            subProjectMilestone.ExecutionPercent,
            subProjectMilestone.Responsible?.Id,
            subProjectMilestone.Responsible?.DisplayValue,
            subProjectMilestone.GateId.Value
           ));
      }
    }
    
    /// <summary>
    /// Получение дочернего объекта иерархии.
    /// </summary>
    /// <param name="rootHierarchyItem">Родительский объект иерархии.</param>
    /// <param name="subProjects">Дочерние объекты управления.</param>
    /// <param name="leadingProjectCoreId">Ид родительского объекта управления.</param>
    /// <returns>Дочерний объект иерархии.</returns>
    private static DirRX.Planner.Model.HierarchyItem GetChildHierarchyItem(List<Structures.Module.IRoadmapProjectDatabaseItem> subProjects, long leadingProjectCoreId, int depth, out int maxDepth)
    {
      var rootHierarchyItem = new DirRX.Planner.Model.HierarchyItem(leadingProjectCoreId);
      var childProjectCoreIds = subProjects.Where(x => x.ParentId == leadingProjectCoreId).Select(x => x.Id);
      maxDepth = depth;
      
      foreach(var childProjectCoreId in childProjectCoreIds)
      {
        int childDepth;
        rootHierarchyItem.ChildItems.Add(GetChildHierarchyItem(subProjects, childProjectCoreId, ++depth, out childDepth));
        maxDepth = Math.Max(childDepth, maxDepth);
      }
      rootHierarchyItem.Depth = maxDepth;
      return rootHierarchyItem;
    }
  }
}
