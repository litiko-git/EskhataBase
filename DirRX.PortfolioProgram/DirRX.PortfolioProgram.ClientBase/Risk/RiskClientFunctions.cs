using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram.Client
{
  partial class RiskFunctions
  {
    /// <summary>
    /// Диалог связывания риска с проектом из шаблонов (типовых рисков).
    /// </summary>
    /// <param name="dialogName">Заголовок диалога.</param>
    /// <param name="_objs">Список типовых рисков.</param>
    public static void ApplyTemplatesToProjectRiskDialog(CommonLibrary.LocalizedString dialogName, System.Collections.Generic.IEnumerable<IRiskTemplate> _objs)
    {
      var dialog = GetApplyProjectDialog(dialogName);
      var projectCore = GetProjectCoreInput(dialog);
      var stage = GetStageInput(dialog);
      
      projectCore.SetOnValueChanged((x) =>
                                    {
                                      ProjectCoreDialogValueChanged(x, stage);
                                    });
      
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        var riskTemplates = _objs.ToList();
        if (Functions.Risk.Remote.CreateProjectRisksFromTemplates(projectCore.Value, stage.Value, riskTemplates))
          Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksSuccessfullyCreated);
        else
          Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksNotCreated);
      }
    }
    
    /// <summary>
    /// Диалог связывания риска с проектом на основе других рисков.
    /// </summary>
    /// <param name="dialogName">Заголовок диалога.</param>
    /// <param name="_objs">Список рисков.</param>
    public static void ApplyRisksToProjectRiskDialog(CommonLibrary.LocalizedString dialogName, System.Collections.Generic.IEnumerable<IRisk> _objs)
    {
      var dialog = GetApplyProjectDialog(dialogName);
      var projectCore = dialog.AddSelect(DirRX.PortfolioProgram.Risks.Resources.Project, true, Sungero.Projects.ProjectCores.Null).Where(p => p.Stage != Sungero.Projects.ProjectCore.Stage.Completed);
      var stage = dialog.AddSelect(DirRX.PortfolioProgram.Risks.Resources.Stage, false, ProjectPlanner.ProjectActivities.Null);
      
      projectCore.SetOnValueChanged((x) =>
                                    {
                                      ProjectCoreDialogValueChanged(x, stage);
                                    });
      
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        // Исключаем из рисков те, которые уже созданы по указанному проекту.
        var risks = _objs.Where(r => !Sungero.Projects.ProjectCores.Equals(r.RelatedProjectCore, projectCore.Value)).ToList();
        
        if (Functions.Risk.Remote.CreateProjectRisksFromRisks(projectCore.Value, stage.Value, risks))
          Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksSuccessfullyCreated);
        else
          Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksNotCreated);
      }
    }

    
    #region Создание диалога
    private static CommonLibrary.IInputDialog GetApplyProjectDialog(CommonLibrary.LocalizedString dialogName)
    {
      return Dialogs.CreateInputDialog(dialogName);
    }
    
    private static Sungero.Core.INavigationDialogValue<Sungero.Projects.IProjectCore> GetProjectCoreInput(CommonLibrary.IInputDialog dialog)
    {
      return dialog.AddSelect(DirRX.PortfolioProgram.Risks.Resources.Project, true, Sungero.Projects.ProjectCores.Null).Where(p => p.Stage != Sungero.Projects.ProjectCore.Stage.Completed);
    }
    
    private static Sungero.Core.INavigationDialogValue<DirRX.ProjectPlanner.IProjectActivity> GetStageInput(CommonLibrary.IInputDialog dialog)
    {
      return dialog.AddSelect(DirRX.PortfolioProgram.Risks.Resources.Stage, false, ProjectPlanner.ProjectActivities.Null);
    }
    
    private static void ProjectCoreDialogValueChanged(
      CommonLibrary.InputDialogValueChangedEventArgs<Sungero.Projects.IProjectCore> x,
      Sungero.Core.INavigationDialogValue<DirRX.ProjectPlanner.IProjectActivity> stage)
    {
      if (x.NewValue == null)
      {
        return;
      }
      
      var project = ProjectPlanning.Projects.As(x.NewValue);
      var stages = new List<ProjectPlanner.IProjectActivity>();
      var emptyText = DirRX.PortfolioProgram.Risks.Resources.NotAssociatedProjectPlan;
      
      if (project != null && project.ProjectPlanDirRX != null)
      {
        stages = Functions.Risk.GetStagesLastVersionApproved(project.ProjectPlanDirRX).ToList();
        if (stages.Any())
          emptyText = DirRX.PortfolioProgram.Risks.Resources.EmptyText;
      }
      stage.WithPlaceholder(emptyText);
      stage.From(stages);
    }
    
    #endregion
    
  }
}