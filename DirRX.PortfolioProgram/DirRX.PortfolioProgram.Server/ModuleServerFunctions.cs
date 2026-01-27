using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PortfolioProgram.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Проверяет наличие лицензии модуля PortfolioProgram.
    /// </summary>
    /// <returns>true - если модуль лицензирован, иначе false.</returns>
    [Public]
    public static bool PortfolioProgramModuleHasLicense()
    {
      return Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Constants.Module.PortfolioProgramModuleGuid);
    }

  }
}