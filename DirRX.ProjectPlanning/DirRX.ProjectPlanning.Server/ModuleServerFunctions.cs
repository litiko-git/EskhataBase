using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Company;
using DirRX.ProjectPlanning;
using DirRX.ProjectPlanning.ProjectCore;

namespace DirRX.ProjectPlanning.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Базовый фильтр для ProjectCore.
    /// </summary>
    /// <param name="query">Изначальный набор данных.</param>
    /// <param name="isActive">Статус - исполнение.</param>
    /// <param name="isClosed">Статус - завершен.</param>
    /// <param name="isClosing">Статус - завершение.</param>
    /// <param name="isInitiation">Статус - инициация.</param>
    /// <param name="isPlanning">Статус - планирование.</param>
    /// <param name="projectManager">Руководитель.</param>
    /// <param name="leadingProject">Входит в.</param>
    /// <param name="internalCustomer">Внутренний заказчик.</param>
    /// <param name="externalCustomer">Внешний заказчик.</param>
    /// <param name="startDateRangeFrom">Дата начала с.</param>
    /// <param name="startDateRangeTo">Дата начала по.</param>
    /// <param name="finishDateRangeFrom">Дата окончания с.</param>
    /// <param name="finishDateRangeTo">Дата окончания по.</param>
    /// <param name="needAssistance">Статус проблем - требуется помощь.</param>
    /// <param name="underControl">Статус проблем - под контролем.</param>
    /// <param name="me">В команде управления - я.</param>
    /// <param name="mySubordinates">В команде управления - мои подчиненные</param>
    /// <param name="employeeSelect">В команде управления - сотрудник.</param>
    /// <returns>Набор ProjectCore.</returns>
    [Public]
    public static IQueryable<IProjectCore> BaseFilter(IQueryable<IProjectCore> query,
                                                      bool isActive,
                                                      bool isClosed,
                                                      bool isClosing,
                                                      bool isInitiation,
                                                      bool isPlanning,
                                                      IEmployee projectManager,
                                                      Sungero.Projects.IProjectCore leadingProject,
                                                      IEmployee internalCustomer,
                                                      Sungero.Parties.ICounterparty externalCustomer,
                                                      DateTime? startDateRangeFrom,
                                                      DateTime? startDateRangeTo,
                                                      DateTime? finishDateRangeFrom,
                                                      DateTime? finishDateRangeTo,
                                                      bool needAssistance,
                                                      bool underControl,
                                                      bool me,
                                                      bool mySubordinates,
                                                      IEmployee employeeSelect)
    {
      
      #region Базовый фильтр
      // Фильтр по состоянию.
      if (isActive || isClosed || isClosing || isInitiation || isPlanning)
      {
        query = query.Where(x => (isActive && x.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Execution) ||
                            (isClosed && x.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Completed) ||
                            (isClosing && x.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Completion) ||
                            (isInitiation && x.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Initiation) ||
                            (isPlanning && x.Stage == DirRX.ProjectPlanning.ProjectCore.Stage.Planning));
      }
      else
      {
        // HACK Balezin_AA: Платформа не дает вернуть пустой IQueryable, поэтому возвращаю
        // набор пустых данных таким способом
        return query.Where(x => false);
      }

      // Фильтр по руководителю.
      if (projectManager != null)
        query = query.Where(x => Equals(x.Manager, projectManager));
      
      // Фильтр по ведущему проекту(входит в).
      if (leadingProject != null)
        query = query.Where(x => Equals(x.LeadingProject, leadingProject));

      // Фильтр по внутреннему заказчику.
      if (internalCustomer != null)
        query = query.Where(x => Equals(x.InternalCustomer, internalCustomer));

      // Фильтр по внешнему заказчику.
      if (externalCustomer != null)
        query = query.Where(x => Equals(x.ExternalCustomer, externalCustomer));

      var today = Calendar.UserToday;
      
      // Фильтр по дате начала проекта.
      var startDateBeginPeriod = startDateRangeFrom ?? Calendar.SqlMinValue;
      var startDateEndPeriod = startDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (startDateRangeFrom != null || startDateRangeTo != null)
        query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Stage, DirRX.ProjectPlanning.ProjectCore.Stage.Completed)) ||
                            (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Stage, DirRX.ProjectPlanning.ProjectCore.Stage.Completed)) ||
                            DirRX.PortfolioProgram.Portfolios.Is(x)
                           );

      // Фильтр по дате окончания проекта.
      var finishDateBeginPeriod = finishDateRangeFrom ?? Calendar.SqlMinValue;
      var finishDateEndPeriod = finishDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (finishDateRangeFrom != null || finishDateRangeTo != null)
        query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Stage, DirRX.ProjectPlanning.ProjectCore.Stage.Completed)) ||
                            (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Stage, DirRX.ProjectPlanning.ProjectCore.Stage.Completed)) ||
                            DirRX.PortfolioProgram.Portfolios.Is(x)
                           );
      #endregion
      
      #region Статус проблем
      if(needAssistance)
      {
        query = query.Where(x => x.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.NeedAssistance);
      }
      
      if(underControl)
      {
        query = query.Where(x => x.StatusIssues == DirRX.ProjectPlanning.ProjectCore.StatusIssues.UnderControl);
      }
      #endregion
      
      #region В команде управления
      if(me)
      {
        var recipientIds = Sungero.CoreEntities.Recipients.OwnRecipientIds;
        query = query.Where(x => x.TeamMembers.Where(tm => tm.Group == DirRX.ProjectPlanning.ProjectCoreTeamMembers.Group.Management)
                            .Any(tm => recipientIds.Contains(tm.Member.Id))
                           );
      }
      
      if(mySubordinates)
      {
        var subordinateEmployeesIds = Module.Projects.PublicFunctions.Module.GetSubordinateEmployees();
        var subordinateRecipients = Sungero.CoreEntities.Recipients.GetAll(x => subordinateEmployeesIds.Contains(x.Id));
        var subordinateRecipientsIds = new HashSet<long>();
        
        foreach(var subRecipient in subordinateRecipients)
        {
          subordinateRecipientsIds.UnionWith(Sungero.CoreEntities.Recipients.OwnRecipientIdsFor(subRecipient));
        }
        
        query = query.Where(x => x.TeamMembers.Where(tm => tm.Group == DirRX.ProjectPlanning.ProjectCoreTeamMembers.Group.Management)
                            .Any(tm => subordinateRecipientsIds.Contains(tm.Member.Id))
                           );
      }
      
      if(employeeSelect != null)
      {
        var recipientIds = Sungero.CoreEntities.Recipients.OwnRecipientIdsFor(employeeSelect);
        query = query.Where(x => x.TeamMembers.Where(tm => tm.Group == DirRX.ProjectPlanning.ProjectCoreTeamMembers.Group.Management)
                            .Any(tm => recipientIds.Contains(tm.Member.Id))
                           );
      }
      #endregion
      
      return query;
    }
    
    /// <summary>
    /// Фильтр по типу для ProjectCore.
    /// </summary>
    /// <param name="query">Изначальный набор данных.</param>
    /// <param name="isProject">Проект.</param>
    /// <param name="isProgram">Программа.</param>
    /// <param name="isPortfolio">Портфель</param>
    /// <returns>Набор ProjectCore.</returns>
    [Public]
    public IQueryable<DirRX.ProjectPlanning.IProjectCore> FilterProjectCoreByType(IQueryable<IProjectCore> query,
                                                                                         bool isProject,
                                                                                         bool isProgram,
                                                                                         bool isPortfolio)
    {
      if(!isProject)
      {
        query = query.Where(x => !DirRX.ProjectPlanning.Projects.Is(x));
      }
      
      if(!isProgram)
      {
        query = query.Where(x => !DirRX.PortfolioProgram.Programs.Is(x));
      }
      
      if(!isPortfolio)
      {
        query = query.Where(x => !DirRX.PortfolioProgram.Portfolios.Is(x));
      }
      
      return query;
    }
    
    /// <summary>
    /// Фильтр по виду проекта.
    /// </summary>
    /// <param name="query">Изначальный набор данных.</param>
    /// <param name="projectKind">Вид проекта.</param>
    /// <returns>Проекты.</returns>
    [Public]
    public static IQueryable<DirRX.ProjectPlanning.IProjectCore> FilterProjectByKind(IQueryable<IProjectCore> query,
                                                                                     Sungero.Projects.IProjectKind projectKind)
    {
      query = query.Where(x => Equals(x.ProjectKind, projectKind));
      
      return query;
    }

    /// <summary>
    /// Возвращает символ валюты по-умолчанию, если это рубли, доллары, евро. Буквенный код для других валют.
    /// Возвращает пустую строку, если значение по-умолчанию не выбрано.
    /// </summary>
    /// <returns>Представление валюты по-умолчанию.</returns>
    [Public]
    public static string GetDefaultCurrencySymbolFromDb()
    {
      var currencySymbols = new Dictionary<string, string>() {{"643", "₽"}, {"978", "€"}, {"840", "$"}};
      
      var defaultCurrency = Sungero.Commons.Currencies.GetAll().FirstOrDefault(c => c.IsDefault.HasValue && c.IsDefault.Value);
      if (defaultCurrency == null)
      {
        return string.Empty;
      }
      
      string symbol = null;
      if (currencySymbols.TryGetValue(defaultCurrency.NumericCode, out symbol))
      {
        return symbol;
      }
      return defaultCurrency.AlphaCode;
    }
  }
}