using Euphonia.Common;
using System.IO.Compression;

namespace Euphonia.API.Services;

public class InstanceExportInfo
{
    public string? LastFile { set; get; } = null;
    public EuphoniaInfo InstanceInfo { set; get; }
    public string BaseFolder { set; get; }
    public ExportStatus IsBusy { set; get; } = ExportStatus.None;
}

public enum ExportStatus
{
    None,
    Building,
    Ready
}

public class ExportManager
{
    InstanceExportInfo _export = new();

    ~ExportManager()
    {
        if (_export.LastFile != null) File.Delete(_export.LastFile);
    }

    public InstanceExportInfo? GetExportPath()
        => _export;

    public bool DownloadAllMusic(ILogger logger)
    {
        lock(_export)
        {
            if (_export.IsBusy == ExportStatus.Building) return false;

            _export.InstanceInfo = Serialization.Deserialize<EuphoniaInfo>(File.ReadAllText($"/data/info.json"));
            if (_export.LastFile != null)
            {
                File.Delete(_export.LastFile);
                _export.LastFile = null;
            }

            _export.IsBusy = ExportStatus.Building;
        }

        Task.Run(() =>
        {
            string file = null;
            try
            {
                var exportPath = "/data/export/";
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }
                else
                {
                    // Just in case
                    foreach (var f in Directory.GetFiles(exportPath))
                    {
                        File.Delete(f);
                    }
                }

                file = $"{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                using var zip = ZipFile.Open($"/data/export/{file}", ZipArchiveMode.Create);
                foreach (var m in _export.InstanceInfo.Musics)
                {
                    if (!m.IsArchived && File.Exists($"/data/normalized/{m.Path}"))
                    {
                        zip.CreateEntryFromFile($"/data/normalized/{m.Path}", m.Path);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                lock (_export)
                {
                    _export.IsBusy = ExportStatus.Ready;
                    _export.LastFile = file;
                }
            }
        });

        return true;
    }
}
