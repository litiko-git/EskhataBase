using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;
using net.sf.mpxj;
using net.sf.mpxj.mspdi;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectPlanRXAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = ProjectPlanRXes.As(e.RootEntity);
      return (root != null && _all == root.ResponsibleNotices || _all == root.PlanDateNotices || _all == root.OtherNotices) 
        ? false 
        : !(_objs.Any(o => o is IProjectPlanRXVersions));
    }

  }


  partial class ProjectPlanRXAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = ProjectPlanRXes.As(e.RootEntity);
      return (root != null && _all == root.ResponsibleNotices || _all == root.PlanDateNotices || _all == root.OtherNotices) 
        ? false 
        : !(_obj is IProjectPlanRXVersions);;
    }


    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = ProjectPlanRXes.As(e.RootEntity);
      return (root != null && _all == root.ResponsibleNotices || _all == root.PlanDateNotices || _all == root.OtherNotices) 
        ? false 
        : !(_obj is IProjectPlanRXVersions);;
    }

  }

  partial class ProjectPlanRXVersionsActions
  {
    public override void ExportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ExportVersion(e);
    }

    public override bool CanExportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }


    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var asyncHandler = AsyncHandlers.DeleteVersionAsyncHandler.Create();
      asyncHandler.PlanId = e.RootEntity.Id;
      asyncHandler.NumberVersion = _obj.Number.Value;
      asyncHandler.ExecuteAsync();
      
      base.DeleteVersion(e);
    }

    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanDeleteVersion(e);
    }

    
    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void SendVersionByMail(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.SendVersionByMail(e);
    }

    public override bool CanSendVersionByMail(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }


    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var projectPlan = ProjectPlanRXes.As(_obj.RootEntity);
      if (projectPlan == null)
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
        Logger.DebugFormat(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.BadCastRootEntityToProjectPlanTextFormat(_obj.RootEntity.Id, _obj.Number));
        
        return;
      }
      
      if (projectPlan.State.IsChanged)
      {
        if (!Functions.ProjectPlanRX.SaveCardDialog(projectPlan))
        {
          return;
        }
        
        projectPlan.Save();
        DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(projectPlan, projectPlan.LastVersion.Number.Value, false);
      }
      
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.RootEntity.Id, _obj.Number.Value, Users.Current.Id, false);
      //Kiselev_EM Необходимо выйти из карточки после отработки действия. Единственная возможность установить флаг
      //CloseFormAfterAction = true - не работает, баг платформы https://rxtfs.directum.ru/IS-Builder8/Kotlin/_workitems/edit/238522
      e.CloseFormAfterAction = true;
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var webSiteStringEmpty = false;
      
      return !Functions.Module.Remote.VersionApproved(_obj.ElectronicDocument, _obj.Number.Value) &&
        _obj.ElectronicDocument.AccessRights.CanUpdate() && !Locks.GetLockInfo(_obj.ElectronicDocument).IsLockedByOther;
    }

    public override void ReadVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.RootEntity.Id, _obj.Number.Value, Users.Current.Id, true);
    }

    public override bool CanReadVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual bool CanCopyVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void CopyVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.Module.Remote.CreateCopyVersion(ProjectPlanRXes.As(e.RootEntity), _obj.Number.Value);
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.ElectronicDocument.Id, _obj.ElectronicDocument.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanImportVersionCustom(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ImportVersionCustom(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(ProjectPlanRXes.As(_obj.ElectronicDocument), _obj.Number.Value);
    }

    public virtual bool CanExportPlanVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.Body != null;
    }

    public virtual void ExportPlanVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(ProjectPlanRXes.Resources.DialogExportTitle, ProjectPlanRXes.Resources.DialogExportText);
      var buttonToMS = dialog.Buttons.AddCustom(ProjectPlanRXes.Resources.DialogExportButtonMsProject);
      var buttonToRXPP = dialog.Buttons.AddCustom(ProjectPlanRXes.Resources.DialogExportButtonRxpp);
      dialog.HelpCode = Constants.Module.ExportDialogHelpCode;
      dialog.Buttons.AddCancel();
      
      var result = dialog.Show();
      if(result == buttonToMS)
      {
        Functions.ProjectPlanRX.ExportToMSProject(ProjectPlanRXes.As(e.RootEntity), _obj.Number.Value);
      }
      if(result == buttonToRXPP)
      {
        _obj.Export();
      }
    }

  }

  partial class ProjectPlanRXCollectionActions
  {
    public override void ExportLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ExportLastVersion(e);
    }

    public override bool CanExportLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void OpenDocumentRead(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      foreach (var project in _objs)
      {
        DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, project.Id, 0, Users.Current.Id, true);
      }
    }

    public override bool CanOpenDocumentRead(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Select(x => x.HasVersions).Any(x => x);
    }

    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      foreach (var projectPlan in _objs)
      {
        try
        {
          var lockMessage = Functions.Module.Remote.DetectProjectPlanLocks(projectPlan);
          if (lockMessage == null || lockMessage.Count < 1)
          {
            DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, projectPlan.Id, projectPlan.LastVersion.Number.Value, Users.Current.Id, false);
            return;
          }
          
          var dialog = Dialogs.CreateTaskDialog(CreateMessageTextByLockMessages(lockMessage, projectPlan.DisplayValue), MessageType.Question);
          dialog.Buttons.AddYesNo();
          
          if (dialog.Show() == DialogButtons.Yes)
          {
            DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, projectPlan.Id, projectPlan.LastVersion.Number.Value, Users.Current.Id, true);
          }
        }
        catch (Exception ex)
        {
          Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
          Logger.DebugFormat("{0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
        }
      }
    }
    
    private static string CreateMessageTextByLockMessages(List<string> lockMessages, string projectPlanName)
    {
      var lockMessagesInOneLine = string.Empty;
      foreach (var lockMessage in lockMessages)
      {
        lockMessagesInOneLine += lockMessage + '\n';
      }
      
      return DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectPlanIsLockedFormat(projectPlanName, lockMessagesInOneLine);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return  _objs.Select(x => x.LastVersion != null && !Functions.Module.Remote.VersionApproved(x, x.LastVersion.Number.Value) && x.AccessRights.CanUpdate()).Any(x => x);
    }

  }


  partial class ProjectPlanRXActions
  {

    public override void ShowComparisonResult(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowComparisonResult(e);
    }

    public override bool CanShowComparisonResult(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CompareDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CompareDocuments(e);
    }

    public override bool CanCompareDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CompareVersions(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CompareVersions(e);
    }

    public override bool CanCompareVersions(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void OpenExchangeOrderReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenExchangeOrderReport(e);
    }

    public override bool CanOpenExchangeOrderReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }


    public virtual void ShowProgressReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var plan = DirRX.ProjectPlanner.ProjectPlanRXes.As(e.Entity);
      
      if(plan == null)
      {
        throw new Exception(ProjectPlanRXes.Resources.ErrorCastToProjectPlanRXMessageFormat(e.Entity.Id));
      }
      
      var report = Reports.GetProgressReport();
      report.ProjectPlanRX = plan;

      report.Open();
    }

    public virtual bool CanShowProgressReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && !_obj.State.IsInserted;;
    }
    
    public virtual void ExportLastVersionToRXPP(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Export();
    }

    public virtual bool CanExportLastVersionToRXPP(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Versions != null && _obj.Versions.Count > 0;
    }

    public virtual void ExportLastVersionToMSProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProjectPlanRX.ExportToMSProject(_obj, _obj.LastVersion.Number.Value);
    }

    public virtual bool CanExportLastVersionToMSProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Versions != null && _obj.Versions.Count > 0;
    }

    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }


    public virtual void CreateEmptyPlan(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!CheckPlanName(e))
      {
        return;
      }
      
      DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, 0, false);
    }

    public virtual bool CanCreateEmptyPlan(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.HasVersions && _obj.Status != Status.Closed;
    }

    public virtual bool CanCreatePlanFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.HasVersions && _obj.Status != Status.Closed;
    }

    public virtual void CreatePlanFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(_obj, 1);
    }

    public override void ShowSignatures(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowSignatures(e);
    }

    public override bool CanShowSignatures(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowSignatures(e) && !_obj.State.IsInserted;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!CheckPlanName(e))
      {
        return;
      }
      
      if (!e.Validate()) {
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.PlanTemplateSelectDialogTitle);
      var templateControl = dialog
        .AddSelect<DirRX.ProjectPlanning.IDocumentTemplate>(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.PlanTemplateSelectDialogTemplateControl, true, null)
        .From(DirRX.ProjectPlanning.DocumentTemplates.GetAll(t => t.DocumentType.HasValue && t.DocumentType.Value.Equals(Constants.Module.ProjectPlanDocKindGuid)));
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        var template = templateControl.Value;
        if (!template.HasVersions)
        {
          return;
        }
        
        try
        {
          Functions.Module.Remote.SaveProjectPlanFromTemplate(template, _obj);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("ProjectPlanRX CreateFromTemplate error: {0}", ex.Message);
          e.AddError(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.PlanTemplateIsInWrongForm);
        }
      }
      
      
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && !_obj.HasVersions;
    }

    public virtual void ShowProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var project = Functions.ProjectPlanRX.Remote.GetLinkedProject(_obj);
      if(project != null)
        project.ShowModal();
      else
        Dialogs.ShowMessage(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ExistProjectError, MessageType.Warning);
    }

    public virtual bool CanShowProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.DeleteAllActivities(_obj);
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e);
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCopyEntity(e);
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProjectPlanRX.CreateProjectFromFileDialog(_obj, 1);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual void CopyVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.CreateCopyVersion(_obj, _obj.LastVersion.Number.Value);
      Dialogs.NotifyMessage("Создана новая версия проекта");
      
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanCopyVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateVersionFromLastVersion(e);
    }

    public virtual void EditLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.State.IsChanged)
      {
        if (!Functions.ProjectPlanRX.SaveCardDialog(_obj))
        {
          return;
        }
        
        _obj.Save();
        DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, _obj.LastVersion.Number.Value, false);
      }
      
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, false);
    }

    public virtual bool CanEditLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var versoinApproved = _obj.LastVersion != null ? !Functions.Module.Remote.VersionApproved(_obj, _obj.LastVersion.Number.Value) : false;
      return !_obj.State.IsInserted && versoinApproved &&
        _obj.AccessRights.CanUpdate() && !Locks.GetLockInfo(_obj).IsLockedByOther && _obj.Status != Status.Closed;
    }

    public virtual void ReadLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.Id, _obj.LastVersion.Number.Value, Users.Current.Id, true);
    }

    public virtual bool CanReadLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && _obj.HasVersions;
    }

    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SaveAndClose(e);
      this.Save();
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Save(e);
      this.Save();
    }
    
    private void Save()
    {
      if (_obj.IsCopy.HasValue && _obj.IsCopy.Value)
      {
        DirRX.ProjectPlanner.Functions.Module.Remote.CreateCopyProject(_obj, _obj.LastVersion.Number.Value);
      }
      else if (_obj.HasVersions)
      {
        DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(_obj, _obj.LastVersion.Number.Value, false);
      }
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }
    
    private bool CheckPlanName(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (string.IsNullOrEmpty(_obj.Name))
      {
        e.AddError(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.NameCannotBeNull);
        return false;
      }
      
      return true;
    }

  }

  

}