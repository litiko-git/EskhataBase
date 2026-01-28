using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI;

namespace DirRX.ProjectPlanner
{
  partial class ProjectPlanRXTeamMembersClientHandlers
  {

    public virtual void TeamMembersGroupValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, ProjectPlanRXes.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void TeamMembersMemberValueInput(DirRX.ProjectPlanner.Client.ProjectPlanRXTeamMembersMemberValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, ProjectPlanRXes.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }
  }

  partial class ProjectPlanRXClientHandlers
  {

    public virtual void IsEnableAutoSendingTasksValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      var textInfo = e.NewValue.Value ?
        ProjectPlanRXes.Resources.EnableAutoSendingTasksInfo :
        ProjectPlanRXes.Resources.DisableAutoSendingTasksInfo;
      
      e.AddInformation(textInfo);
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      Functions.OpensProjectPlansFromCard.Remote.DeleteEntry(_obj);
      
      if (Locks.GetLockInfo(_obj).IsLockedByMe)
      {
        Functions.ProjectPlanRX.Remote.SendPlanCardClosedMessage(_obj);
      }
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {    
      Functions.ProjectPlanRX.SetPropertiesAvailability(_obj);
      if (!_obj.State.IsInserted)
        Functions.OpensProjectPlansFromCard.Remote.CreateEntry(_obj);
      
        // Кэширую таким образом запрос для получения данных для моделей состояния, 
        // чтобы на каждую модель не выполнялся каждый раз запрос в бд.
      ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.Id});
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (_obj.State.IsCopied)
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, ProjectPlanRXes.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
      
      if (_obj.Status != ProjectPlanRX.Status.Closed)
        Functions.ProjectPlanRX.SetPropertiesAvailability(_obj);
    }

    public virtual void ExecutionPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (_obj.HasVersions)
        e.AddError(ProjectPlanRXes.Resources.DisableProperty);
            
      if ((e.NewValue != null) && (e.NewValue.Value < 0 || e.NewValue.Value > 100))
        e.AddError(ProjectPlanRXes.Resources.IncorrectPercent);
    }

    public virtual void ActualFinishDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.ActualStartDate != null) && (_obj.ActualStartDate > e.NewValue))
        e.AddError(ProjectPlanRXes.Resources.IncorrectEndDate, _obj.Info.Properties.ActualFinishDate);
    }

    public virtual void ActualStartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.ActualFinishDate != null) && (e.NewValue > _obj.ActualFinishDate))
        e.AddError(ProjectPlanRXes.Resources.IncorrectStartDate, _obj.Info.Properties.ActualStartDate);
    }

    public virtual void EndDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.HasVersions)
        e.AddError(ProjectPlanRXes.Resources.DisableProperty);
            
      if ((_obj.StartDate != null) && (_obj.StartDate > e.NewValue))
        e.AddError(ProjectPlanRXes.Resources.IncorrectEndDate, _obj.Info.Properties.EndDate);
    }

    public virtual void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.HasVersions)
        e.AddError(ProjectPlanRXes.Resources.DisableProperty);
      
      if ((_obj.EndDate != null) && (e.NewValue > _obj.EndDate))
        e.AddError(ProjectPlanRXes.Resources.IncorrectStartDate, _obj.Info.Properties.StartDate);
    }

  }
}