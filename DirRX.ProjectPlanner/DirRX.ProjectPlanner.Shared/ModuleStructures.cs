using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.Module
{
  
  [Public]
  partial class RootTaskInfo
  {
    public List<long> RootTaskIds { get; set; }
    public List<long> NotRootTaskIds { get; set; }
  }
  
  [Public]
  partial class ProjectCoreDto
  {
    public string Type { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string StatusIssues { get; set; }
    public string ManagerName { get; set; }
    public long? LeadingId { get; set; }
    
    public DateTime? StartDatePlan { get; set; }
    public DateTime? EndDatePlan { get; set; }
    public DateTime? StartDateActual { get; set; }
    public DateTime? EndDateActual { get; set; }
  }
  
  /// <summary>
  /// Строка сводного отчета.
  /// </summary>
  [Public]
  partial class ResourceReportRow
  {
    public long ResourceId { get; set; }
    public long PerformerId { get; set; }
    public double Busy { get; set; }
    public long ActivityId { get; set; }
    public string ActivityName { get; set; }
    public long ProjectPlanId  { get; set; }
    public long? ProjectCoreId { get; set; }
    public string ManagerName { get; set; }
    public DateTime Date { get; set; }
  }
  
  /// <summary>
  /// Строка сводного отчета по оргструктуре.
  /// </summary>
  [Public]
  partial class ResourceReportOrgRow
  {
    public long ResourceId { get; set; }
    public long PerformerId { get; set; }
    public double Busy { get; set; }
    public long ActivityId { get; set; }
    public string ActivityName { get; set; }
    public long ProjectPlanId  { get; set; }
    public long? ProjectCoreId { get; set; }
    public string ManagerName { get; set; }
    public DateTime Date { get; set; }
    public string AvailabilityStatus { get; set; }
  }

  [Public]
  partial class AttachmentDto
  {
    public long? AttachmentId { get; set; }
    public string Url { get; set; }
    public string Name { get; set; }
  }
  
  [Public]
  partial class AttachmentDtoResponse
  {
    public DirRX.ProjectPlanner.Structures.Module.IAttachmentDto Attachment { get; set; }
    public string Error { get; set; }
  }
  
  [Public]
  partial class ResourcesWorkloadDto
  {
    public long ResourceId { get; set; }
    public double Value { get; set; }
  }
  
  [Public]
  partial class PredecessorDto
  {
    public long? Id { get; set; }
    public string LinkType { get; set; }
    public int Lag { get; set; }
  }
  
  [Public]
  partial class TaskDto
  {
    public long? Id { get; set; }
    public string DisplayValue { get; set; }
    public string HyperLink { get; set; }
    public DateTime? Deadline { get; set; }
  }
  
  [Public]
  partial class ActivityStatusDto
  {
    public string EnumValue { get; set; }
    public string LocalizeValue { get; set; }
  }
  
  [Public]
  partial class ProjectDto
  {
    public double? BaselineWork { get; set; }
    public double? ActualWorkload { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ExecutionPercent { get; set; }
    public long Id { get; set; }
    public string Name { get; set; }
    public string Note { get; set; }
    public DateTime? StartDate { get; set; }
    public string TypeActivity { get; set; }
    public double? PlannedCosts { get; set; }
    public double? FactualCosts { get; set; }
    public long? ManagerId { get; set; }
  }
  
  [Public]
  partial class ResourceTypesDto
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string SectionName { get; set; }
  }
  
  [Public]
  partial class UsersDto
  {
    public long? Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Position { get; set; }
  }
  
  [Public]
  partial class MaterialResourcesDto
  {
    public long Id { get; set; }
    public string Name { get; set; }
  }
  
  [Public]
  partial class ResourcesDto
  {
    public long Id { get; set; }
    public long EntityTypeId { get; set; }
    public long EntityId { get; set; }
    public string UnitLabel { get; set; }
  }
  
  [Public]
  partial class CapacityDto
  {
    public long ResourceId { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityValueDto> Values { get; set; }
  }
  
  [Public]
  partial class CapacityValueDto
  {
    public DateTime Date { get; set; }
    public double Busy { get; set; }
  }
  
  [Public]
  partial class WorkingTimeCalendarDto
  {
    public List<long> ResourcesIds { get; set; }
    public List<DateTime> FreeDays { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkDaysDto> WorkDays { get; set; }
  }
  
  [Public]
  partial class WorkDaysDto
  {
    public DateTime Date { get; set; }
    public double Duration { get; set; }
  }
  
  [Public]
  partial class ResourcesDataDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IResourceTypesDto> ResourceTypes { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IUsersDto> Users { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IMaterialResourcesDto> MaterialResources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourcesDto> Resources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityDto> Capacity { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendarDto> WorkingTimeCalendars { get; set; }
  }
  
  [Public]
  partial class ActivityDto
  {
    public int? Priority { get; set; }
    public double? FactualCosts { get; set; }
    public double? PlannedCosts { get; set; }
    public string TypeActivity { get; set; }
    public int SortIndex { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITaskDto> UnfinishedTasks { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITaskDto> SubmittedTasks { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IPredecessorDto> Predecessors { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IActivityStatusDto Status { get; set; }
    public int ExecutionPercent { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime StartDate { get; set; }
    public long? ResponsibleId { get; set; }
    public long? LeadActivityId { get; set; }
    public string Note { get; set; }
    public int? CurrentNumber { get; set; }
    public string Name { get; set; }
    public long Id { get; set; }
    public long? RefId { get; set; }
    public double? BaselineWork { get; set; }
    public double? ActualWorkload { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourcesWorkloadDto> Resources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IAttachmentDto> Attachments { get; set; }
    public long? GateId { get; set; }
  }
  
  [Public]
  partial class GanttProjectPlanDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IActivityDto> Activities { get; set; }
    public long LastActivityId { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IResourcesDataDto ResourcesData { get; set; }
    public DirRX.ProjectPlanner.Structures.Module.IProjectDto Project { get; set; }
    public int NumberVersion { get; set; }
    public long ProjectPlanId { get; set; }
  }
  
  /// <summary>
  /// Результат запроса проверки существования плана.
  /// </summary>
  [Public]
  partial class ProjectPlanLockInfo
  {
    public List<string> lockMessages { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ProjectPlanDto
  {
    public DirRX.ProjectPlanner.Structures.Module.IResourcesData ResourceData  { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ITask> Tasks { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IActivityGateDto> ActivityGates { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ILocalizedEnum> LevelStatuses { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ILocalizedEnum> ActivityStatuses { get; set; }
  }
  
  [Public]
  partial class LocalizedEnum
  {
    public string Name { get; set; }
    public string Localized { get; set; }
  }
  
  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class Task
  {
    public long ActivityId { get; set; }
    public DateTime? Deadline { get; set; }
    public string HyperLink { get; set; }
    public long Id { get; set; }
    public string TaskStatus { get; set; }
    public string DisplayValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate  { get; set; }
    public bool IsRoot  { get; set; }
    public string ActivityStatus { get; set; }
    public int? ExecutionPercent { get; set; }
    public double? ActualWorkload { get; set; }
    public double? FactualCosts {get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class User
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string JobTitle { get; set; }
    public string Department { get; set; }
    public bool IsActive { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ResourceTypes
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string SectionName { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class Resource
  {
    public long Id { get; set; }
    public long EntityTypeId { get; set; }
    public long EntityId { get; set; }
    public string UnitLabel { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class MaterialResource
  {
    public long Id { get; set; }
    public string Name { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ResourcesData
  {
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacity> Capacity { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IMaterialResource> MaterialResources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResource> Resources { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IResourceTypes> ResourceTypes { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IUser> Users { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendar> WorkingTimeCalendars { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class WorkDay
  {
    public DateTime Date { get; set; }
    public double Duration { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class CapacityValue
  {
    public DateTime Date { get; set; }
    public double Busy { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class Capacity
  {
    public long ResourceId { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacityValue> Values { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class WorkingTimeCalendar
  {
    public List<long> ResourcesIds { get; set; }
    public List<DateTime> FreeDays { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkDay> WorkDays { get; set; }
  }

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class CapacityResponseDto
  {
    public List<DirRX.ProjectPlanner.Structures.Module.IWorkingTimeCalendar> WorkingTimeCalendar { get; set; }
    public List<DirRX.ProjectPlanner.Structures.Module.ICapacity> Capacity { get; set; }
  }
  
  partial class ProjectPlanDates
  {
    public DateTime Start {get; set;}
    
    public DateTime End {get; set;}
  }
  
  /// <summary>
  /// Соответствие этапа и ИД ведущего этапа по модели.
  /// </summary>
  partial class LeadActivities
  {
    public DirRX.ProjectPlanner.IProjectActivity Activity { get; set; }
    
    public long LeadActivityId { get; set; }
  }
  
  /// <summary>
  /// Соответствие ИД новых этапов из модели и базы.
  /// </summary>
  partial class IdActivityMapper
  {
    public long ModelActivityId { get; set; }
    
    public long Id { get; set; }
  }
  
  /// <summary>
  /// Средняя нагрузка по периоду
  /// </summary>
  partial class AverageBusy
  {
    public DateTime StartDate {get;set;}
    
    public DateTime EndDate {get;set;}
    
    public double AvgBusy {get;set;}
    
    public long ResourceId {get;set;}
  }
  
  
  partial class Calendars
  {
    public List<Sungero.CoreEntities.IPrivateWorkingTimeCalendar> PrivateCalendars {get;set;}
    public List<Sungero.CoreEntities.IWorkingTimeCalendar> PublicCalendars {get;set;}
  }
  
  [Public]
  partial class SignatureDto
  {
    public long VersionId { get; set; }
    
    public bool IsApproval { get; set; }
    
    public bool IsEndorsing { get; set; }
    
    public DateTime SigningDate { get; set; }
    
    public string SignatoryFullName { get; set; }
    
    public long SignatoryId { get; set; }
  }
  
  [Public]
  partial class GateDto
  {
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string DisplayName { get; set; }
    
    public string Level { get; set; }
    
    public bool? IsPassed { get; set; }
    
    public DateTime? PlanDate { get; set; }
    
    public DateTime? ActualDate { get; set; }
  }
  
  [Public]
  partial class ActivityGateDto
  {
    public long ActivityId { get; set; }
    
    public DirRX.ProjectPlanner.Structures.Module.IGateDto Gate { get; set; }
  }
  
  [Public]
  partial class RoadmapProjectDatabaseItem
  {
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public DirRX.ProjectPlanning.IProjectCore Project { get; set; }
    public long? ManagerId { get; set; }
    public string ManagerName { get; set; }
    public long? ProjectPlanId { get; set; }
  }
}
