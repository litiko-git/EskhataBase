using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning;
using DirRX.PortfolioProgram.Portfolio;

namespace DirRX.PortfolioProgram.Server
{
  partial class PortfolioFunctions
  {
    /// <summary>
    /// Создать портфель.
    /// </summary>
    /// <returns>Портфель.</returns>
    [Public, Remote]
    public static IPortfolio CreatePortfolio()
    {
      return Portfolios.Create();
    }
    
  }
}