using System;

namespace DirRX.ProjectPlanner.Constants
{
  public static class ProjectActivity
  {
    public static class LinkType
    {
      /// <summary>
      /// Связь окончание-начало(ОН).
      /// </summary>
      public const string FS = "0";
      
      /// <summary>
      /// Связь начало-начало(НН).
      /// </summary>
      public const string SS = "1";
      
      /// <summary>
      /// Связь окончание-окончание(ОО).
      /// </summary>
      public const string FF = "2";
      
      /// <summary>
      /// Связь начало-окончание(НО).
      /// </summary>
      public const string SF = "3";
    }
  }
}