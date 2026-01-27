using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.Metadata;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Domain.Clients;
using Sungero.Projects.ProjectTeamMembers;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI.NotifyConditionItem;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;

namespace DirRX.TeamsCommonAPI.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить сущности.
    /// </summary>
    /// <param name="request">Запрос, содержащий информацию о сущностях.</param>
    /// <returns>Json сущностей.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public string GetEntities(Structures.Module.IEntityCollectionRequest request)
    {
      var response = Structures.Module.EntityCollectionResponse.Create();
      response.Entities = new List<DirRX.TeamsCommonAPI.Structures.Module.IEntityResponse>();
      
      using (var session = new Sungero.Domain.Session())
      {
        foreach(var entityRequest in request.EntityCollection)
        {
          IEntity entity;
          Guid guid;
          
          var entityResponse = Structures.Module.EntityResponse.Create();
          response.Entities.Add(entityResponse);
          entityResponse.Properties = new System.Collections.Generic.Dictionary<string, DirRX.TeamsCommonAPI.Structures.Module.IProperty>();
          
          entityResponse.Guid = entityRequest.Guid;
          entityResponse.Id = entityRequest.Id;
          entityResponse.IsUnauthorized = false;
          entityResponse.IsIncludeBinaryValues = entityRequest.IsIncludeBinaryValues;
          entityResponse.IsError = false;
          
          if(!Guid.TryParse(entityRequest.Guid, out guid))
          {
            entityResponse.IsError = true;
            entityResponse.ErrorCause = Constants.Module.IncorrectGuidFormat;
            Logger.Error(Resources.IncorrectGuidFormat(entityRequest.Guid));
            
            continue;
          }
          
          var type = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(guid);
          
          if(type == null)
          {
            entityResponse.IsError = true;
            entityResponse.ErrorCause = Constants.Module.TypeByGuidNotFound;
            Logger.Error(Resources.TypeByGuidNotFoundFormat(guid));
            
            continue;
          }
          
          try
          {
            entity = session.Get(type, entityRequest.Id);
            
            foreach(var propertyMetadata in type.GetEntityMetadata().Properties)
            {
              var property = GetProperty(propertyMetadata, entity, entityRequest.IsIncludeBinaryValues);
              entityResponse.Properties.Add(propertyMetadata.Name, property);
            }
          }
          catch(Sungero.Domain.Shared.Exceptions.SecuritySystemException ex)
          {
            entityResponse.IsUnauthorized = true;
          }
          catch(Sungero.Domain.Shared.Exceptions.ObjectNotFoundException ex)
          {
            entityResponse.IsError = true;
            entityResponse.ErrorCause = Constants.Module.EntityNotFound;
            Logger.Error(Resources.EntityNotFoundFormat(guid, entityRequest.Id), ex);
            
            continue;
          }
        }
      }
      return Newtonsoft.Json.JsonConvert.SerializeObject(response);
    }

    [Public]
    public void ConsolidatedTaskComplemention(ITeamsTask task)
    {
      if (task.IsConsolidatedTaskFinded.HasValue && task.IsConsolidatedTaskFinded.Value)
      {
        return;
      }
      
      var rootTask = GetRootTask(task);
               
      if (rootTask == null)
      {
        Logger.DebugFormat("Не удалось найти rootTask для задачи с id {0}", task.Id);
        return;
      }
      
      task.IsConsolidatedTaskFinded = true;
      var noticeItem = rootTask.NotifiesCollection.AddNew();
      noticeItem.TimeStamp = Calendar.Now;
      
      var settingsId = task.SettingsId;
      
      if (settingsId == null)
      {
        settingsId = 0;
      }
      
      var setting = TeamsNoticesSettingses.GetAll(s => s.Id == (settingsId)).FirstOrDefault();
      if (setting == null)
      {
        Logger.ErrorFormat("Не удалось найти связанную настройку с ID {0}. Guid решения {1}, ID {2}", settingsId, task.SolutionGuid, task.EntityId);
        return;
      }
      var conditionEventType = NotifyEventTypes.GetAll(c => c.EventType == setting.EventType.EventType).FirstOrDefault();
      
      string conditionText = "";
      if (conditionEventType != null)
      {
        conditionText = conditionEventType.NotifyText;
      }
      
      if (task.Attachments.Any() && !string.IsNullOrEmpty(conditionText))
      {
        noticeItem.NotifyText = string.Format(conditionText, task.SubjectParam0, task.SubjectParam1);
      }
      noticeItem.GuidType = rootTask.SolutionGuid;
      noticeItem.RootEntityHyperlink = Hyperlinks.Get(task.Attachments.LastOrDefault());
      
      foreach (var responsible in task.Responsibles)
      {
        if (!rootTask.Responsibles.Any(r => r.Responsible.Id == responsible.Responsible.Id))
        {
          var resp = rootTask.Responsibles.AddNew();
          resp.Responsible = responsible.Responsible;
        }
      }
      
      foreach (var attachment in task.Attachments)
      {
        try
        {
          rootTask.Attachments.Add(attachment);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Ошибка добавления вложения в задачу. {0}", ex);
        }
      }
      try
      {
        task.Save();
        rootTask.Save();
      }
      catch(Exception ex)
      {
        Logger.DebugFormat("Ошибка при сохранении сущностей: {0}", ex);
      }
    }
    
    /// <summary>
    /// Метод ищет получателей, в текущей реализации, только среди сотрудников.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.Module.ISearchRecipientsResult SearchRecipients(string searchValue)
    {
      try
      {
        if (String.IsNullOrWhiteSpace(searchValue)) {
          return Structures.Module.SearchRecipientsResult.Create();
        }
        return this.HandleSearchRecipients(searchValue);
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.TeamsCommonAPI.Resources.SearchRecipientsExceptionFormat(searchValue), ex);
      }
    }
    
    [Public]
    public void RaiseNotify(object noticeItem)
    {
      var noticeItemConverted = noticeItem as RNDNoticesUtils.Structures.NoticeItem;
      
      if (noticeItemConverted == null)
      {
        return;
      }
      
      RaiseConditionNotify(noticeItemConverted);
    }
    
    private void RaiseStandartNotice(List<IRecipient> recipients,
      RNDNoticesUtils.Structures.NoticeItem noticeItem,
     ITeamsNoticesSettingsRecipients recipientSettings)
    {
      var task = TeamsTasks.Create();
      this.ConfigureTask(task, recipientSettings.NotifyCondition, noticeItem);
      
      ConfigureTaskSubject(task, Condition.StandartNotice, noticeItem);
      if (!TryConfigureResponsibles(task, recipients, noticeItem))
      {
        return;
      }
      
      task.Save();
      //сохранение задачи с вложениями триггерит метод сохранения вложений, приводит к зацикливанию.
      AddAttcachments(task, noticeItem);
      var rootEntity = task.Attachments.FirstOrDefault(a => a.Id != task.EntityId);
      if (rootEntity != null)
      {
        task.RootEntityId = rootEntity.Id;
      }
      task.Start();
    }
    
    private void ConfigureTask(ITeamsTask task,
      DirRX.TeamsCommonAPI.INotifyConditionItem notifyCondition,
      RNDNoticesUtils.Structures.NoticeItem noticeItem)
    {
      task.Condition = NotifyConditionItems.GetAll(c => c.Condition == notifyCondition.Condition.Value).FirstOrDefault();
      task.IsRootTaskForConsolidated = false;
      task.IsDelayed = false;
      task.SettingsId = noticeItem.Settings.Id;
      task.EntityHyperlink = Hyperlinks.Get(noticeItem.MainEntity);
      task.SolutionGuid = noticeItem.SolutionGuid;
      task.ClientHyperlink = noticeItem.ModuleWebsite;
      task.MaxDeadline = noticeItem.TaskDeadLine;
      task.EntityId = noticeItem.MainEntity.Id;
    }
    
    private void RaiseCollectiveNotice(List<IRecipient> recipients, 
                                       RNDNoticesUtils.Structures.NoticeItem noticeItem,
                                       DirRX.TeamsCommonAPI.ITeamsNoticesSettingsRecipients recipientSettings)
    {
      var task = TeamsTasks.Create();
      
      if (!TryConfigureResponsibles(task, recipients, noticeItem))
      {
        return;
      }
      
      this.ConfigureCollectiveTaskNotice(task, recipientSettings.NotifyCondition, noticeItem);
      ConfigureTaskSubject(task, Condition.Consolidated, noticeItem);
      
      task.Save();
      //сохранение задачи с вложениями триггерит метод сохранения вложений, приводит к зацикливанию.
      AddAttcachments(task, noticeItem);
      if (task.EntityId != null)
      {
        var taskEntity = task.EntityId;
        var rootEntity = task.Attachments.FirstOrDefault(a => noticeItem.MainEntity.DisplayValue != a.DisplayValue || a.Id != noticeItem.MainEntity.Id);
        if (rootEntity != null)
        {
          task.RootEntityId = rootEntity.Id;
        }
      }
      
      
      
      if (!(task.IsRootTaskForConsolidated.HasValue && task.IsRootTaskForConsolidated.Value))
      {
        task.Start();
      }
    }
    
    private void ConfigureCollectiveTaskNotice(ITeamsTask task,
      INotifyConditionItem notifyCondition,
      RNDNoticesUtils.Structures.NoticeItem noticeItem)
    {
      task.Condition = NotifyConditionItems.GetAll(c => c.Condition == notifyCondition.Condition.Value).FirstOrDefault();
      task.SettingsId = noticeItem.Settings.Id;
      task.EntityHyperlink = Hyperlinks.Get(noticeItem.MainEntity);
      task.EntityId = noticeItem.MainEntity.Id;

      if (noticeItem.Attachments.Any())
      {
        task.RootEntityId = noticeItem.Attachments.LastOrDefault().Id;
      }
      
      task.IsRootTaskForConsolidated = this.IsRootTaskForCollective(task);
      task.IsDelayed = task.IsRootTaskForConsolidated;
      task.SolutionGuid = noticeItem.SolutionGuid;
      task.ClientHyperlink = noticeItem.ModuleWebsite;
      task.MaxDeadline = noticeItem.TaskDeadLine;
      task.PlannedNotifyData = task.MaxDeadline;
      
      if (task.IsRootTaskForConsolidated.HasValue && task.IsRootTaskForConsolidated.Value)
      {
        var notify = task.NotifiesCollection.AddNew();
        notify.TimeStamp = Calendar.Now;
        notify.RootEntityHyperlink = Hyperlinks.Get(noticeItem.Attachments.FirstOrDefault(a => a != noticeItem.MainEntity));
        notify.GuidType = task.SolutionGuid;
        
        var conditionEvent = NotifyEventTypes.GetAll(c => c.EventType == noticeItem.Settings.EventType.EventType).FirstOrDefault();
        
        string conditionText = null;
        
        if (conditionEvent != null)
        {
          conditionText = conditionEvent.NotifyText;
        }
        
        if (!string.IsNullOrEmpty(noticeItem.FirstSubjectParam) && !string.IsNullOrEmpty(conditionText))
        {
          notify.NotifyText = string.Format(conditionText, noticeItem.FirstSubjectParam,  noticeItem.SecondarySubjectParam);
        }
      }
      
    }
    
    [Public]
    public ITeamsTask GetRootTask(ITeamsTask task)
    {
      var responsibleIds = task.Responsibles.Select(r => r.Responsible.Id);
      
      return TeamsTasks.GetAll(t => 
                                       t.IsDelayed.HasValue 
                                       && t.IsDelayed.Value 
                                       && t.IsRootTaskForConsolidated.HasValue 
                                       && t.IsRootTaskForConsolidated.Value 
                                       && t.Id != task.MainTaskId 
                                       && t.Status == Sungero.Workflow.Task.Status.Draft
                                       && t.Responsibles.All(rt => responsibleIds.Contains(rt.Responsible.Id))
                                      )
               .OrderByDescending(t => t.Id)
               .FirstOrDefault();
    }
    
    private bool IsRootTaskForCollective(ITeamsTask task)
    {
      return GetRootTask(task) == null;
    }
    
    private void RaiseConditionNotify(RNDNoticesUtils.Structures.NoticeItem noticeItem)
    { 
      foreach (var recipientSettings in noticeItem.Settings.Recipients)
      {
        if(recipientSettings.Recipient == null || (Sungero.Company.Employees.Current != null && recipientSettings.Recipient.Contains(Sungero.Company.Employees.Current.Id.ToString())))
        {    
          continue;
        }
        
        var deserializedRecipientIds = (List<long>)Newtonsoft.Json.JsonConvert.DeserializeObject(recipientSettings.Recipient, typeof(List<long>));
        List<IRecipient> recipients = new List<IRecipient>();
        AccessRights.AllowRead(() => recipients = Recipients.GetAll(x => deserializedRecipientIds.Contains(x.Id)).ToList());

        if (Sungero.Company.Employees.Current != null && recipientSettings.Recipient.Contains(Sungero.Company.Employees.Current.Id.ToString()))
        {
          continue;
        }
        
        if(!TryGetRecipientsWithRightsOnAttachments(noticeItem.Attachments, ref recipients))
        {
          continue;
        }
        
        if (recipientSettings.NotifyCondition.Condition.Value == Condition.StandartNotice)
        {
          AccessRights.AllowRead(() => RaiseStandartNotice(recipients, noticeItem, recipientSettings));
        }
        
        if (recipientSettings.NotifyCondition.Condition.Value == Condition.Consolidated)
        {
          AccessRights.AllowRead(() => RaiseCollectiveNotice(recipients, noticeItem, recipientSettings));
        }
      }
    }
    
    private bool TryGetRecipientsWithRightsOnAttachments(List<Sungero.Domain.Entity> attachments, ref List<IRecipient> recipients)
    {
      bool result = true;
      
      foreach(var recipient in recipients.ToList())
      {
        if(!attachments.All(a => a.AccessRights.CanRead(recipient)))
        {     
          recipients.Remove(recipient);
        } 
      }
      
      if(!recipients.Any())
      {
        result = false;
      }
      
      return result;
    }
    
    private void ConfigureTaskSubject(ITeamsTask task,
      Sungero.Core.Enumeration notifyType,
      RNDNoticesUtils.Structures.NoticeItem noticeItem)
    {
      
      var firstParamEntity = noticeItem.Attachments.Where(a => Hyperlinks.Get(a) == noticeItem.FirstSubjectParam).FirstOrDefault();
      
      string secondaryParamText = "";
      
      if (!string.IsNullOrEmpty(noticeItem.SecondarySubjectParam))
      {
        var secondaryParamEntity = noticeItem.Attachments.Where(a => Hyperlinks.Get(a) == noticeItem.SecondarySubjectParam).FirstOrDefault();
        
        if (secondaryParamEntity != null)
        {
          secondaryParamText = secondaryParamEntity.DisplayValue;
        }
        else
        {
          secondaryParamText = noticeItem.SecondarySubjectParam;
        }
      }
      
      if (noticeItem.Attachments.Count == 1 && noticeItem.MainEntity != null)
      {
        task.Subject = string.Format(noticeItem.Settings.EventType.NotifyText, firstParamEntity.DisplayValue);
      }
      else if (noticeItem.Attachments.Count == 2 && noticeItem.MainEntity != null)
      {
        task.Subject = string.Format(
          noticeItem.Settings.EventType.NotifyText,
          firstParamEntity.DisplayValue,
          secondaryParamText);
      }
      
      task.SubjectParam0 = noticeItem.FirstSubjectParam;
      task.SubjectParam1 = noticeItem.SecondarySubjectParam;
      task.SubjectParamOptional = noticeItem.OptionalSubjectParam;
      
      if (task.Condition.Condition == Condition.Consolidated && task.IsRootTaskForConsolidated.HasValue && task.IsRootTaskForConsolidated.Value)
      {
        task.Subject = DirRX.TeamsCommonAPI.Resources.CollectiveTaskSubjectFormat(Calendar.Today.ToString("dd MMMM yyy ") + DirRX.TeamsCommonAPI.Resources.Year);
      }
      
      //Kiselev_EM: Обрезать заголовок задачи до 250 символов.
      //http://aura.npo-comp.ru/sungero?type=7197cc31-bf64-406e-8ead-0a7dde1c3c6b&id=134965
      if (task.Subject.Length > Constants.Module.TaskTitleMaxLength)
      {
        task.Subject = task.Subject.Substring(0, Constants.Module.TaskTitleMaxLength);
      }
    }
    
    private bool TryConfigureResponsibles(ITeamsTask task,
      List<IRecipient> recipients,
      RNDNoticesUtils.Structures.NoticeItem noticeItem)
    {
      
      if (!recipients.Any())
      {
        return false;
      }
      
      foreach (var recipient in recipients)
      {
          var responsible = task.Responsibles.AddNew();
          responsible.Responsible = recipient;
      }
      
      return true;
    }
    
    private void AddAttcachments(ITeamsTask task, RNDNoticesUtils.Structures.NoticeItem noticeItem)
    {
      foreach (var attachment in noticeItem.Attachments)
      {
        task.Attachments.Add(attachment);
      }
    }
    
    [Public]
    public void ConfigureCollectiveTask(ITeamsNoticesSettings settings, ITeamsTask task)
    {
      if (settings.SolutionIdentifier == SolutionIdentifier.ProjectPlanner)
      {
        task.IsRootTaskForConsolidated = !TeamsTasks.GetAll(t => t.SettingsId == settings.Id && t.IsRootTaskForConsolidated.HasValue && t.IsRootTaskForConsolidated.Value && t.Id != task.Id).Any();
        task.IsDelayed = !task.IsRootTaskForConsolidated;
        task.SettingsId = settings.Id;
        task.Save();
      }
    }
    
    
    [Public]
    public List<IRecipient> GetTaskUsers(DirRX.TeamsCommonAPI.ITeamsTask task)
    {
      if (task == null)
      {
        return null;
      }
      
      return task.Responsibles.Select(r => r.Responsible).ToList();
    }
    
    [Public(WebApiRequestType = RequestType.Post)]
    public void SetAsRead(long noticeId)
    {
      var notice = TeamsNotices.Get(noticeId);
      
      notice.IsRead = true;
      notice.Save();
      
      var clientIds = GetTaskUsers(notice.Task as DirRX.TeamsCommonAPI.ITeamsTask)
        .SelectMany(user => ClientManager.Instance.GetClientsOfUser(user.Id))
        .Distinct()
        .ToArray();
      
      RNDNoticesUtils.Messages.NotifySender.SendNotifyAckedMessage(clientIds, "TEST_APP_ID", notice.Id.ToString());
    }
    
    [Public(WebApiRequestType = RequestType.Post)]
    public void SetAllAsRead()
    {
      if (Sungero.CoreEntities.Users.Current == null)
      {
        return;
      }
      
      var notices = TeamsNotices.GetAll(n => n.Performer != null && n.Performer.Id == Sungero.CoreEntities.Users.Current.Id).ToList();
      ChangeIsRead(notices, true);
      
      List<Guid> clientIds = new List<Guid>();
      
      foreach (var notice in notices)
      {
        var clientId = ClientManager.Instance.GetClientsOfUser(notice.Performer.Id)
        .Distinct()
        .ToArray();
        clientIds.AddRange(clientId);
      }
      
      RNDNoticesUtils.Messages.NotifySender.SendAllAckedMessage(clientIds.ToArray(), "TEST_APP_ID");
    }
    
    
    private static void ChangeIsRead(IEnumerable<ITeamsNotice> entities, bool isRead)
    {
      using (var session = new Sungero.Domain.Session())
      {
        foreach (var entity in entities)
        {
          if (entity.IsRead != isRead && entity.AccessRights.CanUpdate())
          {
            entity.IsRead = isRead;
            session.Update(entity);
          }
        }
        session.SubmitChanges();
      }
    }
    
    /// <summary>
    /// Получение аватара пользователя.
    /// </summary>
    /// <param name="userId">Ид пользователя.</param>
    /// <returns>Миниатюра фотографии.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public byte[] GetThumbnail(long userId)
    {
      var sizePersonalPhotoThumbnail = Constants.Module.SizePersonalPhotoThumbnail;
      return Users.Get(userId).GetPersonalPhotoThumbnail(sizePersonalPhotoThumbnail);
    }
    
    private List<Structures.Module.IUserThumbnailDto> GetThumbnailsFromCache(List<long> userIds)
    {
      var assemblyServicesShared = System.AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("Sungero.Services.Shared")).FirstOrDefault();
      var assemblyCoreEntitiesServer = System.AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.Contains("Sungero.CoreEntities.Server")).FirstOrDefault();
      
      if(assemblyServicesShared == null)
      {
        throw new Exception(string.Format(Resources.ModuleNotFoundFormat("Sungero.Services.Shared")));
      }
      
      if(assemblyCoreEntitiesServer == null)
      {
        throw new Exception(string.Format(Resources.ModuleNotFoundFormat("Sungero.CoreEntities.Server")));
      }
      
      var typeThumbnailInfo = assemblyServicesShared.GetType("Sungero.Services.Shared.ThumbnailInfo");
      var thumbnailInfoId = typeThumbnailInfo.GetProperty("Id");

      var typeThumbnailsRequest = assemblyServicesShared.GetType("Sungero.Services.Shared.ThumbnailsRequest");
      var sizeField = typeThumbnailsRequest.GetProperty("Size");
      var thumbnailsField = typeThumbnailsRequest.GetProperty("Thumbnails");

      var request = Activator.CreateInstance(typeThumbnailsRequest);
      sizeField.SetValue(request, Constants.Module.SizePersonalPhotoThumbnail);

      var thumbnailInfoListType = typeof(List<>).MakeGenericType(typeThumbnailInfo);
      var thumbnailInfoList = (IList)Activator.CreateInstance(thumbnailInfoListType);

      foreach(var userId in userIds)
      {
        var typeThumbnailInfoInstance = Activator.CreateInstance(typeThumbnailInfo);
        thumbnailInfoId.SetValue(typeThumbnailInfoInstance, userId);
        thumbnailInfoList.Add(typeThumbnailInfoInstance);
      }

      thumbnailsField.SetValue(request, thumbnailInfoList);

      var typeThumbnailsCacheManager = assemblyCoreEntitiesServer.GetType("Sungero.CoreEntities.Server.ThumbnailsCacheManager");
      var instanceProperty = typeThumbnailsCacheManager.GetProperty("Instance");
      var instancePropertyValue = instanceProperty.GetValue(null);
      var getThumbnailsType = typeThumbnailsCacheManager.GetMethod("GetThumbnails");
      var thumbnails = getThumbnailsType.Invoke(instancePropertyValue, new object[] { request });

      var thumbnailType = assemblyServicesShared.GetType("Sungero.Services.Shared.Thumbnail");
      var thumbnailIdProperty = thumbnailType.GetProperty("Id");
      var thumbnailDataProperty = thumbnailType.GetProperty("Data");

      var result = new List<Structures.Module.IUserThumbnailDto>();
      
      if(thumbnails != null)
      {
        foreach(object thumbnail in (IEnumerable)thumbnails)
        {
          var thumbnailIdPropertyValue = (long)thumbnailIdProperty.GetValue(thumbnail);
          var thumbnailDataPropertyValue = (byte[])thumbnailDataProperty.GetValue(thumbnail);
          result.Add(Structures.Module.UserThumbnailDto.Create(thumbnailIdPropertyValue, thumbnailDataPropertyValue));
        }
      }

      return result;
    }
    
    private List<Structures.Module.IUserThumbnailDto> GetThumbnailsFromDatabase(List<long> userIds)
    {      
      var users = Users.GetAll(u => userIds.Contains(u.Id));
      return users
        .Select(u => Structures.Module.UserThumbnailDto.Create(u.Id,
                                                               u.GetPersonalPhotoThumbnail(Constants.Module.SizePersonalPhotoThumbnail)))
        .ToList();
    }
    
    
    /// <summary>
    /// Получение аватарок пользователей.
    /// </summary>
    /// <param name="userId">Ид пользователей.</param>
    /// <returns>Миниатюры фотографий.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public List<Structures.Module.IUserThumbnailDto> GetThumbnails(List<long> userIds)
    {
      if (userIds.Count == 0)
      {
        return new List<Structures.Module.IUserThumbnailDto>();
      }
      
      var result = GetThumbnailsFromCache(userIds);
      if (result.Count == userIds.Count)
      {
        return result;
      }
      
      var returnedIds = new HashSet<long>(result.Select(x => x.Id));
      
      var missedIds = userIds
        .Where(id => !returnedIds.Contains(id))
        .ToList();
      
      var fromDatabase = GetThumbnailsFromDatabase(missedIds);
      return result.Concat(fromDatabase).ToList();
    }

    /// <summary>
    /// Получение адреса веб-клиента.
    /// </summary>
    /// <returns>Адрес веб-клиента.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string GetWebClient()
    {
      var webAdressGetter = new ConfigWrapperV1_0_0.WebClientAddressGetter();
      var webClientAddress = webAdressGetter.GetWebClientAddress();
      
      if(webClientAddress == null)
      {
        var errorMessage = Resources.AddressNotFound;
        Logger.Error(errorMessage);
        throw new Exception(errorMessage);
      }
      
      return webClientAddress.ToString();
    }
    
    private Structures.Module.ISearchRecipientsResult HandleSearchRecipients(string searchValue)
    {
      var searchValueSplitted = searchValue.ToLowerInvariant().Split(default(char[]), StringSplitOptions.RemoveEmptyEntries);
      
      var query = Recipients.GetAll()
        .Where(recipient => !Sungero.CoreEntities.Shared.RolesRegistry.PredefinedRoles.Contains(recipient.Sid.Value))
        .Where(recipient => Sungero.Company.Employees.Is(recipient) || (Roles.Is(recipient) && Roles.As(recipient).IsSingleUser.Value))
        .Where(recipient => !(recipient.IsSystem.HasValue && recipient.IsSystem.Value));

      foreach (var searchWord in searchValueSplitted)
      {
        query = query.Where(recipient => recipient.Name.ToLowerInvariant().Contains(searchWord) ||
          (Sungero.Company.Employees.Is(recipient) && Sungero.Company.Employees.As(recipient).JobTitle.DisplayValue.ToLowerInvariant().Contains(searchWord)));
      }
      
      var recipients = query.OrderBy(x => x.Status)
        .ThenBy(recipient => recipient.Name)
        .Take(Constants.Module.MaxRecepintCount)
        .ToList();
      
      return Structures.Module.SearchRecipientsResult.Create(CreateRecipientsDto(recipients));
    }
    
    private List<Structures.Module.IRecipientDto> CreateRecipientsDto(List<IRecipient> recipients)
    {
      var recipientThumbnails = GetThumbnails(recipients.Select(x => x.Id).ToList());
      var recipientsDto = new List<Structures.Module.IRecipientDto>(recipients.Count);
      
      foreach (var recipient in recipients)
      {
        var employee = Sungero.Company.Employees.As(recipient);
        var recipientDto = new Structures.Module.RecipientDto
        {
          Id = recipient.Id,
          JobTitle = employee?.JobTitle?.DisplayValue,
          Name = recipient.Name,
          Department = employee?.Department?.DisplayValue,
          Thumbnail = GetRecipientThumbnail(recipientThumbnails, recipient.Id),
          IsActive = recipient.Status == Sungero.CoreEntities.Recipient.Status.Active
        };
        
        recipientsDto.Add(recipientDto);
      }
      
      return recipientsDto;
    }
    
    private static string GetRecipientThumbnail(List<Structures.Module.IUserThumbnailDto> thumbnails, long recipientId)
    {
      var recipientThumbnail = thumbnails.Where(x => x.Id == recipientId).FirstOrDefault();
      if (recipientThumbnail == null || recipientThumbnail.Thumbnail == null)
      {
        return string.Empty;
      }
      
      return Convert.ToBase64String(recipientThumbnail.Thumbnail);
    }
    
    /// <summary>
    /// Получить значение свойства.
    /// </summary>
    /// <param name="property">Метаданные свойства.</param>
    /// <param name="entity">Сущность.</param>
    /// <param name="IsIncludeBinaryValues">Признак: включать ли бинарные данные.</param>
    /// <returns>Значение свойства.</returns>
    private Structures.Module.IProperty GetProperty(PropertyMetadata property, IEntity entity, bool IsIncludeBinaryValues)
    {
      var propertyResult = Structures.Module.Property.Create();
      
      switch(property.PropertyType)
      {
          //Если свойство простое, то получаем просто значение
        case Sungero.Metadata.PropertyType.Boolean:
        case Sungero.Metadata.PropertyType.DateTime:
        case Sungero.Metadata.PropertyType.Double:
        case Sungero.Metadata.PropertyType.Guid:
        case Sungero.Metadata.PropertyType.Integer:
        case Sungero.Metadata.PropertyType.LongInteger:
        case Sungero.Metadata.PropertyType.String:
        case Sungero.Metadata.PropertyType.Text:
          propertyResult.Value = property.GetValue(entity);
          break;
          
          //Если тип свойства коллекция, то передаем Guid свойства и Id
        case Sungero.Metadata.PropertyType.Navigation:
          var navigation = property.GetValue(entity) as Sungero.Domain.Shared.IEntity;
          if(navigation != null)
          {
            var navigationStructure = Structures.Module.NavigationValue.Create();
            var navigationMetadata = (NavigationPropertyMetadata)property;
            navigationStructure.Id = navigation.Id;
            navigationStructure.TypeGuid = navigationMetadata.EntityGuid;
            propertyResult.Value = navigationStructure;
          }
          break;
          
          //Если тип свойства изображение или бинарные данные, передаем сковертированное в Base64
        case Sungero.Metadata.PropertyType.Image:
        case Sungero.Metadata.PropertyType.Data:
          if (IsIncludeBinaryValues)
          {
            var dataValue = property.GetValue(entity);
            if(dataValue != null)
            {
              propertyResult.Value = Convert.ToBase64String((byte[])dataValue);
            }
          }
          break;
          
          //Если тип свойства бинарные данные в хранилище, передаем Id бинарных данных,
          //размер в байтах, хэш бинарных данных, Id хранилища,
          //имя хранилища, адрес сервиса хранилища,
        case Sungero.Metadata.PropertyType.BinaryData:
          var binaryData = property.GetValue(entity) as IBinaryData;
          if(binaryData != null)
          {
            var binaryDataStructure = Structures.Module.BinaryDataValue.Create();
            var storageStructure = Structures.Module.Storage.Create();
            if(binaryData.Storage != null)
            {
              storageStructure.Id = binaryData.Storage.Id;
              storageStructure.Name = binaryData.Storage.Name;
              storageStructure.Address = binaryData.Storage.Address;
            }
            binaryDataStructure.Id = binaryData.Id;
            binaryDataStructure.Size = binaryData.Size;
            binaryDataStructure.Hash = binaryData.Hash;
            binaryDataStructure.Storage = storageStructure;
            propertyResult.Value = binaryDataStructure;
          }
          break;
          
          //Если тип свойства перечисление, передаем значение перечисления
        case Sungero.Metadata.PropertyType.Enumeration:
          propertyResult.Value = (property.GetValue(entity) as Nullable<Enumeration>)?.Value;
          break;
          
        case Sungero.Metadata.PropertyType.Collection:
        case Sungero.Metadata.PropertyType.Component:
          // Если тип свойства коллекция или компонент, то не передаем значение в ответ
          break;
          
        default:
          Logger.Debug(Resources.UnknownPropertyTypeFormat(property.PropertyType));
          break;
      }
      propertyResult.Type = property.PropertyType.ToString();
      return propertyResult; 
    }
    
    /// <summary>
    /// Проверить входит ли пользователь в наши роли PP, Agile, Memo.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <returns>True, если входит, иначе false.</returns>
    public static bool IsUserIncludedInOurRoles(IUser user)
    {
      var roles = Roles.GetAll(r => r.Sid == Constants.Module.AgileBoardsRole ||
                               r.Sid == Constants.Module.ProjectManagersRoleGuid ||
                               // в Базе знаний неправильно создана роль, без указания Sid
                               // поиск роли там по имени, поэтому здесь также сделано
                               r.Name == Resources.KnowledgeBaseRoleName);
      
      foreach(var role in roles)
      {
        if (user.IncludedIn(role))
        {
          return true;
        }
      }
      
      return false;
    }
  }
}
