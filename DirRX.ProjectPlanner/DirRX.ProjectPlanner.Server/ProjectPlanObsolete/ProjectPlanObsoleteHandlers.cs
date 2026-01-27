using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanObsolete;

namespace DirRX.ProjectPlanner
{

  partial class ProjectPlanObsoleteFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (_filter.Active || _filter.Closed || _filter.Closing || _filter.Initiation)
        query = query.Where(x => (_filter.Active && x.Stage == Stage.Execution) ||
                            (_filter.Closed && x.Stage == Stage.Completed) ||
                            (_filter.Closing && x.Stage == Stage.Completion) ||
                            (_filter.Initiation && x.Stage == Stage.Initiation));

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
        query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
                            (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Stage, Stage.Completed)));

      // Фильтр по дате окончания проекта.
      var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
      var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.FinishPeriodThisMonth)
      {
        finishDateBeginPeriod = today.BeginningOfMonth();
        finishDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
        query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
                            (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Stage, Stage.Completed)));
      
      return query;
    }
  }

  partial class ProjectPlanObsoleteCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ActualStartDate);
      e.Without(_info.Properties.ActualFinishDate);
      e.Without(_info.Properties.ExecutionPercent);
      e.Without(_info.Properties.Note);
      e.Params.Add("IsCopy", true);
    }
  }

  partial class ProjectPlanObsoleteServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Stage = Stage.Initiation;
      
      _obj.Modified = Calendar.Now;
      
      _obj.BaselineWorkType = ProjectPlanObsolete.BaselineWorkType.Hours;
      
      _obj.StartDate = Calendar.Now;
      
      _obj.DocumentKind = Sungero.Docflow.DocumentKinds.GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString()).First();
      
      var isCopy = false;
      e.Params.TryGetValue("IsCopy", out isCopy);
      _obj.IsCopy = isCopy;
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(_obj, x.ProjectPlanDirRX)).FirstOrDefault();
      
      var recipient = linkedProject != null ? linkedProject.Administrator : _obj.Author;
      // Изъять права на этапы проекта.
      ProjectPlanner.ProjectActivities.AccessRights.RevokeAll(recipient);
      ProjectPlanner.ProjectActivities.AccessRights.Save();
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {

    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
    }
  }

}