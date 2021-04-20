using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DrxTransfer.Engine
{
    /// <summary>
    /// Конвертация объектов.
    /// </summary>
    public class TransferEngine
    {
        #region Singletone Implementation

        private static TransferEngine instance;
        public static TransferEngine Instance
        {
            get
            {
                if (instance == null)
                    instance = new TransferEngine();
                return instance;
            }
        }

        #endregion

        #region Методы

        /// <summary>
        /// Запуск импорта данных.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        public void Import(string filePath)
        {
            try
            {
                Log.Console.Info("Загрузка сериализатора");

                string jsonText = string.Empty;
                using (StreamReader sr = new StreamReader(filePath, System.Text.Encoding.Default))
                {
                    Log.Console.Info("Чтение файла");
                    jsonText = sr.ReadToEnd();
                }
                var jsonBody = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonText);

                SungeroSerializer serializer = null;
                try
                {
                    var entityMeta = jsonBody.FirstOrDefault(z => z.Keys.Contains(SungeroSerializer.MetaSerializerTag))[SungeroSerializer.MetaSerializerTag] as JObject;
                    serializer = SerializersRepository.Instance.GetSerializerForEntityType(entityMeta["EntityName"].ToString());
                }
                catch (Exception ex) 
                {
                    Log.Console.Error(string.Format("Не удалось получить сериализатор для файла: {0}. \n {1}", filePath, ex.Message));
                    return;
                }

                if (serializer != null)
                {
                    Log.Console.Info(string.Format("Сериализатор {0} успешно загружен", CommandLine.options.EntityType));
                    var index = 1;
                    var jsonItemsCount = jsonBody.Count();
                    foreach (var jsonItem in jsonBody)
                    {
                        try
                        {
                            Log.Console.Info(string.Format("Запись {0} из {1}", index, jsonItemsCount));
                            index++;
                            serializer.Import(jsonItem);
                        }
                        catch (Exception ex)
                        {
                            Log.Console.Error(ex);
                            Log.Console.Error("Запись не создана");
                            Session.Current.Dispose();
                        }
                    }
                }
                else
                {
                    throw new DllNotFoundException(string.Format("Сериализатор {0} не найден", CommandLine.options.EntityType));
                }
            }
            catch (Exception ex)
            {
                Log.Console.Error(ex);
            }
        }

        /// <summary>
        /// Запуск экспорта данных в json файл.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        public void Export(string filePath)
        {
            try
            {
                Log.Console.Info("Загрузка сериализатора");
                var serializer = SerializersRepository.Instance.GetSerializerForEntityType(CommandLine.options.EntityType);

                if (serializer != null)
                {
                    Log.Console.Info(string.Format("Сериализатор {0} успешно загружен", CommandLine.options.EntityType));
                    serializer.Serialize(filePath);
                    return;
                }
                else
                {
                    throw new DllNotFoundException(string.Format("Сериализатор {0} не найден", CommandLine.options.EntityType));
                }
            }
            catch (Exception ex)
            {
                Log.Console.Error(ex);
            }
        }

        #endregion
    }
}
