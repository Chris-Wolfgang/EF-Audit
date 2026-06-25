using McMaster.Extensions.CommandLineUtils;

namespace Wolfgang.Audit.Cli.Tests.Unit.TestSupport;



/// <summary>
/// Minimal <see cref="IConsole"/> implementation that captures stdout and
/// stderr into <see cref="StringWriter"/>s the test can read. Avoids dragging
/// in McMaster's TestConsole and its sibling abstractions.
/// </summary>
internal sealed class StringConsole : IConsole, IDisposable
{
    private readonly StringWriter _out = new();
    private readonly StringWriter _err = new();

    public void Dispose()
    {
        _out.Dispose();
        _err.Dispose();
    }

    public TextWriter Out => _out;

    public TextWriter Error => _err;

    public TextReader In => TextReader.Null;

    public bool IsInputRedirected => true;

    public bool IsOutputRedirected => true;

    public bool IsErrorRedirected => true;

    public ConsoleColor ForegroundColor { get; set; }

    public ConsoleColor BackgroundColor { get; set; }

    public event ConsoleCancelEventHandler? CancelKeyPress;

    public string StdOut => _out.ToString();

    public string StdErr => _err.ToString();

    public void ResetColor() { }

    // The event needs at least one publisher to avoid "never used" warnings;
    // tests don't trigger Ctrl-C, so this is exercised only by the analyzer.
    internal void RaiseCancelKeyPressForCoverage() => CancelKeyPress?.Invoke(this, null!);
}
