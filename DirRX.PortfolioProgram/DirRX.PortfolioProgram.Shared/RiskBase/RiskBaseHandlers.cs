using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.RiskBase;

namespace DirRX.PortfolioProgram
{
  partial class RiskBaseSharedHandlers
  {
    public virtual void CostChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        return;
      
      // Подставить по умолчанию валюту рубль.
      if (_obj.Currency == null)
      {
        var defaultCurrency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
        if (defaultCurrency != null)
          _obj.Currency = defaultCurrency;
      }
    }
  }
}