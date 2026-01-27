using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ReviewAssignment;

namespace DirRX.ProjectPlanner
{
  partial class ReviewAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Кэшируем актуальный id этапа при загрузке карточки, чтобы больше в бд не ходить.
      Functions.Module.WriteActivityIdAndVersionToRefreshActionParams(e, _obj.ProjectPlan, _obj.ActivityRefId.HasValue ? _obj.ActivityRefId.Value : 0);
    }

  }
}
