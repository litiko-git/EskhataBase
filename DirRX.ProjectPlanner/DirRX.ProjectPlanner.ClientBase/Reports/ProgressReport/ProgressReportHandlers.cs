using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace DirRX.ProjectPlanner
{
  partial class ProgressReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(Reports.Resources.ProgressReport.DialogName);
      var periodControl = dialog.AddSelect(Reports.Resources.ProgressReport.ReportPeriodDialogName, true, Reports.Resources.ProgressReport.TwoWeeks)
        .From(Reports.Resources.ProgressReport.EntirePlan, Reports.Resources.ProgressReport.OneWeek, Reports.Resources.ProgressReport.TwoWeeks,
              Reports.Resources.ProgressReport.Month, Reports.Resources.ProgressReport.Quarter, Reports.Resources.ProgressReport.CustomDate);

      var dateControl = dialog.AddDate(Reports.Resources.ProgressReport.StartDateName, false);
      dateControl.IsVisible = false;
      
      // TODO Balezin_AA: временно убраны страницы отчета, связанные с Agile.
      // Необходимо выбирать в диалоге отчета нужные колонки, из которых будут браться тикеты.
      // Сейчас нет способа положить в контрол выбора колонки без явной связи с Agile.
      
      //var includeAgileControl = dialog.AddBoolean(Reports.Resources.ProgressReport.IncludeAgileDialogName, false);
      //var typeBoard = TypeExtension.GetTypeByGuid(Guid.Parse(Constants.ProgressReport.AgileBoardGuid));
      //var isIncludeAgile = typeBoard == null ? false : true;
      //includeAgileControl.IsVisible = isIncludeAgile;
      
      periodControl.SetOnValueChanged((x) =>
                                      {
                                        if(periodControl.Value == Reports.Resources.ProgressReport.CustomDate)
                                        {
                                          dateControl.IsVisible = true;
                                          dateControl.IsRequired = true;
                                        }
                                        else
                                        {
                                          dateControl.IsVisible = false;
                                          dateControl.IsRequired = false;
                                        }
                                      });

      dialog.SetOnButtonClick((args) =>
                              {
                                if(dateControl != null && dateControl.Value > Calendar.Now)
                                {
                                  args.AddError(DirRX.ProjectPlanner.Reports.Resources.ProgressReport.StartPeriodDateNotAvailable);
                                }
                              });
      
      if (dialog.Show() == DialogButtons.Cancel)
      {
        e.Cancel = true;
        return;
      }
      
      var currentDate = Calendar.Today;
      ProgressReport.CurrentDate = currentDate;
      ProgressReport.IsIncludeAgile = false;
      
      if(periodControl.Value == Reports.Resources.ProgressReport.OneWeek)
      {
        ProgressReport.StartDate = currentDate.AddDays(-Constants.ProgressReport.OneWeekInDays);
        ProgressReport.EndDate = currentDate.AddDays(Constants.ProgressReport.OneWeekInDays);
      }
      else if(periodControl.Value == Reports.Resources.ProgressReport.TwoWeeks)
      {
        ProgressReport.StartDate = currentDate.AddDays(-Constants.ProgressReport.TwoWeekInDays);
        ProgressReport.EndDate = currentDate.AddDays(Constants.ProgressReport.TwoWeekInDays);
      }
      else if(periodControl.Value == Reports.Resources.ProgressReport.Month)
      {
        ProgressReport.StartDate = currentDate.AddMonths(-Constants.ProgressReport.OneMonth);
        ProgressReport.EndDate = currentDate.AddMonths(Constants.ProgressReport.OneMonth);
      }
      else if(periodControl.Value == Reports.Resources.ProgressReport.Quarter)
      {
        ProgressReport.StartDate = currentDate.AddMonths(-Constants.ProgressReport.QuarterInMonths);
        ProgressReport.EndDate = currentDate.AddMonths(Constants.ProgressReport.QuarterInMonths);
      }
      else if(periodControl.Value == Reports.Resources.ProgressReport.EntirePlan)
      {
        ProgressReport.StartDate = ProgressReport.ProjectPlanRX.StartDate;
        ProgressReport.EndDate = ProjectActivities.GetAll(x => x.ProjectPlan == ProgressReport.ProjectPlanRX).Max(y => y.EndDate) ?? Calendar.SqlMaxValue;
      }
      else if(periodControl.Value == Reports.Resources.ProgressReport.CustomDate)
      {
        ProgressReport.StartDate = dateControl.Value;
        ProgressReport.EndDate = currentDate.Add(dateControl.Value.Value.TimeOfDay);
      }
    }
  }
}
