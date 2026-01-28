using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Server
{
  internal static class DesktopViewHandlers
  {

    private static bool CanApplyProjects()
    {
      return DirRX.ProjectPlanning.ProjectCores.AccessRights.CanRead();
    }
  }


}