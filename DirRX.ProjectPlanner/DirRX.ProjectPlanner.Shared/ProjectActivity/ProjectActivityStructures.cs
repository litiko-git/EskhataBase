using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.ProjectActivity
{

  /// <summary>
  /// Данные этапа/раздела/вехи, которые не относятся к планированию.
  /// Меняются динамически в бд и не фиксируются в версии документа.
  /// </summary>
  partial class DynamicActivityData
  {
    public long Id { get; set; }
    public double? ActualWorkload { get; set; }
    public double? FactualCosts { get; set; }
    public int? ExecutionPercent { get; set; }
    public Sungero.Core.Enumeration? Status { get; set; }
  }

}