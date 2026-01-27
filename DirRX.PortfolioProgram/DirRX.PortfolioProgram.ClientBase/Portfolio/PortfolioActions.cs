using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PortfolioProgram.Client
{
  partial class PortfolioActions
  {
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
      return !_obj.State.IsInserted;
    }

    public override void CreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateSubProjectAsPortfolioDirRX(e);
    }

    public override bool CanCreateSubProjectAsPortfolioDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void ReopenProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = DirRX.ProjectPlanning.PublicFunctions.Module.CreateConfirmDialog(Portfolios.Resources.ReopenPortfolioDialogMessage,
                                                                                    Portfolios.Resources.ReopenPortfolioDialogDescription);
      
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
      var dialog = DirRX.ProjectPlanning.PublicFunctions.Module.CreateConfirmDialog(Portfolios.Resources.ClosePortfolioDialogMessage,
                                                                                    Portfolios.Resources.ClosePortfolioDialogDescription);
      
      if (dialog.Show() == DialogButtons.Yes)
      {
        DirRX.ProjectPlanning.Module.Projects.PublicFunctions.Module.CloseProjectCores(_obj.Id);
      }
    }

    public override bool CanCloseProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCloseProject(e);
    }

  }

}