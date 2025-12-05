// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MapDebugLogger.cs" company="">
//   Debug logging for map loading operations
// </copyright>
// <summary>
//   Static debug logger for map loading operations with detailed offset and source tracking.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Map
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Log entry severity levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Debug level - detailed internal information.</summary>
        DBG,
        /// <summary>Information level - general progress messages.</summary>
        INF,
        /// <summary>Warning level - non-fatal issues.</summary>
        WRN,
        /// <summary>Error level - failures and errors.</summary>
        ERR
    }

    /// <summary>
    /// A single log entry with timestamp, level, and message.
    /// </summary>
    public class LogEntry
    {
        /// <summary>Timestamp when the entry was created.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Severity level of the entry.</summary>
        public LogLevel Level { get; set; }

        /// <summary>Log message content.</summary>
        public string Message { get; set; }

        /// <summary>Source file that generated this log (e.g., lockout.map, shared.map).</summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Format the entry for display.
        /// </summary>
        public override string ToString()
        {
            string src = string.IsNullOrEmpty(SourceFile) ? "" : $" [{SourceFile}]";
            return $"[{Timestamp:HH:mm:ss.fff} {Level}]{src} {Message}";
        }
    }

    /// <summary>
    /// Static debug logger for map loading operations.
    /// Provides comprehensive logging with offset tracking and source file identification.
    /// Automatically saves to "Entity Map Loading.txt" on the desktop.
    /// </summary>
    public static class MapDebugLogger
    {
        #region Fields

        /// <summary>All log entries.</summary>
        private static List<LogEntry> entries = new List<LogEntry>();

        /// <summary>Lock object for thread-safe access.</summary>
        private static object lockObj = new object();

        /// <summary>Whether logging is enabled.</summary>
        private static bool isEnabled = true;

        /// <summary>Maximum number of entries to keep.</summary>
        private static int maxEntries = 5000;

        /// <summary>Current source file context (e.g., which map file is being read).</summary>
        private static string currentSourceFile = "";

        /// <summary>Event raised when a new log entry is added.</summary>
        public static event Action<LogEntry> OnLogEntry;

        /// <summary>Path to the desktop log file.</summary>
        private static string desktopLogPath = null;

        /// <summary>StreamWriter for continuous file logging.</summary>
        private static System.IO.StreamWriter fileWriter = null;

        #endregion

        #region Properties

        /// <summary>Gets or sets whether logging is enabled.</summary>
        public static bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; }
        }

        /// <summary>Gets the current log entry count.</summary>
        public static int EntryCount
        {
            get { lock (lockObj) { return entries.Count; } }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clear all log entries and start fresh log file.
        /// </summary>
        public static void Clear()
        {
            lock (lockObj)
            {
                entries.Clear();

                // Close existing file writer
                if (fileWriter != null)
                {
                    try
                    {
                        fileWriter.Flush();
                        fileWriter.Close();
                        fileWriter.Dispose();
                    }
                    catch { }
                    fileWriter = null;
                }

                // Create new log file on desktop
                try
                {
                    desktopLogPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "Entity Map Loading.txt");

                    fileWriter = new System.IO.StreamWriter(desktopLogPath, false, Encoding.UTF8);
                    fileWriter.AutoFlush = true;
                }
                catch
                {
                    // Desktop logging not available, continue without file
                    fileWriter = null;
                }
            }
        }

        /// <summary>
        /// Set the current source file context for subsequent log entries.
        /// </summary>
        /// <param name="filePath">Full path to the file being read.</param>
        public static void SetSourceFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                currentSourceFile = "";
                return;
            }

            // Extract just the filename
            int lastSlash = Math.Max(filePath.LastIndexOf('\\'), filePath.LastIndexOf('/'));
            currentSourceFile = lastSlash >= 0 ? filePath.Substring(lastSlash + 1) : filePath;
        }

        /// <summary>
        /// Clear the current source file context.
        /// </summary>
        public static void ClearSourceFile()
        {
            currentSourceFile = "";
        }

        /// <summary>
        /// Log a debug message.
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.DBG, message);
        }

        /// <summary>
        /// Log a debug message with format arguments.
        /// </summary>
        public static void Debug(string format, params object[] args)
        {
            Log(LogLevel.DBG, string.Format(format, args));
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.INF, message);
        }

        /// <summary>
        /// Log an info message with format arguments.
        /// </summary>
        public static void Info(string format, params object[] args)
        {
            Log(LogLevel.INF, string.Format(format, args));
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            Log(LogLevel.WRN, message);
        }

        /// <summary>
        /// Log a warning message with format arguments.
        /// </summary>
        public static void Warn(string format, params object[] args)
        {
            Log(LogLevel.WRN, string.Format(format, args));
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string message)
        {
            Log(LogLevel.ERR, message);
        }

        /// <summary>
        /// Log an error message with format arguments.
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            Log(LogLevel.ERR, string.Format(format, args));
        }

        /// <summary>
        /// Log a separator line for visual organization.
        /// </summary>
        public static void Separator(string title = null)
        {
            if (string.IsNullOrEmpty(title))
            {
                Log(LogLevel.INF, "===========================================");
            }
            else
            {
                Log(LogLevel.INF, $"=========== {title} ===========");
            }
        }

        /// <summary>
        /// Log an offset read operation.
        /// </summary>
        /// <param name="description">What is being read.</param>
        /// <param name="offset">File offset.</param>
        /// <param name="value">Value read (as int).</param>
        public static void LogOffset(string description, long offset, int value)
        {
            Debug("{0}: offset=0x{1:X}, value=0x{2:X} ({2})", description, offset, value);
        }

        /// <summary>
        /// Log an offset read operation with hex display.
        /// </summary>
        /// <param name="description">What is being read.</param>
        /// <param name="offset">File offset.</param>
        /// <param name="value">Value read.</param>
        /// <param name="hexOnly">If true, only show hex value.</param>
        public static void LogOffset(string description, long offset, int value, bool hexOnly)
        {
            if (hexOnly)
            {
                Debug("{0}: offset=0x{1:X}, value=0x{2:X}", description, offset, value);
            }
            else
            {
                LogOffset(description, offset, value);
            }
        }

        /// <summary>
        /// Log a tag entry being parsed.
        /// </summary>
        /// <param name="index">Tag index.</param>
        /// <param name="tagType">Tag type (e.g., "sbsp", "mode").</param>
        /// <param name="ident">Tag identifier.</param>
        /// <param name="offset">Data offset.</param>
        /// <param name="size">Data size.</param>
        /// <param name="source">Source file (e.g., "shared.map").</param>
        public static void LogTag(int index, string tagType, int ident, int offset, int size, string source = null)
        {
            string srcInfo = string.IsNullOrEmpty(source) ? "local" : source;
            Debug("Tag {0}: [{1}] ID=0x{2:X8}, Offset=0x{3:X8} ({4}), Size={5}",
                index, tagType, ident, offset, srcInfo, size);
        }

        /// <summary>
        /// Log a hex dump of data at a specific offset.
        /// </summary>
        /// <param name="description">Description of the data.</param>
        /// <param name="offset">File offset.</param>
        /// <param name="data">Byte array to dump.</param>
        /// <param name="maxBytes">Maximum bytes to display.</param>
        public static void LogHexDump(string description, long offset, byte[] data, int maxBytes = 64)
        {
            Debug("Hex dump at 0x{0:X} - {1} ({2} bytes):", offset, description, data.Length);
            int bytesToShow = Math.Min(data.Length, maxBytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytesToShow; i += 16)
            {
                sb.Clear();
                sb.AppendFormat("  {0:X4}: ", i);

                // Hex bytes
                for (int j = 0; j < 16 && i + j < bytesToShow; j++)
                {
                    sb.AppendFormat("{0:X2} ", data[i + j]);
                }

                // Padding if less than 16 bytes
                for (int j = bytesToShow - i; j < 16; j++)
                {
                    sb.Append("   ");
                }

                sb.Append(" ");

                // ASCII representation
                for (int j = 0; j < 16 && i + j < bytesToShow; j++)
                {
                    byte b = data[i + j];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                Debug(sb.ToString());
            }

            if (data.Length > maxBytes)
            {
                Debug("  ... ({0} more bytes)", data.Length - maxBytes);
            }
        }

        /// <summary>
        /// Log BSP information.
        /// </summary>
        public static void LogBSP(int index, int offset, int size, int magic, int ident, string sourceFile = null)
        {
            string src = string.IsNullOrEmpty(sourceFile) ? "local" : sourceFile;
            Debug("BSP {0}: Offset=0x{1:X8}, Size={2}, Magic=0x{3:X8}, Ident=0x{4:X8} ({5})",
                index, offset, size, magic, ident, src);
        }

        /// <summary>
        /// Get all log entries as a formatted string.
        /// </summary>
        public static string GetFullLog()
        {
            lock (lockObj)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var entry in entries)
                {
                    sb.AppendLine(entry.ToString());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Get all log entries.
        /// </summary>
        public static List<LogEntry> GetEntries()
        {
            lock (lockObj)
            {
                return new List<LogEntry>(entries);
            }
        }

        /// <summary>
        /// Get entries since a specific index.
        /// </summary>
        /// <param name="startIndex">Starting index.</param>
        public static List<LogEntry> GetEntriesSince(int startIndex)
        {
            lock (lockObj)
            {
                if (startIndex >= entries.Count)
                    return new List<LogEntry>();

                return entries.GetRange(startIndex, entries.Count - startIndex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Internal log method.
        /// </summary>
        private static void Log(LogLevel level, string message)
        {
            if (!isEnabled)
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                SourceFile = currentSourceFile
            };

            lock (lockObj)
            {
                entries.Add(entry);

                // Trim old entries if over limit
                while (entries.Count > maxEntries)
                {
                    entries.RemoveAt(0);
                }

                // Write to desktop log file
                if (fileWriter != null)
                {
                    try
                    {
                        fileWriter.WriteLine(entry.ToString());
                    }
                    catch
                    {
                        // Ignore file write errors
                    }
                }
            }

            // Raise event for listeners
            OnLogEntry?.Invoke(entry);
        }

        /// <summary>
        /// Flush and close the log file.
        /// </summary>
        public static void CloseLogFile()
        {
            lock (lockObj)
            {
                if (fileWriter != null)
                {
                    try
                    {
                        fileWriter.Flush();
                        fileWriter.Close();
                        fileWriter.Dispose();
                    }
                    catch { }
                    fileWriter = null;
                }
            }
        }

        #endregion
    }
}
