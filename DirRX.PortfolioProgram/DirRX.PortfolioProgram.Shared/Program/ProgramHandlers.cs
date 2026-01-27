using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Program;

namespace DirRX.PortfolioProgram
{
  partial class ProgramSharedHandlers
  {

    public virtual void WorkloadLimitChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.WorkloadLimit = e.OriginalValue;
      }
    }

    public virtual void CostLimitChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.CostLimit = e.OriginalValue;
      }
    }
  }

}