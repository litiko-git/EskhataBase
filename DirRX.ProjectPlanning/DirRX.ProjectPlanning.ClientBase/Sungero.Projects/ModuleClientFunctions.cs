using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Company;
using ResourcesPlanner = DirRX.ProjectPlanner.Resources;

namespace DirRX.ProjectPlanning.Module.Projects.Client
{
  partial class ModuleFunctions
  {
    /// <summary>
    /// Открыть клиент Дорожная карта
    /// </summary>
    public virtual void CreateRoadmap()
    {
      if (!DirRX.PortfolioProgram.PublicFunctions.Module.PortfolioProgramModuleHasLicense())
      {
        Dialogs.ShowMessage(DirRX.PortfolioProgram.Resources.RoadmapIsNotAvailableWitoutPortfolioProgramLicense, MessageType.Error);
        return;
      }
      
      var uriRoadmap = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetUriRoadmap();
      Hyperlinks.Open(uriRoadmap.ToString());
    }

    /// <summary>
    /// Отчет по загрузке ресурсов по оргструктуре.
    /// </summary>
    public virtual void CreateWorkloadAnalysis()
    {
      IQueryable<IBusinessUnit> businessUnits = Sungero.Company.BusinessUnits.GetAll(b => b.Status == Sungero.Company.BusinessUnit.Status.Active);
      var currentEmployee = Sungero.Company.Employees.Current;
      var currentUserDate = Calendar.UserNow.Date;
      var visibilitySettings = VisibilitySettings.GetAll().SingleOrDefault();
      var unrestrictedRecipients = visibilitySettings.UnrestrictedRecipients.Select(r => r.Recipient.Id).ToList();
      
      if (visibilitySettings != null && visibilitySettings.NeedRestrictVisibility == true && currentEmployee != null && !unrestrictedRecipients.Contains(currentEmployee.Id))
      {
        var recipientIds = Sungero.Company.PublicFunctions.Module.GetHeadRecipientsByEmployee(Sungero.Company.Employees.Current.Id);
        businessUnits = businessUnits.Where(x => recipientIds.Contains(x.Id));
      }
      
      #region Создание диалога
      var dialog = Dialogs.CreateInputDialog(ProjectCores.Resources.WorkloadAnalysisTitle);
      dialog.HelpCode = Constants.Module.WorkloadAnalysisDialog;
      
      var defaultBusinessUnit = businessUnits.Count() == 1 ? businessUnits.Single() : null;
      var businessUnitControl = dialog.AddSelectMany(ResourcesPlanner.OrganizationName, false, defaultBusinessUnit);
      var businessUnitsText = dialog
        .AddMultilineString(ResourcesPlanner.OrganizationName, false, defaultBusinessUnit?.Name)
        .WithRowsCount(3);
      var addBusinessUnit = dialog.AddHyperlink(ResourcesPlanner.AddBusinessUnit);
      var deleteBusinessUnit = dialog.AddHyperlink(ResourcesPlanner.RemoveBusinessUnit);
      
      var initialDepartments = defaultBusinessUnit != null ? Departments.GetAll(d => d.Status == Sungero.Company.Department.Status.Active && defaultBusinessUnit == d.BusinessUnit) : null;
      var defaultDepartment = (initialDepartments != null && initialDepartments.Count() == 1) ? initialDepartments.Single() : null;
      var departmentControl = dialog.AddSelectMany(ResourcesPlanner.DepartmentName, false, defaultDepartment);
      var departmentsText = dialog
        .AddMultilineString(ResourcesPlanner.DepartmentName, false, defaultDepartment?.Name)
        .WithRowsCount(3);
      var addDepartment = dialog.AddHyperlink(ResourcesPlanner.AddDepartment);
      var deleteDepartment = dialog.AddHyperlink(ResourcesPlanner.RemoveDepartment);
            
      var startDateControl = dialog.AddDate(ProjectCores.Resources.WorkloadAnalysisStartDateName, true, currentUserDate);
      var endDateControl = dialog.AddDate(ProjectCores.Resources.WorkloadAnalysisEndDateName, true, currentUserDate.AddMonths(3));
      var timelineControl = dialog.AddSelect(ResourcesPlanner.WorkloadAnalysisTimeline, true, ResourcesPlanner.MonthsTimeline).From(ResourcesPlanner.DaysTimeline,
                                                                                                                                         ResourcesPlanner.WeeksTimeline,
                                                                                                                                         ResourcesPlanner.MonthsTimeline,
                                                                                                                                         ResourcesPlanner.QuartersTimeline,
                                                                                                                                         ResourcesPlanner.YearsTimeline);
      #endregion
      
      #region Добавление/исключение организаций     
      addBusinessUnit.SetOnExecute(
        () =>
        {
          var selectedBusinessUnits = businessUnits.ShowSelectMany(ResourcesPlanner.SelectBusinessUnitsForAdd);
          if (selectedBusinessUnits != null && selectedBusinessUnits.Any())
          {
            var newBusinessUnits= new List<IBusinessUnit>();
            newBusinessUnits.AddRange(businessUnitControl.Value);
            newBusinessUnits.AddRange(selectedBusinessUnits);
            var distinctBusinessUnits = newBusinessUnits.Distinct();
            businessUnitControl.Value = distinctBusinessUnits;
            businessUnitsText.Value = string.Join("; ", distinctBusinessUnits.Select(b => b.Name));
          }
        });
      
      deleteBusinessUnit.SetOnExecute(
        () =>
        {
          if (businessUnitControl.Value.Count() <= 1)
          {
            businessUnitControl.Value = Enumerable.Empty<IBusinessUnit>();
            businessUnitsText.Value = string.Empty;
          }
          else
          {
            var selectedBusinessUnits = businessUnitControl.Value.ShowSelectMany(ResourcesPlanner.SelectBusinessUnitsForDelete);
            if (selectedBusinessUnits != null && selectedBusinessUnits.Any())
            {
              var newBusinessUnits = businessUnitControl.Value.Where(b => !selectedBusinessUnits.Contains(b));
              businessUnitControl.Value = newBusinessUnits;
              businessUnitsText.Value = string.Join("; ", newBusinessUnits.Select(b => b.Name));
            }
          }
        });
      #endregion
      
      #region Добавление/исключение подразделений
      addDepartment.SetOnExecute(
        () =>
        {
          var selectedDepartments = Departments.GetAll(d =>
                                                       d.Status == Sungero.Company.Department.Status.Active &&
                                                       businessUnitControl.Value.Contains(d.BusinessUnit)
                                                      )
            .ShowSelectMany(ResourcesPlanner.SelectDepartmentsForAdd);
          
          if (selectedDepartments != null && selectedDepartments.Any())
          {
            var newDepartments= new List<IDepartment>();
            newDepartments.AddRange(departmentControl.Value);
            newDepartments.AddRange(selectedDepartments);
            var distinctDepartments = newDepartments.Distinct();
            departmentControl.Value = distinctDepartments;
            departmentsText.Value = string.Join("; ", distinctDepartments.Select(d => d.Name));
          }
        });
      
      deleteDepartment.SetOnExecute(
        () =>
        {
          if (departmentControl.Value.Count() <= 1)
          {
            departmentControl.Value = Enumerable.Empty<IDepartment>();
            departmentsText.Value = string.Empty;
          }
          else
          {
            var selectedDepartments = departmentControl.Value.ShowSelectMany(ResourcesPlanner.SelectDepartmentsForDelete);
            
            if (selectedDepartments != null && selectedDepartments.Any())
            {
              var newDepartments = departmentControl.Value.Where(d => !selectedDepartments.Contains(d));
              departmentControl.Value = newDepartments;
              departmentsText.Value = string.Join("; ", newDepartments.Select(d => d.Name));
            }
          }
        });
      #endregion

      #region События диалога
      dialog.SetOnButtonClick((args) =>
                              {
                                if (endDateControl.Value < startDateControl.Value)
                                  args.AddError(ProjectCores.Resources.WorloadAnalysisIncorrectDate);
                              }
                             );
      
      businessUnitControl.SetOnValueChanged((x) =>
                                            {
                                              var hasValue = x.NewValue.Any();
                                              deleteBusinessUnit.IsVisible = hasValue;
                                              addDepartment.IsVisible = hasValue;
                                              
                                              var departmentsUpdate = departmentControl.Value.Where(d => x.NewValue.Contains(d.BusinessUnit));
                                              departmentControl.Value = departmentsUpdate;
                                              departmentsText.Value = string.Join("; ", departmentsUpdate.Select(d => d.Name));
                                            });
      
      departmentControl.SetOnValueChanged((x) =>
                                          {
                                            var hasValue = x.NewValue.Any();
                                            deleteDepartment.IsVisible = hasValue;
                                            endDateControl.Value = hasValue ? currentUserDate.AddMonths(1) : currentUserDate.AddMonths(3);
                                            timelineControl.Value = hasValue ? ResourcesPlanner.WeeksTimeline : ResourcesPlanner.MonthsTimeline;
                                          });
      #endregion
      
      businessUnitsText.IsRequired = true;
      businessUnitsText.IsEnabled = false;
      businessUnitControl.IsVisible = false;
      departmentControl.IsVisible = false;
      departmentsText.IsEnabled = false;
      deleteBusinessUnit.IsVisible = defaultBusinessUnit != null;
      deleteDepartment.IsVisible = defaultDepartment != null;
      addDepartment.IsVisible = defaultBusinessUnit != null;
      
      if (dialog.Show() == DialogButtons.Cancel)
      {
        return;
      }
      
      string dateScaleKey = "null";
      
      if (timelineControl.Value == ResourcesPlanner.DaysTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineDays;
      }
      else if (timelineControl.Value == ResourcesPlanner.WeeksTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineWeeks;
      }
      else if (timelineControl.Value == ResourcesPlanner.MonthsTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineMonths;
      }
      else if (timelineControl.Value == ResourcesPlanner.QuartersTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineQuarters;
      }
      else if (timelineControl.Value == ResourcesPlanner.YearsTimeline)
      {
        dateScaleKey = ProjectPlanner.PublicConstants.Module.DateScaleKey.TimelineYears;
      }
      
      var dateRange = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetDateRange(startDateControl.Value.Value, endDateControl.Value.Value);
      var businessUnitIds = businessUnitControl.Value.Select(b => b.Id).ToList();
      var departments = departmentControl.Value.Select(d => d.Id).ToList();
      var uriCompReport = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetUriResourceReportOrg(businessUnitIds, departments, dateRange, dateScaleKey);
      
      Hyperlinks.Open(uriCompReport.ToString());
    }
    /// <summary>
    /// Создать программу.
    /// </summary>
    public virtual void CreateProgram()
    {
      var portfolioProgramModuleFullName = DirRX.PortfolioProgram.PublicConstants.Module.ModluleFullName;
      var licenseIsValid = Sungero.Core.Licenses.IsModuleValidForCurrentUser(portfolioProgramModuleFullName);
      
      if(!licenseIsValid)
      {
        var moduleName = DirRX.PortfolioProgram.Resources.ModuleDisplayName;
        Dialogs.NotifyMessage(DirRX.ProjectPlanning.Resources.NoLicenseForModuleFormat(moduleName));
        return;
      }
      
      var program = DirRX.PortfolioProgram.PublicFunctions.Program.Remote.CreateProgram();
      program.Show();
    }

    /// <summary>
    /// Создать портфель.
    /// </summary>
    public virtual void CreatePortfolio()
    {
      var portfolioProgramModuleFullName = DirRX.PortfolioProgram.PublicConstants.Module.ModluleFullName;
      var licenseIsValid = Sungero.Core.Licenses.IsModuleValidForCurrentUser(portfolioProgramModuleFullName);
        
      if(!licenseIsValid)
      {
        var moduleName = DirRX.PortfolioProgram.Resources.ModuleDisplayName;
        Dialogs.NotifyMessage(DirRX.ProjectPlanning.Resources.NoLicenseForModuleFormat(moduleName));
        return;
      }
      
      var portfolio = DirRX.PortfolioProgram.PublicFunctions.Portfolio.Remote.CreatePortfolio();
      portfolio.Show();
    }
    
    /// <summary>
    /// Действие на обложке. Диалог создания документа.
    /// </summary>
    public override void CreateDocument()
    {
      ProjectDocuments.CreateDocumentWithCreationDialog(ProjectDocuments.Info,
                                                        Sungero.Docflow.SimpleDocuments.Info,
                                                        Sungero.Docflow.Addendums.Info,
                                                        Sungero.Docflow.MinutesBases.Info,
                                                        DirRX.ProjectPlanner.ProjectPlanRXes.Info
                                                       );
    }
  }
}