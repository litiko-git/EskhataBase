using System;
using Sungero.Core;

namespace DirRX.TeamsCommonAPI.Constants
{
  public static class Module
  {
    /// <summary>
    /// Допустимая длина заголовка задачи.
    /// http://aura.npo-comp.ru/sungero?type=7197cc31-bf64-406e-8ead-0a7dde1c3c6b&id=134965
    /// </summary>
    [Public]
    public const int TaskTitleMaxLength = 250;
    
    public const string ProjectPlanGuid = "f2894e4d-a950-497f-a311-1fd7e9665d28";
    public const string AgileBoardsGuid = "c00e3e1f-f948-431b-b211-4924cb2ed84d";
    public const int SizePersonalPhotoThumbnail = 36;
    public const int MaxRecepintCount = 25;
    
    public const string IncorrectGuidFormat = "IncorrectGuidFormat";
    public const string TypeByGuidNotFound = "TypeByGuidNotFound";
    public const string EntityNotFound = "EntityNotFound";
    
    public static readonly Guid AgileBoardsRole = Guid.Parse("6AA1E3CE-DFD0-4CBC-B4AB-EFDA23765795");
    public static readonly Guid ProjectManagersRoleGuid = Guid.Parse("61016C45-E26C-4CF8-B4BE-09F191AC1BCA");
    
  }
}