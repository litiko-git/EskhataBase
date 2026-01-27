using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ProjectPlanning.Module.Projects.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      base.Initializing(e);
      GrantRightsOnProjectFolders();
      
      var projectKinds = GetProjectKinds();
      HideProjectKindsIfNoProjects(projectKinds);
      RenameProjectKinds(projectKinds);
      
      var capitalConstructionProjectKind = CreateCapitalConstructionProjectKind(projectKinds);
      if (capitalConstructionProjectKind != null)
      {
        projectKinds.Add(capitalConstructionProjectKind);
      }
      
      var projectDocumentKindNames = new List<string>()
      {
        Sungero.Contracts.Resources.ContractKindName.ToString(),
        Sungero.Contracts.Resources.SupAgreementKindName.ToString(),
        Sungero.FinancialArchive.Resources.ContractStatementTypeName.ToString(),
        Sungero.FinancialArchive.Resources.UniversalBasicKindName.ToString(),
        Sungero.FinancialArchive.Resources.UniversalTaxInvoiceAndBasicKindName.ToString(),
        Sungero.FinancialArchive.Resources.WaybillDocumentKindName.ToString(),
        Sungero.RecordManagement.Resources.IncomingLetterKindName.ToString(),
        Sungero.Contracts.Resources.IncomingInvoiceKindName.ToString(),
        Sungero.RecordManagement.Resources.OutgoingLetterKindName.ToString(),
        Sungero.Meetings.Resources.AgendaTypeName.ToString(),
        Sungero.Meetings.Resources.MinutesKindName.ToString(),
        Sungero.Projects.Resources.ReportKindName.ToString()
      };
      SetAccountingProjectCheckBox(projectDocumentKindNames);
      CreateClassifierTemplateForProjectKinds(projectDocumentKindNames, projectKinds);
      SaveChangedProjectKinds(projectKinds);
    }
    
    /// <summary>
    /// Выдача прав на папки проекта.
    /// </summary>
    public void GrantRightsOnProjectFolders()
    {
      var allUsers = Roles.AllUsers;
      var projectManagersRole = Sungero.Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
      
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsPlansDirRX.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectTasksSungero.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsDirRX.AccessRights.Grant(projectManagersRole, DefaultAccessRightsTypes.Read);
      
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsPlansDirRX.AccessRights.Save();
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectTasksSungero.AccessRights.Save();
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsDirRX.AccessRights.Save();
      
      var portfolioProgramModuleFullName = DirRX.PortfolioProgram.PublicConstants.Module.ModluleFullName;
      var licenseIsValid = Sungero.Core.Licenses.IsModuleValid(portfolioProgramModuleFullName);
      
      if(licenseIsValid)
      {
        DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectCoreDirRX.AccessRights.Grant(projectManagersRole, DefaultAccessRightsTypes.Read);
      }
      else
      {
        var projectCoreFolder = DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectCoreDirRX;
        RevokeAllRightsFolder(projectCoreFolder);
      }
      
      DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectCoreDirRX.AccessRights.Save();
    }
    
    /// <summary>
    /// Изъять права на папку.
    /// </summary>
    /// <param name="folder">Папка.</param>
    /// <remark>
    /// Изъятие прав осуществляется без сохранения.
    /// </remark>
    private void RevokeAllRightsFolder(IFolder folder)
    {
      var recipients = folder.AccessRights.Current.Select(x => x.Recipient);
      
      foreach(var recipient in recipients)
      {
        folder.AccessRights.RevokeAll(recipient);
      }
    }
    
    private static List<IProjectKind> GetProjectKinds()
    {
      //Kiselev_EM NHibernate падает потому что не может разложить запрос ToString на LocalizesString,
      //Пришлось собрать в список.
      var projectKindNames = new List<string>()
      {
        Sungero.Projects.Resources.ProjectKindNameInvestment.ToString(),
        Sungero.Projects.Resources.ProjectKindNameInformationTechnology.ToString(),
        Sungero.Projects.Resources.ProjectKindNameOrganizationDevelopment.ToString(),
        Sungero.Projects.Resources.ProjectKindNameCreatingNewProduct.ToString(),
        DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindCapitalConstructionProject.ToString(),
        Sungero.Projects.Resources.ProjectKindNameOrganizationSale.ToString(),
        Sungero.Projects.Resources.ProjectKindNameMarketing.ToString()
      };
      
      return ProjectKinds.GetAll(x => projectKindNames.Contains(x.Name))
        .ToList();
    }
    
    private static void HideProjectKindsIfNoProjects(List<IProjectKind> projectKinds)
    {
      if (projectKinds.Any(p => p.IsHiddenDirRX.HasValue && p.IsHiddenDirRX.Value) ||
         //Kiselev_EM Обговорили с Леной, если нет проектов - скрываемм не нужные виды проектов, иначе - оставляем.
         DirRX.ProjectPlanning.Projects.GetAll().Any())
      {
        return;
      }
      
      var hiddenProjectKinds = projectKinds.Where(p => p.Name == Sungero.Projects.Resources.ProjectKindNameInvestment.ToString() ||
        p.Name == Sungero.Projects.Resources.ProjectKindNameOrganizationSale.ToString());
      
      foreach (var hiddenProjectKind in hiddenProjectKinds)
      {
        hiddenProjectKind.IsHiddenDirRX = true;
      }
      
    }
    
    private static void RenameProjectKinds(List<IProjectKind> projectKinds)
    {
      foreach (var projectKind in projectKinds)
      {
        //Kiselev_EM Конструкция Switch не работает. Не дает в case-e преобразовать LocalizesString в строку.
        if (projectKind.DisplayValue == Sungero.Projects.Resources.ProjectKindNameInformationTechnology.ToString())
        {
          projectKind.DisplayValue = DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindITProject;
        }
        
        if (projectKind.DisplayValue == Sungero.Projects.Resources.ProjectKindNameOrganizationDevelopment.ToString())
        {
          projectKind.DisplayValue = DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindOrganizationalProject;
        }
        
        if (projectKind.DisplayValue == Sungero.Projects.Resources.ProjectKindNameCreatingNewProduct.ToString())
        {
          projectKind.DisplayValue = DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindProductionProject;
        }
      }
    }
    
    private static void SaveChangedProjectKinds(List<DirRX.ProjectPlanning.IProjectKind> projectKindsForSave)
    {
      foreach (var projectKind in projectKindsForSave)
      {
        if (!projectKind.State.IsChanged)
        {
          continue;
        }
        
        using(EntityEvents.DisableAll(projectKind.Info))
        {
          projectKind.Save();
        }
      }
    }
    
    private static IProjectKind CreateCapitalConstructionProjectKind(List<IProjectKind> projectKinds)
    {
      //Kiselev_EM Проверяем каждую локаль, потому что вид проекта может быть создан на одной локали,
      //а инициализация запускаться на другой.
      var ruCultureInfo = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
      var enCultureInfo = System.Globalization.CultureInfo.GetCultureInfo("en-EN");
        
      var capitalConstructionAlreadyExist = ProjectKinds.GetAll()
        .Any(x => x.Name == DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindCapitalConstructionProject.ToString(ruCultureInfo) ||
          x.Name == DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindCapitalConstructionProject.ToString(enCultureInfo));
      if (capitalConstructionAlreadyExist)
      {
        return null;
      }
      
      var newProjectKind = ProjectKinds.Create();
      newProjectKind.Name = DirRX.ProjectPlanning.Module.Projects.Resources.ProjectKindCapitalConstructionProject;
      newProjectKind.Save();
      
      return newProjectKind;
    }
    
    private static void SetAccountingProjectCheckBox(List<string> projectDocumentKindNames)
    {
      var projectDocumentKinds = Sungero.Docflow.DocumentKinds.GetAll(x => projectDocumentKindNames.Contains(x.Name));
      foreach (var projectDocumentKind in projectDocumentKinds)
      {
        projectDocumentKind.ProjectsAccounting = true;
        projectDocumentKind.Save();
      }
    }
    
    private static void CreateClassifierTemplateForProjectKinds(List<string> projectDocumentKindNames,
      List<IProjectKind> projectKinds)
    {
      var projectDocumentKinds = Sungero.Docflow.DocumentKinds.GetAll(x => x.ProjectsAccounting.HasValue
        && x.ProjectsAccounting.Value &&
        projectDocumentKindNames.Contains(x.Name))
        .ToList();
      
      foreach (var projectKind in projectKinds)
      {
        if (projectKind.IsHiddenDirRX.HasValue && projectKind.IsHiddenDirRX.Value)
        {
          continue;
        }
        
        CreateProjectKindClassifierTemplate(projectKind, projectDocumentKinds);
      }
    }
    
    private static void CreateProjectKindClassifierTemplate(IProjectKind projectKind,
      List<Sungero.Docflow.IDocumentKind> projectDocumentKinds)
    {
      projectKind.ClassifierDirRX.Clear();
      foreach (var projectDocumentKind in projectDocumentKinds)
      {
        if (projectDocumentKind.Name == Sungero.Contracts.Resources.ContractKindName.ToString() ||
            projectDocumentKind.Name == Sungero.Contracts.Resources.SupAgreementKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.ContractsFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
        
        if (projectDocumentKind.Name == Sungero.FinancialArchive.Resources.ContractStatementTypeName.ToString() ||
            projectDocumentKind.Name == Sungero.FinancialArchive.Resources.UniversalBasicKindName.ToString() ||
            projectDocumentKind.Name == Sungero.FinancialArchive.Resources.UniversalTaxInvoiceAndBasicKindName.ToString() ||
            projectDocumentKind.Name == Sungero.FinancialArchive.Resources.WaybillDocumentKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.ActsAndInvoicesFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
        
        if (projectDocumentKind.Name == Sungero.RecordManagement.Resources.IncomingLetterKindName.ToString() ||
            projectDocumentKind.Name == Sungero.Contracts.Resources.IncomingInvoiceKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.IncomingDocumentsFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
        
        if (projectDocumentKind.Name == Sungero.RecordManagement.Resources.OutgoingLetterKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.OutgoingLettersFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
        
        if (projectDocumentKind.Name == Sungero.Meetings.Resources.AgendaTypeName.ToString() ||
            projectDocumentKind.Name == Sungero.Meetings.Resources.MinutesKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.MeetingsFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
        
        if (projectDocumentKind.Name == Sungero.Projects.Resources.ReportKindName.ToString())
        {
          var classifier = projectKind.ClassifierDirRX.AddNew();
          classifier.FolderName = DirRX.ProjectPlanning.Module.Projects.Resources.ReportsFolderName;
          classifier.DocumentKind = projectDocumentKind;
          
          continue;
        }
      }
      
    }
  }
}
