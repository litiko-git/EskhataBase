using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.TeamsCommonAPI.TeamsTask;
using Sungero.Domain.Clients;
using Newtonsoft.Json;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI.NotifyConditionItem;

namespace DirRX.TeamsCommonAPI.Server
{
  partial class TeamsTaskRouteHandlers
  {

    public virtual bool Monitoring22Result()
    { 
      var rootTask = PublicFunctions.Module.GetRootTask(_obj);
      
      if (rootTask == null)
      {
        return false;
      }
      
      if (Locks.GetLockInfo(_obj).IsLockedByOther || Locks.GetLockInfo(rootTask).IsLockedByOther)
      {
        return false;
      }
      
      foreach (var attachment in _obj.Attachments.Concat(rootTask.Attachments))
      {
        if (Locks.GetLockInfo(attachment).IsLockedByOther)
        {
          return false;
        }
      }
      
      return true;
    }

    public virtual bool Decision17Result()
    {
      var connectedSettings = TeamsNoticesSettingses.Get(((ITeamsTask)_obj.MainTask).SettingsId.Value);
      
      foreach (var recipient in connectedSettings.Recipients.Where(r => !string.IsNullOrEmpty(r.Recipient) && r.NotifyCondition.Condition == Condition.Consolidated))
      {
        var deserializedRecipientIds = (List<long>)Newtonsoft.Json.JsonConvert.DeserializeObject(recipient.Recipient, typeof(List<long>));
        if (_obj.Responsibles.Any(r => deserializedRecipientIds.Contains(r.Responsible.Id)))
        {
          return false;
        }
      }
      return true;
    }
    
    public virtual void StartNotice9(DirRX.TeamsCommonAPI.ITeamsNotice notice, DirRX.TeamsCommonAPI.Server.TeamsNoticeArguments e)
    {
      var clientId = ClientManager.Instance.GetClientsOfUser(notice.Performer.Id)
        .Distinct()
        .ToArray();
      
      notice.EntityHyperlink = Hyperlinks.Get(notice);
      
      var serializedAttachments = JsonConvert.SerializeObject(notice.Attachments, new JsonSerializerSettings { DateFormatString ="yyyy-MM-ddTH:mm:ss.fffZ" });
      
      RNDNoticesUtils.Messages.NotifySender.SendNewNotifyMessage(clientId, "TEST_APP_ID", notice.Id.ToString(), serializedAttachments, _obj.EntityHyperlink, notice.Subject, notice.EntityName, notice.ClientHyperlink);
    }

    public virtual void StartBlock9(DirRX.TeamsCommonAPI.Server.TeamsNoticeArguments e)
    {
      e.Block.Subject = _obj.Subject;
      e.Block.Text = _obj.ActiveText;
      e.Block.IsCollectiveNotice = true;
      e.Block.EntityName = _obj.Attachments.FirstOrDefault().DisplayValue; //FIXME отладочный код - имя надо задавать по главному из вложений.
      
      foreach (var responsible in _obj.Responsibles)
      {
        e.Block.Performers.Add(responsible.Responsible);
      }
      
      foreach (var childSolutionGroup in _obj.NotifiesCollection.OrderBy(n => n.TeamsTask.SolutionGuid).GroupBy(n => n.TeamsTask.SolutionGuid))
      {
        foreach (var entityIdGroup in childSolutionGroup.GroupBy(g => g.RootEntityHyperlink))
        {
          if (childSolutionGroup.Key == Constants.Module.ProjectPlanGuid)
          {
            _obj.ActiveText = "\r\n" + DirRX.TeamsCommonAPI.TeamsTasks.Resources.InProjectPlan + entityIdGroup.Key;
          }
          
          if (childSolutionGroup.Key == Constants.Module.AgileBoardsGuid)
          {
            _obj.ActiveText = "\r\n" + DirRX.TeamsCommonAPI.TeamsTasks.Resources.InAgileBoard + entityIdGroup.Key;
          }
          
          foreach (var entity in entityIdGroup.OrderBy(entity => entity.TimeStamp))
          {
            _obj.ActiveText += string.Format("\r\n{0}", entity.NotifyText);
          }
        }
      }
    }

    public virtual void Script16Execute()
    {
      PublicFunctions.Module.ConsolidatedTaskComplemention(_obj);
    }

    public virtual bool Decision15Result()
    {
      var task = ((ITeamsTask)_obj.MainTask);
      return !task.IsRootTaskForConsolidated.HasValue || !task.IsRootTaskForConsolidated.Value;
    }

    public virtual bool Decision7Result()
    {
      return true;
    }
    
    public virtual void StartNotice6(DirRX.TeamsCommonAPI.ITeamsNotice notice, DirRX.TeamsCommonAPI.Server.TeamsNoticeArguments e)
    {
      var clientId = ClientManager.Instance.GetClientsOfUser(notice.Performer.Id)
        .Distinct()
        .ToArray();
      
      var serializedAttachments = JsonConvert.SerializeObject(notice.Attachments, new JsonSerializerSettings { DateFormatString ="yyyy-MM-ddTH:mm:ss.fffZ" });
      
      RNDNoticesUtils.Messages.NotifySender.SendNewNotifyMessage(clientId, "TEST_APP_ID", notice.Id.ToString(), serializedAttachments, _obj.EntityHyperlink, notice.Subject, notice.EntityName, notice.ClientHyperlink);
    }

    public virtual void StartBlock6(DirRX.TeamsCommonAPI.Server.TeamsNoticeArguments e)
    {
      e.Block.Subject = _obj.Subject;
      e.Block.Text = _obj.ActiveText;
      
      foreach (var responsible in _obj.Responsibles)
      {
        e.Block.Performers.Add(responsible.Responsible);
      }
      
      e.Block.EntityGuid = _obj.SolutionGuid;
      e.Block.EntityHyperlink = _obj.EntityHyperlink;
      e.Block.EntityId = _obj.EntityId;
      e.Block.ClientHyperlink = _obj.ClientHyperlink; 
      e.Block.RootEntityId = _obj.RootEntityId;
      
      //FIXME отладочный код - имя надо задавать по главному из вложений.
      //Kiselev_EM: Обрезать заголовок этапа до 250 символов.
      //http://aura.npo-comp.ru/sungero?type=7197cc31-bf64-406e-8ead-0a7dde1c3c6b&id=134965
      var entityName = _obj.Attachments.FirstOrDefault().DisplayValue;
      e.Block.EntityName = entityName.Length > Constants.Module.TaskTitleMaxLength
        ? entityName.Substring(0, Constants.Module.TaskTitleMaxLength)
        : entityName;
      
    }
  }


}