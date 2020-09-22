using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Domain.Client.Extensions;
using Sungero.Domain.Shared;

namespace DrxTransfer.Engine
{
  /// <summary>
  /// Работа с объектной моделью.
  /// </summary>
  public class Session : IDisposable
  {
    #region Singleton Implementation

    /// <summary>
    /// Сессия DirectumRX.
    /// </summary>
    private Sungero.Domain.Client.Session session;

    private static Session current;
    public static Session Current
    {
      get
      {
        if (current == null)
          current = new Session();
        return current;
      }
    }

    #endregion

    #region Работа с сессией

    /// <summary>
    /// Создать сущность DirectumRX по типу.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <returns>Сущность DirectumRX.</returns>
    public IEntity CreateEntity(Type type)
    {
      return this.session.Create(type);
    }

    /// <summary>
    /// Создать сущность DirectumRX по типу.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <returns>Сущность DirectumRX.</returns>
    public IEntity CreateEntity(string type)
    {
      return this.session.Create(GetAppliedType(type));
    }

    /// <summary>
    /// Обновить сущности в сессии.
    /// </summary>
    /// <param name="entities">Сущности DirectumRX.</param>
    public void UpdateEntities(IEnumerable<IEntity> entities)
    {
      // Bug #67107
      // Упорядочиваем сущности по возрастанию ID, чтобы они обновлялись в порядке создания.
      // Возможны падания платформы если:
      // сущность имеет свойство-ссылку на ранее созданную сущность и
      // обновляется перед тем как обновится сущность, на которую она ссылается.
      entities = entities.OrderBy(e => e.Id);
      this.session.Update(entities);
    }

    /// <summary>
    /// Удалить сущность DirectumRX.
    /// </summary>
    /// <param name="entity">Сущность DirectumRX.</param>
    public void DeleteEntity(IEntity entity)
    {
      this.session.Delete(entity);
    }

    /// <summary>
    /// Применить изменения.
    /// </summary>
    public void SubmitChanges()
    {
      this.session.SubmitChanges();
    }

    /// <summary>
    /// Очистить сессию.
    /// </summary>
    public void Clear()
    {
      this.session.Clear();
    }

    /// <summary>
    /// Удалить сущность из Session.entityCache.
    /// </summary>
    /// <param name="primaryEntity">Сущность DirectumRX.</param>
    public void RemoveFromEntityCache(IEntity primaryEntity)
    {
      var entityCache = this.session.GetType()
                                    .GetField("entityCache",
                                              System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                    .GetValue(session) as List<IEntity>;

      // Удалить из кэша сущностей основную сущность.
      if (entityCache.Contains(primaryEntity))
      {
        entityCache.Remove(primaryEntity);
        //Log.DrxUtilLog.Info("({0}) {1} удалена из сессии.", primaryEntity.Id, primaryEntity.DisplayValue);
      }

      // Удалить из кэша сущностей права на основную сущность, если такие имеются.
      var accessRightsCache = entityCache.Where(x => x is Sungero.CoreEntities.IAccessRights).Cast<Sungero.CoreEntities.IAccessRights>().ToList();
      var primaryEntityBaseGuid = primaryEntity.GetEntityMetadata().BaseGuid;
      accessRightsCache = accessRightsCache.Where(x => x.EntityId == primaryEntity.Id &&
                                                       (x.EntityTypeGuid == primaryEntityBaseGuid ||
                                                        x.EntityTypeGuid == primaryEntity.TypeDiscriminator)).ToList();
      foreach (var accessRights in accessRightsCache)
      {
        entityCache.Remove(accessRights);
        //Log.DrxUtilLog.Info("AccessRights {0} для ({1}) {2} удалены из сессии.", accessRights.DisplayValue, primaryEntity.Id, primaryEntity.DisplayValue);
      }

      this.session.GetType()
                  .GetField("entityCache",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                  .SetValue(session, entityCache);
    }

    #endregion

    #region Получение сущностей

    /// <summary>
    /// Получить сущность DirectumRX по ИД.
    /// </summary>
    /// <param name="type">Тип сущности.</param>
    /// <param name="id">Id.</param>
    /// <returns>Сущность DirectumRX.</returns>
    public IEntity GetEntityById(Type type, int id)
    {
      var entityList = this.session.GetAll(type).Where(x => x.Id == id);
      var entity = entityList.FirstOrDefault();
      return entity;
    }

    /// <summary>
    /// Получить сущность DirectumRX по ИД.
    /// </summary>
    /// <param name="entityTypeGuid">Гуид типа сущности.</param>
    /// <param name="id">Id.</param>
    /// <returns>Сущность DirectumRX.</returns>
    public IEntity GetEntityById(Guid entityTypeGuid, int id)
    {
      return this.GetEntityById(entityTypeGuid.GetTypeByGuid(), id);
    }

    /// <summary>
    /// Получить сущности DirectumRX по гуиду типа.
    /// </summary>
    /// <param name="entityTypeGuid">Guid типа сущности.</param>
    /// <returns>Список сущностей DirectumRX.</returns>
    public IQueryable<IEntity> GetEntities(Guid entityTypeGuid)
    {
      return this.GetEntities(entityTypeGuid.GetTypeByGuid());
    }

    /// <summary>
    /// Получить сущности DirectumRX по типу.
    /// </summary>
    /// <param name="type">Тип сущности.</param>
    /// <returns>Список сущностей DirectumRX.</returns>
    public IQueryable<IEntity> GetEntities(Type type)
    {
      return this.session.GetAll(type);
    }

    /// <summary>
    /// Получить сущности DirectumRX по типу.
    /// </summary>
    /// <param name="type">Тип сущности.</param>
    /// <returns>Список сущностей DirectumRX.</returns>
    public IQueryable<IEntity> GetEntities(string type)
    {
      return this.session.GetAll(GetTypeNameGuid(GetAppliedType(type)).GetTypeByGuid());
    }

    /// <summary>
    /// Получить NameGuid типа сущности DirectumRX.
    /// </summary>
    /// <param name="entityType">Тип сущности DirectumRX.</param>
    /// <returns>NameGuid типа сущности DirectumRX.</returns>
    public static Guid GetTypeNameGuid(Type entityType)
    {
      var entityTypeMetadata = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(entityType);
      return entityTypeMetadata != null
               ? entityTypeMetadata.NameGuid
               : Guid.Empty;
    }

    /// <summary>
    /// Получить прикладной или платформенный тип.
    /// </summary>
    /// <param name="typeName">Имя типа.</param>
    /// <returns>Прикладной или платформенный тип.
    /// Перекрытый тип, если тип перекрыт.</returns>
    /// <remarks>Формат имени типа: <ИмяРешения>.<ИмяМодуля>.<ИнтерфейсТипа>.</remarks>
    public static Type GetAppliedType(string typeName)
    {
      Type type;

      try
      {
        type = Type.GetType(string.Format("{0}, Sungero.Domain.Interfaces", typeName));
        if (type != null)
          type = type.GetFinalType();

        if (type == null)
          type = Type.GetType(string.Format("{0}, Sungero.Domain.Shared", typeName));
        
        if (type == null)
          type = Type.GetType(string.Format("{0}, Sungero.Content.Shared", typeName));

        if (type == null)
        {
          throw new System.TypeLoadException(string.Format("Тип {0} не найден.", typeName));
        }
      }
      catch (Exception ex)
      {
        //Log.Console.Error(ex);
        throw;
      }

      return type;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Освободить ресурсы.
    /// </summary>
    public void Dispose()
    {
      this.session.Clear();
      this.session.Dispose();
    }

    #endregion

    #region Конструктор

    /// <summary>
    /// Создать сессию DirectumRX.
    /// </summary>
    private Session()
    {
      this.session = Sungero.Domain.Client.Session.Current ?? new Sungero.Domain.Client.Session();
    }

    #endregion
  }
}
