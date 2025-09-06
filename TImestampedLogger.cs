using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Singleton logger to be used in all classes and before knowing the log file path
public sealed class Logger : TextWriter
{
    private static readonly Logger _instance = new Logger();
    private readonly TextWriter _originalOut;
    private StreamWriter? _fileWriter;
    private readonly List<string> _buffer = new(); // holds logs before file is attached

    private Logger()
    {
        _originalOut = Console.Out;
    }

    public static Logger Instance => _instance;

    public void SetLogFile(string logFilePath, bool append = true)
    {
        _fileWriter?.Dispose();

        _fileWriter = new StreamWriter(logFilePath, append, Encoding.UTF8)
        {
            AutoFlush = true
        };

        // flush buffer to file
        foreach (var line in _buffer)
        {
            _fileWriter.WriteLine(line);
        }
        _buffer.Clear();

        WriteLine($"Logger now writing to {logFilePath}");
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        string timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {value}";
        
        // Always write to console
        _originalOut.WriteLine(timestamped);

        if (_fileWriter != null)
        {
            _fileWriter.WriteLine(timestamped);
        }
        else
        {
            // keep in memory until file is attached
            _buffer.Add(timestamped);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileWriter?.Dispose();
        }
        base.Dispose(disposing);
    }
}
