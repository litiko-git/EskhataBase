using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PortfolioProgram.Program;

namespace DirRX.PortfolioProgram.Server
{
  partial class ProgramFunctions
  {
    /// <summary>
    /// Создать программу.
    /// </summary>
    /// <returns>Программа.</returns>
    [Public, Remote]
    public static IProgram CreateProgram()
    {
      return Programs.Create();
    }
  }
}