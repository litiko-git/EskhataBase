using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram.Server
{
  partial class RiskFunctions
  {
    /// <summary>
    /// Получить все риски с фильтром по проекту.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Список рисков по проекту.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<PortfolioProgram.IRisk> GetAllProjectRisks(Sungero.Projects.IProjectCore project)
    {
      return PortfolioProgram.Risks.GetAll(r => r.RelatedProjectCore != null && Sungero.Projects.ProjectCores.Equals(project, r.RelatedProjectCore));
    }
    
    /// <summary>
    /// Создание рисков для проекта из шаблонов рисков.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="risks">Список шаблонов рисков.</param>
    /// <returns>True - если что-то создалось, False - если ничего не создалось</returns>
    [Public, Remote]
    public static bool CreateProjectRisksFromTemplates(Sungero.Projects.IProjectCore project, ProjectPlanner.IProjectActivity stage, List<PortfolioProgram.IRiskTemplate> templates)
    {
      //TODO Рассмотреть перенос на создание через АО.
      if (!templates.Any())
      {
        return false;
      }
      
      foreach (var template in templates)
      {
        var newRisk = CreateRiskFromTemplate(template);
        newRisk.RelatedProjectCore = project;
        newRisk.RelatedActivityRef = stage;
        newRisk.Save();
      }
      
      return true;
    }
    
    /// <summary>
    /// Создание рисков для проекта из других рисков.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="risks">Список рисков.</param>
    /// <returns>True - если что-то создалось, False - если ничего не создалось</returns>
    [Public, Remote]
    public static bool CreateProjectRisksFromRisks(Sungero.Projects.IProjectCore project, ProjectPlanner.IProjectActivity stage, List<PortfolioProgram.IRisk> risks)
    {
      //TODO Рассмотреть перенос на создание через АО.
      if (!risks.Any())
      {
        return false;
      }
          
      foreach (var risk in risks)
      {
        var newRisk = CreateRiskFromRisk(risk);
        newRisk.RelatedProjectCore = project;
        newRisk.RelatedActivityRef = stage;
        newRisk.IsImplement = false;
        newRisk.Save();
      }
      
      return true;
    }
    
    /// <summary>
    /// Создание риска на подобии существующего. Проект и этап не копируются.
    /// </summary>
    /// <param name="risk">Риск.</param>
    /// <returns>Созданный риск.</returns>
    /// <remarks>Новый риск не сохраняется.</remarks>
    [Public, Remote]
    public static PortfolioProgram.IRisk CreateRiskFromRisk(PortfolioProgram.IRisk risk)
    {
      var newRisk = PortfolioProgram.Risks.Create();
      newRisk.Name = risk.Name;
      newRisk.Category = risk.Category;
      newRisk.StartDate = risk.StartDate;
      newRisk.DueDate = risk.DueDate;
      newRisk.Type = risk.Type;
      newRisk.Probability = risk.Probability;
      newRisk.Impact = risk.Impact;
      newRisk.Cost = risk.Cost;
      newRisk.Currency = risk.Currency;
      newRisk.Description = risk.Description;
      newRisk.MitigationPlan = risk.MitigationPlan;
      newRisk.ContingencyPlan = risk.ContingencyPlan;
      return newRisk;
    }
    
    /// <summary>
    /// Создание риска по шаблону. Проект и этап не заполняются.
    /// </summary>
    /// <param name="risk">Риск.</param>
    /// <returns>Созданный риск.</returns>
    /// <remarks>Новый риск не сохраняется.</remarks>
    [Public, Remote]
    public static PortfolioProgram.IRisk CreateRiskFromTemplate(PortfolioProgram.IRiskTemplate riskTemplate)
    {
      var newRisk = PortfolioProgram.Risks.Create();
      newRisk.Name = riskTemplate.Name;
      newRisk.Category = riskTemplate.Category;
      newRisk.StartDate = riskTemplate.StartDate;
      newRisk.DueDate = riskTemplate.DueDate;
      newRisk.Type = riskTemplate.Type;
      newRisk.Probability = riskTemplate.Probability;
      newRisk.Impact = riskTemplate.Impact;
      newRisk.Cost = riskTemplate.Cost;
      newRisk.Currency = riskTemplate.Currency;
      newRisk.Description = riskTemplate.Description;
      newRisk.MitigationPlan = riskTemplate.MitigationPlan;
      newRisk.ContingencyPlan = riskTemplate.ContingencyPlan;
      return newRisk;
    }
  }
}