using System;
using Sungero.Core;

namespace DirRX.ProjectPlanning.Module.Projects.Constants
{
	public static class Module
	{
		
		#region Выдача прав на проектные документы

		// Идентификатор фонового процесса выдачи прав на проектные документы.
		public const string LastProjectDocumentRightsUpdateDate = "LastProjectDocumentRightsUpdateDate";
		
		public const string LastProjectRightsUpdateDate = "LastProjectRightsUpdateDate";
		
		public const string AccessRightsReadTypeName = "Read";
		
		public const string AccessRightsEditTypeName = "Edit";
		
		public const string AccessRightsFullAccessTypeName = "FullAccess";
		
		#endregion
		
		public const string WorkloadAnalysisDialog = "pp_otchet_po_zagruzke";
		
		#region Папки проектов

		public static class ProjectFolders
		{
			// UID для корневой папки с проектами.
			public static readonly Guid ProjectFolderUid = Guid.Parse("F7A78196-A1BE-4666-94F4-0DDDD3367A6E");
			
			// UID для корневой папки с проектами.
			public static readonly Guid ProjectArhiveFolderUid = Guid.Parse("C74412F4-7FBB-450F-83A3-BBB9772C6167");
		}
    #endregion
  }
}