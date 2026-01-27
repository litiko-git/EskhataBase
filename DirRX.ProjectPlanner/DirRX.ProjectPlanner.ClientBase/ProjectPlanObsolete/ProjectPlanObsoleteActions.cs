using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanObsolete;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectPlanObsoleteVersionsActions
  {
    public virtual void ImportVersionCustom(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      
    }

    public virtual bool CanImportVersionCustom(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void CopyVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      
    }

    public virtual bool CanCopyVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

  }

  partial class ProjectPlanObsoleteActions
  {
    public virtual void GetOrCreateResource(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanGetOrCreateResource(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void GetCapacity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanGetCapacity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void UpdateResourcesData(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanUpdateResourcesData(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowProject(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanShowProject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CopyVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanCopyVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void EditLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanEditLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ReadLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanReadLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

  
}