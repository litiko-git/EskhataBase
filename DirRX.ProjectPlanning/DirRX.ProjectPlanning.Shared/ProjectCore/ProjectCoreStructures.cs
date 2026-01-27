using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Structures.Projects.ProjectCore
{

  /// <summary>
  /// Высчитанные значения затрат, трудоемкости и прогресса дочерих структур.
  /// </summary>
  partial class CalculatedData
  {
    public double PlannedCosts { get; set; }
    
    public double FactualCosts { get; set; }
    
    public double PlannedWorkload { get; set; }
    
    public double FactualWorkload { get; set; }
    
    public double ExecutionPercentSum { get; set; }
    
    public int RecordsCount { get; set; }
  }
  
  /// <summary>
  /// Данные в структуре для каждого объекта.
  /// </summary>
  partial class CostWorkloadText
  {
    //Лимит только для программы
    public string CostLimit { get; set; }
    
    public string CostPlan { get; set; }
    
    public string CostFact { get; set; }
    
    public string WorkloadLimit { get; set; }
    
    public string WorkloadPlan { get; set; }
    
    public string WorkloadFact { get; set; }
  }

}