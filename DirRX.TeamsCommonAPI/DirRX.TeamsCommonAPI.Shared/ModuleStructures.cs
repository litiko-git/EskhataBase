using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.TeamsCommonAPI.Structures.Module
{
  [Public]
  partial class SearchRecipientsResult
  {
    public List<DirRX.TeamsCommonAPI.Structures.Module.IRecipientDto> recipients { get; set; }
  }
  
  [Public]
  partial class RecipientDto
  {
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string JobTitle { get; set; }
    
    public string Department { get; set; }
    
    public string Thumbnail { get; set; }
    
    public bool IsActive { get; set; }
  }
  
  [Public]
  partial class EntityCollectionRequest
  {
    public List<DirRX.TeamsCommonAPI.Structures.Module.IEntityRequest> EntityCollection { get; set; }
  }
  
  [Public]
  partial class EntityRequest
  {
    public string Guid { get; set; }
    
    public long Id { get; set; }
    
    public bool IsIncludeBinaryValues { get; set; }
  }
  
  [Public]
  partial class EntityCollectionResponse
  {
    public List<DirRX.TeamsCommonAPI.Structures.Module.IEntityResponse> Entities { get; set; }
  }
  
  [Public]
  partial class EntityResponse
  {
    public string Guid { get; set; }
    
    public long Id { get; set; }
    
    public bool IsUnauthorized { get; set; }
    
    public bool IsIncludeBinaryValues { get; set; }
    
    public bool IsError { get; set; }
    
    public string ErrorCause { get; set; }
    
    public System.Collections.Generic.Dictionary<string, DirRX.TeamsCommonAPI.Structures.Module.IProperty> Properties { get; set; }
  }
  
  [Public]
  partial class Property
  {
    public string Type { get; set; }
    
    #pragma warning disable DS0010
    public object Value { get; set; }
  }
  
  [Public]
  partial class NavigationValue
  {
    public long Id { get; set; }
    
    public Guid TypeGuid { get; set; }
  }
  
  [Public]
  partial class BinaryDataValue
  {
    public Guid? Id { get; set; }
    
    public long Size { get; set; }
    
    public string Hash { get; set; }
    
    public DirRX.TeamsCommonAPI.Structures.Module.IStorage Storage { get; set; }

  }
  
  [Public]
  partial class Storage
  {
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    public string Address { get; set; }

  }
  
  [Public]
  partial class UserThumbnailDto
  {
    public long Id { get; set; }
    
    public byte[] Thumbnail { get; set; }
    
  }
}