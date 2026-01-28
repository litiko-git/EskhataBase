using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram;
using DirRX.ProjectPlanning.ProjectCore;

namespace DirRX.ProjectPlanning.Server
{
  partial class ProjectCoreFunctions
  {
    
    /// <summary>
    /// Получить модель контрола состояния дочерних объектов.
    /// </summary>
    /// <returns>Модель контрола состояния дочерних объектов.</returns>
    [Remote]
    public virtual StateView GetProjectCoreState()
    {
      var stateView = StateView.Create();
      stateView.AddDefaultLabel(ProjectCores.Resources.CompositionDefaultText);
      
      if(_obj.State.IsInserted)
      {
        return stateView;
      }
      
      var blockLabelStyle = StateBlockLabelStyle.Create();
      blockLabelStyle.FontWeight = Sungero.Core.FontWeight.SemiBold;
      
      var subProjectCoreIds = DirRX.ProjectPlanning.Module.Projects.PublicFunctions.Module.GetAllChildProjectIds(_obj.Id);
      var subProjectCores = DirRX.ProjectPlanning.ProjectCores.GetAll(x => subProjectCoreIds.Contains(x.Id)).ToList();
      var projectPlanIds = subProjectCores
        .Where(p => Projects.Is(p) && Projects.As(p).ProjectPlanDirRX != null)
        .Select(p => new { ProjectId = p.Id, PlanId = Projects.As(p).ProjectPlanDirRX.Id })
        .ToList();
      var data = ProjectPlanner.PublicFunctions.ProjectPlanRX.GetCalculatedPlanDataCached(projectPlanIds.Select(p => p.PlanId));
      var projectDataDictionary = projectPlanIds.ToDictionary(p => p.ProjectId, p => data.First(d => d.Key == p.PlanId).Value);
      
      var currencySymbol = DirRX.ProjectPlanning.PublicFunctions.Module.GetDefaultCurrencySymbolFromDb();
      
      // заполнение ведущего блока
      var rootBlock = stateView.AddBlock();
      var currentCalculatedData = BuildStateView(_obj, subProjectCores, blockLabelStyle, rootBlock, currencySymbol, projectDataDictionary);
      CustomizeBlock(rootBlock, _obj, blockLabelStyle, currentCalculatedData, currencySymbol, projectDataDictionary);
      rootBlock.Background = Sungero.Core.Colors.Common.LightGray;
      rootBlock.IsExpanded = true;
      
      return stateView;
    }
    
    /// <summary>
    /// Заполнить модель состояния.
    /// </summary>
    /// <param name="currentProjectCore">Объект управления.</param>
    /// <param name="subProjectCores">Дочерние объекты управления.</param>
    /// <param name="blockLabelStyle">Стиль для лейбла блока.</param>
    /// <param name="rootBlock">Ведущий блок.</param>
    /// <param name="currencySymbol">Символ валюты.</param>
    /// <param name="projectDataDictionary">Высчитанные данные для проекта с плана.</param>
    /// <returns>Данные построенного блока.</returns>
    private static Structures.Projects.ProjectCore.CalculatedData BuildStateView(
      IProjectCore currentProjectCore,
      IEnumerable<IProjectCore> subProjectCores,
      StateBlockLabelStyle blockLabelStyle,
      StateBlock rootBlock,
      string currencySymbol,
      Dictionary<long, ProjectPlanner.Structures.ProjectPlanRX.IPlanCalculatedData> projectDataDictionary
     )
    {
      var comparer = new PriorityComparer();
      var childProjectCores = subProjectCores.Where(x => x.LeadingProject == currentProjectCore).OrderByDescending(x => x.Priority, comparer).ThenBy(x => x.StartDate);
      var currentCalculatedData = Structures.Projects.ProjectCore.CalculatedData.Create();
      
      foreach(var childProjectCore in childProjectCores)
      {
        var block = rootBlock.AddChildBlock();
        var childCalculatedData = BuildStateView(childProjectCore, subProjectCores, blockLabelStyle, block, currencySymbol, projectDataDictionary);
        CustomizeBlock(block, childProjectCore, blockLabelStyle, childCalculatedData, currencySymbol, projectDataDictionary);
        AddData(currentCalculatedData, childCalculatedData);
      }
      
      AddData(currentCalculatedData, currentProjectCore, projectDataDictionary);
      
      return currentCalculatedData;
    }
    
    /// <summary>
    /// Заполнение блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="projectCore">Объект управления.</param>
    /// <param name="blockLabelStyle">Стиль для лейбла блока.</param>
    /// <param name="calculatedData">Данные для блока.</param>
    /// <param name="currencySymbol">Символ валюты.</param>
    private static void CustomizeBlock(
      StateBlock block,
      IProjectCore projectCore,
      StateBlockLabelStyle blockLabelStyle,
      Structures.Projects.ProjectCore.CalculatedData calculatedData,
      string currencySymbol,
      Dictionary<long, ProjectPlanner.Structures.ProjectPlanRX.IPlanCalculatedData> data)
    {
      #region Заполнение основной колонки
      block.Entity = projectCore;
      var icon = GetIcon(projectCore);
      
      if(icon == null)
      {
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      }
      else
      {
        block.AssignIcon(icon, StateBlockIconSize.Large);
      }
      
      block.AddLabel(projectCore.Name, blockLabelStyle);
      block.AddLineBreak();
      block.AddLabel(ProjectCores.Resources.ManagerName);
      block.AddLineBreak();
      block.AddLabel(projectCore.Manager.DisplayValue);
      #endregion
      
      #region Заполнение колонки с датами
      var date = block.AddContent();

      if(Projects.Is(projectCore))
      {
        var project = Projects.As(projectCore);
        ProjectPlanner.Structures.ProjectPlanRX.IPlanCalculatedData projectData;
        data.TryGetValue(project.Id, out projectData);
        var planStartDate = project.StartDate;
        var planEndDate = project.EndDate;

        if (projectData != null)
        {
          planStartDate = projectData.PlanStartDate;
          planEndDate = projectData.PlanEndDate;
        }
        
        date.AddLabel(planStartDate != null ? planStartDate.Value.ToShortDateString() : "—");
        date.AddLineBreak();
        date.AddLabel(planEndDate != null ? planEndDate.Value.ToShortDateString() : "—");
      }
      else if (Programs.Is(projectCore))
      {
        var program = Programs.As(projectCore);
        
        date.AddLabel(program.StartDate != null ? program.StartDate.Value.ToShortDateString() : "—");
        date.AddLineBreak();
        date.AddLabel(program.EndDate != null ? program.EndDate.Value.ToShortDateString() : "—");
      }
      else
      {
        date.AddLineBreak();
      }
      #endregion
      
      #region Заполнение колонки с затратами
      var cost = block.AddContent();
      var costsTextStructure = GetCostWorkloadText(projectCore, calculatedData, currencySymbol);
      
      if (Programs.Is(projectCore))
      {
        cost.AddLabel(costsTextStructure.CostLimit);
        cost.AddLineBreak();
      }
      
      cost.AddLabel(costsTextStructure.CostPlan);
      cost.AddLineBreak();
      cost.AddLabel(costsTextStructure.CostFact);
      #endregion
      
      #region Заполнение колонки с трудоемкостью
      var workload = block.AddContent();
      
      if (Programs.Is(projectCore))
      {
        workload.AddLabel(costsTextStructure.WorkloadLimit);
        workload.AddLineBreak();
      }
      
      workload.AddLabel(costsTextStructure.WorkloadPlan);
      workload.AddLineBreak();
      workload.AddLabel(costsTextStructure.WorkloadFact);
      #endregion
      
      #region Заполнение колонки с приоритетом
      var priority = block.AddContent();
      var priorityPropertyInfo = ProjectCores.Info.Properties.Priority;
      var priorityLocalizedValue = projectCore.Priority != null ?
        priorityPropertyInfo.GetLocalizedValue(projectCore.Priority) :
        "—";
      priority.AddLabel(ProjectCores.Resources.PriorityNameFormat(priorityLocalizedValue));
      #endregion
      
      #region Заполнение колонки со статусом
      var stage = block.AddContent();
      var stageLocalizedValue = ProjectCores.Info.Properties.Stage.GetLocalizedValue(projectCore.Stage);
      
      if(DirRX.PortfolioProgram.Portfolios.Is(projectCore))
      {
        stage.AddLabel(stageLocalizedValue);
      }
      else
      {
        stage.AddLabel(ProjectCores.Resources.StagePercentageFormat(stageLocalizedValue, GetExecutionPercent(calculatedData)));
      }
      #endregion
    }
    
    /// <summary>
    /// Получить структуру с затратами и трудоемкостью текстом.
    /// </summary>
    /// <param name="projectCore">Объект управления.</param>
    /// <param name="calculatedData">Данные для блока.</param>
    /// <param name="currencySymbol">Символ валюты.</param>
    /// <returns>Структура с затратами и трудоемкостью текстовая.</returns>
    private static DirRX.ProjectPlanning.Structures.Projects.ProjectCore.CostWorkloadText GetCostWorkloadText(
      IProjectCore projectCore,
      Structures.Projects.ProjectCore.CalculatedData calculatedData,
      string currencySymbol)
    {
      var сostsTextStructure = Structures.Projects.ProjectCore.CostWorkloadText.Create();
      
      if (Programs.Is(projectCore))
      {
        var program = Programs.As(projectCore);
        var costLimitText = program.CostLimit != null ? string.Format("{0} {1}", program.CostLimit, currencySymbol) : "—";
        var workloadLimitText = program.WorkloadLimit != null ? ProjectCores.Resources.HourFormat(program.WorkloadLimit) : "—";
        сostsTextStructure.CostLimit = ProjectCores.Resources.LimitFormat(costLimitText);
        сostsTextStructure.WorkloadLimit = ProjectCores.Resources.LimitFormat(workloadLimitText);
      }
      
      сostsTextStructure.CostPlan = ProjectCores.Resources.CostPlanMoneyFormat(calculatedData.PlannedCosts, currencySymbol);
      сostsTextStructure.CostFact = ProjectCores.Resources.CostFactMoneyFormat(calculatedData.FactualCosts, currencySymbol);
      
      сostsTextStructure.WorkloadPlan = ProjectCores.Resources.CostPlanHourFormat(calculatedData.PlannedWorkload);
      сostsTextStructure.WorkloadFact = ProjectCores.Resources.CostFactHourFormat(calculatedData.FactualWorkload);
      
      return сostsTextStructure;
    }
    
    /// <summary>
    /// Получить иконку для отображения.
    /// </summary>
    /// <param name="projectCore">Проект.</param>
    /// <returns>Иконка.</returns>
    private static string GetIcon(IProjectCore projectCore)
    {
      // TODO: отрефакторить
      string icon = null;
      
      if(DirRX.PortfolioProgram.Portfolios.Is(projectCore))
      {
        if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues)
        {
          icon = DirRX.PortfolioProgram.Portfolios.Resources.PortfolioGreenColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance)
        {
          icon = DirRX.PortfolioProgram.Portfolios.Resources.PortfolioRedColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl)
        {
          icon = DirRX.PortfolioProgram.Portfolios.Resources.PortfolioYellowColor;
        }
      }
      else if(DirRX.PortfolioProgram.Programs.Is(projectCore))
      {
        if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues)
        {
          icon = DirRX.PortfolioProgram.Programs.Resources.ProgramGreenColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance)
        {
          icon = DirRX.PortfolioProgram.Programs.Resources.ProgramRedColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl)
        {
          icon = DirRX.PortfolioProgram.Programs.Resources.ProgramYellowColor;
        }
      }
      else if(DirRX.ProjectPlanning.Projects.Is(projectCore))
      {
        if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues)
        {
          icon = DirRX.ProjectPlanning.Projects.Resources.ProjectGreenColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance)
        {
          icon = DirRX.ProjectPlanning.Projects.Resources.ProjectRedColor;
        }
        else if(projectCore.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl)
        {
          icon =  DirRX.ProjectPlanning.Projects.Resources.ProjectYellowColor;
        }
      }
      
      return icon;
    }
    
    /// <summary>
    /// Добавить данные в текущую структуру из дочерней структуры.
    /// </summary>
    /// <param name="currentCalculatedData">Текущая структура с данными.</param>
    /// <param name="childCalculatedData">Дочерняя структура с данными.</param>
    /// <returns></returns>
    private static void AddData(
      Structures.Projects.ProjectCore.CalculatedData currentCalculatedData,
      Structures.Projects.ProjectCore.CalculatedData childCalculatedData)
    {
      currentCalculatedData.PlannedCosts += childCalculatedData.PlannedCosts;
      currentCalculatedData.FactualCosts += childCalculatedData.FactualCosts;
      
      currentCalculatedData.PlannedWorkload += childCalculatedData.PlannedWorkload;
      currentCalculatedData.FactualWorkload += childCalculatedData.FactualWorkload;
      
      currentCalculatedData.ExecutionPercentSum += GetExecutionPercent(childCalculatedData);
      currentCalculatedData.RecordsCount++;
    }
    
    /// <summary>
    /// Добавить данные в текущую структуру из объекта управления.
    /// </summary>
    /// <param name="currentCalculatedData">Текущая структура с данными.</param>
    /// <param name="projectCore">Объект управления.</param>
    /// <param name="data">Высчитанные данные для проекта с плана.</param>
    private static void AddData(
      Structures.Projects.ProjectCore.CalculatedData currentCalculatedData,
      IProjectCore projectCore,
      Dictionary<long, ProjectPlanner.Structures.ProjectPlanRX.IPlanCalculatedData> data)
    {
      
      if (!Projects.Is(projectCore))
      {
        return;
      }
      
      var project = Projects.As(projectCore);
      currentCalculatedData.RecordsCount++;
      
      if (data.ContainsKey(project.Id))
      {
        var projectData = data[projectCore.Id];
        currentCalculatedData.PlannedCosts += projectData.PlanCosts ?? 0;
        currentCalculatedData.FactualCosts += projectData.FactCosts ?? 0;
        currentCalculatedData.PlannedWorkload += projectData.PlanWorkload ?? 0;
        currentCalculatedData.FactualWorkload += projectData.FactWorkload ?? 0;
        currentCalculatedData.ExecutionPercentSum += projectData.ExecutionPercent ?? 0;
      }
      else
      {
        currentCalculatedData.PlannedCosts += project.PlannedCosts ?? 0;
        currentCalculatedData.FactualCosts += project.FactualCosts ?? 0;
        currentCalculatedData.PlannedWorkload += project.PlannedWorkloadDirRX ?? 0;
        currentCalculatedData.FactualWorkload += project.ActualWorkloadDirRX ?? 0;
        currentCalculatedData.ExecutionPercentSum += project.ExecutionPercent ?? 0;
      }
    }
    
    /// <summary>
    /// Получить процент выполнения.
    /// </summary>
    /// <param name="calculatedData">Структура с данными.</param>
    /// <returns>Процент выполнения.</returns>
    private static double GetExecutionPercent(Structures.Projects.ProjectCore.CalculatedData calculatedData)
    {
      return calculatedData.RecordsCount > 0 ? (int)(calculatedData.ExecutionPercentSum / calculatedData.RecordsCount) : 0;
    }

    
  }

  class PriorityComparer : IComparer<Nullable<Enumeration>>
  {
    public int Compare(Enumeration? first, Enumeration? second)
    {
      Enumeration low = DirRX.ProjectPlanning.ProjectCore.Priority.Low;
      Enumeration medium = DirRX.ProjectPlanning.ProjectCore.Priority.Medium;
      Enumeration high = DirRX.ProjectPlanning.ProjectCore.Priority.High;
      
      if (first == null || first == low || (first == medium && second == high))
      {
        return -1;
      }
      else if (second == null || second == low || (second == medium && first == high))
      {
        return 1;
      }
      
      return 0;
    }
  }
}