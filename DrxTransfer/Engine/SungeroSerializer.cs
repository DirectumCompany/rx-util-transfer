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
            
      var result = new List<Dictionary<string, object>>();

      var index = 1;
      foreach (var entity in entities)
      {
        Log.Console.Info(string.Format("Обработка записи {0} из {1}. ИД = {2}", index, entitiesCount, entity.Id));
        result.Add(new Dictionary<string, object>() { this.Export(entity) });
        index++;
      }

      Log.Console.Info("Сериализация объектов в json");
      string JSONBody = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
      {
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        ContractResolver = new SungeroEntitiesContractResolver()
      });

      using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default))
      {
        Log.Console.Info("Запись результата в файл");
        sw.WriteLine(JSONBody);
      }
    }
  }
}
