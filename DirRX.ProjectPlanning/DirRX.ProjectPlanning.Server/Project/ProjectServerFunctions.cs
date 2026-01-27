using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning.Server
{
  partial class ProjectFunctions
  {
    /// <summary>
    /// Получить модель состояния для плановых затрат.
    /// </summary>
    /// <returns>Модель состояния для плановых затрат.</returns>
    [Remote(IsPure = true)]
    public StateView TableStatePlanCosts()
    {
      var stateView = StateView.Create();
      if (_obj.ProjectPlanDirRX == null)
      {
        return stateView;
      }
      var data = ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.ProjectPlanDirRX.Id})[_obj.ProjectPlanDirRX.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      
      var block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.PlannedCostsName, bold);
      block.AddContent().AddLabel(data.PlanCosts.ToString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.PlanWorkloadName, bold);
      block.AddContent().AddLabel(data.PlanWorkload.ToString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      return stateView;
    }
    
    /// <summary>
    /// Получить модель состояния для фактических затрат.
    /// </summary>
    /// <returns>Модель состояния для фактических затрат.</returns>
    [Remote(IsPure = true)]
    public StateView TableStateFactCosts()
    {
      var stateView = StateView.Create();
      
      if (_obj.ProjectPlanDirRX == null)
      {
        return stateView;
      }
      
      var data = ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.ProjectPlanDirRX.Id})[_obj.ProjectPlanDirRX.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      
      var block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.FactCostsName, bold);
      block.AddContent().AddLabel(data.FactCosts.ToString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.FactWorkloadName, bold);
      block.AddContent().AddLabel(data.FactWorkload.ToString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.ExecutionPercentageName, bold);
      block.AddContent().AddLabel(data.ExecutionPercent.ToString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
            
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      return stateView;
    }
    
    /// <summary>
    /// Получить модель состояния для фактических дат.
    /// </summary>
    /// <returns>Модель состояния для фактических дат.</returns>
    [Remote(IsPure = true)]
    public StateView TableStateFactDates()
    {
      var stateView = StateView.Create();
      
      if (_obj.ProjectPlanDirRX == null)
      {
        return stateView;
      }
      
      var data = ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.ProjectPlanDirRX.Id})[_obj.ProjectPlanDirRX.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      
      var block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.FactStartDateName, bold);
      block.AddContent().AddLabel(data.FactStartDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.FactEndDateName, bold);
      block.AddContent().AddLabel(data.FactEndDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      return stateView;
    }
    
    /// <summary>
    /// Поулчить модель состояния для плановых дат.
    /// </summary>
    /// <returns>Модель состояния для плановых дат.</returns>
    [Remote(IsPure = true)]
    public StateView TableStatePlanDates()
    {
      var stateView = StateView.Create();
      
      if (_obj.ProjectPlanDirRX == null)
      {
        return stateView;
      }
      
      var data = ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.ProjectPlanDirRX.Id})[_obj.ProjectPlanDirRX.Id];
      
      var bold = StateBlockLabelStyle.Create();
      bold.Color = Colors.Common.Gray;
      var thin = StateBlockLabelStyle.Create();
      thin.FontSize = 1;
      
      var block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.PlanStartDateName, bold);
      block.AddContent().AddLabel(data.PlanStartDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel(ProjectPlanner.Resources.PlanEndDateName, bold);
      block.AddContent().AddLabel(data.PlanEndDate?.ToShortDateString());
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      block = stateView.AddBlock();
      block.AddContent().AddLabel("", thin);
      block.ShowBorder = true;
      block.DockType = DockType.Bottom;
      
      return stateView;
    }
    
    /// <summary>
    /// Возвращает информацию о блокировке проекта.
    /// </summary>
    /// <returns>Информация о блокировке проекта.</returns>
    /// <remarks>
    /// Если проект заблокирован, то возвращается информация о блокировке.
    /// Если же не заблокирован, то пустая строка.
    /// </remarks>
    [Remote]
    public string GetLockInfo()
    {
      var lockInfo = Locks.GetLockInfo(_obj);
      return lockInfo.IsLockedByOther && !string.IsNullOrEmpty(lockInfo.LockedMessage) ? lockInfo.LockedMessage.ToString() : string.Empty;
    }
    
    /// <summary>
    /// Создает план проекта на основании проекта
    /// </summary>
    /// <returns>План проекта</returns>
    [Remote]
    public DirRX.ProjectPlanner.IProjectPlanRX CreateProjectPlan()
    {
      var plan = DirRX.ProjectPlanner.ProjectPlanRXes.Create();
      plan.Name = string.Format(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan, _obj.ShortName);
      plan.StartDate = _obj.StartDate;
      plan.EndDate = _obj.EndDate;
      plan.ActualStartDate = _obj.ActualStartDate;
      plan.ActualFinishDate = _obj.ActualFinishDate;
      plan.ExecutionPercent = _obj.ExecutionPercent;
      plan.BaselineWork = _obj.PlannedWorkloadDirRX;
      plan.PlannedCosts = _obj.PlannedCosts;
      plan.FactualCosts = _obj.FactualCosts;
      
      foreach (var member in _obj.TeamMembers)
      {
        var planMember = plan.TeamMembers.AddNew();
        planMember.Member = member.Member;
        planMember.Group = member.Group;
      }
      return plan;
    }
    
    public void ApplyNewProjectPlan()
    {
      UpdateProjectPlanInFolder();
      
      if (_obj.ProjectPlanDirRX == null)
      {
        return;
      }
      
      foreach (var tm in _obj.ProjectPlanDirRX.TeamMembers)
      {
        if (!_obj.TeamMembers.Any(x => Recipients.Equals(x.Member, tm.Member)))
        {
          var newRow = _obj.TeamMembers.AddNew();
          newRow.Member = tm.Member;
          newRow.Group = tm.Group;
        }
      }
      
      if(_obj.State.Properties.Stage.IsChanged)
      {
        if (_obj.Stage == Stage.Completed)
        {
          _obj.ProjectPlanDirRX.Status = ProjectPlanner.ProjectPlanRX.Status.Closed;
        }
        else
        {
          _obj.ProjectPlanDirRX.Status = ProjectPlanner.ProjectPlanRX.Status.Active;
        }
      }
    }
    
    private void UpdateProjectPlanInFolder()
    {
      if (_obj.Folder == null)
      {
        Logger.ErrorFormat("Попытка назначить проекту план. В проекте(ИД={0}) не создана папка.", _obj.Id);
        return;
      }
      
      if (!_obj.Folder.AccessRights.CanChangeFolderContent())
      {
        Logger.ErrorFormat("Попытка назначить проекту план. У пользователя (ИД={0}) Нет прав на изменение содержимого папки проекта(ИД={1}).", Users.Current.Id, _obj.Folder.Id);
        return;
      }
      
      var oldPlan = _obj.State.Properties.ProjectPlanDirRX.OriginalValue;
      if (oldPlan != null)  // удаляем из папки прежний план
      {
        _obj.Folder.Items.Remove(oldPlan);
      }

      if (_obj.ProjectPlanDirRX != null)  // кладем новый
      {
        _obj.Folder.Items.Add(_obj.ProjectPlanDirRX);
      }
    }
  }
}