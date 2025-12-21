// The whole purpose of this crap is to intercept the 'Socket in use' exception
// and NOT have it printed -- with annoying traceback -- by the default logger.
// We catch it in Program.cs and print a much nicer error message.
//
// Thanks to this blog for getting me going:
// https://medium.com/@bhargavkoya56/build-a-production-ready-minimal-logger-in-c-for-asp-net-core-apis-343a8c544024
//
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System;

namespace photo_reviewer_4net;

public sealed class MinimalConsoleLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, MinimalConsoleLogger> _loggers = new();

    public MinimalConsoleLoggerProvider() {
    }

    public ILogger CreateLogger(string categoryName) {
        return _loggers.GetOrAdd(categoryName, name => new MinimalConsoleLogger(name));
    }

    public void Dispose() {
        _loggers.Clear();
    }
}

public sealed class MinimalConsoleLogger : ILogger
{
    private readonly string _categoryName;

    public MinimalConsoleLogger(string categoryName)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                           Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Don't show this ugly traceback for a known human error
        if (_categoryName == "Microsoft.Extensions.Hosting.Internal.Host"
                && exception != null
                && exception.ContainsException<Microsoft.AspNetCore.Connections.AddressInUseException>())
        {
            exception = null;
        }

        var message = formatter(state, exception);
        Console.WriteLine($"{_categoryName}: {message}");
        if (exception != null) {
            Console.WriteLine(exception.ToString());
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState:notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
