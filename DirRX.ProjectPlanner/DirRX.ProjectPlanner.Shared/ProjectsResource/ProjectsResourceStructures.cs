using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Structures.ProjectsResource
{

  /// <summary>
  /// 
  /// </summary>
  [Public]
  partial class ProjectResourceDto
  {
    public long ResourceId { get; set; }
    public string ResourceName { get; set; }
  }

}