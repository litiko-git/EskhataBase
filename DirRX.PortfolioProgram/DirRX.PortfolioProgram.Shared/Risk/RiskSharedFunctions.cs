using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Risk;

namespace DirRX.PortfolioProgram.Shared
{
  partial class RiskFunctions
  {
    /// <summary>
    /// Установить обязательность, доступность, видимость свойств.
    /// </summary>
    public void SetStateProperties()
    {
      var isRequired = _obj.Type == PortfolioProgram.Risk.Type.Risk;
      _obj.State.Properties.Category.IsRequired = isRequired;
      if (!isRequired && _obj.Category != null)
        _obj.Category = null;
      
      _obj.State.Properties.RelatedActivityRef.IsVisible = _obj.RelatedProjectCore != null;
    }
    
    /// <summary>
    /// Получить этапы проекта последней утвержденной версии. Если такой нет, то возвращает этапы последней версии.
    /// </summary>
    /// <param name="projectPlan">План проекта.</param>
    /// <returns>Список этапов.</returns>
    public static IQueryable<ProjectPlanner.IProjectActivity> GetStagesLastVersionApproved(ProjectPlanner.IProjectPlanRX projectPlan)
    {
      if (projectPlan == null)
        return Enumerable.Empty<ProjectPlanner.IProjectActivity>().AsQueryable();
      
      var signVersion = Functions.Risk.GetLatestSignedVersion(projectPlan);
      var stages = ProjectPlanner.PublicFunctions.ProjectActivity.Remote.GetActivities(projectPlan).Where(s => s.NumberVersion == signVersion);
      return stages;
    }
    
    /// <summary>
    /// Найти последнюю утвержденную версию документа. Если такой нет, то вернуть последнюю версию.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Номер версии.</returns>
    /// <remarks>Если у документа версии отсутствуют, то возвращается -1</remarks>
    public static int? GetLatestSignedVersion(Sungero.Docflow.IOfficialDocument document)
    {
      if (!document.HasVersions)
        return -1;
      
      if (document.LastVersionApproved == true)
        return document.LastVersion.Number;
      
      var versions = document.Versions.OrderByDescending(v => v.Number);
      foreach(var version in versions)
      {
        if (Signatures.Get(version).Any(s => s.SignatureType == SignatureType.Approval))
          return version.Number;
      }
      
      return document.LastVersion.Number;
    }
  }
}