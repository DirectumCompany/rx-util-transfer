using System.Collections.Generic;
using System.Linq;
using DrxTransfer.Engine;
using Newtonsoft.Json.Linq;

namespace DrxTransfer
{
  /// <summary>
  /// Обработчик элементов json.
  /// </summary>
  public static class SungeroRepository
  {
    /// <summary>
    /// Получение объектов по имени из json.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="content">json.</param>
    /// <param name="contentItem">Наименование объектов в json.</param>
    /// <param name="ignoreErrorObjectsFound">Если запись отсутствует или найдено несколько записей, обработка записи продолжается. Информация пишется в лог.</param>
    /// <param name="takeActiveOnly">Не учитывать закрытые записи.</param>
    /// <returns>список объектов.</returns>
    public static List<T> GetEntities<T>(Dictionary<string, object> content, string contentItem, bool ignoreErrorObjectsFound, bool takeActiveOnly)
    {
      var jArray = content[contentItem] as JArray;
      var jObjects = jArray.ToObject<List<JObject>>();

      var result = new List<T>();
      foreach (var jObject in jObjects)
      {
        var name = jObject.Property("Name").Value.ToString();
        var nameProperty = typeof(T).GetProperty("Name");

        if (nameProperty == null)
        {
          nameProperty = typeof(T).GetInterfaces().SelectMany(i => i.GetProperties()).FirstOrDefault(p => p.Name == "Name");

          if (nameProperty == null)
            throw new System.Reflection.TargetException(string.Format("Для объекта с типом {0} нет реквизита Name", typeof(T).Name));
        }

        var entities = Session.Current.GetEntities(typeof(T).FullName).Cast<T>().ToList().Where(i => nameProperty.GetValue(i).ToString() == name);

        if (takeActiveOnly)
        {
          var statusProperty = typeof(T).GetInterface("IDatabookEntry").GetProperty("Status");
          if (statusProperty == null)
          {
            throw new System.Reflection.TargetException(string.Format("Для объекта с типом {0} нет реквизита Status", typeof(T).Name));
          }
          entities = entities.Where(i => statusProperty.GetValue(i).ToString() == "Active");
        }

        if (entities.Any())
        {
          if (entities.Count() > 1)
          {
            if (ignoreErrorObjectsFound)
              Log.Console.Warn(string.Format("Найдено несколько сущностей с наименованием {0}", name));
            else
              throw new System.IO.InvalidDataException(string.Format("Найдено несколько сущностей с наименованием {0}", name));
          }
          else
            result.Add(entities.FirstOrDefault());
        }
        else
        {
          if (ignoreErrorObjectsFound)
            Log.Console.Warn(string.Format("Сущность {0} не найдена", name));
          else
            throw new System.IO.InvalidDataException(string.Format("Сущность {0} не найдена", name));
        }
      }

      return result;
    }
  }
}
