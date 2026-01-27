using System;
using Sungero.Core;

namespace DirRX.ProjectPlanner.Constants
{
  
  public static class Module
  {
    /// <summary>
    /// Ключ записи о последней синхронизации прав проекта и его плана в таблице Sungero_Docflow_Params
    /// </summary>
    public const string LastSyncAccessRightsOfProject = "LastSyncAccessRightsOfProject";
    
    /// <summary>
    /// Ключ записи о последней синхронизации прав проекта и его контрольных точек в таблице Sungero_Docflow_Params
    /// </summary>
    public const string LastSyncAccessRightsOfProjectGates = "LastSyncAccessRightsOfProjectGates";
    
    public const string ExportDialogHelpCode = "project_planning_export_dialog";
    
    public const string ProjectPlanGuid = "f2894e4d-a950-497f-a311-1fd7e9665d28";
    
    public const string ProjectPlanDocKindGuidString = "a319de81-7897-43e0-8f52-3eefbc011f91";
    
    public const string ProjectBaseGuidString = "4383f2ff-56e6-46f4-b4ef-cc17e6aeef40";
    
    /// <summary>
    /// Промежуток в минуток, по истечении которого снимать блокировку.
    /// </summary>
    public const int UnlockMinutes = 5;
    
    // GUID парсится из явно заданного значения, иначе не виден вне модуля.
    [Public]
    public static readonly Guid ProjectPlanDocKindGuid = Guid.Parse("a319de81-7897-43e0-8f52-3eefbc011f91");
    
    [Public]
    public static readonly Guid ProjectPlanObsoleteDocKindGuid = Guid.Parse("b0af2406-a454-48d5-8e94-e9f69012c8a1");
    
    /// <summary>
    /// Гуид модуля планирования проектов.
    /// </summary>
    public static readonly Guid ProjectPlannerModuleGuid = Guid.Parse(ProjectPlanGuid);
    
    /// <summary>
    /// Guid тела версии документа.
    /// </summary>
    public static readonly Guid BodyVersionGuid = Guid.Parse("CF874C8E-0098-47FF-B313-AA67DFF190F1");
    
    #region Папки проектов

    public static class ProjectFolders
    {
      // UID для корневой папки с проектами.
      public static readonly Guid ProjectFolderUid = Guid.Parse("F7A78196-A1BE-4666-94F4-0DDDD3367A6E");
      
      // UID для корневой папки с проектами.
      public static readonly Guid ProjectArhiveFolderUid = Guid.Parse("C74412F4-7FBB-450F-83A3-BBB9772C6167");
    }
    
    #endregion

    #region Группы, роли, тип прав
    
    public static class RoleGuid
    {
      // GUID роли "Проектные команды".
      [Sungero.Core.Public]
      public static readonly Guid ParentProjectTeam = Guid.Parse("2062682D-745C-4E02-AF2F-26AD229E8C61");
    }
    
    #endregion
    
    public static class Initialize
    {
      public static readonly Guid CustomerRequirementsKind = Guid.Parse("FC5B2F85-548D-4DE0-B1E9-66C873932111");
      public static readonly Guid RegulationsKind = Guid.Parse("1B1F18B1-F42E-4939-B6ED-14D555D5FAAA");
      public static readonly Guid ReportKind = Guid.Parse("D0859D72-15C7-4CA7-B81E-611A5DF1F112");
      public static readonly Guid ScheduleKind = Guid.Parse("F5869F19-67D3-47C2-9024-C60A96F2685B");
      public static readonly Guid ProjectSolutionKind = Guid.Parse("479B7A76-3F38-434C-897E-733198C4F260");
      public static readonly Guid AnalyticNoteKind = Guid.Parse("205D0822-EEAB-4EB9-813D-738ECEEFE303");
      public static readonly Guid ProjectKindInvestment = Guid.Parse("38DE9EBF-8733-41B9-88B5-8C1884075E2C");
      public static readonly Guid ProjectKindInformationTechnology = Guid.Parse("2C569F84-709F-40C8-A179-97D58037B6A8");
      public static readonly Guid ProjectKindOrganizationDevelopment = Guid.Parse("F4BC2B22-7E28-4EED-B9A9-8A8DAB275DA4");
      public static readonly Guid ProjectKindCreatingNewProduct = Guid.Parse("D3568B17-0FA1-4D29-8ABB-046A8DDE4796");
      public static readonly Guid ProjectKindOrganizationSale = Guid.Parse("53FBC9CF-726C-45AE-BD77-8C9BFEC2A0C0");
      public static readonly Guid ProjectKindMarketing = Guid.Parse("04FF23F3-33A5-4EAE-A6B7-6398A71D0866");
    }

    public const string DontUpdateModified = "DontUpdateModified";
    
    /// <summary>
    /// Типы ресурсов.
    /// </summary>
    public static class ResourceTypes
    {
      public const string Users = "Users";
      public const string MaterialResources = "MaterialResources";
    }
    
    /// <summary>
    /// Изменение содержимого папки.
    /// </summary>
    public static readonly Guid ChangeContent = Guid.Parse("344A32D8-9814-4BB8-8D86-1F65E43FDA25");
    
    /// <summary>
    /// Относительный адрес клиента.
    /// </summary>
    public const string DefaultClientAddress = "projectplanning";
    
    /// <summary>
    /// Относительный адрес для отчета.
    /// </summary>
    [Public]
    public const string DefaultPivotTableAddress = "pivot-table";
    
    /// <summary>
    /// Путь к методу получения метаданных для сводного отчета.
    /// </summary>
    [Public]
    public const string PathResourcesReportMetadata = "data=ProjectPlanner/ResourcesReportMetadata{0}";
    
    /// <summary>
    /// Путь к методу получения метаданных для сводного отчета для оргструктуры.
    /// </summary>
    [Public]
    public const string PathResourcesReportMetadataOrg = "data=ProjectPlanner/ResourcesReportMetadataOrg{0}";
    
    /// <summary>
    /// Путь к методу получения данных для сводного отчета.
    /// </summary>
    [Public]
    public const string PathResourcesReport = "ProjectPlanner/ResourcesReport";
    
    /// <summary>
    /// Путь к методу получения данных для сводного отчета для оргструктуры.
    /// </summary>
    [Public]
    public const string PathResourcesReportOrg = "ProjectPlanner/ResourcesReportOrg";
    
    /// <summary>
    /// Путь к методу получения локализации для сводного отчета.
    /// </summary>
    [Public]
    public const string PathLocaleResourcesReport = "ProjectPlanner/GetLocaleResourcesReport";
    
    /// <summary>
    /// Относительный адрес клиента Agile.
    /// </summary>
    [Public]
    public const string AgileClientAddress = "agile";
    
    /// <summary>
    /// Относительный адрес клиента Roadmap.
    /// </summary>
    [Public]
    public const string RoadmapClientAddress = "roadmap";

    public static class Type
    {
      public const string Program = "Program";
      public const string Project = "Project";
      public const string Portfolio = "Portfolio";
    }

    # region Строковая детализация сводного очета
    public static class DateScale
    { 
      /// <summary>
      /// Детализация до дней.
      /// </summary>
      [Public]
      public const string TimelineDays = "days";
      
      /// <summary>
      /// Детализация до недель.
      /// </summary>
      [Public]
      public const string TimelineWeeks = "weeks";
      
      /// <summary>
      /// Детализация до месяцев.
      /// </summary>
      [Public]
      public const string TimelineMonths = "months";
      
      /// <summary>
      /// Детализация до кварталов.
      /// </summary>
      [Public]
      public const string TimelineQuarters = "quarters";
      
      /// <summary>
      /// Детализация до лет.
      /// </summary>
      [Public]
      public const string TimelineYears = "years";
    }
    #endregion
    
    # region Ключи детализации сводного очета
    public static class DateScaleKey
    {
      /// <summary>
      /// Детализация до дней.
      /// </summary>
      [Public]
      public const string TimelineDays = "d";
      
      /// <summary>
      /// Детализация до недель.
      /// </summary>
      [Public]
      public const string TimelineWeeks = "w";
      
      /// <summary>
      /// Детализация до месяцев.
      /// </summary>
      [Public]
      public const string TimelineMonths = "m";
      
      /// <summary>
      /// Детализация до кварталов.
      /// </summary>
      [Public]
      public const string TimelineQuarters = "q";
      
      /// <summary>
      /// Детализация до лет.
      /// </summary>
      [Public]
      public const string TimelineYears = "y";
    }
    #endregion
    
    #region Типы данных сводного отчета
    public static class TypeData
    {
      [Public]
      public const string Number = "number";
      
      [Public]
      public const string String = "string";
      
      [Public]
      public const string Boolean = "boolean";
      
      [Public]
      public const string Date = "date";
      
      [Public]
      public const string DateTime = "datetime";
      
      [Public]
      public const string DateRange = "daterange";
      
      [Public]
      public const string DateTimeRange = "datetimerange";
      
      [Public]
      public const string DateScale = "date_scales";
      
      [Public]
      public const string Employee = "employee";
      
      [Public]
      public const string Array = "array";
    }
    #endregion
    
    #region Типы аггрегирования данных
    public static class AggregatorsKeys
    {
      public const string Sum = "Sum";
      public const string Average = "Average";
      public const string Count = "Count";
    }
    #endregion
    
    public static class LocalizationKeys
    {
      public const string Type = "type";
      public const string TypeArray = "type_array";
      public const string NumberFormat = "number_format";
      public const string Integer = "integer";
      public const string Default = "default";
      public const string MinValue = "min_value";
      public const string MaxValue = "max_value";
      public const string DateRange = "dateRange";
      public const string DateScale = "dateScale";
      public const string DateScaleKey = "dateScaleKey";
      public const string DateScalesParam = "date_scales";
      public const string IsImmutable = "isImmutable";
      public const string Rows = "rows";
      public const string Cols = "cols";
      public const string AggregateType = "aggregate_type";
      public const string AggregateCol = "aggregate_col";
      public const string Project = "project";
      public const string Name = "name";
      public const string ActivityName = "activityName";
      public const string Performer = "performer";
      public const string Date = "date";
      public const string Week = "week";
      public const string Month = "month";
      public const string Quarter = "quarter";
      public const string Year = "year";
      public const string Busy = "busy";
      public const string AvailabilityStatus = "availabilityStatus";
      public const string FreeTime = "freeTime";
      public const string ShortName = "shortName";
      public const string IncludedIn = "includedIn";
      public const string ProjectCoreId = "projectCoreId";
      public const string Kind = "kind";
      public const string State = "state";
      public const string BusinessUnit = "businessUnit";
      public const string Department = "department";
      public const string JobTitle = "jobTitle";
      public const string Manager = "manager";
      
      public const string BusinessUnitIds = "businessUnitIds";
      public const string DepartmentIds = "departmentIds";
      
      public const string ProjectShortName = LocalizationKeys.Project + "." + LocalizationKeys.ShortName;// "project.shortName";
      public const string ProjectIncludedIn = LocalizationKeys.Project + "." + LocalizationKeys.IncludedIn; //"project.includedIn";
      public const string ProjectKind = LocalizationKeys.Project + "." + LocalizationKeys.Kind;//"project.kind";
      public const string ProjectState = LocalizationKeys.Project + "." + LocalizationKeys.State;//"project.state";
      public const string EmployeeName = TypeData.Employee + "." + LocalizationKeys.Name;//"employee.name";
      public const string EmployeeDepartment = TypeData.Employee + "." + LocalizationKeys.Department;// "employee.department";
      public const string EmployeeBusinessUnit = TypeData.Employee + "." + LocalizationKeys.BusinessUnit;// "employee.businessUnit";
      public const string EmployeeJobTitle = TypeData.Employee + "." + LocalizationKeys.JobTitle;// "employee.jobTitle";
      public const string ManagerProjectName =  LocalizationKeys.Project + "." + LocalizationKeys.Manager;// "project.manager";
      
    }
    
    public static class TaskDtoStatuses
    {
      public const string Submitted = "Submitted";
      public const string Unfinished = "Unfinished";
    }
    
    /// <summary>
    /// Ограничение числа контрольных точек при поиске из Вехи.
    /// </summary>
    public const int GatesSearchLimit = 100;
    
    /// <summary>
    /// Количество акутальных контрольных точек при поиске из Вехи.
    /// </summary>
    public const int ActualGatesSearchLimit = 5;
  }
}