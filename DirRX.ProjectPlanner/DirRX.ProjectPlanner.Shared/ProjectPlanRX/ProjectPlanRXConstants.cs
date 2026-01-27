using System;
using Sungero.Core;

namespace DirRX.ProjectPlanner.Constants
{
  public static class ProjectPlanRX
  {

    /// <summary>
    /// Формат документа MS Project.
    /// </summary>
    public const string MSProjectFormat = "xml";
    
    /// <summary>
    /// Наименование параметра "Сайт клиента"
    /// </summary>
    public const string WebSiteParam = "WebSite";
    
    /// <summary>
    /// Изменение - логическая операция для истории.
    /// </summary>
    public const string ChangeOperation = "Change";
    
    /// <summary>
    /// Ключ кэша данных моделей состояний карточки плана и проекта
    /// </summary>
    public const string DynamicDataCacheKeyFormat = "ProjectPlanRX.DynamicDataCache{0}";
    public const int DynamicDataCacheMinutes = 1;
  }
}