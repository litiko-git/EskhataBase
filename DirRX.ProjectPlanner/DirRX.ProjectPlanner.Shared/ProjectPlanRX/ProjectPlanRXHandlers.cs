using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanRX;
using DirRX.TeamsCommonAPI.TeamsNoticesSettingsRecipients;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.TeamsNoticesSettings;
using DirRX.TeamsCommonAPI.NotifyEventType;

namespace DirRX.ProjectPlanner
{
  partial class ProjectPlanRXOtherNoticesSharedHandlers
  {

    public virtual void OtherNoticesLinkedSettingChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesLinkedSettingChangedEventArgs e)
    {
      var activityRepCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ActivityRep = activityRepCondition;
      
      var customerCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.CustomerInternal = customerCondition;
      
      var mgmntTeamCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.MgmntTeam = mgmntTeamCondition;
      
      var observersCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Observers = observersCondition;
      
      var participantCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Participant = participantCondition;
      
      var projectAdminCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectAdmin = projectAdminCondition;
      
      var projectLeadCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectLead = projectLeadCondition;
      
      var repSectionCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.RepSection = repSectionCondition;
      
      _obj.EventType = _obj.LinkedSetting.EventType;
    }

    public virtual void OtherNoticesObserversChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesObserversChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).FirstOrDefault();
      entity.NotifyCondition = _obj.Observers;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesMgmntTeamChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesMgmntTeamChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).FirstOrDefault();
      entity.NotifyCondition = _obj.MgmntTeam;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesParticipantChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesParticipantChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).FirstOrDefault();
      entity.NotifyCondition = _obj.Participant;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesCustomerInternalChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesCustomerInternalChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).FirstOrDefault();
      entity.NotifyCondition = _obj.CustomerInternal;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesProjectAdminChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesProjectAdminChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectAdmin;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesActivityRepChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesActivityRepChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).FirstOrDefault();
      entity.NotifyCondition = _obj.ActivityRep;
      _obj.LinkedSetting.Save();  
    }

    public virtual void OtherNoticesRepSectionChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesRepSectionChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).FirstOrDefault();
      entity.NotifyCondition = _obj.RepSection;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesProjectLeadChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesProjectLeadChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectLead;
      _obj.LinkedSetting.Save();
    }

    public virtual void OtherNoticesEventTypeChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXOtherNoticesEventTypeChangedEventArgs e)
    {
      _obj.LinkedSetting.EventType = _obj.EventType;
      _obj.LinkedSetting.Save();
    }
  }


  partial class ProjectPlanRXPlanDateNoticesSharedHandlers
  {

    public virtual void PlanDateNoticesLinkedSettingChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesLinkedSettingChangedEventArgs e)
    {
      var activityRepCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ActivityRep = activityRepCondition;
      
      var customerCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.CustomerInternal = customerCondition;
      
      var mgmntTeamCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.MgmntTeam = mgmntTeamCondition;
      
      var observersCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Observers = observersCondition;
      
      var participantCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Participant = participantCondition;
      
      var projectAdminCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectAdmin = projectAdminCondition;
      
      var projectLeadCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectLead = projectLeadCondition;
      
      var repSectionCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.RepSection = repSectionCondition;
      
      _obj.EventType = _obj.LinkedSetting.EventType;
    }

    public virtual void PlanDateNoticesObserversChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesObserversChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).FirstOrDefault();
      entity.NotifyCondition = _obj.Observers;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesMgmntTeamChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesMgmntTeamChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).FirstOrDefault();
      entity.NotifyCondition = _obj.MgmntTeam;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesParticipantChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesParticipantChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).FirstOrDefault();
      entity.NotifyCondition = _obj.Participant;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesCustomerInternalChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesCustomerInternalChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).FirstOrDefault();
      entity.NotifyCondition = _obj.CustomerInternal;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesProjectAdminChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesProjectAdminChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectAdmin;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesActivityRepChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesActivityRepChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).FirstOrDefault();
      entity.NotifyCondition = _obj.ActivityRep;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesRepSectionChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesRepSectionChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).FirstOrDefault();
      entity.NotifyCondition = _obj.RepSection;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesProjectLeadChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesProjectLeadChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectLead;
      _obj.LinkedSetting.Save();
    }

    public virtual void PlanDateNoticesEventTypeChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXPlanDateNoticesEventTypeChangedEventArgs e)
    {
      _obj.LinkedSetting.EventType = _obj.EventType;
      _obj.LinkedSetting.Save();
    }
  }

  partial class ProjectPlanRXTeamMembersSharedHandlers
  {

    public virtual void TeamMembersMemberChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXTeamMembersMemberChangedEventArgs e)
    {
      if (e.NewValue == null || (e.OldValue != null && e.OldValue.Id == e.NewValue.Id))
      {
        return;
      }
      
      var newNotifyDiff = NotifyDiffs.Create();
      newNotifyDiff.ConnectedPlanId = _obj.Id;
      newNotifyDiff.NewValue = e.NewValue.Id.ToString();
      newNotifyDiff.PreviousValue = e.OldValue?.Id.ToString();
      
      newNotifyDiff.EventTypeId = NotifyEventTypes.GetAll(t => t.EventType == EventType.PlanParticipAdd).FirstOrDefault().Id;
      newNotifyDiff.Save();
    }
  }


  partial class ProjectPlanRXResponsibleNoticesSharedHandlers
  {

    public virtual void ResponsibleNoticesEventTypeChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesEventTypeChangedEventArgs e)
    {
      _obj.LinkedSetting.EventType = _obj.EventType;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesObserversChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesObserversChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).FirstOrDefault();
      entity.NotifyCondition = _obj.Observers;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesMgmntTeamChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesMgmntTeamChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).FirstOrDefault();
      entity.NotifyCondition = _obj.MgmntTeam;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesParticipantChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesParticipantChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).FirstOrDefault();
      entity.NotifyCondition = _obj.Participant;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesCustomerInternalChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesCustomerInternalChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).FirstOrDefault();
      entity.NotifyCondition = _obj.CustomerInternal;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesProjectAdminChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesProjectAdminChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectAdmin;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesActivityRepChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesActivityRepChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).FirstOrDefault();
      entity.NotifyCondition = _obj.ActivityRep;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesRepSectionChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesRepSectionChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).FirstOrDefault();
      entity.NotifyCondition = _obj.RepSection;
      _obj.LinkedSetting.Save();
    }

    public virtual void ResponsibleNoticesProjectLeadChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesProjectLeadChangedEventArgs e)
    {
      var entity = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).FirstOrDefault();
      entity.NotifyCondition = _obj.ProjectLead;
      _obj.LinkedSetting.Save();
    }
    
    public virtual void ResponsibleNoticesLinkedSettingChanged(DirRX.ProjectPlanner.Shared.ProjectPlanRXResponsibleNoticesLinkedSettingChangedEventArgs e)
    {
      var activityRepCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ActivityRep).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ActivityRep = activityRepCondition;
      
      var customerCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.CustomerInterna).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.CustomerInternal = customerCondition;
      
      var mgmntTeamCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.MgmntTeam).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.MgmntTeam = mgmntTeamCondition;
      
      var observersCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Observers).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Observers = observersCondition;
      
      var participantCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.Participant).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.Participant = participantCondition;
      
      var projectAdminCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectAdmin).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectAdmin = projectAdminCondition;
      
      var projectLeadCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.ProjectLead).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.ProjectLead = projectLeadCondition;
      
      var repSectionCondition = _obj.LinkedSetting.Recipients.Where(r => r.RecipientType == RecipientType.RepSection).Select(r => r.NotifyCondition).FirstOrDefault();
      _obj.RepSection = repSectionCondition;
      
      _obj.EventType = _obj.LinkedSetting.EventType;
      
    }
  }

  partial class ProjectPlanRXTeamMembersSharedCollectionHandlers
  {
    public virtual void TeamMembersAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (_added.Group == null)
        _added.Group = ProjectPlanRXTeamMembers.Group.Change;
    }
  }

  partial class ProjectPlanRXSharedHandlers
  {

    public virtual void BaselineWorkChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.BaselineWork = e.OriginalValue;
      }
    }

    public virtual void ActualWorkloadChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.ActualWorkload = e.OriginalValue;
      }
    }

    public virtual void FactualCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      
      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.FactualCosts = e.OriginalValue;
      }
    }

    public virtual void PlannedCostsChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {

      if (e.NewValue.HasValue && e.NewValue < 0.0)
      {
        _obj.PlannedCosts = e.OriginalValue;
      }
    }

  }
}