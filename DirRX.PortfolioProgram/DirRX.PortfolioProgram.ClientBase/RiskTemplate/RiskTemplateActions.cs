using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.RiskTemplate;

namespace DirRX.PortfolioProgram.Client
{
  partial class RiskTemplateActions
  {
    public virtual void ApplyToProjectRisk(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var riskTemplateList = new List<IRiskTemplate> { _obj };
      Functions.Risk.ApplyTemplatesToProjectRiskDialog(Risks.Resources.AddRisk, riskTemplateList);
    }

    public virtual bool CanApplyToProjectRisk(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !CallContext.CalledFrom(Sungero.Projects.ProjectCores.Info) && !_obj.State.IsInserted;
    }

  }
  
  partial class RiskTemplateCollectionActions
  {
    public virtual void ApplyToProjectRisk(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Risk.ApplyTemplatesToProjectRiskDialog(Risks.Resources.AddRisk, _objs);
    }
    
    public virtual bool CanApplyToProjectRisk(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !CallContext.CalledFrom(Sungero.Projects.ProjectCores.Info) && !_objs.Any(r => r.State.IsInserted);
    }
  }

}