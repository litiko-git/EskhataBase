using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.TeamsCommonAPI.TeamsAssignment;

namespace DirRX.TeamsCommonAPI.Client
{
  partial class TeamsAssignmentActions
  {
    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}