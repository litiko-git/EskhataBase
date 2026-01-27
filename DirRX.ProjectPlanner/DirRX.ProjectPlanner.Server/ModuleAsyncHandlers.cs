using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Clients;
using PplanMessages;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void UpdateProjectTaskFactData(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.UpdateProjectTaskFactDataInvokeArgs args)
    {
      if (args.RetryIteration > 30)
      {
        args.Retry = false;
        Logger.ErrorFormat(
          "Не обновлены свойства \"% выполнения\", \"факт. трудоемкость\", \"факт. затраты\" для задачи с ИД={0}. Превышено количество попыток.",
          args.MainTaskId);
        return;
      }
      
      var mainTask = DirRX.ProjectPlanner.ProjectActivityTasks.GetAll(t => t.Id == args.MainTaskId).SingleOrDefault();
      if (mainTask == null)
      {
        return;
      }
      
      mainTask.ExecutionPercent = args.HasExecutionPercent ? args.ExecutionPercent : (int?)null;
      mainTask.ActualWorkload = args.HasActualWorkload ? args.ActualWorkload : (double?)null;
      mainTask.FactualCosts = args.HasFactualCosts ? args.FactualCosts : (double?)null;
      mainTask.Save();

      Functions.ProjectActivityTask.OnTaskFactChanged(mainTask, args.ActivityRefId);
    }
    
    public virtual void DeleteVersionAsyncHandler(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.DeleteVersionAsyncHandlerInvokeArgs args)
      {
      var plan = ProjectPlanRXes.GetAll(p => p.Id == args.PlanId).FirstOrDefault();
      
      if (plan == null)
      {
        Logger.ErrorFormat("Не удалось найти план с Id={0}", plan.Id);
        return;
      }
      
      Functions.Module.DeleteProjectActiviesByNumberVersion(plan.Id, args.NumberVersion);
      }
      
    public virtual void UpdateProjectAssignmentFactData(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.UpdateProjectAssignmentFactDataInvokeArgs args)
    {
      if (args.RetryIteration > 30)
      {
        args.Retry = false;
        Logger.ErrorFormat(
          "Не обновлены свойства \"% выполнения\", \"факт. трудоемкость\", \"факт. затраты\" для задания по этапу с ИД={0}. Превышено количество попыток.",
          args.TaskId);
        return;
      }
      
      var relatedAssignment = Assignments.GetAll(a => a.Task.Id == args.TaskId).FirstOrDefault();
      if (relatedAssignment == null)
      {
        return;
      }
      
      relatedAssignment.ExecutionPercent = args.ExecutionPercent;
      relatedAssignment.Save();
    }

    private List<long> StringToLongArray(string s)
    {
      if (string.IsNullOrEmpty(s))
      {
        return new List<long>();
      }
      return s.Split(';').Select(long.Parse).ToList();
    }
    
    public virtual void UpdateProjectActivityTasksAsync(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.UpdateProjectActivityTasksAsyncInvokeArgs args)
    {
      var updatedActivityRefIds = this.StringToLongArray(args.ListAllUpdatedActivityRefIds);
      var changedResponsibleActivityRefIds = this.StringToLongArray(args.ListChangedResponsibleActivityRefIds);
      var changedStartDateActivityRefIds = this.StringToLongArray(args.ListChangedStartDateActivityRefIds);
      
      var updatedProjectActivitiesByVersion = ProjectActivities.GetAll(pa => updatedActivityRefIds.Contains(pa.RefId.Value) &&
                                                                             pa.NumberVersion == args.NumberVersion &&
                                                                             pa.ProjectPlan != null && 
                                                                             pa.ProjectPlan.Id == args.ProjectPlanId);
      
      // получение refId этапов, где ответсвенный унаслендован
      var inheritedActivityRefIds = this.GetInheritedActivityRefIds(changedResponsibleActivityRefIds, args.NumberVersion);
      
      var sectionMilestoneRefIds = updatedProjectActivitiesByVersion
        .Where(pa => pa.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section ||
                     pa.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone)
        .Select(pa => pa.RefId);
      var activityRootTaskIds = ProjectActivityTasks.GetAll(t => t.MainTask.Id == t.Id &&
                                                            t.ActivityRefId != null &&
                                                            t.ProjectPlan != null &&
                                                            t.ProjectPlan.Id == args.ProjectPlanId)
        .GroupBy(g => g.ActivityRefId)
        .Select(v => v.Min(t => t.Id))
        .ToList();
      var allRootTasks = ProjectActivityTasks.GetAll(task => activityRootTaskIds.Contains(task.Id) &&
                                                             updatedProjectActivitiesByVersion.Select(pa => pa.RefId).Contains(task.ActivityRefId.Value) ||
                                                     inheritedActivityRefIds.Contains(task.ActivityRefId) &&
                                                     task.ProjectPlan != null &&
                                                     task.ProjectPlan.Id == args.ProjectPlanId);

      var allAbortTasks = Sungero.Workflow.Tasks.GetAll(task => allRootTasks.Contains(task.MainTask) &&
                                                                task.Status != Sungero.Workflow.Task.Status.Aborted &&
                                                                task.Status != Sungero.Workflow.Task.Status.Completed)
        .ToList();

      var startedByDate = updatedProjectActivitiesByVersion.Where(a => a.StartDate.HasValue && a.StartDate.Value.Date <= Calendar.Now.Date);
      var startedByPredecessors = this.GetActivatedByPredecessors(updatedProjectActivitiesByVersion, activityRootTaskIds);
      var restartRootTasks = allRootTasks.Where(task => 
          !sectionMilestoneRefIds.Contains(task.ActivityRefId) &&
          (startedByDate.Where(a => a.RefId == task.ActivityRefId).Any() ||
           startedByPredecessors.Where(a => a.RefId == task.ActivityRefId && !changedStartDateActivityRefIds.Contains(a.RefId.Value)).Any()))
        .ToList();

      // HACK: если после изменения этапа, у которого прекращена задача отправить заново вручную, происходит сохранение плана,
      // после которого происходит запуск АО по перезапуску задач, а затем отправка задачи вручную. 
      // И происходит так, что одновременно начинается гонка АО и запуска задачи вручную.
      // Поэтому не прекращаю задачи, которые стартовали после сохранения плана
      foreach(var task in allAbortTasks.Distinct().Where(t => t.Started < args.PlanSaveTime))
      {
        try
        {
          task.Abort();
        }
        catch(Exception ex)
        {
          Logger.Error(Resources.CouldNotStopProjectActivityTaskErrorFormat(task.Id, ex.Message));
        }
      }
      // HACK Yarovikov_GV Столкнулись с проблеммой, что задания по задаче иногда не прекращались.
      // В платформе подсказали, что это из-за слишком быстрого рестарта задачи. Добавил задержку.
      System.Threading.Thread.Sleep(2000);
      foreach(var rootTask in restartRootTasks.Distinct().Where(t => t.Started < args.PlanSaveTime))
      {
        try
        {
          rootTask.Restart();
          var isTaskValid = Functions.ProjectActivityTask.UpdateProjectActivityTaskFields(rootTask, false);
          
          if (isTaskValid)
          {
            rootTask.Start();
          }
        }
        catch(Exception ex)
        {
          Logger.Error(Resources.CouldNotUpdateAndRestartProjectActivityTaskErrorFormat(rootTask.Id, ex.Message));
        }
      }

    }

    public virtual void LaunchTasksByPlanLastVersionAsync(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.LaunchTasksByPlanLastVersionAsyncInvokeArgs args)
    {
      var planId = args.PlanId;
      var versionNumber = args.LastVersionNumber;
      
      var activityRootTaskIds = ProjectActivityTasks.GetAll(t => t.MainTask.Id == t.Id &&
                                                            t.ActivityRefId != null &&
                                                            t.ProjectPlan != null &&
                                                            t.ProjectPlan.Id == planId)
        .GroupBy(g => g.ActivityRefId)
        .Select(v => v.Min(t => t.Id))
        .ToList();
      
      var activitityWithRootTaskRefIds = ProjectActivityTasks.GetAll(t => activityRootTaskIds.Contains(t.Id))
        .Select(pa => pa.ActivityRefId);
      var activitiesFromLastVersion = ProjectActivities.GetAll(pa => !activitityWithRootTaskRefIds.Contains(pa.RefId) &&
                                                               pa.ProjectPlan != null &&
                                                               pa.ProjectPlan.Id == planId &&
                                                               pa.NumberVersion == versionNumber &&
                                                               pa.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task &&
                                                               pa.Status != DirRX.ProjectPlanner.ProjectActivity.Status.Closed &&
                                                               pa.StartDate >= Calendar.Today);
      
      var activitiesWithTodayStartDate = activitiesFromLastVersion.Where(pa => pa.StartDate.Value.Date == Calendar.Today).ToList();
      var activitiesWithCompletedPredecessors = this.GetActivatedByPredecessors(activitiesFromLastVersion, activityRootTaskIds);
      var activitiesForTasks = activitiesWithTodayStartDate.Union(activitiesWithCompletedPredecessors).ToList();
      
      foreach(var activity in activitiesForTasks)
      {
        try
        {
          // Передаем дополнительно поля из Этапа явно, потому что в других местах они могут приходить в метод извне.
          var task = Functions.Module.CreateProjectActivityTask(activity, activity.ExecutionPercent, activity.ActualWorkload, activity.FactualCosts, false);
      
          if (task != null)
          {
            task.Start();
          }
        }
        catch(Exception ex)
        {
          Logger.Error(Resources.CouldNotCreateAndStartProjectActivityTaskErrorFormat(activity.Id, ex.Message));
        }
      }
    }

    private IQueryable<IProjectActivity> GetActivatedByPredecessors(IQueryable<IProjectActivity> baseQuery, List<long> activityRootTaskIds)
    {
      var predecessorRefIds = baseQuery.SelectMany(a => a.Predecessors.Where(p => p.LinkType == Constants.ProjectActivity.LinkType.FS ||
                                                                                             p.LinkType == Constants.ProjectActivity.LinkType.SS)
                                                                   .Select(p => p.Activity.RefId));
      
      var predecessorRootTasks = ProjectActivityTasks.GetAll(t => activityRootTaskIds.Contains(t.Id) &&
        predecessorRefIds.Contains(t.ActivityRefId));
      
      var predecessorWithRootTaskRefIds = new HashSet<long>(predecessorRootTasks.Select(x => x.ActivityRefId.Value).Distinct());
      
      var predecessorWithCompletedRootTaskRefIds = new HashSet<long>(predecessorRootTasks.Where(t => t.Status == DirRX.ProjectPlanner.ProjectActivityTask.Status.Completed)
                                                                    .Select(x => x.ActivityRefId.Value)
                                                                    .Distinct());
      
      var activitiesWithCompletedPredecessors = baseQuery.Where(a => a.Predecessors.Any() &&
                                                                                (a.Predecessors.Any(p =>
                                                                                                    p.LinkType == Constants.ProjectActivity.LinkType.FS ||
                                                                                                    p.LinkType == Constants.ProjectActivity.LinkType.SS) ?
                                                                                 a.Predecessors.All(p =>
                                                                                                    p.Activity.RefId.HasValue &&
                                                                                                    (p.LinkType == Constants.ProjectActivity.LinkType.FS &&
                                                                                                     predecessorWithCompletedRootTaskRefIds.Contains(p.Activity.RefId.Value)) ||
                                                                                                    (p.LinkType == Constants.ProjectActivity.LinkType.SS &&
                                                                                                     predecessorWithRootTaskRefIds.Contains(p.Activity.RefId.Value)) ||
                                                                                                    p.LinkType == Constants.ProjectActivity.LinkType.FF ||
                                                                                                    p.LinkType == Constants.ProjectActivity.LinkType.SF) :
                                                                                 false)
                                                                               );
      return activitiesWithCompletedPredecessors;
    }
      
    /// <summary>
    /// Получить ссылочные ид этапов с унаследованным ответсвенным из обновленных этапов.
    /// </summary>
    /// <param name="changedIds">Ссылочные ид обновленных этапов.</param>
    /// <param name="numberVersion">Номер версии.</param>
    /// <returns>Ссылочные ид этапов.</returns>
    private List<long?> GetInheritedActivityRefIds(List<long> changedRefIds, int numberVersion)
    {
      var activitiyRefIds = new List<long?>();
      
      if (!changedRefIds.Any())
      {
        return activitiyRefIds;
      }
      
      using (var connection = SQL.CreateConnection())
      using (var command = connection.CreateCommand())
        {
        command.CommandText = Queries.Module.GetActivitiesWithInheritedResponsibles;
          
        // AddArrayParameter работает нестабильно, поэтому коллекция changedActivityRefIds не должна быть пустой,
        // чтобы параметр IN(@changedActivityRefIds) в результате не привел к строке IN()
        SQL.AddArrayParameter(command, "@changedActivityRefIds", changedRefIds, System.Data.DbType.Int64);
        SQL.AddParameter(command, "@numberVersion", numberVersion, System.Data.DbType.Int32);
        using (var reader = command.ExecuteReader())
          {
          while (reader.Read())
        {
            activitiyRefIds.Add((long)reader[0]);
        }
      }
      }
      
      return activitiyRefIds;
    }

    public virtual void DeleteExportDocumentAsync(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.DeleteExportDocumentAsyncInvokeArgs args)
    {
      if (args.RetryIteration < 1)
      {
        args.Retry = true;
        return;
      }
      
      try
      {
        var document = Sungero.Docflow.SimpleDocuments.Get(args.documentId);
        Sungero.Docflow.SimpleDocuments.Delete(document);
      }
      finally
      {
        args.Retry = false;
      }
    }

    public virtual void GeneratePlanBody(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.GeneratePlanBodyInvokeArgs args)
    {
      if (args.RetryIteration > 20)
      {
        args.Retry = false;
        return;
      }
      Logger.Debug("Init: rebuild project models.");
      foreach(var projectPlan in ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false))
      {
        var planLockInfo = Locks.GetLockInfo(projectPlan);
			  if (projectPlan.LastVersion != null && planLockInfo != null && !planLockInfo.IsLockedByOther)
			  {
			    try
			    {
			      Transactions.Execute(() => {
			      foreach (var version in projectPlan.Versions) 
			      {
			        if (Locks.GetLockInfo(version.Body).IsLockedByOther || Locks.GetLockInfo(version.PublicBody).IsLockedByOther)
			        {
			          Logger.DebugFormat("Тело версии {0} плана проекта {1} заблокировано другим пользователем системы. перегенерация тела пропущена.", version.Number.ToString(), projectPlan.Name);
			          args.Retry = true;
			          continue;
			        }
			        
			        var versionApproved = Signatures.Get(version)
			          .Where(s => s.SignCertificate != null).Where(s => s.SignatureType == SignatureType.Approval).Any();
			           DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(projectPlan, version.Number.Value, versionApproved);
		          
			      }
	         projectPlan.BodyConverted = true;
			     projectPlan.Save();
			     });
	         Logger.DebugFormat("Конвертация плана проекта {0} успешна.", projectPlan.Name);
			    }
			    catch(Exception ex)
			    {
			      Logger.ErrorFormat("Перегенерация плана проекта Id = \"{0}\" завершилась с ошибкой: {1}{2}{3}", projectPlan.Id, ex.Message, Environment.NewLine, ex.StackTrace);
	          args.Retry = true;
	          continue;
			    }
			  }
			  else if (planLockInfo == null || planLockInfo.IsLockedByOther)
			  {
			     args.Retry = true;
			     Logger.DebugFormat("Конвертация плана проекта {0} пропущена из-за блокировки.", projectPlan.Name);
			  }
      }
      
      var notConvertedPlans = ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false).Select(p => p.Name).ToArray();
      
      if (notConvertedPlans.Any())
      {
        Logger.DebugFormat("Конвертация планов проекта завершена. Не сконвертированы планы: {0}", string.Join("; ", notConvertedPlans));
      }
      else
      {
        Logger.Debug("Конвертация планов проекта завершена. Все планы успешно сконвертированы.");
      }
      
    }

    public virtual void ConvertPlanAsync(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.ConvertPlanAsyncInvokeArgs args)
    {
      if (args.RetryIteration > 30)
      {
        args.Retry = false;
        StartRebuildBody();
        return;
      }
      
      var olds = ProjectPlanObsoletes.GetAll().ToList();
      var needsToRebuild = ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false);
      if (olds.Count == 0 && needsToRebuild.Any())
      {
        StartRebuildBody();
        args.Retry = false;
        return;
      }
      
      try 
      {
        foreach (var oldPlan in olds)
        {
          var lockPlanInfo = Locks.GetLockInfo(oldPlan);
          if(lockPlanInfo.IsLockedByOther)
          {
            Logger.DebugFormat(DirRX.ProjectPlanner.Resources.CannotConvertFormat(oldPlan.Id, lockPlanInfo.OwnerName));
            args.Retry = true;
            continue;
          }
          else
          {
            try 
            {
              Transactions.Execute( () => 
                                   {
              var edoc = ProjectPlanObsoletes.Get(oldPlan.Id);
              var convertedPlan = edoc.ConvertTo(ProjectPlanRXes.Info);
              convertedPlan.Save();
              
              var newPlan = ProjectPlanRXes.Get(edoc.Id);
              newPlan.DocumentKind = Sungero.Docflow.DocumentKinds.GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString()).FirstOrDefault();
              newPlan.BodyConverted = false;
              newPlan.Save();
                                   });
              
            }
            catch (Exception ex) 
            {
              Logger.DebugFormat(DirRX.ProjectPlanner.Resources.CouldNotConvertProjectPlanFormat(oldPlan.Id), ex.ToString());
              args.Retry = true;
            }
          }
        }
        
        using (var command = SQL.GetCurrentConnection().CreateCommand())
        {
          Logger.Debug("Execute restore plan links query");
          command.CommandText = Queries.Module.RestorePlanLinks;
          command.ExecuteNonQuery();
        }
        
        if (!args.PlansGenerated) 
        {
          StartRebuildBody();
        }
        
      }
      catch(Exception ex) 
      {
        Logger.Error(ex + "Произошла ошибка при конвертации планов проекта");
        args.Retry = true;
      }
      finally
      {
        StartRebuildBody();
      }
      args.RetryIteration++;
    }
    
    private static void StartRebuildBody() 
    {
      var handler = new ProjectPlanner.AsyncHandlers.GeneratePlanBody();
      handler.ExecuteAsync();
    }
  }
}
