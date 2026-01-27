using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using DirRX.ProjectPlanning.ProjectCore;
using ResourcesPlanner = DirRX.ProjectPlanner.Resources;

namespace DirRX.ProjectPlanning.Client
{

  partial class ProjectCoreActions
  {

    public virtual void ShowRoadmapDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var uriRoadmap = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetUriRoadmap(_obj.Id);
      Hyperlinks.Open(uriRoadmap.ToString());  
    }
    
    public virtual bool CanShowRoadmapDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return DirRX.PortfolioProgram.PublicFunctions.Module.PortfolioProgramModuleHasLicense() && !_obj.State.IsInserted;
    }

    public virtual void CreateGateDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var gate = DirRX.ProjectPlanner.Gates.Create();
      gate.Owner = _obj.Manager;
      gate.ShowModal();
      
      if (gate.State.IsInserted)
      {
        return;
      }
      
      var gateChild = _obj.GatesDirRX.AddNew();
      gateChild.Gate = gate;
      
      var managmentTeamRecipients = _obj.TeamMembers.Where(tm => tm.Group == ProjectCoreTeamMembers.Group.Change).Select(tm => tm.Member);
      
      foreach(var recipient in managmentTeamRecipients)
      {
        gate.AccessRights.Grant(recipient, DefaultAccessRightsTypes.Change);
      }
      
      gate.AccessRights.Save();
      
      _obj.Save();
    }

    public virtual bool CanCreateGateDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && _obj.AccessRights.CanUpdate();
    }

    public virtual void CreateSubProjectAsProjectDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var project = Projects.Create();
      project.LeadingProject = _obj;
      
      project.Show();
    }

    public virtual bool CanCreateSubProjectAsProjectDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CreateSubProjectAsProgramDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var program = DirRX.PortfolioProgram.Programs.Create();
      program.LeadingProject = _obj;
      
      program.Show();
    }

    public virtual bool CanCreateSubProjectAsProgramDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      try
      {
        var portfolio = DirRX.PortfolioProgram.Portfolios.Create();
        var thisAsPortfolio = PortfolioProgram.Portfolios.As(_obj);
        portfolio.LeadingProject = thisAsPortfolio;
        
        portfolio.Show();
      }
      catch (Exception ex)
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
        Logger.Debug(DirRX.ProjectPlanning.ProjectCores.Resources.BadAttemptAddSubprojectErrorMessageFormat(ex.Message));
      }
    }

    public virtual bool CanCreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }


    public virtual void RisksDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      PortfolioProgram.PublicFunctions.Risk.Remote.GetAllProjectRisks(_obj).Show();
    }

    public virtual bool CanRisksDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void CloseProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CloseProject(e);
    }

    public override bool CanCloseProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCloseProject(e) && !_obj.State.IsInserted;
    }

    public virtual bool CanShowWorkloadAnalysis(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void ShowWorkloadAnalysis(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(ProjectCores.Resources.WorkloadAnalysisTitle);
      var startDate = dialog.AddDate(ProjectCores.Resources.WorkloadAnalysisStartDateName, true, Calendar.Now.Date);
      var endDate = dialog.AddDate(ProjectCores.Resources.WorkloadAnalysisEndDateName, true, Calendar.Now.Date.AddMonths(1));
      var defaultDateScale = ResourcesPlanner.MonthsTimeline;
      
      if (Projects.Is(_obj))
      {
        defaultDateScale = ResourcesPlanner.WeeksTimeline;
      }
      
      var timelineControl = dialog.AddSelect(ResourcesPlanner.WorkloadAnalysisTimeline, true, defaultDateScale).From(ResourcesPlanner.DaysTimeline,
                                                                                                                     ResourcesPlanner.WeeksTimeline,
                                                                                                                     ResourcesPlanner.MonthsTimeline,
                                                                                                                     ResourcesPlanner.QuartersTimeline,
                                                                                                                     ResourcesPlanner.YearsTimeline);
        
      dialog.SetOnButtonClick((args) => 
                                  {
                                    if (endDate.Value < startDate.Value)
                                      args.AddError(ProjectCores.Resources.WorloadAnalysisIncorrectDate);
                                  }
                                 );
      
      if (dialog.Show() == DialogButtons.Cancel)
      {
        return;
      }
      
      string dateScaleKey = "null";
      
      if (timelineControl.Value == ResourcesPlanner.DaysTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineDays;
      }
      else if (timelineControl.Value == ResourcesPlanner.WeeksTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineWeeks;
      }
      else if (timelineControl.Value == ResourcesPlanner.MonthsTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineMonths;
      }
      else if (timelineControl.Value == ResourcesPlanner.QuartersTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineQuarters;
      }
      else if (timelineControl.Value == ResourcesPlanner.YearsTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineYears;
      }
      
      var dateRange = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetDateRange(startDate.Value.Value, endDate.Value.Value);
      var uriCompReport = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetUriResourceReport(_obj.Id, dateRange, dateScaleKey);
      
      Hyperlinks.Open(uriCompReport.ToString());
      
    }
  }

}