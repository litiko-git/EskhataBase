using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.ProjectKind;

namespace DirRX.ProjectPlanning
{
  partial class ProjectKindClassifierDirRXDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ClassifierDirRXDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.ProjectsAccounting.HasValue && x.ProjectsAccounting.Value);
    }
  }

}