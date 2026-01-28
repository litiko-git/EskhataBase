using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Client
{
  public class ModuleFunctions
  { 
    /// <summary>
    /// Снятие блокировки с карточки проекта.
    /// </summary>
    /// <param name="projectId">Id проекта</param>
    [Hyperlink]
    public void UnlockProject(string projectId)
    {
      //TODO Kiselev_EM Наверное стоит переписать метод на СИ.
      var projectPlan = Functions.ProjectPlanRX.Remote.GetProjectPlan(long.Parse(projectId));
      if (projectPlan == null)
      {
        return;
      }
      
      if (CanUnlockProjectPlan(projectPlan))
      {
        Locks.Unlock(projectPlan);
      }
      
      var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(projectPlan);
      if (linkedProject == null)
      {
        return;
      }
      
      if (CanUnlockLinkedProject(linkedProject))
      {
        Locks.Unlock(linkedProject);
      }
    }
    
    private static bool CanUnlockProjectPlan(IProjectPlanRX projectPlan)
    {
      var projectPlanLockInfo = Locks.GetLockInfo(projectPlan);
      var openedProjectPlansFromCard = Functions.Module.Remote.GetOpensProjectPlansFromCard(projectPlan, null).FirstOrDefault();
      
      return projectPlanLockInfo != null &&
        projectPlanLockInfo.IsLockedByMe &&
        (openedProjectPlansFromCard == null || openedProjectPlansFromCard.User != Users.Current);
    } 
    
    private static bool CanUnlockLinkedProject(DirRX.ProjectPlanning.IProject linkedProject)
    {
      var linkedProjectLockInfo = Locks.GetLockInfo(linkedProject);
      var openedProjectsFromCard = Functions.Module.Remote.GetOpensProjectsFromCard(linkedProject).FirstOrDefault();
      
      return linkedProjectLockInfo != null &&
        linkedProjectLockInfo.IsLockedByMe &&
        (openedProjectsFromCard == null || openedProjectsFromCard.User != Users.Current);
    }
    
    [Hyperlink]
    public void UpdateActivity(string projectId)
    {
      var pp = Functions.ProjectPlanRX.Remote.GetProjectPlan(long.Parse(projectId));
      if (pp == null)
        return;
      
      var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(pp);
      
      try
      {
        var successfullyLocked = Locks.TryLock(pp);
        // Zheleznov_AV HACK если удалось заблокировать карточку плана проекта, значит либо план проекта был открыт из списка,
        // либо он был открыт из карточки, но карточка уже закрыта. В обоих случаях справочник OpensProjectPlansFromCard не должен
        // содержать записей о данном плане.
        if (successfullyLocked)
          Functions.OpensProjectPlansFromCard.Remote.DeleteEntry(pp);
        
        if (linkedProject != null)
          Locks.TryLock(linkedProject);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Ошибка в UpdateActivity: {0}", ex.Message);
      }
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(long projectId, int numVersion, long employeeId, bool isReadOnly)
    {
      RunPlannerApp(projectId, numVersion, 0, employeeId, isReadOnly);
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="webSite">Сайт сервиса.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(string webSite, long projectId, int numVersion, long employeeId, bool isReadOnly)
    {
      RunPlannerApp(webSite, projectId, numVersion, 0, employeeId, isReadOnly);
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="activityId">ИД активити.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(long projectId, int numVersion, long activityId, long employeeId, bool isReadOnly)
    {
      var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      RunPlannerApp(webSite, projectId, numVersion, activityId, employeeId, isReadOnly);
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="webSite">Сайт сервиса.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="activityId">ИД активити.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(string webSite, long projectId, int numVersion, long activityId, long employeeId, bool isReadOnly)
    {
      if (webSite != null)
        GoToWebsite(webSite, projectId, numVersion, activityId, employeeId, isReadOnly);
      else
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Resources.WebClientURIEmptyError, MessageType.Warning);
    }
    
    /// <summary>
    /// Открытие клиента по планированию проектов.
    /// </summary>
    /// <param name="website">Адрес клиента</param>
    /// <param name="projectId">Id проекта</param>
    /// <param name="numVersion">Номер версии</param>
    /// <param name="activityId">Id этапа</param>
    /// <param name="userId">Id пользователя</param>
    /// <param name="isReadOnly">Только чтение</param>
    public static void GoToWebsite(string website, long projectId, int numVersion, long activityId, long userId, bool isReadOnly)
    {
      website = GetWebsiteLink(website, projectId, numVersion, activityId, userId, isReadOnly);
      Hyperlinks.Open(website);
    }
    
    /// <summary>
    ///  Собрать ссылку клиента планирования проентов из параметров.
    /// </summary>
    /// <param name="projectId">Id проекта</param>
    /// <param name="numVersion">Номер версии</param>
    /// <param name="activityId">Id этапа</param>
    /// <param name="userId">Id пользователя</param>
    /// <param name="isReadOnly">Только чтение</param>
    [Public]
    public static string GetWebsiteLink(long projectId, int numVersion, long activityId, long userId, bool isReadOnly)
    {
      var website = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      if (website == null)
      {
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Resources.WebClientURIEmptyError, MessageType.Warning);
      }
      return GetWebsiteLink(website, projectId, numVersion, activityId, userId, isReadOnly);
    }
    
    /// <summary>
    /// Собрать ссылку клиента планирования проентов из параметров.
    /// </summary>
    /// <param name="website">Адрес клиента</param>
    /// <param name="projectId">Id проекта</param>
    /// <param name="numVersion">Номер версии</param>
    /// <param name="activityId">Id этапа</param>
    /// <param name="userId">Id пользователя</param>
    /// <param name="isReadOnly">Только чтение</param>
    /// <returns>Ссылку на план</returns>
    [Public]
    public static string GetWebsiteLink(string website, long projectId, int numVersion, long activityId, long userId, bool isReadOnly)
    {
      // TODO: Отрефакторить с нормальным строителем запросов.
      var projectParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", projectId ));
      var readonlyParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  isReadOnly ? isReadOnly : IsReadOnlyProject(projectId, userId)));
      var numVersionParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  numVersion == 0 ? Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId).LastVersion.Number : numVersion));
      website = string.Format("{0}?projectId={1}&readonly={2}&numberVersion={3}", website.ToLower(), projectParam, readonlyParam, numVersionParam);
      
      if (activityId != 0)
      {
        var activityParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", activityId ));
        website = string.Format("{0}&activityId={1}", website, activityParam);
      }
      return website;
    }
    
    /// <summary>
    /// Показывает список задач по проекту.
    /// </summary>
    /// <param name="tasksIds">Список ИД задач по проекту.</param>
    [Public]
    public static void ShowProjectTasks(List<long> tasksIds)
    {
      var tasks = Functions.Module.Remote.GetTasksByIds(tasksIds);
      tasks.Show();
    }
    
    public static void ShowActivityOnProjectPlanFromActionParams(Sungero.Domain.Client.ExecuteActionArgs e, IProjectPlanRX projectPlan)
    {
      long activityId;
      int versionNumber;
      var activityIdParseSuccess = e.Params.TryGetValue(Constants.ProjectActivityTask.ActualActivityIdParamName, out activityId);
      var versionNumberParseSuccess = e.Params.TryGetValue(Constants.ProjectActivityTask.ActualActivityVersionNumberParamName, out versionNumber);
      
      if (activityIdParseSuccess)
      {
        DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(projectPlan.Id, versionNumberParseSuccess ? versionNumber : projectPlan.LastVersion.Number.Value, activityId, Users.Current.Id, true);
      }
    }
    
    public static void WriteActivityIdAndVersionToRefreshActionParams(Sungero.Presentation.FormRefreshEventArgs e, IProjectPlanRX projectPlan, long activityRefId)
    {
      long activityId;
      int versionNumber;
      
      if (!e.Params.TryGetValue(Constants.ProjectActivityTask.ActualActivityIdParamName, out activityId))
      {
        var activity = Functions.ProjectActivity.Remote.GetLatestActivityByPlanAndRefId(projectPlan, activityRefId);
        activityId = activity?.Id ?? 0;
        versionNumber = activity?.NumberVersion ?? 0;
        
        e.Params.AddOrUpdate(Constants.ProjectActivityTask.ActualActivityIdParamName, activityId);
        e.Params.AddOrUpdate(Constants.ProjectActivityTask.ActualActivityVersionNumberParamName, versionNumber);
      }
    }
    
    /// <summary>
    /// Проверка наличия лицензии на планирование проектов.
    /// </summary>
    /// <returns>True - наличие лицензии, False - отсутствие лицензии.</returns>
    [Public]
    public bool CheckProjectPlannerLicence()
    {
      return Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Constants.Module.ProjectPlannerModuleGuid);
    }
    
    /// <summary>
    /// Определение режима доступа к проекту.
    /// </summary>
    /// <param name="projectId">Id проекта</param>
    /// <param name="userId">Id пользователя</param>
    private static bool IsReadOnlyProject(long projectId, long userId)
    {
      var project = Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId);
      return !project.AccessRights.CanUpdate();
    }
    
    /// <summary>
    /// Снятие блокировки с карточки проекта.
    /// </summary>
    private static void Unlock(long projectId, long loginId)
    {
      var project = Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId);
      var lockInfo = Locks.GetLockInfo(project);
      if (lockInfo.LoginId == loginId && lockInfo.IsLocked)
        Locks.Unlock(project);
    }
    
  }
}
