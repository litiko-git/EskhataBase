using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.PortfolioProgram.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      var projectManagersRole = Sungero.Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();

      if (projectManagersRole != null)
      {
        GrantRightsOnPortfolios(projectManagersRole);
        GrantRightsOnPrograms(projectManagersRole);
      }
      
      GrantRightsRiskTemplates(projectManagersRole);
      GrantRightsRisks(projectManagersRole);
      GrantRightsRiskCategories();
      GrantRightsProbabilities();
      GrantRightsImpacts();
      GrantRightsFolderRisksList();
      CreateRiskCategories();
      CreateProbabilities();
      CreateImpacts();
    }
    
    /// <summary>
    /// Выдать права на Портфели.
    /// </summary>
    public static void GrantRightsOnPortfolios(IRole role)
    {
      InitializationLogger.Debug("Init: Grant rights on Portfolios to project manager role.");
      
      DirRX.PortfolioProgram.Portfolios.AccessRights.Grant(role, DefaultAccessRightsTypes.Create);
      DirRX.PortfolioProgram.Portfolios.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на Программы.
    /// </summary>
    public static void GrantRightsOnPrograms(IRole role)
    {
      InitializationLogger.Debug("Init: Grant rights on Programs to to project manager role.");
      
      DirRX.PortfolioProgram.Programs.AccessRights.Grant(role, DefaultAccessRightsTypes.Create);
      DirRX.PortfolioProgram.Programs.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на Типовые риски.
    /// </summary>
    public static void GrantRightsRiskTemplates(IRole projectManagersRole)
    {
      InitializationLogger.Debug("Init: Grant rights on risk templates.");
      
      var rightsChanged = false;
      
      if (!PortfolioProgram.RiskTemplates.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        PortfolioProgram.RiskTemplates.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        rightsChanged = true;
      }
      
      if (!PortfolioProgram.RiskTemplates.AccessRights.IsGranted(DefaultAccessRightsTypes.Create, Roles.AllUsers))
      {
        PortfolioProgram.RiskTemplates.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
        rightsChanged = true;
      }
      
      if (!PortfolioProgram.RiskTemplates.AccessRights.IsGranted(DefaultAccessRightsTypes.FullAccess, projectManagersRole))
      {
        PortfolioProgram.RiskTemplates.AccessRights.Grant(projectManagersRole, DefaultAccessRightsTypes.FullAccess);
        rightsChanged = true;
      }

      if (rightsChanged)
      {
        PortfolioProgram.RiskTemplates.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права на Риски.
    /// </summary>
    public static void GrantRightsRisks(IRole projectManagersRole)
    {
      InitializationLogger.Debug("Init: Grant rights on risks.");
      
      var rightsChanged = false;
      
      if (!PortfolioProgram.Risks.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        PortfolioProgram.Risks.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        rightsChanged = true;
      }
      
      if (!PortfolioProgram.Risks.AccessRights.IsGranted(DefaultAccessRightsTypes.Create, Roles.AllUsers))
      {
        PortfolioProgram.Risks.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
        rightsChanged = true;
      }
      
      if (!PortfolioProgram.Risks.AccessRights.IsGranted(DefaultAccessRightsTypes.FullAccess, projectManagersRole))
      {
        PortfolioProgram.Risks.AccessRights.Grant(projectManagersRole, DefaultAccessRightsTypes.FullAccess);
        rightsChanged = true;
      }
      
      if (rightsChanged)
      {
        PortfolioProgram.Risks.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права на Категории рисков.
    /// </summary>
    public static void GrantRightsRiskCategories()
    {
      InitializationLogger.Debug("Init: Grant rights on risk categories.");
      if (!PortfolioProgram.RiskCategories.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        PortfolioProgram.RiskCategories.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        PortfolioProgram.RiskCategories.AccessRights.Save();
      }
    }

    /// <summary>
    /// Выдать права на Вероятности.
    /// </summary>
    public static void GrantRightsProbabilities()
    {
      InitializationLogger.Debug("Init: Grant rights on probabilities.");
      if (!PortfolioProgram.Probabilities.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        PortfolioProgram.Probabilities.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        PortfolioProgram.Probabilities.AccessRights.Save();
      }
    }

    /// <summary>
    /// Выдать права на Последствия.
    /// </summary>
    public static void GrantRightsImpacts()
    {
      InitializationLogger.Debug("Init: Grant rights on impacts.");
      if (!PortfolioProgram.Impacts.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        PortfolioProgram.Impacts.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        PortfolioProgram.Impacts.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права на вычисляемую папку с рисками.
    /// </summary>
    public static void GrantRightsFolderRisksList()
    {
      InitializationLogger.Debug("Init: Grant rights on risk folder.");
      if (!ProjectPlanning.Module.Projects.SpecialFolders.RisksListDirRX.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Roles.AllUsers))
      {
        ProjectPlanning.Module.Projects.SpecialFolders.RisksListDirRX.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
        ProjectPlanning.Module.Projects.SpecialFolders.RisksListDirRX.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Создать дефолтные Категории для рисков.
    /// </summary>
    public static void CreateRiskCategories()
    {
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.ShipmentRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.PlanningRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.SecurityRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.FinancialRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.CommunicationRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.BusinessRisk);
      CreateRiskCategory(DirRX.PortfolioProgram.Resources.QualityRisk);
    }
    
    /// <summary>
    /// Создать дефолтные Вероятности.
    /// </summary>
    public static void CreateProbabilities()
    {
      CreateProbability(1, DirRX.PortfolioProgram.Resources.RiskWillNotOccur, Constants.Module.ProbabilityOccurInPercent.FivePercent);
      CreateProbability(2, DirRX.PortfolioProgram.Resources.RiskIsUnlikelyToOccur, Constants.Module.ProbabilityOccurInPercent.TenPercent);
      CreateProbability(3, DirRX.PortfolioProgram.Resources.ProbabilitiesOfRiskAreTheSame, Constants.Module.ProbabilityOccurInPercent.FiftyPercent);
      CreateProbability(4, DirRX.PortfolioProgram.Resources.RiskIsLikelyToOccur, Constants.Module.ProbabilityOccurInPercent.SeventyFivePercent);
      CreateProbability(5, DirRX.PortfolioProgram.Resources.RiskWillMostCertainlyOccur, Constants.Module.ProbabilityOccurInPercent.NinetyFivePercent);
    }
    
    /// <summary>
    /// Создать дефолтные Последствия.
    /// </summary>
    public static void CreateImpacts()
    {
      CreateImpact(1, DirRX.PortfolioProgram.Resources.ImpactDescriptionMinimal, DirRX.PortfolioProgram.Resources.ImpactContentChangeMinimal);
      CreateImpact(2, DirRX.PortfolioProgram.Resources.ImpactDescriptionAcceptable, DirRX.PortfolioProgram.Resources.ImpactContentChangeAcceptable);
      CreateImpact(3, DirRX.PortfolioProgram.Resources.ImpactDescriptionSignificant, DirRX.PortfolioProgram.Resources.ImpactContentChangeSignificant);
      CreateImpact(4, DirRX.PortfolioProgram.Resources.ImpactDescriptionCritical, DirRX.PortfolioProgram.Resources.ImpactContentChangeCritical);
      CreateImpact(5, DirRX.PortfolioProgram.Resources.ImpactDescriptionDisastrous, DirRX.PortfolioProgram.Resources.ImpactContentChangeDiasastrous);
    }
    
    /// <summary>
    /// Создать Категорию риска.
    /// </summary>
    /// <param name="name">Наименование.</param>
    /// <returns>Категория риска.</returns>
    public static PortfolioProgram.IRiskCategory CreateRiskCategory(string name)
    {
      InitializationLogger.Debug($"Init: Create risk category, name: {name}");
      
      var category = PortfolioProgram.RiskCategories.GetAll(c => c.Name == name).FirstOrDefault();
      if (category == null)
      {
        category = PortfolioProgram.RiskCategories.Create();
        category.Name = name;
        category.Save();
      }
      return category;
    }
    
    /// <summary>
    /// Создать Вероятность риска.
    /// </summary>
    /// <param name="index">Индекс.</param>
    /// <param name="description">Описание.</param>
    /// <param name="percentProbability">Вероятность в процентах.</param>
    /// <returns>Вероятность риска.</returns>
    public static PortfolioProgram.IProbability CreateProbability(int index, string description, int percentProbability)
    {
      InitializationLogger.Debug($"Init: Create probability, index: {index}.");
      
      var probability = PortfolioProgram.Probabilities.GetAll(p => p.Index == index).FirstOrDefault();
      if (probability == null)
      {
        probability = PortfolioProgram.Probabilities.Create();
        probability.Description = description;
        probability.Index = index;
        probability.PercentProbability = percentProbability;
        probability.Save();
      }
      return probability;
    }
    
    /// <summary>
    /// Создать Последствие риска.
    /// </summary>
    /// <param name="index">Индекс.</param>
    /// <param name="description">Описание.</param>
    /// <param name="contentChange">Изменение содержания.</param>
    /// <returns>Последствие риска.</returns>
    public static PortfolioProgram.IImpact CreateImpact(int index, string description, string contentChange)
    {
      InitializationLogger.Debug($"Init: Create impact, index: {index}.");
      
      var impact = PortfolioProgram.Impacts.GetAll(i => i.Index == index).FirstOrDefault();
      if (impact == null)
      {
        impact = PortfolioProgram.Impacts.Create();
        impact.Index = index;
        impact.Description = description;
        impact.ContentChange = contentChange;
        impact.Save();
      }
      return impact;
    }
  }
}
