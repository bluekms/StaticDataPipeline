using System.Text;

namespace ExcelColumnExtractor.Scanners;

// Excel 이 쓰기 잠금을 쥔 파일도 ReadWrite 공유로 열린다. 완전 배타 잠금(공유 위반)일 때만 복사본으로 읽는다.
public class LockedFileStreamOpener : IDisposable
{
    private const int SharingViolation = 32;
    private const int LockViolation = 33;

    public Stream Stream { get; }
    public bool IsTemp => !string.IsNullOrEmpty(TempFileName);
    private string? TempFileName { get; }

    public LockedFileStreamOpener(string fileName)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            Stream = Open(fileName);
        }
        catch (IOException e) when (IsSharingViolation(e))
        {
            TempFileName = CopyToTemp(fileName, e);
            Stream = Open(TempFileName);
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
            return new LockedFileStreamOpener(Open(fileName), null);
        }
        catch (IOException e) when (IsSharingViolation(e))
        {
            var tempFileName = await Task.Run(() => CopyToTemp(fileName, e), cancellationToken).ConfigureAwait(false);

            return new LockedFileStreamOpener(Open(tempFileName), tempFileName);
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

    private static FileStream Open(string fileName)
    {
        return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
    }

    private static bool IsSharingViolation(IOException e)
    {
        var code = e.HResult & 0xFFFF;

        return code is SharingViolation or LockViolation;
    }

    private static string CopyToTemp(string fileName, IOException cause)
    {
        var tempFileName = Path.GetTempFileName();
        try
        {
            File.Copy(fileName, tempFileName, overwrite: true);
        }
        catch (Exception e)
        {
            File.Delete(tempFileName);
            throw new IOException(e.Message, cause);
        }

        return tempFileName;
    }
}
