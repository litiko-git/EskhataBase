using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectsResource;

namespace DirRX.ProjectPlanner
{
  partial class ProjectsResourceClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if(_obj.Type != null)
        _obj.State.Properties.Employee.IsVisible = _obj.Type.ServiceName == Constants.Module.ResourceTypes.Users;
    }

  }
}