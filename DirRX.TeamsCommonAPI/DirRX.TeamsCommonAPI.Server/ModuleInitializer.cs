using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;
using DirRX.TeamsCommonAPI.NotifyConditionItem;
using DirRX.TeamsCommonAPI.NotifyEventType;

namespace DirRX.TeamsCommonAPI.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      GrantRightsOnSpecialFolders();
      GrantRightsOnDatabooks();
      CreateNoticeConditions();
      CreateDefaultEventTypes();
      CreateAssociatedApplication();
    }
    
    private void GrantRightsOnDatabooks()
    {
      NotifyConditionItems.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      NotifyConditionItems.AccessRights.Save();
      NotifyEventTypes.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      NotifyEventTypes.AccessRights.Save();
      TeamsNoticesSettingses.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      TeamsNoticesSettingses.AccessRights.Save();
      TeamsTasks.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      TeamsTasks.AccessRights.Save();
    }
    
    private void CreateEventType(Sungero.Core.Enumeration solutionIdentifier, Sungero.Core.Enumeration eventType, Sungero.Core.Enumeration sectionIdentifier, string notifyText)
    {
      var type = DirRX.TeamsCommonAPI.NotifyEventTypes.Create();
      type.Name = type.Info.Properties.EventType.GetLocalizedValue(eventType);
      type.SolutionIdentifier = solutionIdentifier;
      type.EventType = eventType;
      type.SectionIdentifier = sectionIdentifier;
      type.NotifyText = notifyText;
      type.Save();
    }
    
    private void UpdateEventType(DirRX.TeamsCommonAPI.INotifyEventType type, Sungero.Core.Enumeration solutionIdentifier, Sungero.Core.Enumeration eventType, Sungero.Core.Enumeration sectionIdentifier, string notifyText)
    {
      type.Name = type.Info.Properties.EventType.GetLocalizedValue(eventType);
      type.SolutionIdentifier = solutionIdentifier;
      type.EventType = eventType;
      type.SectionIdentifier = sectionIdentifier;
      type.NotifyText = notifyText;
      type.Save();
    }
    
    [Public]
    public void CreateDefaultEventTypes()
    {
      var existingEventTypes = DirRX.TeamsCommonAPI.NotifyEventTypes.GetAll().ToList();
      
      var actCannotStartEventType = existingEventTypes.Where(t => t.EventType == EventType.ActCannotStart).FirstOrDefault();
      var actMustEndedEventType = existingEventTypes.Where(t => t.EventType == EventType.ActMustEnded).FirstOrDefault();
      var actMustStartedEventType = existingEventTypes.Where(t => t.EventType == EventType.ActMustStarted).FirstOrDefault();
      var actRespAssignEventType = existingEventTypes.Where(t => t.EventType == EventType.ActRespAssign).FirstOrDefault();
      var actOverdatedEventType = existingEventTypes.Where(t => t.EventType == EventType.ActOverdated).FirstOrDefault();
      var actStatusChangdEventType = existingEventTypes.Where(t => t.EventType == EventType.ActStatusChangd).FirstOrDefault();
      var milesRespAssignEventType = existingEventTypes.Where(t => t.EventType == EventType.MilesRespAssign).FirstOrDefault();
      var milestMustReachEventType = existingEventTypes.Where(t => t.EventType == EventType.MilestMustReach).FirstOrDefault();
      var milestOverdatedEventType = existingEventTypes.Where(t => t.EventType == EventType.MilestOverdated).FirstOrDefault();
      var planMustEndedEventType = existingEventTypes.Where(t => t.EventType == EventType.PlanMustEnded).FirstOrDefault();
      var planMustStartedEventType = existingEventTypes.Where(t => t.EventType == EventType.PlanMustStarted).FirstOrDefault();
      var planOverdatedEventType = existingEventTypes.Where(t => t.EventType == EventType.PlanOverdated).FirstOrDefault();
      var planParticipAddEventType = existingEventTypes.Where(t => t.EventType == EventType.PlanParticipAdd).FirstOrDefault();
      var planRespAssignEventType = existingEventTypes.Where(t => t.EventType == EventType.PlanRespAssign).FirstOrDefault();
      var sectMustEndedEventType = existingEventTypes.Where(t => t.EventType == EventType.SectMustEnded).FirstOrDefault();
      var sectMustStartedEventType = existingEventTypes.Where(t => t.EventType == EventType.SectMustStarted).FirstOrDefault();
      var sectOverdatedEventType = existingEventTypes.Where(t => t.EventType == EventType.SectOverdated).FirstOrDefault();
      var sectRespAssignEventType = existingEventTypes.Where(t => t.EventType == EventType.SectRespAssign).FirstOrDefault();
      var startedActChdEventType = existingEventTypes.Where(t => t.EventType == EventType.StartedActChd).FirstOrDefault();
      var assignTicketsEventType = existingEventTypes.Where(t => t.EventType == EventType.AssignedTickets).FirstOrDefault();
      var ticketCommentsEventType = existingEventTypes.Where(t => t.EventType == EventType.TicketComments).FirstOrDefault();
      
      if (actCannotStartEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActCannotStart, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityCannotStartNotifyText);
      }
      else
      {
        UpdateEventType(actCannotStartEventType, SolutionIdentifier.ProjectPlanning, EventType.ActCannotStart, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityCannotStartNotifyText);
      }
      
      if (actMustEndedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityMustEndedNotifyText);
      }
      else
      {
        UpdateEventType(actMustEndedEventType, SolutionIdentifier.ProjectPlanning, EventType.ActMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityMustEndedNotifyText);
      }
      
      if (actMustStartedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityMustStartNotifyText);
      }
      else
      {
        UpdateEventType(actMustStartedEventType, SolutionIdentifier.ProjectPlanning, EventType.ActMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityMustStartNotifyText);
      }
      
      if (actOverdatedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityOverdatedNotifyText);
      }
      else
      {
        UpdateEventType(actOverdatedEventType, SolutionIdentifier.ProjectPlanning, EventType.ActOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.ActivityOverdatedNotifyText);
      }
      
      if (actRespAssignEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.ActivityRespAssignNotifyText);
      }
      else
      {
        UpdateEventType(actRespAssignEventType, SolutionIdentifier.ProjectPlanning, EventType.ActRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.ActivityRespAssignNotifyText);
      }
      
      if (actStatusChangdEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.ActStatusChangd, SectionIdentifier.Other, DirRX.TeamsCommonAPI.Resources.ActivityStatusChangedNotifyText);
      }
      else
      {
        UpdateEventType(actStatusChangdEventType, SolutionIdentifier.ProjectPlanning, EventType.ActStatusChangd, SectionIdentifier.Other, DirRX.TeamsCommonAPI.Resources.ActivityStatusChangedNotifyText);
      }
      
      if (milesRespAssignEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.MilesRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.ActivityRespAssignNotifyText);
      }
      else
      {
        UpdateEventType(milesRespAssignEventType, SolutionIdentifier.ProjectPlanning, EventType.MilesRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.ActivityRespAssignNotifyText);
      }
      
      if (milestMustReachEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.MilestMustReach, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.MilestoneMustReachNotifyText);
      }
      else
      {
        UpdateEventType(milestMustReachEventType, SolutionIdentifier.ProjectPlanning, EventType.MilestMustReach, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.MilestoneMustReachNotifyText);
      }
      
      if (milestOverdatedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.MilestOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.MilestoneOverdatedNotifyText);
      }
      else
      {
        UpdateEventType(milestOverdatedEventType, SolutionIdentifier.ProjectPlanning, EventType.MilestOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.MilestoneOverdatedNotifyText);
      }
      
      if (planMustEndedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.PlanMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanMustEndedNotifyText);
      }
      else
      {
        UpdateEventType(planMustEndedEventType, SolutionIdentifier.ProjectPlanning, EventType.PlanMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanMustEndedNotifyText);
      }
      
      if (planMustStartedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.PlanMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanMustStartedNotifyText);
      }
      else
      {
        UpdateEventType(planMustStartedEventType, SolutionIdentifier.ProjectPlanning, EventType.PlanMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanMustStartedNotifyText);
      }
      
      if (planOverdatedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.PlanOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanOverdatedNotifyText);
      }
      else
      {
        UpdateEventType(planOverdatedEventType, SolutionIdentifier.ProjectPlanning, EventType.PlanOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.PlanOverdatedNotifyText);
      }
      
      if (planParticipAddEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.PlanParticipAdd, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.PlanParticipantsAddedNotifyText);
      }
      else
      {
        UpdateEventType(planParticipAddEventType, SolutionIdentifier.ProjectPlanning, EventType.PlanParticipAdd, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.PlanParticipantsAddedNotifyText);
      }
      
      if (planRespAssignEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.PlanRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.PlanRespAssignNotifyText);
      }
      else
      {
        UpdateEventType(planRespAssignEventType, SolutionIdentifier.ProjectPlanning, EventType.PlanRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.PlanRespAssignNotifyText);
      }
      
      if (sectMustStartedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.SectMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionMustStartNotifyText);
      }
      else
      {
        UpdateEventType(sectMustStartedEventType, SolutionIdentifier.ProjectPlanning, EventType.SectMustStarted, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionMustStartNotifyText);
      }
      
      if (sectMustEndedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.SectMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionMustEndNotifyText);
      }
      else
      {
        UpdateEventType(sectMustEndedEventType, SolutionIdentifier.ProjectPlanning, EventType.SectMustEnded, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionMustEndNotifyText);
      }
      
      if (sectOverdatedEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.SectOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionOverdatedNotifyText);
      }
      else
      {
        UpdateEventType(sectOverdatedEventType, SolutionIdentifier.ProjectPlanning, EventType.SectOverdated, SectionIdentifier.PlanDate, DirRX.TeamsCommonAPI.Resources.SectionOverdatedNotifyText);
      }
      
      if (sectRespAssignEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.SectRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.SectionRespAssignNotifyText);
      }
      else
      {
        UpdateEventType(sectRespAssignEventType, SolutionIdentifier.ProjectPlanning, EventType.SectRespAssign, SectionIdentifier.RespAssign, DirRX.TeamsCommonAPI.Resources.SectionRespAssignNotifyText);
      }
      
      if (startedActChdEventType == null)
      {
        CreateEventType(SolutionIdentifier.ProjectPlanning, EventType.StartedActChd, SectionIdentifier.Other, DirRX.TeamsCommonAPI.Resources.StartedActivityChangedNotifyText);
      }
      else
      {
        UpdateEventType(startedActChdEventType, SolutionIdentifier.ProjectPlanning, EventType.StartedActChd, SectionIdentifier.Other, DirRX.TeamsCommonAPI.Resources.StartedActivityChangedNotifyText);
      }
      
      if (assignTicketsEventType == null)
      {
        CreateEventType(SolutionIdentifier.AgileBoards, EventType.AssignedTickets, SectionIdentifier.AssignedTickets, DirRX.TeamsCommonAPI.Resources.AssignedTicketNotifyText);
      }
      else
      {
        UpdateEventType(assignTicketsEventType, SolutionIdentifier.AgileBoards, EventType.AssignedTickets, SectionIdentifier.AssignedTickets, DirRX.TeamsCommonAPI.Resources.AssignedTicketNotifyText);
      }
      
      if (ticketCommentsEventType == null)
      {
        CreateEventType(SolutionIdentifier.AgileBoards, EventType.TicketComments, SectionIdentifier.AssignedTickets, DirRX.TeamsCommonAPI.Resources.TicketCommentNotifyText);
      }
      else
      {
        UpdateEventType(ticketCommentsEventType, SolutionIdentifier.AgileBoards, EventType.TicketComments, SectionIdentifier.AssignedTickets, DirRX.TeamsCommonAPI.Resources.TicketCommentNotifyText);
      }
      
    }
    
    private void GrantRightsOnSpecialFolders()
    {
      SpecialFolders.IncomingTeamsFolder.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      SpecialFolders.IncomingTeamsFolder.AccessRights.Save();
      SpecialFolders.SendedTeamsFolder.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      SpecialFolders.SendedTeamsFolder.AccessRights.Save();
    }
    
    private void CreateNoticeConditions()
    {
      //предварительная материализация в небольшом справочнике выглядит оптимальнее.
      var existingConditions = NotifyConditionItems.GetAll().ToList();
      
      var early = existingConditions.Where(c => c.Condition == Condition.Early).FirstOrDefault();
      
      if (early == null)
      {
        var newCondition = NotifyConditionItems.Create();
        newCondition.Name = newCondition.Info.Properties.Condition.GetLocalizedValue(Condition.Early);
        newCondition.Condition = Condition.Early;
        newCondition.Save();
      }
      else
      {
        early.Name = early.Info.Properties.Condition.GetLocalizedValue(Condition.Early);
        early.Save();
      }
      
      var never = existingConditions.Where(c => c.Condition == Condition.Never).FirstOrDefault();
      
      if (never == null)
      {
        var newCondition = NotifyConditionItems.Create();
        newCondition.Name = newCondition.Info.Properties.Condition.GetLocalizedValue(Condition.Never);
        newCondition.Condition = Condition.Never;
        newCondition.Save();
      }
      else
      {
        never.Name = never.Info.Properties.Condition.GetLocalizedValue(Condition.Never);
        never.Save();
      }
      
      var standart = existingConditions.Where(c => c.Condition == Condition.StandartNotice).FirstOrDefault();
      
      if (standart == null)
      {
        var newCondition = NotifyConditionItems.Create();
        newCondition.Name = newCondition.Info.Properties.Condition.GetLocalizedValue(Condition.StandartNotice);
        newCondition.Condition = Condition.StandartNotice;
        newCondition.Save();
      }
      else
      {
        standart.Name = standart.Info.Properties.Condition.GetLocalizedValue(Condition.StandartNotice);
        standart.Save();
      }
      
      var collective = existingConditions.Where(c => c.Condition == Condition.Consolidated).FirstOrDefault();
      
      if (collective == null)
      {
        var newCondition = NotifyConditionItems.Create();
        newCondition.Name = newCondition.Info.Properties.Condition.GetLocalizedValue(Condition.Consolidated);
        newCondition.Condition = Condition.Consolidated;
        newCondition.Save();
      }
      else
      {
        collective.Name = collective.Info.Properties.Condition.GetLocalizedValue(Condition.Consolidated);
        collective.Save();
      }
      
    }
    
    /// <summary>
    /// Задание приложений-обработчиков md и rxmd
    /// </summary>
    private void CreateAssociatedApplication()
    {
      InitializationLogger.Debug("Init: Create associated application.");
      
      var fileType = Sungero.Content.FilesTypes.GetAll(t => t.Name == DirRX.TeamsCommonAPI.Resources.TextDocuments).FirstOrDefault();
      if (fileType == null)
      {
        fileType = Sungero.Content.FilesTypes.Create();
        fileType.Name = DirRX.TeamsCommonAPI.Resources.TextDocuments;
        fileType.Save();
      }
      
      var oldApps = Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == DirRX.TeamsCommonAPI.Resources.MdExtension && x.Name != DirRX.TeamsCommonAPI.Resources.MdEditorName);
      if (oldApps.Any())
      {
        var app = oldApps.FirstOrDefault();
        app.Name = DirRX.TeamsCommonAPI.Resources.MdEditorName;
        app.MonitoringType = Sungero.Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
        app.FilesType = fileType;
        app.Save();
      }
      else if (!Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == DirRX.TeamsCommonAPI.Resources.MdExtension).Any())
      {
        CreateExtension(DirRX.TeamsCommonAPI.Resources.MdExtension, DirRX.TeamsCommonAPI.Resources.MdEditorName, fileType);
      }
      
      if (!Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == DirRX.TeamsCommonAPI.Resources.RxmdExtension).Any())
      {
        CreateExtension(DirRX.TeamsCommonAPI.Resources.RxmdExtension, DirRX.TeamsCommonAPI.Resources.RxmdEditorName, fileType);
      }
    }
    
    private void CreateExtension(string extension, string appName, Sungero.Content.IFilesType fileType)
    {
      var app = Sungero.Content.AssociatedApplications.Create();
      app.Extension = extension;
      app.Name = appName;
      app.MonitoringType = Sungero.Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
      app.FilesType = fileType;
      app.Save();
    }
  }
}
