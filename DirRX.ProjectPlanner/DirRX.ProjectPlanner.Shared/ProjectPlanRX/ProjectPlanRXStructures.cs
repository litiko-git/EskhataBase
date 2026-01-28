using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.ProjectPlanRX
{

  /// <summary>
  /// Структура для временного сохранения значений карточки плана проекта,
  /// чтобы восстановить их после сохранения тела документа
  /// </summary>
  partial class PlanCardInfoBackup
  {
    public int? ExecutionPercent { get; set; }
    public double? FactualCosts { get; set; }
    public double? ActualWorkload { get; set; }
    public double? BaselineWork { get; set; }
    public double? PlannedCosts { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
  }

  /// <summary>
  /// Соответствие ИД этапов нового и копируемого проекта.
  /// </summary>
  partial class IDActivities
  {
    /// <summary>
    /// ИД копируемого этапа.
    /// </summary>
    public long IDSource { get; set; }
    
    /// <summary>
    /// Новый этап.
    /// </summary>
    public ProjectPlanner.IProjectActivity TargetActitvity { get; set; }
  }
  
  /// <summary>
  /// Данные значений для карточки плана проекта.
  /// </summary>
  [Public]
  partial class PlanCalculatedData
  {
    public long PlanId { get; set; }
    public int? ExecutionPercent { get; set; }
    public double? PlanCosts { get; set; }
    public double? FactCosts { get; set; }
    public double? PlanWorkload { get; set; }
    public double? FactWorkload { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? FactStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public DateTime? FactEndDate { get; set; }
  }
}
