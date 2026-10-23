using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ExcelColumnExtractor.Scanners;

public class LockedFileStreamOpener : IDisposable
{
    public Stream Stream { get; }
    public bool IsTemp => !string.IsNullOrEmpty(TempFileName);
    private string? TempFileName { get; }

    public LockedFileStreamOpener(string fileName)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Stream = File.Open(fileName, FileMode.Open, FileAccess.Read);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (IOException)
        {
            TempFileName = Path.GetTempFileName();
            ForceCopy(fileName, TempFileName);
            Stream = File.Open(TempFileName, FileMode.Open, FileAccess.Read);
        }
    }

    private LockedFileStreamOpener(Stream stream, string? tempFileName)
    {
        Stream = stream;
        TempFileName = tempFileName;
    }

    public static async Task<LockedFileStreamOpener> CreateAsync(string fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            var stream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return new LockedFileStreamOpener(stream, null);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (IOException)
        {
            var tempFileName = Path.GetTempFileName();
            await ForceCopyAsync(fileName, tempFileName, cancellationToken);
            var stream = new FileStream(
                tempFileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return new LockedFileStreamOpener(stream, tempFileName);
        }
    }

    public void Dispose()
    {
        Stream.Dispose();
        if (!string.IsNullOrEmpty(TempFileName))
        {
            if (File.Exists(TempFileName))
            {
                File.Delete(TempFileName);
            }
        }

        GC.SuppressFinalize(this);
    }

    private static void ForceCopy(string src, string dst)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c copy /y \"{src}\" \"{dst}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            process.WaitForExit();
        }
        else
        {
            File.Copy(src, dst, overwrite: true);
        }
    }

    private static async Task ForceCopyAsync(string src, string dst, CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c copy /y \"{src}\" \"{dst}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            await process.WaitForExitAsync(cancellationToken);
        }
        else
        {
            await Task.Run(() => File.Copy(src, dst, overwrite: true), cancellationToken);
        }
    }
}
