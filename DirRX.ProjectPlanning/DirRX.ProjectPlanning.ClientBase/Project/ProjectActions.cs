using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;
using DirRX.ProjectPlanning;

namespace DirRX.ProjectPlanning.Client
{
  partial class ProjectActions
  {
    public virtual void ShowProgressReportDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.ProjectPlanDirRX == null)
      {
        e.AddError(ProjectPlanning.Projects.Resources.ProjectPlanNotExists);
        return;
      }
      
      var report = DirRX.ProjectPlanner.Reports.GetProgressReport();
      report.ProjectPlanRX = _obj.ProjectPlanDirRX;

      report.Open();
    }

    public virtual bool CanShowProgressReportDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ProjectPlanDirRX != null && _obj.ProjectPlanDirRX.HasVersions;
    }

    public override void CreateSubProjectAsProjectDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateSubProjectAsProjectDirRX(e);
    }

    public override bool CanCreateSubProjectAsProjectDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void CreateSubProjectAsProgramDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateSubProjectAsProgramDirRX(e);
    }

    public override bool CanCreateSubProjectAsProgramDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateSubProjectAsPortfolioDirRX(e);
    }

    public override bool CanCreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }



    public override void ReopenProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = DirRX.ProjectPlanning.PublicFunctions.Module.CreateConfirmDialog(Projects.Resources.ReopenProjectDialogMessage,
                                                                                    Projects.Resources.ReopenProjectDialogDescription);
      
      if (dialog.Show() == DialogButtons.Yes)
      {
        base.ReopenProject(e);
      }
    }

    public override bool CanReopenProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanReopenProject(e);
    }

    public override void CloseProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if(_obj.ProjectPlanDirRX != null)
      {
        _obj.StartDate = _obj.ProjectPlanDirRX.StartDate;
        _obj.EndDate = _obj.ProjectPlanDirRX.EndDate;
        _obj.ActualStartDate = _obj.ProjectPlanDirRX.ActualStartDate;
        _obj.ActualFinishDate = _obj.ProjectPlanDirRX.ActualFinishDate;
        _obj.Save();
      }
      
      
      var dialog = DirRX.ProjectPlanning.PublicFunctions.Module.CreateConfirmDialog(Projects.Resources.CloseProjectDialogMessage,
                                                                                    Projects.Resources.CloseProjectDialogDescription);
      
      if (dialog.Show() == DialogButtons.Yes)
      {
        DirRX.ProjectPlanning.Module.Projects.PublicFunctions.Module.CloseProjectCores(_obj.Id);
      }
    }

    public override bool CanCloseProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCloseProject(e) && !_obj.State.IsInserted;
    }


    public virtual void PlanProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.ProjectPlanDirRX != null)
      {
        _obj.ProjectPlanDirRX.Show();
      }
      else
      {
        var dialog = Dialogs.CreateTaskDialog(DirRX.ProjectPlanning.Projects.Resources.NotFoundProjectPlan, MessageType.Question);
        dialog.Buttons.AddYesNo();
        
        if (dialog.Show() == DialogButtons.Yes)
        {
          if (!_obj.Folder.AccessRights.CanChangeFolderContent())
          {
            e.AddError(Projects.Resources.ProjectPlanCreateErrorMessage);
            return;
          }
          e.CloseFormAfterAction = true;

          var plan = Functions.Project.Remote.CreateProjectPlan(_obj);
          _obj.ProjectPlanDirRX = plan;
          _obj.Folder.Items.Add(plan);
          plan.Show();
        }
      }
      
      // Kiselev_EM Добавил выход из карточки проекта, чтобы при открытии из него плана на редактирование снять блокировку с проекта.
      // В этот же момент сохраняется связанный план пректа
      base.SaveAndClose(e);
    }

    public virtual bool CanPlanProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && ClientApplication.ApplicationType == ApplicationType.Web;
    }
  }

}