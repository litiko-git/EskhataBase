using System;
using System.Linq;
using System.Collections.Generic;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Module.Projects.Server
{
  partial class ProjectsDirRXFolderHandlers
  {

    public virtual IQueryable<DirRX.ProjectPlanning.IProject> ProjectsDirRXDataQuery(IQueryable<DirRX.ProjectPlanning.IProject> query)
    {
      if(_filter == null)
        return query;
      
      query = DirRX.ProjectPlanning.PublicFunctions.Module.BaseFilter(query,
                                                                      _filter.Active,
                                                                      _filter.Closed,
                                                                      _filter.Closing,
                                                                      _filter.Initiation,
                                                                      _filter.PlanningDirRX,
                                                                      _filter.ProjectManager,
                                                                      _filter.LeadingProject,
                                                                      _filter.InternalCustomer,
                                                                      _filter.ExternalCustomer,
                                                                      _filter.StartDateRangeFrom,
                                                                      _filter.StartDateRangeTo,
                                                                      _filter.FinishDateRangeFrom,
                                                                      _filter.FinishDateRangeTo,
                                                                      _filter.NeedAssistance,
                                                                      _filter.UnderControl,
                                                                      _filter.Me,
                                                                      _filter.MySubordinates,
                                                                      _filter.EmployeeSelect
                                                                     ).Cast<IProject>();
      if(_filter.ProjectKind != null)
      {
        query = DirRX.ProjectPlanning.PublicFunctions.Module.FilterProjectByKind(query, _filter.ProjectKind).Cast<IProject>();
      }
      
      return query;
    }
  }

  partial class ProjectCoreDirRXFolderHandlers
  {

    public virtual IQueryable<DirRX.ProjectPlanning.IProjectCore> ProjectCoreDirRXDataQuery(IQueryable<DirRX.ProjectPlanning.IProjectCore> query)
    {
      if(_filter == null)
        return query;
      
      query = DirRX.ProjectPlanning.PublicFunctions.Module.BaseFilter(query,
                                                                      _filter.Active,
                                                                      _filter.Closed,
                                                                      _filter.Closing,
                                                                      _filter.Initiation,
                                                                      _filter.PlanningDirRX,
                                                                      _filter.ProjectManager,
                                                                      _filter.LeadingProject,
                                                                      _filter.InternalCustomer,
                                                                      _filter.ExternalCustomer,
                                                                      _filter.StartDateRangeFrom,
                                                                      _filter.StartDateRangeTo,
                                                                      _filter.FinishDateRangeFrom,
                                                                      _filter.FinishDateRangeTo,
                                                                      _filter.NeedAssistance,
                                                                      _filter.UnderControl,
                                                                      _filter.Me,
                                                                      _filter.MySubordinates,
                                                                      _filter.EmployeeSelect
                                                                     );
      
      query = DirRX.ProjectPlanning.PublicFunctions.Module.FilterProjectCoreByType(query, _filter.Project, _filter.Program, _filter.Portfolio);
      
      return query;
    }
  }

  partial class RisksListDirRXFolderHandlers
  {

    public virtual IQueryable<DirRX.PortfolioProgram.IRiskTemplate> RisksListDirRXDataQuery(IQueryable<DirRX.PortfolioProgram.IRiskTemplate> query)
    {
      return query;
    }
  }



  partial class ProjectTasksSungeroFolderHandlers
  {

    public virtual IQueryable<DirRX.ProjectPlanner.IProjectActivityTask> ProjectTasksSungeroDataQuery(IQueryable<DirRX.ProjectPlanner.IProjectActivityTask> query)
    {
      
      if(_filter == null)
        return query;
      
      var tasks = query.Where(t => t.MainTaskId.HasValue && t.Id == t.MainTaskId.Value);
      var currentEmployee = Sungero.Company.Employees.Current;
      IQueryable<DirRX.ProjectPlanner.IAssignment> assignments = null;
      
      if(_filter.ProjectSungero != null)
      {
        if (_filter.ChildProjectTasksFlagDirRX)
        {
          var childProjectIds = Functions.Module.GetAllChildProjectIds(_filter.ProjectSungero.Id);
          
          var childProjectPlans = DirRX.ProjectPlanning.Projects.GetAll(p => childProjectIds.Contains(p.Id))
                                                                .Select(x => x.ProjectPlanDirRX);
          
          tasks = tasks.Where(x => Equals(x.ProjectPlan, _filter.ProjectSungero.ProjectPlanDirRX) ||
                              childProjectPlans.Contains(x.ProjectPlan));
        }
        else
        {
          tasks = tasks.Where(x => Equals(x.ProjectPlan, _filter.ProjectSungero.ProjectPlanDirRX));
        }
      }

      #region Исполнитель
      if (currentEmployee == null && (_filter.MeSungero || _filter.MySubordinatesSungero))
      {
        // HACK Balezin_AA: Платформа не дает вернуть пустой IQueryable, поэтому возвращаю
        // набор пустых данных таким способом
        return query.Where(x => false);
      }

      if(_filter.MeSungero)
      {
        assignments = DirRX.ProjectPlanner.Assignments.GetAll(x => x.Performer.Id == currentEmployee.Id);
      }
      else if(_filter.MySubordinatesSungero)
      {
        var subordinates = Functions.Module.GetSubordinateEmployees();
        assignments = DirRX.ProjectPlanner.Assignments.GetAll(x => subordinates.Contains(x.Performer.Id));
      }
      else if(_filter.SelectAnotherSungero)
      {
        assignments = DirRX.ProjectPlanner.Assignments.GetAll(x => x.Performer == _filter.EmployeeSungero);
      }

      if(assignments != null)
      {
        tasks = tasks.Where(x => assignments.Select(a => a.Task.Id).Contains(x.Id));
      }
      #endregion
      
      #region Состояние
      if(_filter.InWorkSungero)
      {
        tasks = tasks.Where(x => x.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.InProcess);
      }
      else if(_filter.ExpiredSungero)
      {
        //фильтрация по статусу
        tasks = tasks.Where(x => x.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.InProcess ||
                            x.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.UnderReview);
        //фильтрация по дате
        tasks = tasks.Where(x => x.MaxDeadline.HasValue && (x.MaxDeadline.Value.HasTime() ? x.MaxDeadline < Calendar.Now : x.MaxDeadline < Calendar.UserToday));
      }
      #endregion
      
      #region Период
      if(_filter.AllSungero)
      {
        if(_filter.Days30Sungero)
        {
          tasks = tasks.Where(x => x.Started >= Calendar.Today.AddDays(-30));
        }
        else if(_filter.Days90Sungero)
        {
          tasks = tasks.Where(x => x.Started >= Calendar.Today.AddDays(-90));
        }
        else if(_filter.Days180Sungero)
        {
          tasks = tasks.Where(x => x.Started >= Calendar.Today.AddDays(-180));
        }
      }
      #endregion
      
      return tasks;
    }
  }

  partial class ProjectsPlansDirRXFolderHandlers
  {

    public virtual IQueryable<DirRX.ProjectPlanner.IProjectPlanRX> ProjectsPlansDirRXDataQuery(IQueryable<DirRX.ProjectPlanner.IProjectPlanRX> query)
    {
      if (_filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (_filter.Active || _filter.DraftDirRX || _filter.ObsoleteDirRX)
        query = query.Where(x => (_filter.Active && x.LifeCycleState == DirRX.ProjectPlanner.ProjectPlanRX.LifeCycleState.Active) ||
                            (_filter.DraftDirRX && x.LifeCycleState == DirRX.ProjectPlanner.ProjectPlanRX.LifeCycleState.Draft) ||
                            (_filter.ObsoleteDirRX && x.LifeCycleState == DirRX.ProjectPlanner.ProjectPlanRX.LifeCycleState.Obsolete));

      var today = Calendar.UserToday;
      
      // Фильтр по дате начала проекта.
      var startDateBeginPeriod = _filter.StartDateRangeFrom ?? Calendar.SqlMinValue;
      var startDateEndPeriod = _filter.StartDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.StartPeriodThisMonth)
      {
        startDateBeginPeriod = today.BeginningOfMonth();
        startDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.StartPeriodThisMonth || (_filter.StartDateRangeFrom != null || _filter.StartDateRangeTo != null))
        query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Status, DirRX.ProjectPlanner.ProjectPlanRX.Status.Closed)) ||
                            (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Status, DirRX.ProjectPlanner.ProjectPlanRX.Status.Closed)));

      // Фильтр по дате окончания проекта.
      var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
      var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.FinishPeriodThisMonth)
      {
        finishDateBeginPeriod = today.BeginningOfMonth();
        finishDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
        query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Status, DirRX.ProjectPlanner.ProjectPlanRX.Status.Closed)) ||
                            (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Status, DirRX.ProjectPlanner.ProjectPlanRX.Status.Closed)));
      
      return query;
    }
  }

  partial class ProjectsHandlers
  {
  }
}
