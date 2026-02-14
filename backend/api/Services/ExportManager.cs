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
    private Dictionary<string, InstanceExportInfo> _exports = new();

    ~ExportManager()
    {
        foreach (var e in _exports.Values)
        {
            if (e.LastFile != null) File.Delete(e.LastFile);
        }
    }

    public InstanceExportInfo? GetExportPath(string path)
        => _exports.TryGetValue(path, out var exportPath) ? exportPath : null;

    public bool DownloadAllMusic(string path)
    {
        InstanceExportInfo info;
        lock(_exports)
        {
            if (_exports.TryGetValue(path, out info) && info.IsBusy == ExportStatus.Building) return false;

            if (info == null)
            {
                info = new()
                {
                    BaseFolder = path,
                    InstanceInfo = Serialization.Deserialize<EuphoniaInfo>(File.ReadAllText($"{path}/info.json"))
                };
                _exports.Add(path, info);
            }
            else if (info.LastFile != null)
            {
                File.Delete(info.LastFile);
                info.LastFile = null;
            }

            info.IsBusy = ExportStatus.Building;
        }

        Task.Run(() =>
        {
            var exportPath = Path.Combine(path, "export");
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

            var file = $"{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            using var zip = ZipFile.Open($"{path}export/{file}", ZipArchiveMode.Create);
            foreach (var m in info.InstanceInfo.Musics)
            {
                if (!m.IsArchived)
                {
                    zip.CreateEntryFromFile($"{path}normalized/{m.Path}", m.Path);
                }
            }

            lock (_exports)
            {
                _exports[path].IsBusy = ExportStatus.Ready;
                _exports[path].LastFile = file;
            }
        });

        return true;
    }
}
