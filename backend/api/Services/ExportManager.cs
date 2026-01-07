using System.IO.Compression;

namespace Euphonia.API.Services;

public class InstanceExportInfo
{
    public string? LastFile { set; get; } = null;
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
        lock(_exports)
        {
            if (_exports.TryGetValue(path, out var info) && info.IsBusy == ExportStatus.Building) return false;

            if (info == null)
            {
                info = new();
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
            ZipFile.CreateFromDirectory($"{path}normalized/", $"{path}/export/{file}");

            lock (_exports)
            {
                _exports[path].IsBusy = ExportStatus.Ready;
                _exports[path].LastFile = file;
            }
        });

        return true;
    }
}
