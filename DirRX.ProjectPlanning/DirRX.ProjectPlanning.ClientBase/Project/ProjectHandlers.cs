using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning
{
  partial class ProjectClientHandlers
  {

    public override void ProjectKindValueInput(Sungero.Projects.Client.ProjectCoreProjectKindValueInputEventArgs e)
    {
      SynchronizeProjectClassifiersAndMembersWithProjectKind(e);
      base.ProjectKindValueInput(e);
    }
    
    private void SynchronizeProjectClassifiersAndMembersWithProjectKind(Sungero.Projects.Client.ProjectCoreProjectKindValueInputEventArgs e)
    {
      if (e.NewValue == null)
      {
        return;
      }
      
      if (_obj.Classifier.Any() || _obj.TeamMembers.Any())
      {
        e.AddWarning(DirRX.ProjectPlanning.Projects.Resources.ClassifiersOrMembersAlreadyConfigured);
      }
      
      var projectKindDirRX = ProjectKinds.As(e.NewValue);
      if (projectKindDirRX == null)
      {
        Logger.Debug(DirRX.ProjectPlanning.Projects.Resources.ProjectKindCastErrorFormat(_obj.Id));
        Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
        
        return;
      }
      
      if (projectKindDirRX.ClassifierDirRX.Any() && !_obj.Classifier.Any())
      {
        this.SynchronizeClassifiers(projectKindDirRX);
      }
      
      if (projectKindDirRX.MembersDirRX.Any() && !_obj.TeamMembers.Any())
      {
        this.SynchronizeMembers(projectKindDirRX);
      }
    }
    
    private void SynchronizeClassifiers(IProjectKind projectKind)
    {
      foreach (var sourceClassifier in projectKind.ClassifierDirRX)
      {
        var targetClassifier = _obj.Classifier.AddNew();
        targetClassifier.FolderName = sourceClassifier.FolderName;
        targetClassifier.DocumentKind = sourceClassifier.DocumentKind;
      }
    }
    
    private void SynchronizeMembers(IProjectKind projectKind)
    {
      foreach (var sourceMember in projectKind.MembersDirRX)
      {
        var targetMember = _obj.TeamMembers.AddNew();
        targetMember.Member = sourceMember.Member;
        targetMember.Group = sourceMember.Group;
      }
    }
    
    public override void ActualStartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
       if (_obj.ActualFinishDate != null && e.NewValue >= _obj.ActualFinishDate)
       {
         e.AddError(DirRX.ProjectPlanning.Projects.Resources.IncorrectActDates, _obj.Info.Properties.ActualStartDate);
       }
    }

    public override void ActualFinishDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.ActualStartDate != null && _obj.ActualStartDate >= e.NewValue)
      {
        e.AddError(DirRX.ProjectPlanning.Projects.Resources.IncorrectActDates, _obj.Info.Properties.ActualFinishDate);
      }
    }

    public virtual void ProjectPlanDirRXValueInput(DirRX.ProjectPlanning.Client.ProjectProjectPlanDirRXValueInputEventArgs e)
    {
      if (!_obj.Folder.AccessRights.CanChangeFolderContent())
      {
        e.AddError(Projects.Resources.ProjectPlanChangeErrorMessage);
      }
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      if (Sungero.Core.Licenses.IsModuleValidForCurrentUser(Constants.Projects.Project.ProjectPlannerModuleFullName))
      {
        DirRX.ProjectPlanner.PublicFunctions.OpensProjectPlansFromCard.Remote.DeleteEntry(_obj);
      }

      base.Closing(e);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      Functions.Project.SetPropertiesAvailability(_obj);
      
      if (!_obj.State.IsInserted && Sungero.Core.Licenses.IsModuleValidForCurrentUser(Constants.Projects.Project.ProjectPlannerModuleFullName))
      {
        DirRX.ProjectPlanner.PublicFunctions.OpensProjectPlansFromCard.Remote.CreateEntry(_obj);
      }
      
      if (_obj.ProjectPlanDirRX != null)
      {
        // Кэширую таким образом запрос для получения данных для моделей состояния, 
        // чтобы на каждую модель не выполнялся каждый раз запрос в бд.
        ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(new List<long>() {_obj.ProjectPlanDirRX.Id});
      }
    }

    public override void EndDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.ProjectPlanDirRX != null && _obj.ProjectPlanDirRX.HasVersions)
      {
        e.AddError(Projects.Resources.DisableProperty);
      }
      
      base.EndDateValueInput(e);
    }

    public override void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (_obj.ProjectPlanDirRX != null && _obj.ProjectPlanDirRX.HasVersions)
      {
        e.AddError(Projects.Resources.DisableProperty);
      }

      base.StartDateValueInput(e);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (_obj.Stage != Stage.Completed)
      {
        Functions.Project.SetPropertiesAvailability(_obj);
      }
      Functions.Project.TogglePlanFields(_obj);
    }

    public virtual void PlannedWorkloadDirRXValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (_obj.ProjectPlanDirRX != null && _obj.ProjectPlanDirRX.HasVersions)
      {
        e.AddError(Projects.Resources.DisableProperty);
      }
      
      if ((e.NewValue != null) && (e.NewValue.Value < 0))
      {
        e.AddError(Projects.Resources.IncorrectBaselineWork);
      }
    }

  }
}