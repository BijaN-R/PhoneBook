// FILE: src/PhoneBook.Core/Layout/FontProvider.cs
using SkiaSharp;

namespace PhoneBook.Core.Layout;

public sealed class FontProvider : IDisposable
{
    private const string DefaultFamily = "Vazirmatn";
    private readonly string _baseFontsDirectory;
    private readonly Lazy<SKTypeface> _regular;
    private readonly Lazy<SKTypeface> _bold;
    private bool _disposed;

    public FontProvider(string baseFontsDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFontsDirectoryPath);

        _baseFontsDirectory = Path.GetFullPath(baseFontsDirectoryPath);
        _regular = new Lazy<SKTypeface>(
            () => LoadTypeface(GetFontPath(DefaultFamily)),
            LazyThreadSafetyMode.ExecutionAndPublication);
        _bold = new Lazy<SKTypeface>(
            () => LoadTypeface(Path.Combine(_baseFontsDirectory, $"{DefaultFamily}-Bold.ttf")),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SKTypeface GetRegular()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _regular.Value;
    }

    public SKTypeface GetBold()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _bold.Value;
    }

    public string GetFontPath(string family)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(family);

        string safeFamily = Path.GetFileName(family);
        if (!string.Equals(safeFamily, family, StringComparison.Ordinal))
        {
            throw new ArgumentException("Font family must be a file-name-safe family name.", nameof(family));
        }

        return Path.Combine(_baseFontsDirectory, $"{safeFamily}-Regular.ttf");
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

    private static SKTypeface LoadTypeface(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required font file was not found: '{path}'.", path);
        }

        return SKTypeface.FromFile(path)
            ?? throw new InvalidDataException($"The font file '{path}' could not be loaded by SkiaSharp.");
    }
}
