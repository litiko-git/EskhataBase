using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.RiskTemplate;

namespace DirRX.PortfolioProgram.Shared
{
  partial class RiskTemplateFunctions
  {
    /// <summary>
    /// Установить обязательность, доступность, видимость свойств.
    /// </summary>
    public void SetStateProperties()
    {
      var isRequired = _obj.Type == PortfolioProgram.Risk.Type.Risk;
      
      _obj.State.Properties.Category.IsRequired = isRequired;
      
      if (!isRequired && _obj.Category != null)
      {
        _obj.Category = null;
      }
    }
  }
}