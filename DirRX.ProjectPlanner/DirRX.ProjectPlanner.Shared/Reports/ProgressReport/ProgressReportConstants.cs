using System;
using Sungero.Core;

namespace DirRX.ProjectPlanner.Constants
{
  public static class ProgressReport
  {
    public const string MilestonesTableName = "DirRX_PP_Reports_Milestones";
    public const string OverdueStagesTableName = "DirRX_PP_Reports_Overdue_Stages";
    public const string CompletedStagesTableName = "DirRX_PP_Reports_Completed_Stages";
    public const string CurrentStagesTableName = "DirRX_PP_Reports_Current_Stages";
    public const string CompletedTicketsTableName = "DirRX_PP_Reports_Completed_Tickets";
    
    public const string AgileBoardGuid = "3507255b-e3e9-47ac-8e1e-99d046ab93c1";
    
    public const int CountTableFirst = 1;
    public const int DurationFactFirst = 0;
    
    public const int OneWeekInDays = 7;
    public const int TwoWeekInDays = 14;
    public const int OneMonth = 1;
    public const int QuarterInMonths = 3;
  }
}