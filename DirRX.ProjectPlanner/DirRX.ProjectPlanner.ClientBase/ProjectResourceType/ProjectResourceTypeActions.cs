using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectResourceType;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectResourceTypeActions
  {
    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

  }


}