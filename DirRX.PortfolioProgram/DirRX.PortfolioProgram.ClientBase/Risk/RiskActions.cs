using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram.Client
{
  internal static class RiskStaticActions
  {

    public static void AddRisk(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (CallContext.CalledFrom(Sungero.Projects.ProjectCores.Info))
      {
        var projectId = CallContext.GetCallerEntityId(Sungero.Projects.ProjectCores.Info);
        var project = Sungero.Projects.ProjectCores.Get(projectId);
        
        var templateRisks = Functions.RiskTemplate.Remote.GetAllTemplateRisks().ShowSelectMany().ToList();
        if (templateRisks.Count() == 1)
        {
          var risk = Functions.Risk.Remote.CreateRiskFromTemplate(templateRisks.FirstOrDefault());
          risk.RelatedProjectCore = project;
          risk.ShowModal();
        }
        else if (templateRisks.Any())
        {
          if (Functions.Risk.Remote.CreateProjectRisksFromTemplates(project, null, templateRisks))
            Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksSuccessfullyCreated);
          else
            Dialogs.NotifyMessage(DirRX.PortfolioProgram.Risks.Resources.RisksNotCreated);
        }
      }
    }
    
    public static bool CanAddRisk(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !CallContext.CalledDirectlyFrom(PortfolioProgram.Risks.Info);
    }
  }

  partial class RiskCollectionActions
  {
    

    public virtual void CopyToProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Risk.ApplyRisksToProjectRiskDialog(Risks.Resources.CopyRisk, _objs);
    }
    
    public virtual bool CanCopyToProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CallContext.CalledFrom(Sungero.Projects.ProjectCores.Info) && _objs.Where(r => r.RelatedProjectCore != null).Any();
    }
  }
}