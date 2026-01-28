using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.ProgressReport
{
  /// <summary>
  /// Строчка отчета по вехам проекта.
  /// </summary>
  [Public]
  partial class MilestoneTableLine
  {
    public string ReportSessionId { get; set; }
    
    public long Id { get; set; }
    
    public string Wbs { get; set; }
    
    public string Name { get; set; }
    
    public DateTime? Date { get; set; }

    public string Status { get; set; }
    
    public string Responsible { get; set; }
    
    public int NumberVersion { get; set; }
  }
  
  /// <summary>
  /// Строчка отчета по этапам.
  /// </summary>
  [Public]
  partial class StageTableLine
  {
    public string ReportSessionId { get; set; }
    
    public long IdStage { get; set; }
    
    public string Wbs { get; set; }
    
    public string NameStage { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public int ExecutionPercent { get; set; }
    
    public string Responsible { get; set; }
    
    public int NumberVersion { get; set; }
  }

}