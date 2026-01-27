using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram
{
  partial class RiskClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      
    }

    public override void TypeValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      Functions.Risk.SetStateProperties(_obj);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.Risk.SetStateProperties(_obj);
    }

  }
}