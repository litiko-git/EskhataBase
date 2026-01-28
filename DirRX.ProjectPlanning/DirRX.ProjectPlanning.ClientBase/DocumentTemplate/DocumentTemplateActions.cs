using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.DocumentTemplate;

namespace DirRX.ProjectPlanning.Client
{
  partial class DocumentTemplateActions
  {
    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanScanInNewVersion(e) &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid;
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromFile(e);
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromScanner(e) &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromTemplate(e) &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid &&
        _obj.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid;
    }

  }

  partial class DocumentTemplateVersionsActions
  {
    public override void ReadVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ReadVersion(e);
    }

    public override bool CanReadVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var templateEntity = DirRX.ProjectPlanning.DocumentTemplates.As(_obj.RootEntity);
      
      return base.CanReadVersion(e) &&
        templateEntity.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid &&
        templateEntity.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid;
    }

    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var templateEntity = DirRX.ProjectPlanning.DocumentTemplates.As(_obj.RootEntity);
       
      return base.CanEditVersion(e) &&
        templateEntity.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid &&
        templateEntity.DocumentType != ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid;
    }

  }

  partial class DocumentTemplateCollectionActions
  {
    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanOpenDocumentEdit(e) &&
        !_objs.Any(x => x.DocumentType == ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid || x.DocumentType == ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid);
    }

    public override void OpenDocumentRead(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentRead(e);
    }

    public override bool CanOpenDocumentRead(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanOpenDocumentEdit(e) &&
        !_objs.Any(x => x.DocumentType == ProjectPlanner.PublicConstants.Module.ProjectPlanDocKindGuid || x.DocumentType == ProjectPlanner.PublicConstants.Module.ProjectPlanObsoleteDocKindGuid);
    }

  }

}