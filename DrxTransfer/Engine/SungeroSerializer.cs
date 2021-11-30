using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Domain.Client;
using Sungero.Domain.Shared;

namespace DrxTransfer.Engine
{
  /// <summary>
  /// Сериализатор объекта.
  /// </summary>
  public abstract class SungeroSerializer
  {
    // TODO: убрать сеттер, передовать в конструкторе.
    public string EntityName { get; set; }
    public string EntityTypeName { get; set; }

    public Dictionary<string, object> content;

    /// <summary>
    /// Описание экспорта сущности.
    /// </summary>
    /// <param name="entity">Объект.</param>
    /// <returns>Словарь с описанием реквизитов сущности.</returns>
    protected virtual Dictionary<string, object> Export(IEntity entity)
    {
      content["Card"] = entity;
      return content;
    }

    /// <summary>
    /// Создание, заполнение реквизитов и сохранение сущности.
    /// </summary>
    /// <param name="jsonBody"></param>
    public virtual void Import(Dictionary<string, object> jsonBody)
    {

    }

    /// <summary>
    /// Фильтрация выгружаемых записей.
    /// </summary>
    /// <param name="entities">Все сущности выгружаемого типа.</param>
    /// <returns>Список отфильтрованных сущностей.</returns>
    public virtual IEnumerable<IEntity> Filter(IEnumerable<IEntity> entities)
    {
      return entities;
    }

    /// <summary>
    /// Сериализация объектов в json.
    /// </summary>
    /// <param name="filePath">Путь к файлу для записи.</param>
    internal void Serialize(string filePath)
    {
      Log.Console.Info(string.Format("Сериализация объектов типа {0}", this.EntityTypeName));
      this.content = new Dictionary<string, object>();

      var type = Session.GetTypeNameGuid(Session.GetAppliedType(this.EntityTypeName)).GetTypeByGuid();

      var entities = Session.Current.GetEntities(this.EntityTypeName).AsEnumerable();
      entities = this.Filter(entities);

      var entitiesCount = entities.Count();
      Log.Console.Info(string.Format("Найдено {0} объектов", entitiesCount));

      List<string> result = new List<string>();
      Dictionary<string, object> entityTypeName = new Dictionary<string, object>();
      entityTypeName.Add(CommandLine.options.EntityType, type);
      result.Add(JsonConvert.SerializeObject(entityTypeName, Formatting.Indented, new JsonSerializerSettings
      {
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        ContractResolver = new SungeroEntitiesContractResolver()
      }));

      var index = 1;
      foreach (var entity in entities)
      {
        try
        {
          Log.Console.Info(string.Format("Обработка записи {0} из {1}. ИД = {2}", index, entitiesCount, entity.Id));
          index++;
          result.Add(JsonConvert.SerializeObject(this.Export(entity), Formatting.Indented, new JsonSerializerSettings
          {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new SungeroEntitiesContractResolver()
          }));
        }
        catch (Exception ex)
        {
          Log.Console.Error(string.Format("Ошибка при попытке обработки и сериализации записи с ИД = {0}", entity.Id));
          Client.Log.Fatal(ex.Message);
        }
      }
      Log.Console.Info(string.Format("Обработано {0} записей из {1}.", result.Count - 1, entitiesCount));
      using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default))
      {
        Log.Console.Info("Запись результата в файл");
        var body = string.Format("[{0}]", string.Join(",", result));
        sw.WriteLine(body);
      }
    }
  }
}
