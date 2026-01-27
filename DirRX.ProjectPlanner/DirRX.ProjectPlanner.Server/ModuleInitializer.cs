using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;
using DirRX.TeamsCommonAPI.NotifyConditionItem;
using DirRX.TeamsCommonAPI.NotifyEventType;

namespace DirRX.ProjectPlanner.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Init();
    }
    
    [Public]
    public static void Init()
    {
      GrantRightsOnProjectPlans();
      GrantRightsOnProjectResources();
      GrantRightsOnProjectResourceTypes();
      GrantRightsOnNotifyDiffs();
      GrantRightsOnActivityTasks();
      GrantRightsOnReports();
      GrantRightsOnGates();
      CreateAssociatedApplication();
      CreateDocumentType();
      CreateDocumentKinds();
      CreateResourceTypes();
      GrantRightsOnOpensProjectPlansFromCards();
      ConvertObsoletePlans();
      CreateResourceLinksIfNotExists();
      CreateReportsTables();
      CreateModifiedPlanIfNotExists();
    }
    
    public static void GrantRightsOnActivityTasks()
    {
      // TODO: изначально права выдавались полные на задачу. Поэтому, чтобы выдать сейчас права только на создание,
      // необходимо забрать права. Убрать после выпуска.
      ProjectPlanner.ProjectActivityTasks.AccessRights.RevokeAll(Roles.AllUsers);
      ProjectPlanner.ProjectActivityTasks.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
      ProjectPlanner.ProjectActivityTasks.AccessRights.Save();
    }
    
    private static void GrantRightsOnNotifyDiffs()
    {
      NotifyDiffs.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
      NotifyDiffs.AccessRights.Save();
    }
    
    #region Выдача прав на объекты системы.
    public static void GrantRightsOnOpensProjectPlansFromCards()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on Opens project Plans from Card.");
        ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
        ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Save();
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.Debug(DirRX.ProjectPlanner.Resources.FailedToGrantPermissionsToDocTypeFormat("Открытые из карточек планы проектов", ex));
      }
    }
    
    public static void GrantRightsOnProjectPlans()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project plans.");
        ProjectPlanner.ProjectPlanRXes.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
        ProjectPlanner.ProjectPlanRXes.AccessRights.Save();
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.Debug(DirRX.ProjectPlanner.Resources.FailedToGrantPermissionsToDocTypeFormat("Планы проектов", ex));
      }
    }
    
    public static void GrantRightsOnProjectResources()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project resources.");
        ProjectPlanner.ProjectsResources.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
        ProjectPlanner.ProjectsResources.AccessRights.Save();
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.Debug(DirRX.ProjectPlanner.Resources.FailedToGrantPermissionsToDocTypeFormat("Ресурсы плана", ex));
      }
      
    }
    
    public static void GrantRightsOnProjectResourceTypes()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project resource types.");
        ProjectPlanner.ProjectResourceTypes.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        ProjectPlanner.ProjectResourceTypes.AccessRights.Save();
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.Debug(DirRX.ProjectPlanner.Resources.FailedToGrantPermissionsToDocTypeFormat("Типы ресурсов", ex));
      }
    }
    
    public static void GrantRightsOnGates()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on gates.");
        ProjectPlanner.Gates.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        ProjectPlanner.Gates.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
        ProjectPlanner.Gates.AccessRights.Save();
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.Debug(DirRX.ProjectPlanner.Resources.FailedToGrantPermissionsToDocTypeFormat("Контрольные точки", ex));
      }
    }
    #endregion
    
    public static void CreateResourceLinksIfNotExists()
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        InitializationLogger.Debug("Execute create (if not exists) resourcelinks query");
        command.CommandText = Queries.Module.CreateResourceLinksIfNotExist;
        command.ExecuteNonQuery();
      }
    }
    
    /// <summary>
    /// Преобразование старых планов проектов в новые
    /// </summary>
    public static void ConvertObsoletePlans() 
    {
      InitializationLogger.Debug("Start async convert obsolete plans query");
      var handler = new ProjectPlanner.AsyncHandlers.ConvertPlanAsync();
      handler.ExecuteAsync();
    }
    
    /// <summary>
    /// Создание типа документа.
    /// </summary>
    public static void CreateDocumentType()
    {
      InitializationLogger.Debug("Init: Create document type");
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(DirRX.ProjectPlanner.Resources.ProjectPlanName, ProjectPlanRX.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true);
    }
    
    /// <summary>
    /// Создание вида документа.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var notNumerable = Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
      var autoFormattedName = false;
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.ProjectPlanner.Resources.ProjectPlanName,
                                                                              DirRX.ProjectPlanner.Resources.ProjectPlanName, notNumerable,
                                                                              Sungero.Docflow.DocumentKind.DocumentFlow.Inner, autoFormattedName, false, ProjectPlanRX.ClassTypeGuid, null,
                                                                              Constants.Module.ProjectPlanDocKindGuid, true);
    }
    
    /// <summary>
    /// Задание приложения-обработчика для расширения .rxpp.
    /// </summary>
    public static void CreateAssociatedApplication()
    {
      InitializationLogger.Debug("Init: Create associated application.");
      if (!Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").Any())
      {
        var app = Sungero.Content.AssociatedApplications.Create();
        app.Extension = "rxpp";
        app.Name = DirRX.ProjectPlanner.Resources.AplicationName;
        app.MonitoringType = Sungero.Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
        var fileType = Sungero.Content.FilesTypes.Create();
        fileType.Name = DirRX.ProjectPlanner.Resources.ProjectsFileTypeName;
        app.FilesType = fileType;
        app.Save();
      }
    }
    
    /// <summary>
    /// Создание типов ресурсов.
    /// </summary>
    public static void CreateResourceTypes()
    {
      if (!ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.Users).Any())
      {
        var resourceType = ProjectResourceTypes.Create();
        resourceType.Name = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeName;
        resourceType.MeasureUnit = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeUnit;
        resourceType.ServiceName = Constants.Module.ResourceTypes.Users;
        resourceType.Save();
      }
      
      if (!ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.MaterialResources).Any())
      {
        var resourceType = ProjectResourceTypes.Create();
        resourceType.Name = DirRX.ProjectPlanner.Resources.MaterialResourcesTypeName;
        resourceType.MeasureUnit = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeUnit;
        resourceType.ServiceName = Constants.Module.ResourceTypes.MaterialResources;
        resourceType.Save();
      }     
    }
    
    /// <summary>
    /// Создать таблицы для хранения ид измененных планов.
    /// </summary>
    public static void CreateModifiedPlanIfNotExists()
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.CreateModifiedPlanIfNotExists;
        command.ExecuteNonQuery();
      }
    }
    
    #region Отчеты
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var milestonesProgressReportTableName = DirRX.ProjectPlanner.Constants.ProgressReport.MilestonesTableName;
      var overdueStagesProgressReportTableName = DirRX.ProjectPlanner.Constants.ProgressReport.OverdueStagesTableName;
      var completedStagesProgressReportTableName = DirRX.ProjectPlanner.Constants.ProgressReport.CompletedStagesTableName;
      var currentStagesProgressReportTableName = DirRX.ProjectPlanner.Constants.ProgressReport.CurrentStagesTableName;
      
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] {
                                                                    milestonesProgressReportTableName,
                                                                    overdueStagesProgressReportTableName,
                                                                    completedStagesProgressReportTableName,
                                                                    currentStagesProgressReportTableName
                                                                  });
      
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ProgressReport.CreateMilestonesTable, new[] { milestonesProgressReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ProgressReport.CreateStagesTable, new[] { overdueStagesProgressReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ProgressReport.CreateStagesTable, new[] { completedStagesProgressReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ProgressReport.CreateStagesTable, new[] { currentStagesProgressReportTableName });
    }
    
    /// <summary>
    /// Выдать права всем пользователям на создание отчетов.
    /// </summary>
    public static void GrantRightsOnReports()
    {
      var allUsers = Roles.AllUsers;
      
      if (allUsers != null)
      {
        InitializationLogger.Debug("Init: Grant right on reports to all users.");
        Reports.AccessRights.Grant(Reports.GetProgressReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      }
      #endregion
    }
  }
}
