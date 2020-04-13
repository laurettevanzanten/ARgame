using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PostStressTest
{
    [Flags]
    public enum OutputChannel
    {
        Console = 1,
        Debug   = 2,
        Memory  = 4,
    }

    public enum OutputLevel
    {
        Info = 1,
        Warning = 2,
        Error = 4,
        Fatal = 8
     };

    public class LogEntry
    {
        public string      Source { get; set; }

        public OutputLevel Level { get; set; }

        public string Message { get; set; }

        public DateTime TimeStamp { get; set; }
    }

    public class LogTable : ConcurrentDictionary<string, ConcurrentDictionary<LogEntry, LogEntry>>
    {
    }

    public class Log
    {
        public string DefaultChannel { get; set; } = "default";
        public string UnknownSource { get; set; } = "???";

        public OutputLevel LogLevel { get; set; } = OutputLevel.Info;

        public OutputChannel OutputSet { get; set; } = OutputChannel.Debug;

        private LogTable _log = new LogTable();


        public void Put(string message) => Put(DefaultChannel, UnknownSource, LogLevel, message);

        public void Put(OutputLevel level, string message) => Put(DefaultChannel, UnknownSource, level, message);

        public void Put(string source, string message) => Put(DefaultChannel, source, LogLevel, message);

        public void Put(OutputLevel level, string source, string message) => Put(DefaultChannel, source, level, message);

        public void Put(string channel, string source, OutputLevel level, string message)
        {
            if (level >= LogLevel)
            {
                var text = DateTime.Now + " [" + level + "], " + source + "@" + channel + ": " + message;
                
                if (OutputSet.HasFlag(OutputChannel.Console)) Console.WriteLine(text);
                if (OutputSet.HasFlag(OutputChannel.Debug)) Debug.WriteLine(text);
                if (OutputSet.HasFlag(OutputChannel.Memory)) TryWriteToTable(channel, source, level, message);
            }
        }

        public void FlushCSV(string fileName)
        {
            Flush(fileName, 
                () => "TIMESTAMP, LEVEL, CHANNEL, SOURCE, MESSAGE", 
                (channel, entry) =>
                {
                    return entry.TimeStamp + ", "
                           + entry.Level + ", "
                           + entry.Source + "@" + channel + ", "
                           + entry.Message;
                });
        }

        public void Flush(string fileName, Func<string> headerFormat, Func<string, LogEntry, string> format)
        {
            var timeStamp = DateTime.Now;
            var builder = new StringBuilder();

            // write all entries to the builder with a timestamp smaller than timeStamp. This way
            // entries added while flushing will be igored and saved for a later flush
            foreach (var kvp in _log)
            {
                foreach (var entry in kvp.Value.Keys)
                {
                    if (entry.TimeStamp < timeStamp)
                    {
                        var line = format(kvp.Key, entry);
                        Debug.WriteLine(line);
                        builder.AppendLine(line);
                    }
                }
            }

            if (File.Exists(fileName))
            {
                File.AppendAllText(fileName, builder.ToString());
            }
            else
            {
                if (headerFormat != null)
                {
                    builder.Insert(0, headerFormat() + "\r\n");
                }

                File.WriteAllText(fileName, builder.ToString());
            }

            // remove flushed items from the log
            foreach (var kvp in _log)
            {
                var entryArray = kvp.Value.Keys.Where( entry => entry.TimeStamp >= timeStamp);

                foreach (var entry in entryArray)
                {
                    _log[kvp.Key].TryRemove(entry, out var dummy);
                }
            }
        }

        private void TryWriteToTable(string channel, string source, OutputLevel level, string message)
        {
            if (!_log.TryGetValue(channel, out var queue))
            {
                queue = new ConcurrentDictionary<LogEntry, LogEntry>();
                _log[channel] = queue;
            }

            var newItem = new LogEntry()
            {
                Level = level,
                Source = string.IsNullOrEmpty(source) ? UnknownSource : source,
                Message = message,
                TimeStamp = DateTime.Now,
            };

            queue[newItem] = newItem;
        }
    }
}
