using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanObsolete;
using DirRX.Planner.Model;
using net.sf.mpxj;
using net.sf.mpxj.mspdi;
using net.sf.mpxj.common;
using net.sf.mpxj.MpxjUtilities;
using net.sf.mpxj.reader;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectPlanRXFunctions
  {

    /// <summary>
    /// Показывает диалог с предложением сохранить данные в карточке плана проекта.
    /// </summary>       
    public bool SaveCardDialog()
    {
      var dialog = Dialogs.CreateTaskDialog(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.SaveChangesDialogText, MessageType.Question);
      var saveAndContinueButton = dialog.Buttons.AddCustom(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.SaveAndContinueDilogButtonText);
      dialog.Buttons.AddCancel();
      
      return dialog.Show() == saveAndContinueButton;
    }

    /// <summary>
    /// Конвертирует и экспортирует план проекта в формат MS Project.
    /// </summary>
    /// <param name="projectPlan">План проекта.</param>
    /// <param name="versionNumber">Номер версии.</param>
    public static void ExportToMSProject(IProjectPlanRX projectPlan, int versionNumber)
    {
      var projectPlanModel = GetVersionModelProjectPlan(projectPlan, versionNumber);
      ProjectFile msProject = new ProjectFile();
      ConvertProjectPlanToMSProject(projectPlanModel, msProject);
      ExportMSProject(msProject, msProject.ProjectProperties.ProjectTitle);
    }
 
    /// <summary>
    /// Собирает модель плана проекта из указанной версии.
    /// </summary>
    /// <param name="projectPlan">План проекта.</param>
    /// <param name="versionNumber">Номер версии.</param>
    /// <returns>Модель плана проекта.</returns>
    /// <exception cref="Exception">Если не удалось собрать модель плана проекта.</exception>
    public static DirRX.Planner.Model.Model GetVersionModelProjectPlan(IProjectPlanRX projectPlan, int versionNumber)
    {
      try
      {
        var jsonModel = string.Empty;
        
        using (var reader = new System.IO.StreamReader(projectPlan.Versions.Where(x => x.Number == versionNumber)
          .First()
          .Body
          .Read()))
        {
          jsonModel = reader.ReadToEnd();
        }
        
        var projectPlanModel = Newtonsoft.Json.JsonConvert.DeserializeObject<DirRX.Planner.Model.Model>(jsonModel);
        
        return projectPlanModel;
      }
      catch(Newtonsoft.Json.JsonReaderException ex)
      {
        throw new Exception(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.FailedDeserializationAttemptExceptionText);
      }  
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.GetVersionProjectPlanExceptionTextFormat(versionNumber, projectPlan.Id), ex);
      }
    }

    /// <summary>
    /// Экспортирует файл MS Project на клиенте.
    /// </summary>
    /// <param name="buffer">Массив байт.</param>
    public static void ExportMSProject(net.sf.mpxj.ProjectFile msProject, string projectName)
    {
      var memoryStream = new System.IO.MemoryStream();
      using(var memoryStreamWrapper = new net.sf.mpxj.MpxjUtilities.DotNetOutputStream(memoryStream))
      {
        MSPDIWriter writer = new MSPDIWriter();
        writer.MicrosoftProjectCompatibleOutput = true;
        
        writer.Write(msProject, memoryStreamWrapper);
        
        var document = CreateSimpleDocumentFromStream(memoryStream, projectName);
        document.Export();
        
        DeleteExportDocumentAsync(document.Id);
      }
    }
    
    private static Sungero.Docflow.ISimpleDocument CreateSimpleDocumentFromStream(System.IO.MemoryStream memoryStream, string projectName)
    {
      var document = Sungero.Docflow.SimpleDocuments.Create();
      document.Name = projectName;
      
      var simpleDocumentTypeGuid = Functions.ProjectPlanRX.Remote.GetSimpleDocumentTypeGuid();
      var simpleDocumentKind = Sungero.Docflow.DocumentKinds.GetAll(x => x.DocumentType.DocumentTypeGuid == simpleDocumentTypeGuid).FirstOrDefault();
      if (simpleDocumentKind != null)
      {
        document.DocumentKind = simpleDocumentKind;
      }
      
      document.CreateVersionFrom(memoryStream, Constants.ProjectPlanRX.MSProjectFormat);
      document.Save();
      
      return document;
    }
    
    private static void DeleteExportDocumentAsync(long documentId)
    {
      var deleteDocumentAsync = DirRX.ProjectPlanner.AsyncHandlers.DeleteExportDocumentAsync.Create();
      deleteDocumentAsync.documentId = documentId;
      deleteDocumentAsync.ExecuteAsync();
    }
     
    /// <summary>
    /// Конвертирует план проекта в формат MS Project.
    /// </summary>
    /// <param name="projectPlanModel">Модель плана проекта.</param>
    /// <param name="msProject">ProjectFile.</param>
    public static void ConvertProjectPlanToMSProject(DirRX.Planner.Model.Model projectPlanModel, net.sf.mpxj.ProjectFile msProject)
    {
      SetMSProjectProperties(msProject, projectPlanModel.Project);
      
      foreach (var activity in projectPlanModel.Activities)
      {
        if (IsInvalidActivity(activity))
        {
          Logger.Debug(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ConversionErrorTextFormat(activity.Id, projectPlanModel.Project.Id));
          continue;
        }
        
        var task = msProject.Tasks.Add();
        ConvertActivityToMSTask(activity, task);
      }
      
      foreach (var activity in projectPlanModel.Activities)
      {
        try
        {
          if (activity.Predecessors != null && activity.Predecessors.Count > 0)
          {
            CreateTaskPredecessors(msProject, activity);
          }
          
          if (activity.LeadActivityId != null)
          {
            MakeTaskChild(msProject, activity);
          }
        }
        catch (Exception ex)
        {
          Logger.Debug(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ConversionErrorTextFormat(activity.Id, projectPlanModel.Project.Id) +
            ex.Message);
        }
      }
    }
    
    private static void SetMSProjectProperties(ProjectFile msProject, DirRX.Planner.Model.Project projectModel)
    {
      var projectProperties = msProject.ProjectProperties;
      projectProperties.StartDate = projectModel.StartDate.ToJavaDate();
      projectProperties.FinishDate = projectModel.EndDate.ToJavaDate();
      projectProperties.ProjectTitle = projectModel.Name;
    }
    
    public static void ConvertActivityToMSTask(DirRX.Planner.Model.Activity activity, net.sf.mpxj.Task task)
    {
      //Kiselev_EM MSProject падает если в имени встречаются управляющие символы.
      task.Name = new string(activity.Name.Select(x => char.IsControl(x) ? ' ' : x).ToArray());
      
      //Kiselev_EM Сохраняем последовательность задач.
      //HACK Kiselev_EM Не работает перегрузка с integer. Пришлось явным образ вызвать перегрузку с аргументом string.
      task.ID = NumberHelper.getInteger(Convert.ToString(activity.SortIndex.Value));
      
      //HACK Kiselev_EM DateTime.ToJavaDate() некорректно формирует даты. Возможно, стоит разобраться почему.
      // Написал простую конвертацию даты в формат Java.
      task.Start = ConvertDateToJava(activity.StartDate.Value);
      task.ActualStart = task.Start;
      task.Finish = ConvertDateToJava(activity.EndDate.Value);
      task.ActualFinish = task.Finish;
      task.Duration = Duration.getInstance(CalculateDuration(activity.StartDate.Value, activity.EndDate.Value), TimeUnit.DAYS);
      task.Notes = activity.Note;
      
      if (activity.BaselineWork != null)
      {
        task.BaselineWork = Duration.getInstance(activity.BaselineWork.Value, TimeUnit.HOURS);
      }
      
      if (activity.ExecutionPercent != null)
      {
        task.PercentageComplete = NumberHelper.getDouble(activity.ExecutionPercent.Value);
      }
      
      if (activity.FactualCosts != null)
      {
        task.ActualCost = NumberHelper.getDouble(activity.FactualCosts.Value);
      }
      
      if (activity.Id != null)
      {
        task.UniqueID = NumberHelper.getInteger(Convert.ToString(activity.Id.Value));
      }
      
      if (activity.TypeActivity == DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone.ToString())
      {
        task.Milestone = true;
      }
      
      if (activity.Priority != null)
      {
        task.Priority = net.sf.mpxj.Priority.getInstance(activity.Priority.Value * 100);
      }
    }
    
    private static java.util.Date ConvertDateToJava(DateTime date)
    {
      //HACK Kiselev_EM Отсчет лет начинается с 1900 года, месяцы начинаются с 0-ля.
      return new java.util.Date(date.Year - 1900, date.Month - 1, date.Day);
    }
    
    private static int CalculateDuration(DateTime startDate, DateTime endDate)
    {
      if (startDate > endDate)
      {
        return 0;
      }
      
      TimeSpan delta = endDate - startDate;
      
      return delta.Days;
    }
    
    private static bool IsInvalidActivity(DirRX.Planner.Model.Activity activity)
    {
      return string.IsNullOrEmpty(activity.Name) ||
        activity.StartDate == null ||
        activity.EndDate == null;
    }
    
    private static void CreateTaskPredecessors(ProjectFile msProject, DirRX.Planner.Model.Activity activity)
    {
      try
      {
        var task = msProject.Tasks.ToIEnumerable<net.sf.mpxj.Task>().First(x => x.UniqueID.intValue() == activity.Id);
        
        foreach (var predecessor in activity.Predecessors)
        {
          var predecessorTask = msProject.Tasks.ToIEnumerable<net.sf.mpxj.Task>().First(x => x.UniqueID.intValue() == predecessor.Id);
          task.AddPredecessor(predecessorTask, GetTaskPredecessorLinkType(predecessor.LinkType), null);
        }
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.FailToCreateLinksText);
      }
    }
    
    private static net.sf.mpxj.RelationType GetTaskPredecessorLinkType(string linkType)
    {
      switch (linkType)
      {
        case "1":
          return net.sf.mpxj.RelationType.START_START;
        case "2":
          return net.sf.mpxj.RelationType.FINISH_FINISH;
        case "3":
          return net.sf.mpxj.RelationType.START_FINISH;
        default:
          return net.sf.mpxj.RelationType.FINISH_START;
      }
    }
    
    private static void MakeTaskChild(ProjectFile msProject, DirRX.Planner.Model.Activity activity)
    {
      try
      {
        var leadTask = msProject.Tasks.ToIEnumerable<net.sf.mpxj.Task>().First(x => x.UniqueID.intValue() == activity.LeadActivityId);
        var childTask = msProject.Tasks.ToIEnumerable<net.sf.mpxj.Task>().First(x => x.UniqueID.intValue() == activity.Id);
        
        leadTask.AddChildTask(childTask);
      }
      catch
      {
        throw new Exception(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.FailAddToLeadTaskText);
      }
    }
    
    public virtual void RestoreDefaultSettings()
    {
      if (!_obj.AccessRights.CanManageOrDelegate())
      {
        return;
      }
      
      Functions.ProjectPlanRX.Remote.RecreateDefaultSettingsWithoutSave(_obj);
    }
    
    public void CreateProjectFromFileDialog(int numVersion)
    {
      var dfile = Dialogs.CreateInputDialog(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.SelectFileDialogTitle);
      var file = dfile.AddFileSelect(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectFile, true);
      file.WithFilter(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectFiles, "rxpp");
      file.WithFilter(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.MsProject, "mpp");
      
      if (dfile.Show() == DialogButtons.Ok)
      {
        var model = string.Empty;
        
        if (string.IsNullOrEmpty(_obj.Name))
        {
          _obj.Name = System.IO.Path.GetFileNameWithoutExtension(file.Value.Name);
        }
        
        var ext = System.IO.Path.GetExtension(file.Value.Name).ToLower();
        if (ext == ".mpp")
        {
          model = Functions.ProjectPlanRX.GetModelFromMSProject(file);
        }
        else if (ext == ".rxpp")
        {
          model = System.Text.Encoding.UTF8.GetString(file.Value.Content);
        }
        else
        {
          Dialogs.ShowMessage(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ErrorSelectFile, MessageType.Error);
          return;
        }
        
        Functions.Module.Remote.SaveModelFromString(model, _obj, numVersion);
        DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(_obj.Id, _obj.LastVersion.Number.Value, Sungero.CoreEntities.Users.Current.Id, false);
      }
    }
    
		/// <summary>
		/// Создать модель проекта из проекта MS Project.
		/// </summary>
		/// <param name="file">Диалог выбора файла msProject</param>
		/// <returns>Модель</returns>
		[Public]
		public static string GetModelFromMSProject(CommonLibrary.IFileSelectDialogValue file)
		{
			var model = new Model();
			var reader = new UniversalProjectReader();
			ProjectFile msProject = null;

			using (var stream = new java.io.ByteArrayInputStream(file.Value.Content))
			{
				msProject = reader.Read(stream);
			}

			foreach (net.sf.mpxj.Task task in msProject.Tasks)
			{
				if (task.ID.intValue() == 0)
					model.Project = CreateProject(task);
				else
				{
				  var activity = CreateActivity(task, msProject);
				  if (activity != null)
				    model.Activities.Add(activity);
				}
			}

			model.LastActivityId = -1;
			model.NumberVersion = 0;

      return Newtonsoft.Json.JsonConvert.SerializeObject(model);
    }

		private static DirRX.Planner.Model.Activity CreateActivity(net.sf.mpxj.Task task, ProjectFile msProject)
		{
			var activity = new Activity();
			if (task.BaselineWork != null)
			  activity.BaselineWork = task.BaselineWork.Duration;
			activity.Name = task.Name;
			
			if (task.Name == null || task.Name == "" || task.Start == null || task.Finish == null)
			  return null;
			
			if (task.Start != null)
			 activity.StartDate = task.Start.ToDateTime();
			
			if (task.Finish != null)
			 activity.EndDate = task.Finish.ToDateTime().AddDays(1);
			
			if (task.PercentageComplete != null)
			 activity.ExecutionPercent = task.PercentageComplete.intValue();
			
			if (task.ActualCost != null)
			 activity.FactualCosts = task.ActualCost.intValue();
			
			activity.Id = task.ID.intValue();
			activity.Note = task.Notes;
		  activity.SortIndex = task.ID.intValue();
			activity.TypeActivity = GetTypeActivity(task);
			activity.LeadActivityId = GetLeadActivity(msProject, task);
			
			if (task.Predecessors != null)
			activity.Predecessors = task.Predecessors.ToIEnumerable<net.sf.mpxj.Relation>().Select(x => new Predecessor { LinkType = GetLinkType(x.Type.name()), Id = x.TargetTask.ID.intValue()}).ToList();
			
			if (task.Priority != null)
			activity.Priority = task.Priority.Value / 100;
			
			if (activity.ExecutionPercent != null)
			activity.Status = GetActivtyStatus(activity.ExecutionPercent.Value);
			
			return activity;
		}

		private static Project CreateProject(net.sf.mpxj.Task task)
		{
			var project = new Project();
			project.Name = task.Name;
			project.StartDate = task.Start.ToDateTime();
			project.EndDate = task.Finish.ToDateTime();
			project.BaselineWork = task.BaselineWork.Duration;
			project.ExecutionPercent = task.PercentageComplete.intValue();
			project.FactualCosts = task.ActualCost.intValue();
			project.Id = task.ID.intValue();
			project.Note = task.Notes;
			project.Stage = StageType.Initiation.ToString();

			return project;
		}

		private static int? GetLeadActivity(ProjectFile msProject, net.sf.mpxj.Task task)
		{
			int? result = null;

			foreach (net.sf.mpxj.Task t in msProject.Tasks)
			{
				if (t.ChildTasks.ToIEnumerable<net.sf.mpxj.Task>().Any(x => x.ID == task.ID) && t.ID.intValue() != 0)
				{
					result = t.ID.intValue();
					break;
				}
			}

			return result;
		}

		private static string GetTypeActivity(net.sf.mpxj.Task task)
		{
			var type = string.Empty;
			if (task.Milestone)
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone.ToString();
			else if (task.ChildTasks.ToIEnumerable<net.sf.mpxj.Task>().Any())
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section.ToString();
			else
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task.ToString();

			return type;
		}

		private static string GetLinkType(string msType)
		{
			var type = string.Empty;

			if (msType == "FINISH_START")
				type = "0";
			else if (msType == "START_START")
				type = "1";
			else if (msType == "FINISH_FINISH")
				type = "2";
			else if (msType == "START_FINISH")
				type = "3";
			return type;
		}

		private static DirRX.Planner.Model.ActivityStatus GetActivtyStatus(int persentComplite)
		{
			var status = new DirRX.Planner.Model.ActivityStatus();
			if (persentComplite == 100)
			{
				status.EnumValue = DirRX.ProjectPlanner.ProjectActivity.Status.Closed.ToString();
				status.LocalizeValue = DirRX.ProjectPlanner.ProjectActivities.Info.Properties.Status.GetLocalizedValue(DirRX.ProjectPlanner.ProjectActivity.Status.Closed);

			}
			else
			{
				status.EnumValue = DirRX.ProjectPlanner.ProjectActivity.Status.InWork.ToString();
				status.LocalizeValue = DirRX.ProjectPlanner.ProjectActivities.Info.Properties.Status.GetLocalizedValue(DirRX.ProjectPlanner.ProjectActivity.Status.InWork);
			}

			return status;
		}
  }
}
