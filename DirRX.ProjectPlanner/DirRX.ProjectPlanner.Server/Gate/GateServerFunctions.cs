using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning;
using DirRX.ProjectPlanner.Gate;

namespace DirRX.ProjectPlanner.Server
{
  partial class GateFunctions
  {
    [Remote]
    public static void TryReleaseDeletedGateLinksByTransaction(long deletedGateId)
    {
      using(var connection = Functions.Module.CreateDBConnectionPublic())
      using(var transaction = connection.BeginTransaction())
      {
        ReleaseProjectCoreFromDeletedGate(deletedGateId);
        ReleaseActivitiesFromDeletedGate(deletedGateId);
      }
    }
    
    private static void ReleaseProjectCoreFromDeletedGate(long deletedGateId)
    {
      var projectCore = GetProjectCoreOfDeletedGate(deletedGateId);
      var deletedGate = projectCore.GatesDirRX.Where(g => g.Gate != null && g.Gate.Id == deletedGateId)
        .FirstOrDefault();
      if (deletedGate == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Gates.Resources.FindGateErrorTextFormat(deletedGateId, projectCore.Id));
      }
      
      projectCore.GatesDirRX.Remove(deletedGate);
      projectCore.Save();
    }
    
    private static DirRX.ProjectPlanning.IProjectCore GetProjectCoreOfDeletedGate(long deletedGateId)
    {
      long projectCoreId = -1;
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Gate.GetProjectCoreIdByGate;
        SQL.AddParameter(command, "@GateId", deletedGateId, System.Data.DbType.Int64);
        
        using (var reader = command.ExecuteReader())
        {
          if (reader.Read())
          {
            projectCoreId = reader.GetInt64(0);
          }
        }
      }
      
      if (projectCoreId < 0)
      {
        throw new Exception(DirRX.ProjectPlanner.Gates.Resources.FindProjectCoreErrorTextFormat(deletedGateId));
      }
      
      return DirRX.ProjectPlanning.ProjectCores.Get(projectCoreId);
    }
    
    private static void ReleaseActivitiesFromDeletedGate(long deletedGateId)
    {
      var activitiesToRelease = DirRX.ProjectPlanner.ProjectActivities.GetAll(a => a.GateId.HasValue &&
        a.GateId.Value == deletedGateId);
      
      foreach (var activityToRelease in activitiesToRelease)
      {
        activityToRelease.GateId = null;
      }
      
      SaveReleasedActivitiesByDisableEvents(activitiesToRelease);
    }
    
    private static void SaveReleasedActivitiesByDisableEvents(IQueryable<IProjectActivity> activities)
    {
      foreach (var activity in activities)
      {
        using(EntityEvents.DisableAll(activity.Info))
        {
          activity.Save();
        }
      }
    }

    /// <summary>
    /// Получить связанный с КТ объект управления.
    /// </summary>
    /// <param name="gate">Контрольная точка.</param>
    /// <returns>Объект управления, null - если связи нет.</returns>
    [Remote(IsPure = true)]
    public static IProjectCore GetLinkedProjectCoreAsAdmin(IGate gate)
    {
      IProjectCore projectCore = null;
      AccessRights.AllowRead(() =>
                             {
                               projectCore = DirRX.ProjectPlanning.ProjectCores.GetAll(p =>
                                                                                       p.GatesDirRX.Any(g => Equals(g.Gate, gate))
                                                                                      ).FirstOrDefault();
                             });
      return projectCore;
    }

    /// <summary>
    /// Разблокировать проект, связанный с контрольной точкой. Блокировка может ставиться во время открытия карточки КТ.
    /// </summary>
    /// <param name="projectCoreId">ID объекта управления для снятия блокировки.</param>
    /// <param name="gate">Контрольная точка.</param>
    /// <param name="projectCoreIsLockedByMeFromCard">Признак, что проект заблокирован сам по себе, не через КТ.</param>
    [Remote]
    public static void TryUnlockProject(long? projectCoreId, IGate gate, bool projectCoreIsLockedByMeFromCard)
    {
      IProjectCore projectCore = null;
      if (projectCoreId != null)
      {
        projectCore = DirRX.ProjectPlanning.ProjectCores.GetAll(p => p.Id == projectCoreId).SingleOrDefault();
      }
      else if (gate != null)
      {
        projectCore = DirRX.ProjectPlanning.ProjectCores.GetAll(p =>p.GatesDirRX.Any(g => Equals(g.Gate, gate))).FirstOrDefault();
      }

      if (projectCore == null)
      {
        return;
      }
      
      var lockInfoProjectCore = Locks.GetLockInfo(projectCore);
      if (lockInfoProjectCore.IsLockedByMe && projectCoreIsLockedByMeFromCard)
      {
        Locks.Unlock(projectCore);
      }
    }
    
    
    /// <summary>
    /// Получение модели контрола состояние вех.
    /// </summary>
    /// <returns>Модель контрола состояния вех.</returns>
    [Remote]
    public StateView GetGateState()
    {
      var stateView = StateView.Create();
      stateView.AddDefaultLabel(Gates.Resources.RelationsDefaultText);
      
      if(_obj.State.IsInserted)
      {
        return stateView;
      }
      
      var hyperlinkStyle = StateBlockHyperlinkStyle.Create();
      hyperlinkStyle.FontSize = 16;
      
      var milestones = Functions.ProjectActivity.GetMilestonesForGates(new long[] {_obj.Id});
      var projects = DirRX.ProjectPlanning.Projects.GetAll(p => p.ProjectPlanDirRX != null);

      var milestoneProjectDict = milestones.Join(
        projects,
        m => m.ProjectPlan.Id,
        p => p.ProjectPlanDirRX.Id,
        (m, p) => new { Milestone = m, ProjectName = p.Name }
       )
        .ToDictionary(x => x.Milestone, x => x.ProjectName);
      
      var user = Users.Current;
      foreach(var milestone in milestoneProjectDict.Keys)
      {
        var block = stateView.AddBlock();
        var mm = ProjectActivities.As(milestone);
        var link = ProjectPlanner.PublicFunctions.Module.GetWebsiteLink(mm.ProjectPlan.Id, mm.NumberVersion.Value, mm.Id, user.Id, false);
        
        block.AssignIcon(DirRX.ProjectPlanner.Gates.Resources.MilestoneIcon, StateBlockIconSize.Large);
        block.AddHyperlink(milestone.Name, link, hyperlinkStyle);
        block.AddLineBreak();
        block.AddLabel(Resources.PerformerName);
        block.AddLineBreak();
        block.AddLabel(milestone.Responsible?.DisplayValue ?? DirRX.ProjectPlanner.Gates.Resources.ResponsibleNotSpecified);
        
        var date = block.AddContent();
        if (milestone.EndDate != _obj.PlanDate)
        {
          var dateLabelStyle = StateBlockLabelStyle.Create();
          dateLabelStyle.Color = Colors. FromRgb(231, 76, 60);
          date.AddLabel(milestone.EndDate != null ? milestone.EndDate.Value.ToShortDateString() : "—", dateLabelStyle);
        }
        else
        {
          date.AddLabel(milestone.EndDate != null ? milestone.EndDate.Value.ToShortDateString() : "—");
        }
        
        var project = block.AddContent();
        project.AddLabel(milestoneProjectDict[milestone]);
        
        var isPassed = block.AddContent();
        isPassed.AddLabel(milestone.ExecutionPercent > 99 ? DirRX.ProjectPlanner.Gates.Resources.GateIsPassed : DirRX.ProjectPlanner.Gates.Resources.GateNotPassed);
      }
      
      return stateView;
    }
  }
}