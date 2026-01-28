using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectsResource;

namespace DirRX.ProjectPlanner
{
  partial class ProjectsResourceSharedHandlers
  {

    public virtual void TypeChanged(DirRX.ProjectPlanner.Shared.ProjectsResourceTypeChangedEventArgs e)
    {
      _obj.State.Properties.Employee.IsVisible = e.NewValue.ServiceName == Constants.Module.ResourceTypes.Users;
    }

  }
}