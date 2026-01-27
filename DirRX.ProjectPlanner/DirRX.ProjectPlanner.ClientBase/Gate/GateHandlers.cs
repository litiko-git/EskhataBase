using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning;
using DirRX.ProjectPlanner.Gate;

namespace DirRX.ProjectPlanner
{
  partial class GateClientHandlers
  {


    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.State.IsInserted)
      {
        return;
      }
      
      var projectCore = Functions.Gate.Remote.GetLinkedProjectCoreAsAdmin(_obj);
      if (projectCore == null)
      {
        return;
      }
      
      if (!CallContext.CalledFrom(projectCore.Info))
      {
        DisallowProperties(_obj);
        if (Locks.GetLockInfo(_obj).IsLockedByMe)
        {
          Locks.Unlock(_obj);
        }
        
        return;
      }
      
      if (!projectCore.AccessRights.CanUpdate())
      {
        e.AddWarning(Gates.Resources.NoAccessLinkedProjectCore);
        DisallowProperties(_obj);
        if (Locks.GetLockInfo(_obj).IsLockedByMe)
        {
          Locks.Unlock(_obj);
        }
        
        return;
      }
      
      var lockInfo = Locks.GetLockInfo(projectCore);
      if (lockInfo.IsLockedByOther)
      {
        DisallowProperties(_obj);
        if (Locks.GetLockInfo(_obj).IsLockedByMe)
        {
          Locks.Unlock(_obj);
        }
      }
    }

    public virtual void IsPassedValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      _obj.State.Properties.ActualDate.IsRequired = e.NewValue.Value;
    }
    
    private static void DisallowProperties(IGate gate)
    {
      foreach(var property in gate.State.Properties)
      {
        property.IsEnabled = false;
      }
      
    }

  }
}