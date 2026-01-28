using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Program;

namespace DirRX.PortfolioProgram
{
  partial class ProgramClientHandlers
  {

    public override IEnumerable<Enumeration> StageFiltering(IEnumerable<Enumeration> query)
    {
      query = base.StageFiltering(query);
      query = query.Where(e => e != DirRX.ProjectPlanning.ProjectCore.Stage.Planning);
      return query;
    }

  }
}