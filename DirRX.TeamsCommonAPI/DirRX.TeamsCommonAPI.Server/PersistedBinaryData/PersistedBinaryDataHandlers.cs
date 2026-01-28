using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.TeamsCommonAPI
{
  partial class PersistedBinaryDataServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      this._obj.Author = Users.Current;
    }
  }

  
}