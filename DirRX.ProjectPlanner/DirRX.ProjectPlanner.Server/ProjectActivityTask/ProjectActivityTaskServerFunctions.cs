using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivityTask;
using PplanMessages;
using Sungero.Domain.Clients;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityTaskFunctions
  {
    
    /// <summary>
    /// Обновить свойства у автозапускаемой задачи по этапу.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True - обновление успешно, false - не удалось обновить.</returns>
    /// <remarks>У задачи обязательно должны быть заполнены свойства: ProjectActivity и ProjectPlan, 
    /// иначе будет исключение.</remarks>
    public static bool UpdateProjectActivityTaskFields(IProjectActivityTask task, bool isManual)
    {
      if (task.ActivityRefId == null || task.ProjectPlan == null)
      {
        throw new Exception(ProjectActivityTasks.Resources.NotFilledActivityOrPlanErrorFormat(task.Id));
      }
      
      var projectPlan = task.ProjectPlan;
      IProjectActivity projectActivity;
      if (isManual)
      {
        projectActivity = GetLatestActivityByPlanAndRefIdOrThrowException(task.ProjectPlan, task.Id, task.ActivityRefId.Value);
      }
      else
      {
        projectActivity = GetActivityFromOnlyLastVersionOrThrowException(task.ProjectPlan, task.Id, task.ActivityRefId.Value);
      }
      
      var sectionResponsibleOrProjectManager = GetSectionResponsible(projectActivity.LeadingActivity) ??
        GetProjectManagerByProjectPlan(projectPlan.Id);
      
      if (sectionResponsibleOrProjectManager == null && projectActivity.Responsible == null)
      {
        Logger.Debug(ProjectActivityTasks.Resources.NotFindActivityResponsibleFormat(projectActivity.Name, projectActivity.Id));
        return false;
      }
      
      task.Responsible = projectActivity.Responsible ?? sectionResponsibleOrProjectManager;
      task.Author = sectionResponsibleOrProjectManager ?? projectPlan.Author;
      task.Subject = DirRX.ProjectPlanner.ProjectActivityTasks.Resources.TaskSubjectFormat(projectActivity.Name);
      
      //Kiselev_EM: Обрезать заголовок задачи до 250 символов.
      //http://aura.npo-comp.ru/sungero?type=7197cc31-bf64-406e-8ead-0a7dde1c3c6b&id=134965
      if (task.Subject.Length > DirRX.TeamsCommonAPI.PublicConstants.Module.TaskTitleMaxLength)
      {
        task.Subject = task.Subject.Substring(0, DirRX.TeamsCommonAPI.PublicConstants.Module.TaskTitleMaxLength);
      }
      
      task.ActiveText = PrepareTaskText(projectActivity);
      task.MaxDeadline = Functions.Module.CalculateActivityEndDateForTasks(projectActivity);
      
      if (task.MaxDeadline < Calendar.Today)
      {
        var operation = new Enumeration(Constants.ProjectPlanRX.ChangeOperation);
        task.History.Write(operation, operation, Resources.OverdueTaskDateHistoryComment);
      }
      
      task.Save();
      
      return true;
    }
    
    private static IProjectActivity GetActivityFromOnlyLastVersionOrThrowException(IProjectPlanRX projectPlan, long taskId, long activityRefId)
    {
      var projectActivity = Functions.ProjectActivity.GetActivityFromOnlyLastVersion(projectPlan, activityRefId);
      if (projectActivity == null)
      {
        throw new Exception(ProjectActivityTasks.Resources.NotFoundLastVersionActivityFormat(taskId));
      }
      
      return projectActivity;
    }
    
    private static IProjectActivity GetLatestActivityByPlanAndRefIdOrThrowException(IProjectPlanRX projectPlan, long taskId, long activityRefId)
    {
      var projectActivity = Functions.ProjectActivity.GetLatestActivityByPlanAndRefId(projectPlan, activityRefId);
      if (projectActivity == null)
      {
        throw new Exception(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.RelatedStageIsNotFoundFormat(taskId));
      }
      
      return projectActivity;
    }
    
    /// <summary>
    /// Рекурсивно, по разделам ищет ответственного вверх по ИСР, в который входит этап.
    /// Сейчас раздел может входить в раздел, но вроде как хотели запретить, тогда можно будет избавиться от рекурсии.
    /// </summary>
    /// <param name="activity">Родительский этап.</param>
    /// <returns>Возвращает ответственного за раздел, иначе null.</returns>
    private static Sungero.Company.IEmployee GetSectionResponsible(IProjectActivity activity)
    {
      if (activity == null)
      {
        return null;
      }
      
      if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section &&
         activity.Responsible != null)
      {
        return activity.Responsible;
      }
      
      return GetSectionResponsible(activity.LeadingActivity);
    }
    
    private static Sungero.Company.IEmployee GetProjectManagerByProjectPlan(long projectPlanId)
    {
      var project = DirRX.ProjectPlanning.Projects.GetAll(p => p.ProjectPlanDirRX.Id == projectPlanId)
        .FirstOrDefault();
      
      return project != null ? project.Manager : null;
    }
    
    /// <summary>
    /// Сформировать текст автозапускаемой задачи.
    /// </summary>
    /// <param name="activity">Этап.</param>
    /// <returns>Текст задачи.</returns>
    private static string PrepareTaskText(IProjectActivity activity)
    {
      var startDateShortDateString = activity.StartDate.HasValue ? activity.StartDate.Value.ToShortDateString() : string.Empty;
      var endDateShortDateString = activity.EndDate.HasValue ?
        Functions.Module.CalculateActivityEndDateForTasks(activity).ToShortDateString() :
        string.Empty;
      
      //Kiselev_EM Нас интересуют только непосредственные предшественники и связи начало - начало (0) и конец - начало (1).
      var activityPredecessorIds = activity.Predecessors.Where(p => p.LinkType == Constants.ProjectActivity.LinkType.FS ||
                                                               p.LinkType == Constants.ProjectActivity.LinkType.SS)
        .Select(p => p.Activity.Id)
        .ToList();
      var taskText = DirRX.ProjectPlanner.ProjectActivityTasks.Resources.TaskTextFormat(activity.Name, startDateShortDateString, endDateShortDateString)
        .ToString();
      
      if (!activityPredecessorIds.Any())
      {
        return taskText;
      }
      
      taskText += DirRX.ProjectPlanner.ProjectActivityTasks.Resources.TaskTextPredecessors.ToString();
      
      foreach (var activityPredecessor in ProjectActivities.GetAll(a => activityPredecessorIds.Contains(a.Id)))
      {
        var localizedStatus = ProjectActivities.Info.Properties.Status.GetLocalizedValue(activityPredecessor.Status);
        taskText += $"{activityPredecessor.Name} - {localizedStatus}\n";
      }
      
      return taskText;
    }
    
    public static void SendTaskAdded(IProjectActivityTask task)
    {
      var clientIds = GetClientIds(task.ProjectPlan);
      if (task.MaxDeadline == null)
      {
        task.MaxDeadline = Calendar.Now;
      }
      
      //Kiselev_EM Походу после запуска задачи свойство не успевает установиться и может быть null.
      if (!task.Started.HasValue)
      {
        task.Started = Calendar.Now;
      }
      
      PplanMessages.PplanMessageSender.SendTaskAdded(
        clientIds,
        "gantt",
        task.ProjectPlan.Id,
        task.ActivityRefId.Value,
        task.Id,
        task.DisplayValue,
        Hyperlinks.Get(task),
        task.MaxDeadline.ToUserTime().Value.Ticks,
        task.Started.Value.ToUserTime().Ticks,
        task.Status == Sungero.Workflow.Task.Status.Completed ? task.Modified.Value.ToUserTime().Ticks : 0,
        ProjectActivityTaskFunctions.GetTasksByRefId(task.ActivityRefId, task.ProjectPlan.Id).Min(t => t.Id) == task.Id
      );
      SendTaskStatusChanged(task, DirRX.ProjectPlanner.ProjectActivity.Status.Active.Value, clientIds);
    }

    /// <summary>
    /// Выполняет действия при завершении задачи.
    /// </summary>
    public void OnTaskEnded()
    {
      this.SendTaskEnded();
    }
    
    public void OnTaskFactChanged(long activityRefId)
    {
      var clientIds = GetClientIds(_obj.ProjectPlan);
      PplanMessages.PplanMessageSender.SendTaskFactChanged(
        clientIds,
        "gantt",
        _obj.ProjectPlan.Id,
        activityRefId,
        _obj.Id,
        _obj.ExecutionPercent,
        _obj.ActualWorkload,
        _obj.FactualCosts,
        "");
    }
    
    public static void SendTaskStatusChanged(IProjectActivityTask task, string enumValue, System.Collections.Generic.IReadOnlyCollection<Guid> clientIds = null)
    {
      clientIds = clientIds ?? GetClientIds(task.ProjectPlan);
      PplanMessages.PplanMessageSender.SendTaskStatusChanged(
        clientIds,
        "gantt",
        task.ProjectPlan.Id,
        task.ActivityRefId.Value,
        task.Id,
        Hyperlinks.Get(task),
        enumValue
       );
    }
    
    private static IReadOnlyCollection<Guid> GetClientIds(IProjectPlanRX plan)
    {
      return plan.AccessRights.Current.Where(ar => Users.Is(ar.Recipient))
        .Select(ar => Users.As(ar.Recipient))
        .ToList()
        .SelectMany(user => ClientManager.Instance.GetClientsOfUser(user.Id))
        .Distinct()
        .ToArray();
    }
    
    private void SendTaskEnded()
    {
      var clientIds = GetClientIds(_obj.ProjectPlan);
      PplanMessages.PplanMessageSender.SendTaskFinished(
        clientIds,
        "gantt",
        _obj.ProjectPlan.Id,
        _obj.ActivityRefId.Value,
        _obj.Id,
        Hyperlinks.Get(_obj));
      SendTaskStatusChanged(_obj, DirRX.ProjectPlanner.ProjectActivity.Status.Closed.Value, clientIds);
    }

    /// <summary>
    /// Получить самую позднюю дату окончания среди этапов плана последней версии.
    /// </summary>
    /// <param name="plan">План проекта</param>
    /// <returns>Дату или null.</returns>
    public static DateTime? MaxActivityDate(IProjectPlanRX plan)
    {
      return MaxActivityDate(plan, new long[]{}, new long[]{});
    }
    
    /// <summary>
    /// Получить самую позднюю дату окончания среди этапов плана последней версии.
    /// </summary>
    /// <param name="plan">План проекта</param>
    /// <param name="callTaskRefId">RefId из таска, который сейчас выполняется.</param>
    /// <returns>Дату или null.</returns>
    /// <remarks>Текущий таск еще не завершился, прокидываем отдельно его в выборку.</remarks>
    private static DateTime? MaxActivityDate(IProjectPlanRX plan, long[] excludedRefIds, long[] includedRefIds)
    {
      //Kiselev_EM: Добавил проверку plan.LastVersion == null, потому что метод SaveModelFromGanttService используется для
      //создания версии и тела в автотестах http://aura.npo-comp.ru/sungero?type=7197cc31-bf64-406e-8ead-0a7dde1c3c6b&id=130387.
      if (plan == null || plan.LastVersion == null)
      {
        return null;
      }
      
      // RefId по выполненным задачам
      var planTasksRefIds = ProjectActivityTasks.GetAll()
        .Where(t => t.ProjectPlan == plan && t.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.Completed && t.ActivityRefId != null)
        .Select(t => t.ActivityRefId);
      
      var activityWithMaxDate = ProjectActivities.GetAll()
        .Where(a => a.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task &&
               a.ProjectPlan == plan &&
               a.NumberVersion == plan.LastVersion.Number &&
               a.RefId != null &&
               ((!excludedRefIds.Contains(a.RefId.Value) && !planTasksRefIds.Contains(a.RefId)) ||
                includedRefIds.Contains(a.RefId.Value)))
        .OrderByDescending(a => a.EndDate)
        .FirstOrDefault();
      
      // HACK Yarovikov_GV граничная дата нового дня
      return activityWithMaxDate?.EndDate?.AddDays(-1);
    }
    
    public static DateTime? MaxTaskDate(IProjectPlanRX plan)
    {
      return MaxTaskDate(plan, new long[]{});
    }
    
    private static DateTime? MaxTaskDate(IProjectPlanRX plan, IEnumerable<long> excludeIds)
    {
      if (plan == null)
      {
        return null;
      }
      
      var planTasksIds = ProjectActivityTasks.GetAll(t => !excludeIds.Contains(t.Id))
        .Where(t => t.ProjectPlan == plan && t.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.Completed)
        .Select(t => (long?)t.Id);
      
      var lastCompletedHistory = Sungero.Workflow.WorkflowHistories.GetAll(h => h.Operation == Sungero.Workflow.WorkflowHistory.Operation.CompleteTask)
        .Where(h => h.EntityType == ProjectActivityTask.ClassTypeGuid && planTasksIds.Contains(h.EntityId))
        .OrderByDescending(h => h.HistoryDate)
        .FirstOrDefault();
      
      return lastCompletedHistory?.HistoryDate;
    }
    
    /// <summary>
    /// Признак, что по плану запускались задачи.
    /// </summary>
    /// <param name="plan">План проекта.</param>
    /// <returns>Логичское значение</returns>
    public static bool HasStartedTasks(IProjectPlanRX plan)
    {
      return ProjectActivityTasks.GetAll()
        .Where(t => t.ProjectPlan == plan &&
               t.Status != DirRX.ProjectPlanner.ProjectActivityTask.Status.Aborted &&
               t.Status != DirRX.ProjectPlanner.ProjectActivityTask.Status.Draft)
        .Any();
    }
    
    public static IQueryable<IProjectActivityTask> GetTasksByRefId(System.Collections.Generic.IEnumerable<long?> refIds, long projectPlanId)
    {
      return ProjectActivityTasks.GetAll(t => t.ProjectPlan != null && t.ProjectPlan.Id == projectPlanId && t.ActivityRefId != null && refIds.Contains(t.ActivityRefId));
    }
    
    public static IQueryable<IProjectActivityTask> GetTasksByRefId(IProjectActivity activity)
    {
      return ProjectActivityTasks.GetAll(t => t.ActivityRefId != null && t.ProjectPlan != null && t.ProjectPlan == activity.ProjectPlan && t.ActivityRefId == activity.RefId);
    }
    
    public static IQueryable<IProjectActivityTask> GetTasksByRefId(long? activityRefId, long projectPlanId)
    {
      return ProjectActivityTasks.GetAll(t => t.ActivityRefId != null && t.ProjectPlan != null && t.ProjectPlan.Id == projectPlanId && t.ActivityRefId == activityRefId);
    }
  }
}
