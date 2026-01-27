using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.Gate;

namespace DirRX.ProjectPlanner.Client
{
  partial class GateActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      try
      {
        //Kiselev_EM Сначала необходимо везде избавиться от ссылок на КТ, иначе упадет ошибка.
        Functions.Gate.Remote.TryReleaseDeletedGateLinksByTransaction(_obj.Id);
        
        base.DeleteEntity(e);
      }
      catch (Sungero.Domain.Shared.Exceptions.LockManagementException ex)
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Gates.Resources.DeleteGateLockErrorTextFormat(ex.Message));
      }
      catch (Exception ex)
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
        Logger.Error(DirRX.ProjectPlanner.Gates.Resources.DeleteGateErrorTextFormat(_obj.Id), ex);
      }
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (!base.CanDeleteEntity(e))
      {
        return false;
      }

      var projectCore = Functions.Gate.Remote.GetLinkedProjectCoreAsAdmin(_obj);
      if (projectCore != null)
      {
        var lockInfo = Locks.GetLockInfo(projectCore);
        return projectCore.AccessRights.CanUpdate() && (!lockInfo.IsLocked || lockInfo.IsLockedByMe);
      }
      return true;
    }

  }

}