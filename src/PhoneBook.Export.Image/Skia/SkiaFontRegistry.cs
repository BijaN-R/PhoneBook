// FILE: src/PhoneBook.Export.Image/Skia/SkiaFontRegistry.cs
using SkiaSharp;

namespace PhoneBook.Export.Image.Skia;

public sealed class SkiaFontRegistry : IDisposable
{
    private const string RegularFileName = "Vazirmatn-Regular.ttf";
    private const string BoldFileName = "Vazirmatn-Bold.ttf";

    private readonly string _fontsDirectory;
    private readonly Lazy<SKTypeface> _regular;
    private readonly Lazy<SKTypeface> _bold;
    private bool _disposed;

    public SkiaFontRegistry()
        : this(Path.Combine(AppContext.BaseDirectory, "wwwroot", "fonts"))
    {
    }

    public SkiaFontRegistry(string fontsDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontsDirectoryPath);

        _fontsDirectory = Path.GetFullPath(fontsDirectoryPath);
        _regular = new Lazy<SKTypeface>(
            () => LoadTypeface(RegularFileName),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _bold = new Lazy<SKTypeface>(
            () => LoadTypeface(BoldFileName),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SKTypeface Regular
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _regular.Value;
        }
    }

    public SKTypeface Bold
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bold.Value;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_regular.IsValueCreated)
        {
            _regular.Value.Dispose();
        }

        if (_bold.IsValueCreated)
        {
            _bold.Value.Dispose();
        }

        _disposed = true;
    }

    private SKTypeface LoadTypeface(string fileName)
    {
        string path = Path.Combine(_fontsDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required font file was not found: '{path}'.", path);
        }

        return SKFontManager.Default.CreateTypeface(path, 0)
            ?? throw new InvalidDataException($"The font file '{path}' could not be loaded by SkiaSharp.");
    }
}
