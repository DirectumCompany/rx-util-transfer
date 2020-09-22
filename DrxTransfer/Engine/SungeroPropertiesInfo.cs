using System;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace DrxTransfer.Engine
{
  /// <summary>
  /// Класс для работы с платформенными даннми свойств.
  /// </summary>
  class SungeroPropertiesInfo : EntityItemsInfo
  {
    public SungeroPropertiesInfo(Type entityType) : base(entityType)
    {

    }

    /// <summary>
    /// Проверка возможности сериализации свойства.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns>Метаданные свойства.</returns>
    public bool IsSerializibleProperty(string propertyName)
    {
      if (this.EntityMetadata != null)
      {
        var metadata = this.EntityMetadata.GetProperty(propertyName);
        if (metadata != null)
          return !metadata.IsInternal && metadata.PropertyType != PropertyType.Navigation;
      } 

      return false;
    }
  }
}
