namespace DrxTransfer
{
  class CommandLine
  {
    /// <summary>
    ///   Параметры запуска приложения.
    /// </summary>
    internal static CommandLineOptions options;

    /// <summary>
    /// Проверка на прикладные параметры запуска утилиты.
    /// </summary>
    internal static void RunParsedArgs()
    {
      if (options == null)
      {
        Log.Console.Info("Необходимо указать параметры запуска");
        return;
      }

      if (options.Import != null)
      {
        Log.Console.Info("Старт импорта данных");
        Engine.TransferEngine.Instance.Import(options.Import);
        return;
      }

      if (options.Export != null)
      {
        Log.Console.Info("Старт экспорта данных");
        Engine.TransferEngine.Instance.Export(options.Export);
        return;
      }
    }
  }
}
