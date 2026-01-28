using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning;

namespace DirRX.ProjectPlanner
{
  partial class ProgressReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ProgressReport.MilestonesTableName, ProgressReport.ReportSessionId);
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ProgressReport.OverdueStagesTableName, ProgressReport.ReportSessionId);
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ProgressReport.CompletedStagesTableName, ProgressReport.ReportSessionId);
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ProgressReport.CurrentStagesTableName, ProgressReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      IProjectDocument charter = null;
      var reportSessionId = System.Guid.NewGuid().ToString();
      var plan = ProgressReport.ProjectPlanRX;
      
      int versionNumber = plan.LastVersion.Number.Value;

      var activities = GetAllStages(plan, versionNumber);
      var project = DirRX.ProjectPlanner.Functions.ProjectPlanRX.GetLinkedProject(plan);
      var startDate = ProgressReport.StartDate;
      var currentDate = ProgressReport.CurrentDate;
      var endDate = ProgressReport.EndDate;
      var currencySymbol = DirRX.ProjectPlanning.PublicFunctions.Module.GetDefaultCurrencySymbolFromDb();
      
      #region Заполнение параметров отчета
      ProgressReport.ReportSessionId = reportSessionId;
      ProgressReport.ProjectPlanRXLink = Hyperlinks.Get(plan);
      ProgressReport.BudgetTitle = currencySymbol.Length == 0 ? 
        DirRX.ProjectPlanner.Reports.Resources.ProgressReport.ProjectCostsTitle :
        DirRX.ProjectPlanner.Reports.Resources.ProgressReport.ProjectCostsTitleCurrencyFormat(currencySymbol);
      ProgressReport.CountTable = Constants.ProgressReport.CountTableFirst;
      ProgressReport.HasAgileProject = false;

      FillPredictTable();
      FillActualTable();
      
      if(project != null)
      {
        ProgressReport.ProjectId = project.Id;
        var externalLink = Sungero.Docflow.PublicFunctions.Module.GetExternalLink(Sungero.Docflow.Server.DocumentKind.ClassTypeGuid,
                                                                                  Sungero.Projects.PublicConstants.Module.Initialize.RegulationsKind);
        
        if(externalLink != null)
        {
          var documentKindId = externalLink.EntityId;
          charter = ProjectDocuments.GetAll(x => x.Project == project && x.DocumentKind.Id == documentKindId).FirstOrDefault();
          
          if(charter != null)
          {
            ProgressReport.ProjectCharter = charter.Name;
            ProgressReport.ProjectCharterLink = Hyperlinks.Get(charter);
          }
        }
        
        if(project.Folder != null)
        {
          ProgressReport.ProjectFolder = project.Folder.Name;
          ProgressReport.ProjectFolderLink = Hyperlinks.Get(project.Folder);
        }
        
        if(ProgressReport.IsIncludeAgile == true)
        {
          using (var command = SQL.GetCurrentConnection().CreateCommand())
          {
            command.CommandText = Queries.ProgressReport.SelectBoardWithProject;
            SQL.AddParameter(command, "@projectId", project.Id, System.Data.DbType.Int64);
            var boardId = command.ExecuteScalar();
            ProgressReport.HasAgileProject = boardId != null;
          }
        }
      }
      #endregion
      
      #region Заполнение отчета по плану проекта
      var milestones = activities.Where(x => x.TypeActivity == ProjectActivity.TypeActivity.Milestone).Distinct().ToList();
      
      var stages = activities.Where(x => x.TypeActivity == ProjectActivity.TypeActivity.Task);
      var overdueStages = stages.Where(x => x.ExecutionPercent != 100)
        .Where(x => x.EndDate.HasValue && x.EndDate.Value.AddDays(-1) < currentDate)
        .Distinct().ToList();
      var completedStages = stages.Where(x => x.ExecutionPercent == 100)
        .Where(x => x.EndDate.HasValue && x.EndDate.Value.AddDays(-1) >= startDate)
        .Distinct().ToList();
      var currentStages = stages.Where(x => x.ExecutionPercent != 100)
        .Where(x => x.EndDate.HasValue && x.EndDate.Value.AddDays(-1) >= currentDate && x.StartDate <= endDate)
        .Distinct().ToList();

      var allStagesReport = milestones.Union(overdueStages).Union(completedStages).Union(currentStages);
      var allStagesReportWithWbs = FillWbs(allStagesReport, activities);

      this.FillTableMilestones(milestones, allStagesReportWithWbs, reportSessionId);
      this.FillTableOverdueStages(overdueStages, allStagesReportWithWbs,  reportSessionId);
      this.FillTableCompletedStages(completedStages, allStagesReportWithWbs, reportSessionId);
      this.FillTableCurrentStages(currentStages, allStagesReportWithWbs, reportSessionId);
      #endregion
    }
    
    private void FillPredictTable()
    {
      var plan = ProgressReport.ProjectPlanRX;
      var activities = GetAllStages(plan, plan.LastVersion.Number.Value).Where(x => x.TypeActivity == ProjectActivity.TypeActivity.Task);
      
      ProgressReport.WorkloadPredictTotal = activities.Sum(a => a.BaselineWork) ?? 0;
      ProgressReport.WorkloadApprovedTotal = plan.BaselineWork;
      ProgressReport.BudgetPredictTotal = activities.Sum(a => a.PlannedCosts) ?? 0;
      ProgressReport.BudgetApprovedTotal = plan.PlannedCosts;
      ProgressReport.DurationPredictTotal = Constants.ProgressReport.DurationFactFirst;

      if(plan.StartDate.HasValue && plan.EndDate.HasValue)
      {
        ProgressReport.DurationApprovedTotal = WorkingTime.GetDurationInWorkingDays(plan.StartDate.Value, plan.EndDate.Value);
      }
      
      var earlestActionDate = activities.OrderBy(a => a.StartDate).FirstOrDefault()?.StartDate;
      var latestActionDate = activities.OrderByDescending(a => a.EndDate).FirstOrDefault()?.EndDate;
      if(earlestActionDate.HasValue && latestActionDate.HasValue)
      {
        // -1 потому что дата - нули следующего дня.
        ProgressReport.DurationPredictTotal = WorkingTime.GetDurationInWorkingDays(earlestActionDate.Value, latestActionDate.Value.AddDays(-1));
      }
    }
    
    private void FillActualTable()
    {
      var plan = ProgressReport.ProjectPlanRX;
      var approvedVersionNumber = Functions.Module.GetActualProjectPlanVersionNumber(plan.Id);

      var activities = GetAllStages(plan, approvedVersionNumber).Where(x => x.TypeActivity == ProjectActivity.TypeActivity.Task && x.StartDate < Calendar.Today);
      
      ProgressReport.WorkloadFact = plan.ActualWorkload;
      ProgressReport.WorkloadApproved = activities.Sum(a => a.BaselineWork) ?? 0;
      ProgressReport.BudgetFact = plan.FactualCosts;
      ProgressReport.BudgetApproved = activities.Sum(a => a.PlannedCosts) ?? 0;
      ProgressReport.DurationFact = Constants.ProgressReport.DurationFactFirst;

      if(plan.StartDate.HasValue)
      {
        // по требованиям, сегодняшний день не считаем, только прошедшие
        ProgressReport.DurationApproved = WorkingTime.GetDurationInWorkingDays(plan.StartDate.Value, Calendar.Today.AddDays(-1));
      }
      
      if(plan.ActualStartDate.HasValue)
      {
        // по требованиям, сегодняшний день не считаем, только прошедшие
        ProgressReport.DurationFact = WorkingTime.GetDurationInWorkingDays(plan.ActualStartDate.Value, Calendar.Today.AddDays(-1));
      }
    }
    
    /// <summary>
    /// Заполнение вех плана.
    /// </summary>
    /// <param name="reportSessionId">ИД сессии.</param>
    private void FillTableMilestones(List<IProjectActivity> milestones, Dictionary<long, string> allStagesReportWithWbs, string reportSessionId)
    {
      var tableData = new List<Structures.ProgressReport.IMilestoneTableLine>();
      var startDate = ProgressReport.StartDate;
      var currentDate = ProgressReport.CurrentDate;
      var endDate = ProgressReport.EndDate;
      var plan = ProgressReport.ProjectPlanRX;
      var statusDone = Reports.Resources.ProgressReport.MilestoneStatusDone;
      var statusNotDone = Reports.Resources.ProgressReport.MilestoneStatusNotDone;
      
      var rowsMilestone = milestones.Select(m =>
                                            Structures.ProgressReport.MilestoneTableLine.Create(
                                              reportSessionId,
                                              m.Id,
                                              allStagesReportWithWbs[m.Id],
                                              m.Name,
                                              m.EndDate.Value,
                                              m.ExecutionPercent == 100 ? statusDone : statusNotDone,
                                              m.Responsible?.Name,
                                              m.NumberVersion.Value)
                                           )
        .Cast<Structures.ProgressReport.IMilestoneTableLine>();
      
      tableData.AddRange(rowsMilestone);
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ProgressReport.MilestonesTableName, tableData);
    }
    
    /// <summary>
    /// Заполнение просроченных этапов.
    /// </summary>
    /// <param name="overdueStages">Просроченные этапы.</param>
    /// <param name="allStagesReportWithWbs">Этапы с ИСР.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    private void FillTableOverdueStages(List<IProjectActivity> overdueStages, Dictionary<long, string> allStagesReportWithWbs,  string reportSessionId)
    {
      var tableData = new List<Structures.ProgressReport.IStageTableLine>();
      
      var rowsOverdueStage = overdueStages.Select(stage =>
                                                  Structures.ProgressReport.StageTableLine.Create(
                                                    reportSessionId,
                                                    stage.Id,
                                                    allStagesReportWithWbs[stage.Id],
                                                    stage.Name,
                                                    stage.StartDate.Value,
                                                    stage.EndDate.Value.AddDays(-1),
                                                    stage.ExecutionPercent.Value,
                                                    stage.Responsible?.Name,
                                                    stage.NumberVersion.Value)
                                                 )
        .Cast<Structures.ProgressReport.IStageTableLine>();
      
      tableData.AddRange(rowsOverdueStage);
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ProgressReport.OverdueStagesTableName, tableData);
    }
    
    /// <summary>
    /// Заполнение выполненных эапов.
    /// </summary>
    /// <param name="completedStages">Выполненные этапы.</param>
    /// <param name="allStagesReportWithWbs">Этапы с ИСР.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    private void FillTableCompletedStages(List<IProjectActivity> completedStages, Dictionary<long, string> allStagesReportWithWbs, string reportSessionId)
    {
      var tableData = new List<Structures.ProgressReport.IStageTableLine>();
      
      var rowsCompletedStage = completedStages.Select(stage =>
                                                      Structures.ProgressReport.StageTableLine.Create(
                                                        reportSessionId,
                                                        stage.Id,
                                                        allStagesReportWithWbs[stage.Id],
                                                        stage.Name,
                                                        stage.StartDate.Value,
                                                        stage.EndDate.Value.AddDays(-1),
                                                        stage.ExecutionPercent.Value,
                                                        stage.Responsible?.Name,
                                                        stage.NumberVersion.Value)
                                                     )
        .Cast<Structures.ProgressReport.IStageTableLine>();
      
      tableData.AddRange(rowsCompletedStage);
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ProgressReport.CompletedStagesTableName, tableData);
    }
    
    /// <summary>
    /// Заполнение текущих и ближайших этапов.
    /// </summary>
    /// <param name="currentStages">Текущие и ближайшие этапы.</param>
    /// <param name="allStagesReportWithWbs">Этапы с ИСР.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    private void FillTableCurrentStages(List<IProjectActivity> currentStages, Dictionary<long, string> allStagesReportWithWbs, string reportSessionId)
    {
      var tableData = new List<Structures.ProgressReport.IStageTableLine>();
      
      var rowsCurrentStage = currentStages.Select(stage =>
                                                  Structures.ProgressReport.StageTableLine.Create(
                                                    reportSessionId,
                                                    stage.Id,
                                                    allStagesReportWithWbs[stage.Id],
                                                    stage.Name,
                                                    stage.StartDate.Value,
                                                    stage.EndDate.Value.AddDays(-1),
                                                    stage.ExecutionPercent.Value,
                                                    stage.Responsible?.Name,
                                                    stage.NumberVersion.Value)
                                                 )
        .Cast<Structures.ProgressReport.IStageTableLine>();
      
      tableData.AddRange(rowsCurrentStage);

      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ProgressReport.CurrentStagesTableName, tableData);
    }
    
    /// <summary>
    /// Заполнение ИСР у этапов.
    /// </summary>
    /// <param name="activitiesInReport">Этапы.</param>
    /// <param name="plan">План.</param>
    /// <returns>Словарь ИД этапов с ИСР.</returns>
    private static Dictionary<long, string> FillWbs(IEnumerable<IProjectActivity> activitiesInReport, IQueryable<IProjectActivity> allActivitiesQuery)
    {
      Stack<long> currentIsr = new Stack<long>();
      var activitiesResult = new Dictionary<long, string>();
      IProjectActivity lastParent = null;
      var listParent = new List<IProjectActivity>();
      
      if(!activitiesInReport.Any())
      {
        return activitiesResult;
      }
      
      var allActivities = allActivitiesQuery.OrderBy(x => x.SortIndex).Distinct().ToList();
      var firstActivity = allActivities.First();
      listParent.Add(firstActivity.LeadingActivity);
      currentIsr.Push(1);
      
      activitiesResult.Add(firstActivity.Id, CastWbs(currentIsr));
      
      for(int i = 1; i < allActivities.Count; i++)
      {

        if (listParent.Contains(allActivities[i].LeadingActivity))
        {
          var indexLastParent = listParent.IndexOf(lastParent);
          var indexCurrentParent = listParent.IndexOf(allActivities[i].LeadingActivity);
          
          for(int j = 0; j < indexLastParent - indexCurrentParent; j++)
          {
            currentIsr.Pop();
            listParent.Remove(listParent.Last());
          }
          
          lastParent = allActivities[i].LeadingActivity;
        }
        
        if (allActivities[i].LeadingActivity != lastParent && !listParent.Contains(allActivities[i].LeadingActivity))
        {
          lastParent = allActivities[i].LeadingActivity;
          listParent.Add(lastParent);
          currentIsr.Push(0);
        }

        currentIsr.Push(currentIsr.Pop() + 1);
        
        if(activitiesInReport.Contains(allActivities[i]))
        {
          activitiesResult.Add(allActivities[i].Id, CastWbs(currentIsr));
        }
      }
      
      return activitiesResult;
    }
    
    /// <summary>
    /// Привести ИСР в стэке к строке.
    /// </summary>
    /// <param name="s">Стек.</param>
    /// <returns>ИСР.</returns>
    private static string CastWbs(Stack<long> s)
    {
      return String.Join(".", s.Reverse());
    }
    
    /// <summary>
    /// Получить все активити в плане по определенной версии.
    /// </summary>
    /// <param name="plan">План проекта.</param>
    /// <param name="versionNumber">Версия документа.</param>
    /// <returns>Активити.</returns>
    private static IQueryable<IProjectActivity> GetAllStages(IProjectPlanRX plan, int versionNumber)
    {
      return ProjectActivities.GetAll(x => x.ProjectPlan == plan && x.NumberVersion.Value == versionNumber);
    }

  }
}
