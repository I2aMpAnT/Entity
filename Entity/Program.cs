// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
//   
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace entity
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using entity.Main;
    using entity.Renderers;
    using System.Threading;
    using System.Diagnostics;

    using HaloMap.Map;
    using HaloMap.Render;
    using HaloMap.Meta;
    using HaloMap.RawData;

    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        #region Methods

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(Form1_UIThreadException);

            // Set the unhandled exception mode to force all Windows Forms errors to go through
            // our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event. 
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                // Load preferences first so we have access to maps folder path
                Globals.Prefs.Load();

                // Show startup mode selector
                StartupMode mode = ShowStartupSelector();

                if (mode == StartupMode.TheaterMode)
                {
                    // Launch Theater Mode directly
                    LaunchTheaterMode();
                }
                else if (mode == StartupMode.Editor)
                {
                    // Launch normal Entity editor
                    Application.Run(new Form1());
                }
                // If cancelled, just exit
            }
            catch (Exception e)
            {
                Globals.Global.ShowErrorMsg("Program Error Exception", e);
            }
        }

        private enum StartupMode
        {
            Cancelled,
            Editor,
            TheaterMode
        }

        /// <summary>
        /// Shows the startup mode selector dialog.
        /// </summary>
        private static StartupMode ShowStartupSelector()
        {
            using (Form startupDialog = new Form())
            {
                StartupMode selectedMode = StartupMode.Cancelled;

                startupDialog.Text = "Entity 2.1";
                startupDialog.Size = new Size(450, 250);
                startupDialog.StartPosition = FormStartPosition.CenterScreen;
                startupDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                startupDialog.MaximizeBox = false;
                startupDialog.MinimizeBox = false;
                startupDialog.BackColor = Color.FromArgb(20, 30, 40);

                // Title label
                Label titleLabel = new Label();
                titleLabel.Text = "Select Mode";
                titleLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold);
                titleLabel.ForeColor = Color.FromArgb(0, 200, 255);
                titleLabel.AutoSize = true;
                titleLabel.Location = new Point(165, 15);
                startupDialog.Controls.Add(titleLabel);

                // Editor button
                Button btnEditor = new Button();
                btnEditor.Text = "🔧 EDITOR";
                btnEditor.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                btnEditor.Size = new Size(180, 90);
                btnEditor.Location = new Point(25, 60);
                btnEditor.BackColor = Color.FromArgb(40, 60, 80);
                btnEditor.ForeColor = Color.FromArgb(100, 200, 255);
                btnEditor.FlatStyle = FlatStyle.Flat;
                btnEditor.FlatAppearance.BorderColor = Color.FromArgb(0, 150, 200);
                btnEditor.FlatAppearance.BorderSize = 2;
                btnEditor.Click += (s, e) => { selectedMode = StartupMode.Editor; startupDialog.Close(); };
                startupDialog.Controls.Add(btnEditor);

                // Theater Mode button
                Button btnTheater = new Button();
                btnTheater.Text = "🎬 THEATER";
                btnTheater.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                btnTheater.Size = new Size(180, 90);
                btnTheater.Location = new Point(235, 60);
                btnTheater.BackColor = Color.FromArgb(40, 60, 80);
                btnTheater.ForeColor = Color.FromArgb(255, 100, 100);
                btnTheater.FlatStyle = FlatStyle.Flat;
                btnTheater.FlatAppearance.BorderColor = Color.FromArgb(255, 80, 80);
                btnTheater.FlatAppearance.BorderSize = 2;
                btnTheater.Click += (s, e) => { selectedMode = StartupMode.TheaterMode; startupDialog.Close(); };
                startupDialog.Controls.Add(btnTheater);

                // Description labels
                Label editorDesc = new Label();
                editorDesc.Text = "Full map editor\nwith all tools";
                editorDesc.Font = new Font("Segoe UI", 9);
                editorDesc.ForeColor = Color.FromArgb(180, 180, 180);
                editorDesc.TextAlign = ContentAlignment.MiddleCenter;
                editorDesc.Size = new Size(180, 40);
                editorDesc.Location = new Point(25, 155);
                startupDialog.Controls.Add(editorDesc);

                Label theaterDesc = new Label();
                theaterDesc.Text = "Live telemetry viewer\nfor Halo 2 gameplay";
                theaterDesc.Font = new Font("Segoe UI", 9);
                theaterDesc.ForeColor = Color.FromArgb(180, 180, 180);
                theaterDesc.TextAlign = ContentAlignment.MiddleCenter;
                theaterDesc.Size = new Size(180, 40);
                theaterDesc.Location = new Point(235, 155);
                startupDialog.Controls.Add(theaterDesc);

                startupDialog.ShowDialog();
                return selectedMode;
            }
        }

        private enum TheaterSubMode
        {
            Cancelled,
            Live,
            Replay
        }

        /// <summary>
        /// Launches Theater Mode by first asking LIVE or REPLAY.
        /// </summary>
        private static void LaunchTheaterMode()
        {
            // First, ask LIVE or REPLAY
            TheaterSubMode subMode = ShowTheaterSubModeSelector();

            if (subMode == TheaterSubMode.Cancelled)
                return;

            string mapFilePath = null;
            string csvFilePath = null;

            if (subMode == TheaterSubMode.Replay)
            {
                // REPLAY mode - pick CSV file first
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select Telemetry CSV File";
                    ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;

                    csvFilePath = ofd.FileName;
                }

                // Try to detect map name from CSV
                string detectedMap = DetectMapFromCsv(csvFilePath);
                if (!string.IsNullOrEmpty(detectedMap))
                {
                    mapFilePath = FindMapFile(detectedMap);
                }

                // If no map detected, ask user to pick one
                if (string.IsNullOrEmpty(mapFilePath))
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Select Map File (could not auto-detect from CSV)";
                        ofd.Filter = "Halo Map Files (*.map)|*.map|All Files (*.*)|*.*";

                        string mapsFolder = Globals.Prefs.pathMapsFolder;
                        if (!string.IsNullOrEmpty(mapsFolder) && Directory.Exists(mapsFolder))
                            ofd.InitialDirectory = mapsFolder;

                        if (ofd.ShowDialog() != DialogResult.OK)
                            return;

                        mapFilePath = ofd.FileName;
                    }
                }

                // Launch with the map and CSV file
                LaunchTheaterWithMap(mapFilePath, csvFilePath, false);
            }
            else // LIVE mode
            {
                // For LIVE mode, show waiting screen and listen for telemetry to determine map
                LaunchLiveTheaterMode();
            }
        }

        /// <summary>
        /// Launches LIVE theater mode - waits for telemetry to determine which map to load.
        /// </summary>
        private static void LaunchLiveTheaterMode()
        {
            string detectedMapName = null;

            using (Form waitingForm = new Form())
            {
                waitingForm.Text = "Theater Mode - LIVE";
                waitingForm.Size = new Size(500, 300);
                waitingForm.StartPosition = FormStartPosition.CenterScreen;
                waitingForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                waitingForm.MaximizeBox = false;
                waitingForm.MinimizeBox = false;
                waitingForm.BackColor = Color.FromArgb(20, 30, 40);

                // Recording indicator
                Label recordingLabel = new Label();
                recordingLabel.Text = "● LIVE";
                recordingLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
                recordingLabel.ForeColor = Color.FromArgb(255, 80, 80);
                recordingLabel.AutoSize = true;
                recordingLabel.Location = new Point(185, 30);
                waitingForm.Controls.Add(recordingLabel);

                // Status label
                Label statusLabel = new Label();
                statusLabel.Text = "Listening on port 2222 (UDP + TCP)...";
                statusLabel.Font = new Font("Segoe UI", 12);
                statusLabel.ForeColor = Color.FromArgb(180, 180, 180);
                statusLabel.AutoSize = true;
                statusLabel.Location = new Point(130, 90);
                waitingForm.Controls.Add(statusLabel);

                // Info label
                Label infoLabel = new Label();
                infoLabel.Text = "Start HaloCaster and connect to begin streaming.\nThe map will be loaded automatically.";
                infoLabel.Font = new Font("Segoe UI", 10);
                infoLabel.ForeColor = Color.FromArgb(120, 120, 120);
                infoLabel.TextAlign = ContentAlignment.MiddleCenter;
                infoLabel.Size = new Size(400, 50);
                infoLabel.Location = new Point(50, 130);
                waitingForm.Controls.Add(infoLabel);

                // Cancel button
                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Font = new Font("Segoe UI", 10);
                btnCancel.Size = new Size(100, 35);
                btnCancel.Location = new Point(200, 210);
                btnCancel.BackColor = Color.FromArgb(60, 60, 60);
                btnCancel.ForeColor = Color.White;
                btnCancel.FlatStyle = FlatStyle.Flat;
                btnCancel.Click += (s, e) => waitingForm.Close();
                waitingForm.Controls.Add(btnCancel);

                // Start both UDP and TCP listeners in background
                System.Net.Sockets.UdpClient udpClient = null;
                System.Net.Sockets.TcpListener tcpListener = null;
                Thread udpListenerThread = null;
                Thread tcpListenerThread = null;
                bool listening = true;

                try
                {
                    // UDP listener
                    udpClient = new System.Net.Sockets.UdpClient();
                    udpClient.Client.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                        System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                    udpClient.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 2222));
                    udpClient.Client.ReceiveTimeout = 500;

                    udpListenerThread = new Thread(() =>
                    {
                        System.Net.IPEndPoint remoteEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                        while (listening && detectedMapName == null)
                        {
                            try
                            {
                                byte[] data = udpClient.Receive(ref remoteEP);
                                string packet = System.Text.Encoding.UTF8.GetString(data).Trim();
                                string[] lines = packet.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string line in lines)
                                {
                                    string mapName = DetectMapFromLine(line);
                                    if (!string.IsNullOrEmpty(mapName))
                                    {
                                        detectedMapName = mapName;
                                        try { waitingForm.BeginInvoke(new System.Action(() => waitingForm.Close())); } catch { }
                                        break;
                                    }
                                }
                            }
                            catch (System.Net.Sockets.SocketException) { }
                        }
                    });
                    udpListenerThread.IsBackground = true;
                    udpListenerThread.Start();

                    // TCP listener
                    tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 2222);
                    tcpListener.Server.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket,
                        System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                    tcpListener.Start();

                    tcpListenerThread = new Thread(() =>
                    {
                        while (listening && detectedMapName == null)
                        {
                            try
                            {
                                if (!tcpListener.Pending())
                                {
                                    Thread.Sleep(100);
                                    continue;
                                }

                                using (var client = tcpListener.AcceptTcpClient())
                                using (var stream = client.GetStream())
                                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                                {
                                    while (listening && detectedMapName == null && client.Connected)
                                    {
                                        string line = reader.ReadLine();
                                        if (line == null) break;
                                        if (string.IsNullOrWhiteSpace(line)) continue;

                                        string mapName = DetectMapFromLine(line);
                                        if (!string.IsNullOrEmpty(mapName))
                                        {
                                            detectedMapName = mapName;
                                            try { waitingForm.BeginInvoke(new System.Action(() => waitingForm.Close())); } catch { }
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (System.Net.Sockets.SocketException) { }
                            catch (IOException) { }
                        }
                    });
                    tcpListenerThread.IsBackground = true;
                    tcpListenerThread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start listener: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                waitingForm.ShowDialog();

                // Cleanup - stop listening first
                listening = false;

                // Close sockets
                try { udpClient?.Close(); } catch { }
                try { tcpListener?.Stop(); } catch { }

                // Wait for threads to finish
                udpListenerThread?.Join(1000);
                tcpListenerThread?.Join(1000);

                // Give the OS time to release the ports
                Thread.Sleep(500);
            }

            // If we detected a map, load it
            if (!string.IsNullOrEmpty(detectedMapName))
            {
                string mapFilePath = FindMapFile(detectedMapName);
                if (string.IsNullOrEmpty(mapFilePath))
                {
                    MessageBox.Show($"Could not find map file for '{detectedMapName}'.\nPlease select it manually.",
                        "Map Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = $"Select Map File for '{detectedMapName}'";
                        ofd.Filter = "Halo Map Files (*.map)|*.map|All Files (*.*)|*.*";

                        string mapsFolder = Globals.Prefs.pathMapsFolder;
                        if (!string.IsNullOrEmpty(mapsFolder) && Directory.Exists(mapsFolder))
                            ofd.InitialDirectory = mapsFolder;

                        if (ofd.ShowDialog() != DialogResult.OK)
                            return;

                        mapFilePath = ofd.FileName;
                    }
                }

                LaunchTheaterWithMap(mapFilePath, null, true);
            }
        }

        /// <summary>
        /// Detects map name from a single telemetry line.
        /// </summary>
        private static string DetectMapFromLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3)
                return null;

            // Check if this looks like a header row
            string first = parts[0].Trim().ToLowerInvariant();
            if (first == "timestamp" || first == "playername" || first == "mapname")
            {
                // This is a header, find mapname column for future reference
                return null;
            }

            // Assume default column order: Timestamp, MapName, GameType, ...
            // MapName is typically at index 1
            if (parts.Length > 1)
            {
                string possibleMap = parts[1].Trim();
                // Validate it looks like a map name (not a timestamp or number)
                if (!string.IsNullOrEmpty(possibleMap) &&
                    !possibleMap.Contains(":") &&
                    !possibleMap.Contains("-") &&
                    !float.TryParse(possibleMap, out _))
                {
                    return possibleMap;
                }
            }

            return null;
        }

        /// <summary>
        /// Launches Theater Mode with a specific map file.
        /// </summary>
        private static void LaunchTheaterWithMap(string mapFilePath, string csvFilePath, bool startLive)
        {
            Meta meta = null;
            try
            {
                // Validate file path
                if (string.IsNullOrEmpty(mapFilePath) || !File.Exists(mapFilePath))
                {
                    MessageBox.Show($"Map file not found: {mapFilePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Load the map
                Map map = Map.LoadFromFile(mapFilePath);
                if (map == null)
                {
                    MessageBox.Show("Failed to load map file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create BSP model
                if (map.BSP?.sbsp == null || map.BSP.sbsp.Length == 0)
                {
                    MessageBox.Show("Map has no BSP data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Validate Functions object
                if (map.Functions?.ForMeta == null)
                {
                    MessageBox.Show("Map functions not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int BSPId = map.Functions.ForMeta.FindMetaByID(map.BSP.sbsp[0].ident);
                if (BSPId < 0)
                {
                    MessageBox.Show("Could not find BSP meta in map.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                meta = new Meta(map);
                meta.TagIndex = BSPId;
                meta.ScanMetaItems(true, false);
                BSPModel bsp = new BSPModel(ref meta);

                if (bsp == null)
                {
                    MessageBox.Show("Failed to create BSP model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Launch BSPViewer in Theater Mode with the appropriate startup mode
                BSPViewer theaterViewer = new BSPViewer(bsp, map,
                    theaterMode: true,
                    startLive: startLive,
                    csvFile: csvFilePath);

                Application.Run(theaterViewer);
            }
            catch (ObjectDisposedException)
            {
                // Form was closed - this is normal, ignore it
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching Theater Mode: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup
                try { meta?.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Shows the LIVE/REPLAY selector for Theater Mode.
        /// </summary>
        private static TheaterSubMode ShowTheaterSubModeSelector()
        {
            using (Form dialog = new Form())
            {
                TheaterSubMode selectedMode = TheaterSubMode.Cancelled;

                dialog.Text = "Theater Mode";
                dialog.Size = new Size(400, 220);
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(20, 30, 40);

                // Title label
                Label titleLabel = new Label();
                titleLabel.Text = "Select Source";
                titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
                titleLabel.ForeColor = Color.FromArgb(0, 200, 255);
                titleLabel.AutoSize = true;
                titleLabel.Location = new Point(135, 15);
                dialog.Controls.Add(titleLabel);

                // LIVE button
                Button btnLive = new Button();
                btnLive.Text = "● LIVE";
                btnLive.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                btnLive.Size = new Size(160, 80);
                btnLive.Location = new Point(25, 60);
                btnLive.BackColor = Color.FromArgb(40, 60, 80);
                btnLive.ForeColor = Color.FromArgb(255, 100, 100);
                btnLive.FlatStyle = FlatStyle.Flat;
                btnLive.FlatAppearance.BorderColor = Color.FromArgb(255, 80, 80);
                btnLive.FlatAppearance.BorderSize = 2;
                btnLive.Click += (s, e) => { selectedMode = TheaterSubMode.Live; dialog.Close(); };
                dialog.Controls.Add(btnLive);

                // REPLAY button
                Button btnReplay = new Button();
                btnReplay.Text = "📁 REPLAY";
                btnReplay.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                btnReplay.Size = new Size(160, 80);
                btnReplay.Location = new Point(210, 60);
                btnReplay.BackColor = Color.FromArgb(40, 60, 80);
                btnReplay.ForeColor = Color.FromArgb(100, 200, 255);
                btnReplay.FlatStyle = FlatStyle.Flat;
                btnReplay.FlatAppearance.BorderColor = Color.FromArgb(0, 150, 200);
                btnReplay.FlatAppearance.BorderSize = 2;
                btnReplay.Click += (s, e) => { selectedMode = TheaterSubMode.Replay; dialog.Close(); };
                dialog.Controls.Add(btnReplay);

                // Description labels
                Label liveDesc = new Label();
                liveDesc.Text = "Listen for live\ntelemetry data";
                liveDesc.Font = new Font("Segoe UI", 9);
                liveDesc.ForeColor = Color.FromArgb(180, 180, 180);
                liveDesc.TextAlign = ContentAlignment.MiddleCenter;
                liveDesc.Size = new Size(160, 35);
                liveDesc.Location = new Point(25, 145);
                dialog.Controls.Add(liveDesc);

                Label replayDesc = new Label();
                replayDesc.Text = "Browse for\nCSV telemetry files";
                replayDesc.Font = new Font("Segoe UI", 9);
                replayDesc.ForeColor = Color.FromArgb(180, 180, 180);
                replayDesc.TextAlign = ContentAlignment.MiddleCenter;
                replayDesc.Size = new Size(160, 35);
                replayDesc.Location = new Point(210, 145);
                dialog.Controls.Add(replayDesc);

                dialog.ShowDialog();
                return selectedMode;
            }
        }

        /// <summary>
        /// Tries to detect map name from CSV file by reading first few lines.
        /// </summary>
        private static string DetectMapFromCsv(string csvFilePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(csvFilePath))
                {
                    // Read header line
                    string header = reader.ReadLine();
                    if (string.IsNullOrEmpty(header))
                        return null;

                    // Find mapname column index
                    string[] columns = header.ToLowerInvariant().Split(',');
                    int mapIndex = -1;
                    for (int i = 0; i < columns.Length; i++)
                    {
                        if (columns[i].Trim() == "mapname")
                        {
                            mapIndex = i;
                            break;
                        }
                    }

                    if (mapIndex < 0)
                        return null;

                    // Read first data line
                    string dataLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(dataLine))
                        return null;

                    string[] values = dataLine.Split(',');
                    if (values.Length > mapIndex)
                    {
                        return values[mapIndex].Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Finds a map file matching the given map name.
        /// </summary>
        private static string FindMapFile(string mapName)
        {
            string mapsFolder = Globals.Prefs.pathMapsFolder;
            if (string.IsNullOrEmpty(mapsFolder) || !Directory.Exists(mapsFolder))
                return null;

            string normalizedName = mapName.ToLowerInvariant().Replace(" ", "");

            string[] mapFiles = Directory.GetFiles(mapsFolder, "*.map");
            foreach (string filePath in mapFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
                if (fileName == normalizedName ||
                    fileName.Replace("_", "") == normalizedName ||
                    normalizedName.Contains(fileName) ||
                    fileName.Contains(normalizedName))
                {
                    return filePath;
                }
            }

            return null;
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        private static void Form1_UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = ShowThreadExceptionDialog("Windows Forms Error", t.Exception);
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal Windows Forms Error",
                        "Fatal Windows Forms Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
                Application.Exit();
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        // NOTE: This exception cannot be kept from terminating the application - it can only 
        // log the event, and inform the user about it. 
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                string errorMsg = "An application error occurred. Please contact the adminstrator " +
                    "with the following information:\n\n";

                // Since we can't prevent the app from terminating, log this to the event log.
                if (!EventLog.SourceExists("ThreadException"))
                {
                    EventLog.CreateEventSource("ThreadException", "Application");
                }

                // Create an EventLog instance and assign its source.
                EventLog myLog = new EventLog();
                myLog.Source = "ThreadException";
                myLog.WriteEntry(errorMsg + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace);
            }
            catch (Exception exc)
            {
                try
                {
                    MessageBox.Show("Fatal Non-UI Error",
                        "Fatal Non-UI Error. Could not write the error to the event log. Reason: "
                        + exc.Message, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }

        // Creates the error message and displays it.
        private static DialogResult ShowThreadExceptionDialog(string title, Exception e)
        {
            string errorMsg = "An application error occurred. Please contact the adminstrator " +
                "with the following information:\n\n";
            errorMsg = errorMsg + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
            return MessageBox.Show(errorMsg, title, MessageBoxButtons.AbortRetryIgnore,
                MessageBoxIcon.Stop);
        }

        #endregion
    }
}