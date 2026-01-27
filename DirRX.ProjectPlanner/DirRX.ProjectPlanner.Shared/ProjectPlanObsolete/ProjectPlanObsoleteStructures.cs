using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.ProjectPlanObsolete
{

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
}