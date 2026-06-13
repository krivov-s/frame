using Frame.Shared;

namespace Frame.WebLib;

/// <summary>
/// Конфигурации путей. Единственная причина, для чего создан этот класс - из-за принудительного
/// использования относительного пути /api для AppFarm. Это приводит к тому, что в нескольких местах
/// при формировании url для клиента нужно использовать этот базовый /api вручную. А этого не хочется :)
/// Поэтому вынес в отдельный класс настроек.
/// <para>Предполагаются следующие значения:</para>
/// <list type="bullet">
/// <item>"BasePath":"/api"</item>
/// <item>"LoginControllerPath":"/api/login"</item>
/// <item>"GetFileControllerPath":"/api/getfile"</item>
/// </list>
/// </summary>
public class CoreWebSettings
{
    /// <summary>
    /// Смещение от корневого Url, которое может быть указано в настройках системы 
    /// </summary>
    public string BasePath { get; set; } = "";
    
    /// <summary>
    /// Относительный путь к контроллеру Login 
    /// </summary>
    public string LoginControllerPath { get; set; } = "";

    /// <summary>
    /// Относительный путь к контроллеру GetFile 
    /// </summary>
    public string GetFileControllerPath { get; set; } = "";

    /// <summary>
    /// Время хранения считанной в память фотографии в кэше, мин.
    /// </summary>
    public int ImagesInCacheLifetime { get; set; } = 5;
    
    public void Verify()
    {
        if (string.IsNullOrEmpty(LoginControllerPath))
        {
            throw new FrameException($"Параметр {nameof(CoreWebSettings)}.{nameof(LoginControllerPath)} не установлен");
        }
        
        if (string.IsNullOrEmpty(GetFileControllerPath))
        {
            throw new FrameException($"Параметр {nameof(CoreWebSettings)}.{nameof(GetFileControllerPath)} не установлен");
        }
        
        if(BasePath.Length > 0 && BasePath[0] != '/') BasePath = $"/{BasePath}";
        if(LoginControllerPath[0] != '/') LoginControllerPath = $"/{LoginControllerPath}";
        if(GetFileControllerPath[0] != '/') GetFileControllerPath = $"/{GetFileControllerPath}";
    }
}