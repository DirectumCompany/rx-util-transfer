using NLog;
using NLog.Targets;

namespace DrxTransfer
{
  /// <summary>
  /// Логгер синхронизации.
  /// </summary>
  public static class Log
  {
    /// <summary>
    /// Консоль.
    /// </summary>
    public static readonly ILogger Console = LogManager.GetLogger("Transfer.Console");

    /// <summary>
    /// Указать файл логгеру протокола.
    /// </summary>
    /// <param name="protocolFilePath">Путь к файлу протокола.</param>
    public static void SetProtocolLoggerFileName(string protocolFilePath)
    {
      var logsConfiguration = LogManager.Configuration;
      var wrappedProtocolTarget = logsConfiguration.FindTargetByName("protocolfile");
      var protocolTarget = wrappedProtocolTarget.GetType().GetProperty("WrappedTarget").GetValue(wrappedProtocolTarget) as FileTarget;
      if (protocolTarget == null)
        return;

      if (System.IO.File.Exists(protocolFilePath))
        protocolTarget.FileName = protocolFilePath;

      LogManager.ReconfigExistingLoggers();
    }
  }
}
