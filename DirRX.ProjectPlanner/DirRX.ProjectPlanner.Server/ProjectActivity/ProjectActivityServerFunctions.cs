using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.NotifyEventType;
using CommonLibrary.Linq;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityFunctions
  {
    
    /// <summary>
    /// Возвращает этапы по проекту.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Этапы по проекту.</returns>
    [Public, Remote]
    public static IQueryable<IProjectActivity> GetActivities(IProjectPlanRX projectPlan)
    {
      return ProjectActivities.GetAll(a => a.ProjectPlan.Id == projectPlan.Id);
    }
    
    /// <summary>
    /// Получить этап, связанный с автозапускаемой задачей. Всегда из последней версии.
    /// </summary>
    [Remote]
    public static IProjectActivity GetActivityFromOnlyLastVersion(IProjectPlanRX plan, long? refId)
    {
      if (plan == null || !plan.HasVersions || !plan.LastVersion.Number.HasValue || !refId.HasValue)
      {
        return null;
      }
      return ProjectActivities.GetAll(a => a.ProjectPlan == plan && a.NumberVersion == plan.LastVersion.Number && a.RefId == refId).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить этап по плану проекта и RefId. Возвращается из максимальной версии, в которой он есть.
    /// </summary>
    [Remote]
    public static IProjectActivity GetLatestActivityByPlanAndRefId(IProjectPlanRX plan, long? refId)
    {
      if (plan == null || !plan.HasVersions || !plan.LastVersion.Number.HasValue || !refId.HasValue)
      {
        return null;
      }
      return ProjectActivities.GetAll(a => a.ProjectPlan == plan && a.RefId == refId).OrderByDescending(a => a.NumberVersion).FirstOrDefault();
    }
    
    /// <summary>
    /// Возвращает подчиненные этапы.
    /// </summary>
    /// <param name="activity">Этап.</param>
    /// <returns>Подчиненные этапы.</returns>
    [Remote]
    public static IQueryable<IProjectActivity> GetChildActivities(IProjectActivity activity)
    {
      return ProjectActivities.GetAll().Where(c => ProjectPlanRXes.Equals(c.ProjectPlan, activity.ProjectPlan) && ProjectActivities.Equals(activity, c.LeadingActivity));
    }

    /// <summary>
    /// Генерирует полный номер этапа.
    /// </summary>
    /// <returns>Полный номер этапа.</returns>
    [Remote]
    public string GenerateFullNumber()
    {
      return _obj.LeadingActivity != null ? string.Format("{0}.{1}", _obj.LeadingActivity.FullNumber, _obj.Number.Value) : _obj.Number.Value.ToString();
    }

    /// <summary>
    /// Возвращает новый номер этапа.
    /// </summary>
    /// <returns>Новый номер этапа.</returns>
    [Remote]
    public int GetNewNumber()
    {
      int? maxNumber = null;
      if (_obj.LeadingActivity == null)
        maxNumber = GetActivities(_obj.ProjectPlan).Where(c => c.LeadingActivity == null).Max(c => c.Number);
      else
        maxNumber = GetChildActivities(_obj.LeadingActivity).Max(c => c.Number);
      
      return maxNumber.HasValue ? maxNumber.Value + 1 : 1;
    }
    
    /// <summary>
    /// Признак того, что у этапа существуют подчиненные этапы.
    /// </summary>
    /// <returns>True, если у этапа существуют подчиненные этапы.</returns>
    [Remote]
    public bool ExistChildActivity()
    {
      return GetChildActivities(_obj).Any();
    }

    /// <summary>
    /// Создает объект уведомления об изменении ответственного в этапе.
    /// </summary>
    [Remote]
    public void CreateResponsibleDiff(long? newValue, long? oldValue)
    {
      var newNotifyDiff = NotifyDiffs.Create();
      newNotifyDiff.ConnectedActId = _obj.Id;
      newNotifyDiff.ConnectedPlanId = _obj.ProjectPlan.Id;
      newNotifyDiff.NewValue = newValue.ToString();
      newNotifyDiff.PreviousValue = oldValue.ToString();
      
      var eventType = new Nullable<Enumeration>();
      
      if (_obj.TypeActivity == TypeActivity.Section)
      {
        eventType = EventType.SectRespAssign;
      }
      else if (_obj.TypeActivity == TypeActivity.Milestone)
      {
        eventType = EventType.MilesRespAssign;
      }
      else
      {
        eventType = EventType.ActRespAssign;
      }
      
      newNotifyDiff.EventTypeId = NotifyEventTypes.GetAll(t => t.EventType == eventType).FirstOrDefault()?.Id;
      newNotifyDiff.Save();
    }
    
    /// <summary>
    /// Выбираем вехи из нашей иерархии, которые связаны с КТ внутри иерархии.
    /// </summary>
    /// <param name="gateIds">Список ИД КТ нашей иерархии.</param>
    /// <returns>Коллекцию вех</returns>
    [Public]
    public static System.Collections.Generic.IEnumerable<IProjectActivity> GetMilestonesForGates(System.Collections.Generic.IEnumerable<long> gateIds)
    {
      var mailstonesBaseQuery = DirRX.ProjectPlanner.ProjectActivities.GetAll()
        .Where(pa => pa.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone &&
               pa.ProjectPlan != null &&
               pa.GateId.HasValue && 
               gateIds.Contains(pa.GateId.Value));

      return GetMilestonesOfVersion(mailstonesBaseQuery);
    }
    
    /// <summary>
    /// Выбираем вехи из нашей иерархии, которые связаны с КТ другой иерархии.
    /// </summary>
    /// <param name="gateIds">Список ИД КТ нашей иерархии.</param>
    /// <param name="planIds">Список ИД планов нашей иерархии.</param>
    /// <returns>Коллекцию вех</returns>
    [Public]
    public static System.Collections.Generic.IEnumerable<IProjectActivity> GetOuterMilestonesForGates(
      System.Collections.Generic.IEnumerable<long> gateIds,
      System.Collections.Generic.IEnumerable<long> planIds)
    {
      var mailstonesBaseQuery = DirRX.ProjectPlanner.ProjectActivities.GetAll()
        .Where(pa => pa.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone &&
                pa.ProjectPlan != null &&
                planIds.Contains(pa.ProjectPlan.Id) &&  // принадлежат проектам внутри нашей иерархии
                pa.GateId.HasValue &&
                !gateIds.Contains(pa.GateId.Value));  // связаны с КТ другой иерархии

      return GetMilestonesOfVersion(mailstonesBaseQuery);
    }
    
    private static IEnumerable<IProjectActivity> GetMilestonesOfVersion(IQueryable<IProjectActivity> mailstonesBaseQuery)
    {
      var planVersionPairs = mailstonesBaseQuery
        .Where(pa => pa.ProjectPlan != null)
        .Select(pa => new KeyValuePair<long, long?>(pa.ProjectPlan.Id, pa.ProjectPlan.Versions.Select(v => v.Number).OrderByDescending(n => n).FirstOrDefault()))
        .ToList()
        .Distinct();

      var predicate = PredicateBuilder.False<IProjectActivity>();
      foreach (var pair in planVersionPairs)
      {
        predicate = predicate.Or(pa => pair.Key == pa.ProjectPlan.Id && pair.Value == pa.NumberVersion);
      }
      return mailstonesBaseQuery.Where(predicate.Compile());
    }
  }
}
