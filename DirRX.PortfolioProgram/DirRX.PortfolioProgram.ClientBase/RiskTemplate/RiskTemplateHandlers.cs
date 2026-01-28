using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.RiskTemplate;

namespace DirRX.PortfolioProgram
{
  partial class RiskTemplateClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
     Functions.RiskTemplate.SetStateProperties(_obj);
    }

  }
}