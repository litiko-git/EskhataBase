using System;
using Sungero.Core;

namespace DirRX.PortfolioProgram.Constants
{
  public static class Module
  {
    
    public static readonly Guid PortfolioProgramModuleGuid = Guid.Parse("1d43aeae-77cf-41e0-a54f-a6c4ee80474e");

    /// <summary>
    /// Полное название модуля Портфели и программы
    /// </summary>
    [Public]
    public const string ModluleFullName = "DirRX.PortfolioProgram";
    
    /// <summary>
    /// Дефолтные варианты процентов для справочника Вероятности.
    /// </summary>
    public static class ProbabilityOccurInPercent
    {
      public const int FivePercent = 5;
      public const int TenPercent = 10;
      public const int FiftyPercent = 50;
      public const int SeventyFivePercent = 75;
      public const int NinetyFivePercent = 95;
    }
    
    #region Виджеты
    
    /// <summary>
    /// Идентификаторы значений для виджета "Здоровье".
    /// </summary>
    public static class HealthValueId
    {
      public const string NoIssues = "NoIssues";
      public const string UnderControl = "UnderControl";
      public const string NeedAssistance = "NeedAssistance";
      public const string Other = "Other";
    }
    
    #endregion
  }
}