using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Company;

namespace DirRX.ProjectPlanning.Module.Projects.Server
{
  partial class ModuleFunctions
  {
    /// <summary>
    /// Рекурсивный запрос на БД. Получает идентификаторы дочерних проектов на всю глубину иерархии.
    /// </summary>
    /// <param name="projectId">ИД ведущего проекта.</param>
    /// <returns>Набор уникальных идентификаторов дочерних проектов.</returns>
    /// <remarks>
    /// Результат включает Id ведущего проекта.
    /// </remarks>
    [Public]
    public System.Collections.Generic.HashSet<long> GetAllChildProjectIds(long projectId)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.GetSubProjectIds;
        SQL.AddParameter(command, "@projectId", projectId, System.Data.DbType.Int64);
        var childProjectIds = new HashSet<long>();
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var childProjectId = (long?)reader[0];
            if (!childProjectId.HasValue || childProjectId == 0)
            {
              continue;
            }
            
            childProjectIds.Add(childProjectId.Value);
          }
        }
        
        return childProjectIds;
      }
    }
    
    /// <summary>
    /// Рекурсивный запрос на БД. Получает идентификаторы дочерних проектов на всю глубину иерархии.
    /// </summary>
    /// <param name="projectIds">Коллекция ИД ведущих проектов.</param>
    /// <returns>Набор уникальных идентификаторов дочерних проектов.</returns>
    /// <remarks>
    /// Результат включает Id ведущего проекта.
    /// </remarks>
    [Public]
    public System.Collections.Generic.HashSet<long> GetAllChildProjectIds(System.Collections.Generic.IEnumerable<long> projectIds)
    {
      var result = new HashSet<long>();
      foreach (var projectId in projectIds)
      {
        result.UnionWith(this.GetAllChildProjectIds(projectId));
      }
      return result;
    }
    
    /// <summary>
    /// Получить подчиненных сотрудников для текущего сотрудника.
    /// </summary>
    /// <returns>Ид подчиненных сотрудников.</returns>
    [Public]
    public List<long> GetSubordinateEmployees()
    {
      var employeeIds = new List<long>();
      var currentEmployee = Sungero.Company.Employees.Current;
      var currentRecipientsIds = Sungero.Docflow.PublicFunctions.Module.GetCurrentRecipients(true);
      
      if(currentEmployee != null && currentRecipientsIds.Any())
      {
        var allBusinessUnits = Sungero.Company.BusinessUnits.GetAll();
        var allDepartments = Sungero.Company.Departments.GetAll();
        var allEmployees = Sungero.Company.Employees.GetAll();
        
        var businessUnits = allBusinessUnits.Where(x => x.CEO != null && currentRecipientsIds.Contains(x.CEO.Id));
        
        var subManagerIds = allDepartments
          .Where(x => x.Manager != null && businessUnits.Contains(x.BusinessUnit))
          .Select(x => x.Manager.Id);
        
        employeeIds.AddRange(subManagerIds);
        
        var subCeoIds = allBusinessUnits
          .Where(b => b.HeadCompany != null && businessUnits.Contains(b.HeadCompany) && b.CEO != null)
          .Select(b => b.CEO.Id);
        
        employeeIds.AddRange(subCeoIds);
        
        var departments = allDepartments.Where(x => currentRecipientsIds.Contains(x.Manager.Id));
        var subEmployeeIds = allEmployees.Where(x => departments.Contains(x.Department)).Select(x => x.Id);
        
        employeeIds.AddRange(subEmployeeIds);
        
        //исключаем текущего сотрудника
        employeeIds = employeeIds.Where(x => x != currentEmployee.Id).ToList();
      }
      
      return employeeIds;
    }
    
    /// <summary>
    /// Массовое сохранение сущностей в отдельной сессии БД.
    /// </summary>
    /// <param name="activities">Сущности для сохранения.</param>
    private static void BatchSave(System.Collections.Generic.IEnumerable<object> entities)
    {
      if (!entities.Any())
        return;
      
      using (var session = Sungero.Domain.Session.CreateIndependentSession())
      {
        foreach (var entity in entities)
        {
          session.Update(entity);
        }
        
        session.SubmitChanges();
      }
    }
    
    /// <summary>
    /// Закрыть проект с подпроектами.
    /// </summary>
    /// <param name="leadingProjectId">ИД ведущего проекта.</param>
    [Public]
    public static void CloseProjectCores(long leadingProjectId)
    {
      var subProjectIds = DirRX.ProjectPlanning.Module.Projects.PublicFunctions.Module.GetAllChildProjectIds(leadingProjectId);
      var subProjects = DirRX.ProjectPlanning.ProjectCores.GetAll(x => subProjectIds.Contains(x.Id));
      
      foreach(var subProject in subProjects)
      {
        subProject.Stage =  DirRX.ProjectPlanning.ProjectCore.Stage.Completed;
      }
      
      BatchSave(subProjects);
    }
    
  }
}
