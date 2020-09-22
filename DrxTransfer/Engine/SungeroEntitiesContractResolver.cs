using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DrxTransfer.Engine
{
  /// <summary>
  /// Класс переопределения процесса сериализации объектов, для исключения зацикливания и ненужных свойств.
  /// </summary>
  class SungeroEntitiesContractResolver : DefaultContractResolver
  {
    public SungeroEntitiesContractResolver()
    {

    }

    /// <summary>
    /// Создание json объекта из свойства.
    /// </summary>
    /// <param name="member">Свойство.</param>
    /// <param name="memberSerialization">Параметр сериализации.</param>
    /// <returns>json объект.</returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
      // TODO: вынести в список явно исключаемые свойства объектов.
      if (member.Name == "Id" || member.Name == "PersonalPhoto")
        return null;

      var entity = base.CreateProperty(member, memberSerialization);

      if (entity.DeclaringType.IsValueType)
        return entity;

      var prop = new SungeroPropertiesInfo(entity.DeclaringType);
      if (prop.IsSerializibleProperty(member.Name))
        return entity;

      return null;
    }
  }
}
