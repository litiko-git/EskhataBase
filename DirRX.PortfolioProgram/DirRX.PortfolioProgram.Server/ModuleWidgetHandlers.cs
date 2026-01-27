using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PortfolioProgram.Server
{
  partial class HealthWidgetWidgetHandlers
  {

    public virtual void GetHealthWidgetChartValue(Sungero.Domain.GetWidgetPieChartValueEventArgs e)
    {
      var query = DirRX.ProjectPlanning.ProjectCores.GetAll();
      
      var valueId = Constants.Module.HealthValueId.NoIssues;
      var count = GetProjects(query, valueId).Count();
      if (count > 0)
        e.Chart.AddValue(valueId, GetDisplayValue(valueId), count, Colors.Charts.Green);
      
      valueId = Constants.Module.HealthValueId.NeedAssistance;
      count = GetProjects(query, valueId).Count();
      if (count > 0)
        e.Chart.AddValue(valueId, GetDisplayValue(valueId), count, Colors.Charts.Red);
      
      valueId = Constants.Module.HealthValueId.UnderControl;
      count = GetProjects(query, valueId).Count();
      if (count > 0)
        e.Chart.AddValue(valueId, GetDisplayValue(valueId), count, Colors.Charts.Yellow);
      
      valueId = Constants.Module.HealthValueId.Other;
      count = GetProjects(query, valueId).Count();
      if (count > 0)
        e.Chart.AddValue(valueId, GetDisplayValue(valueId), count, Sungero.Core.Colors.Common.Gray);
    }

    public virtual IQueryable<DirRX.ProjectPlanning.IProjectCore> HealthWidgetChartFiltering(IQueryable<DirRX.ProjectPlanning.IProjectCore> query, Sungero.Domain.WidgetPieChartFilteringEventArgs e)
    {
      return GetProjects(query, e.ValueId);
    }
    
    /// <summary>
    /// Получить отфильтрованный список проектов.
    /// </summary>
    /// <param name="query">Список проектов.</param>
    /// <param name="valueId">ИД значения серии.</param>
    /// <returns>Список проектов.</returns>
    public IQueryable<DirRX.ProjectPlanning.IProjectCore> GetProjects(IQueryable<DirRX.ProjectPlanning.IProjectCore> query, string valueId)
    {
      if (_parameters.ProjectType == Widgets.HealthWidget.ProjectType.Project)
        query = query.Where(x => ProjectPlanning.Projects.Is(x));
      else if (_parameters.ProjectType == Widgets.HealthWidget.ProjectType.Portfolio)
        query = query.Where(x => Portfolios.Is(x));
      else if (_parameters.ProjectType == Widgets.HealthWidget.ProjectType.Program)
        query = query.Where(x => Programs.Is(x));
      
      if (valueId == Constants.Module.HealthValueId.NoIssues)
        query = query.Where(x => x.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues);
      else if (valueId == Constants.Module.HealthValueId.NeedAssistance)
        query = query.Where(x => x.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance);
      else if (valueId == Constants.Module.HealthValueId.UnderControl)
        query = query.Where(x => x.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl);
      else if (valueId == Constants.Module.HealthValueId.Other)
        query = query.Where(x => x.StatusIssues != DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues
                            && x.StatusIssues != DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance
                            && x.StatusIssues != DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl);
      return query;
    }
    
    /// <summary>
    /// Получить локализованное значение серии.
    /// </summary>
    /// <param name="valueId">ИД значения серии.</param>
    /// <returns>Локализованное значение серии.</returns>
    public string GetDisplayValue(string valueId)
    {
      Enumeration? status = null;
      if (valueId == Constants.Module.HealthValueId.NoIssues)
        status = DirRX.ProjectPlanning.ProjectCore.StatusIssues.NoIssues;
      else if (valueId == Constants.Module.HealthValueId.NeedAssistance)
        status = DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance;
      else if (valueId == Constants.Module.HealthValueId.UnderControl)
        status = DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl;
      
      if (status != null) 
        return DirRX.ProjectPlanning.ProjectCores.Info.Properties.StatusIssues.GetLocalizedValue(status);
      return DirRX.PortfolioProgram.Resources.Undefined;
    }
  }


}