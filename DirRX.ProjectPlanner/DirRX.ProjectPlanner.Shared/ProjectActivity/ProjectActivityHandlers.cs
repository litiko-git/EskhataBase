using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;
using DirRX.TeamsCommonAPI;
using DirRX.TeamsCommonAPI.NotifyEventType;

namespace DirRX.ProjectPlanner
{
	partial class ProjectActivitySharedHandlers
	{

    public override void StatusChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null || (e.OldValue != null && e.OldValue.Value.Value == e.NewValue.Value.Value) || _obj.ProjectPlan == null || _obj.State.IsInserted)
      {
        return;
      }
      
      var newNotifyDiff = NotifyDiffs.Create();
      newNotifyDiff.ConnectedActId = _obj.Id;
      newNotifyDiff.ConnectedPlanId = _obj.ProjectPlan.Id;
      newNotifyDiff.NewValue = e.NewValue.Value.Value;
      newNotifyDiff.PreviousValue = e.OldValue?.Value;
      
      var eventType = new Nullable<Enumeration>();
      
      newNotifyDiff.EventTypeId = NotifyEventTypes.GetAll(t => t.EventType == EventType.ActStatusChangd).FirstOrDefault().Id;
      newNotifyDiff.Save();
    }

    public virtual void ResponsibleChanged(DirRX.ProjectPlanner.Shared.ProjectActivityResponsibleChangedEventArgs e)
    {
      if (e.OldValue?.Id == e.NewValue?.Id || _obj.ProjectPlan == null || _obj.State.IsInserted)
      {
        return;
      }
      
      Functions.ProjectActivity.Remote.CreateResponsibleDiff(_obj, e.NewValue?.Id, e.OldValue?.Id);
    }

		public virtual void PriorityChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue != e.OldValue)
			{
				if (e.NewValue > 10 || e.NewValue < 1)
					_obj.Priority = 1;
			}
		}

		public virtual void NumberChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
		{
			if (e.NewValue.HasValue && !e.NewValue.Equals(e.OldValue))
			{
				_obj.Number = e.NewValue;
				Functions.ProjectActivity.UpdateFullNumber(_obj);
			}
		}

	}
}