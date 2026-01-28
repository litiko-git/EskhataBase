using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.TeamsCommonAPI.Server
{
  partial class SendedTeamsFolderFolderHandlers
  {

    public virtual bool IsSendedTeamsFolderVisible()
    {
      return Functions.Module.IsUserIncludedInOurRoles(Users.Current);
    }

    public virtual IQueryable<DirRX.TeamsCommonAPI.ITeamsTask> SendedTeamsFolderDataQuery(IQueryable<DirRX.TeamsCommonAPI.ITeamsTask> query)
    {
      return query.Where(t => t.IsManuallyStarted.HasValue && t.IsManuallyStarted.Value);
    }
  }

  partial class IncomingTeamsFolderFolderHandlers
  {

    public virtual bool IsIncomingTeamsFolderVisible()
    {
      return Functions.Module.IsUserIncludedInOurRoles(Users.Current);
    }

    public virtual IQueryable<DirRX.TeamsCommonAPI.ITeamsNotice> IncomingTeamsFolderDataQuery(IQueryable<DirRX.TeamsCommonAPI.ITeamsNotice> query)
    {
      
      //HACK Balezin_AA: по умолчанию фильтр выставлена на "Получено сегодня".
      //Однако может возникнуть ситуация, что фильтр окажется null.
      //В таком случае во избежании ошибки, возвращаем весь набор данных.
      if(_filter == null)
      {
        return query;
      }
      
      if(_filter.Today)
      {
        query = query.Where(x => x.Created >= Calendar.UserToday);
      }
      
      if(_filter.Unread)
      {
        query = query.Where(x => x.IsRead != true);
      }
      
      return query;
    }
  }


  partial class TeamsCommonAPIHandlers
  {
  }
}