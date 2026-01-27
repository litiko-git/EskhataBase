using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.Gate;

namespace DirRX.ProjectPlanner
{
  partial class GateServerHandlers
  {

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      var projectCoreIsLockedByMeFromCard = false;
      e.Params.TryGetValue(Constants.Gate.LockProjectCoreParam, out projectCoreIsLockedByMeFromCard);
      long? projectCoreId = null;
      e.Params.TryGetValue(Constants.Gate.UnlockLockProjectCoreParam, out projectCoreId);
      if (projectCoreId != null)
      {
        Functions.Gate.TryUnlockProject(projectCoreId, null, projectCoreIsLockedByMeFromCard);
      }
    }
    
    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var projectCore = DirRX.ProjectPlanning.ProjectCores.GetAll(p =>
                                                                  p.GatesDirRX.Any(g => Equals(g.Gate, _obj))
                                                                 ).SingleOrDefault();
      if (projectCore == null)
      {
        return;
      }
      
      var gateLink = projectCore.GatesDirRX.Where(g => g.Gate.Equals(_obj)).Single();
      projectCore.GatesDirRX.Remove(gateLink);

      e.Params.Add(Constants.Gate.UnlockLockProjectCoreParam, projectCore.Id);
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      var projectCore = DirRX.ProjectPlanning.ProjectCores.GetAll(p =>
                                                                  p.GatesDirRX.Any(g => Equals(g.Gate, _obj))
                                                                 ).SingleOrDefault();
      
      if (projectCore != null)
      {
        foreach (var gateLink in projectCore.GatesDirRX.Where(g => g.Gate.Equals(_obj)).ToList())
        {
          gateLink.Level = _obj.Level;
          gateLink.PlanDate = _obj.PlanDate;
          gateLink.ActualDate = _obj.ActualDate;
          gateLink.IsPassed = _obj.IsPassed;
          gateLink.Assessment = _obj.Assessment;
          gateLink.Performer = _obj.Performer;
        }
        
        projectCore.Save();
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.IsPassed = false;
    }
  }

}