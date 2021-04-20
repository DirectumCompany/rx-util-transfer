using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public const string MetaSerializerTag = "Meta";
        private readonly string AppendNewCardFormat = ",\n\t \"Card\" : \n\t\t{0}";
        private readonly string AppendNewMetaFormat = ",\n\t  \"Meta\" : { \n\t\t{0} \n}";

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
            var metaData = SetMetaData();

            StringBuilder JSONBody = new StringBuilder();
            JSONBody.Append("[\n");
            var metaString = string.Empty;
            foreach (var data in metaData.First().Value as Dictionary<string, object>)
                metaString += string.Format("{0}:{0}\n", data.Key, data.Value);

            metaString += "}";
            Log.Console.Info(metaString);
            JSONBody.Append(string.Format(AppendNewMetaFormat, metaString));

            var index = 1;
            int successful = 0, error = 0;
            foreach (var entity in entities)
            {
                try
                {
                    Log.Console.Info(string.Format("Обработка записи {0} из {1}. ИД = {2}", index, entitiesCount, entity.Id));

                    Log.Console.Info(string.Format("Сериализация объекта с ИД = {0} в json", entity.Id));
                    string serializedObject = JsonConvert.SerializeObject(entity, Formatting.Indented, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        ContractResolver = new SungeroEntitiesContractResolver()
                    });
                    Log.Console.Info(string.Format("Сериализация объекта с ИД = {0} завершена", entity.Id));

                    JSONBody.Append(string.Format(AppendNewCardFormat, serializedObject));
                    //result.Add(new Dictionary<string, object>() { this.Export(entity) });
                    index++;
                    successful++;
                }
                catch (Exception ex)
                {
                    error++;
                    Log.Console.Error(ex, string.Format("Ошибка при серализации объекта в json. ИД = {0}", entity.Id));
                }
            }

            JSONBody.Append("\n]");
            using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default))
            {
                Log.Console.Info("Запись результата в файл");
                sw.WriteLine(JSONBody);
            }
        }

        private Dictionary<string,object> SetMetaData()
        {
            return new Dictionary<string, object>()
            {   new KeyValuePair<string, object>(MetaSerializerTag,
                new Dictionary<string, object>()
                {
                    new KeyValuePair<string, object>("EntityName", EntityName),
                    new KeyValuePair<string, object>("EntityTypeName", EntityTypeName)
                })
            };

        }
    }
}
