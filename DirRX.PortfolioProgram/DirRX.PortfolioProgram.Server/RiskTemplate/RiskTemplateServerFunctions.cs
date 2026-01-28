using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.RiskTemplate;

namespace DirRX.PortfolioProgram.Server
{
  partial class RiskTemplateFunctions
  {
    /// <summary>
    /// Получить все типовые риски.
    /// </summary>
    /// <returns>Список типовых рисков.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<PortfolioProgram.IRiskTemplate> GetAllTemplateRisks()
    {
      return PortfolioProgram.RiskTemplates.GetAll();
    }
  }
}