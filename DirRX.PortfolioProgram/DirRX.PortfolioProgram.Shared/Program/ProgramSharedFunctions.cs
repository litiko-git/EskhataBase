using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Program;

namespace DirRX.PortfolioProgram.Shared
{
  partial class ProgramFunctions
  {

    /// <summary>
    /// Скрыть поля на карточке Программы, связанные с планированием.
    /// </summary>
    public void SetPlanningFieldsState(bool? state)
    {
      var boolState = state ?? true;
      _obj.State.Properties.ExecutionPercent.IsVisible = boolState;
    }

  }
}