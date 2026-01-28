using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;

namespace DirRX.ProjectPlanner.Shared
{
  partial class ProjectPlanRXFunctions
  {
    /// <summary>
    /// Установить доступность свойств в зависимости от наличия этапов по проекту.
    /// </summary>
    public void SetPropertiesAvailability()
    {
      // TODO: используется серверная функция для получения списка проектов.
      _obj.State.Properties.StartDate.IsEnabled = !_obj.HasVersions;
      _obj.State.Properties.EndDate.IsEnabled = !_obj.HasVersions;
      _obj.State.Properties.BaselineWork.IsEnabled = false;
      _obj.State.Properties.ActualWorkload.IsEnabled = false;
      
      _obj.State.Properties.PlannedCosts.IsEnabled = false;
      _obj.State.Properties.FactualCosts.IsEnabled = false;
      _obj.State.Properties.ActualStartDate.IsEnabled = !_obj.HasVersions;
      _obj.State.Properties.ActualFinishDate.IsEnabled = !_obj.HasVersions;
      _obj.State.Properties.LifeCycleState.IsVisible = true;
      
      _obj.State.Properties.ActualWorkload.IsVisible = false;
      _obj.State.Properties.FactualCosts.IsVisible = false;
      _obj.State.Properties.ExecutionPercent.IsVisible = false;
      
      foreach(var property in _obj.State.Properties.ResponsibleNotices.Properties)
      {
        property.IsEnabled = _obj.AccessRights.CanManageOrDelegate();
      }
      
      foreach(var property in _obj.State.Properties.PlanDateNotices.Properties)
      {
        property.IsEnabled = _obj.AccessRights.CanManageOrDelegate();
      }
      
      foreach(var property in _obj.State.Properties.OtherNotices.Properties)
      {
        property.IsEnabled = _obj.AccessRights.CanManageOrDelegate();
      }
      
      //Добавить условие наличия соответствующих ролей.
      var noticeAddittionalColumnsVisibility = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj) != null;
      
      _obj.State.Properties.ResponsibleNotices.Properties.EventType.IsEnabled = false;
      _obj.State.Properties.ResponsibleNotices.Properties.ProjectAdmin.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.ResponsibleNotices.Properties.CustomerInternal.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.ResponsibleNotices.Properties.Participant.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.ResponsibleNotices.Properties.MgmntTeam.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.ResponsibleNotices.Properties.Observers.IsVisible = noticeAddittionalColumnsVisibility;
      
      _obj.State.Properties.PlanDateNotices.Properties.EventType.IsEnabled = false;
      _obj.State.Properties.PlanDateNotices.Properties.ProjectAdmin.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.PlanDateNotices.Properties.CustomerInternal.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.PlanDateNotices.Properties.Participant.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.PlanDateNotices.Properties.MgmntTeam.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.PlanDateNotices.Properties.Observers.IsVisible = noticeAddittionalColumnsVisibility;
      
      _obj.State.Properties.OtherNotices.Properties.EventType.IsEnabled = false;
      _obj.State.Properties.OtherNotices.Properties.ProjectAdmin.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.OtherNotices.Properties.CustomerInternal.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.OtherNotices.Properties.Participant.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.OtherNotices.Properties.MgmntTeam.IsVisible = noticeAddittionalColumnsVisibility;
      _obj.State.Properties.OtherNotices.Properties.Observers.IsVisible = noticeAddittionalColumnsVisibility;
    }
    
    public override void SetLifeCycleState()
    {
      // Не нужно наследовать логику OfficialDocument, сами управляем статусом.
    }
  }
}