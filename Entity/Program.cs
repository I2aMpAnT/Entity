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
            }
            else // LIVE mode
            {
                // For LIVE mode, we need a default map to start with
                // Try to find any map in the maps folder
                string mapsFolder = Globals.Prefs.pathMapsFolder;
                if (!string.IsNullOrEmpty(mapsFolder) && Directory.Exists(mapsFolder))
                {
                    string[] mapFiles = Directory.GetFiles(mapsFolder, "*.map");
                    if (mapFiles.Length > 0)
                    {
                        // Use first available map as placeholder (will auto-switch when telemetry arrives)
                        mapFilePath = mapFiles[0];
                    }
                }

                // If no maps folder configured or no maps found, ask user
                if (string.IsNullOrEmpty(mapFilePath))
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Select Initial Map (will auto-switch based on telemetry)";
                        ofd.Filter = "Halo Map Files (*.map)|*.map|All Files (*.*)|*.*";

                        if (ofd.ShowDialog() != DialogResult.OK)
                            return;

                        mapFilePath = ofd.FileName;
                    }
                }
            }

            try
            {
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

                int BSPId = map.Functions.ForMeta.FindMetaByID(map.BSP.sbsp[0].ident);
                Meta meta = new Meta(map);
                meta.TagIndex = BSPId;
                meta.ScanMetaItems(true, false);
                BSPModel bsp = new BSPModel(ref meta);

                // Launch BSPViewer in Theater Mode
                BSPViewer theaterViewer = new BSPViewer(bsp, map, theaterMode: true);

                // Set the mode so BSPViewer knows what to do on startup
                if (subMode == TheaterSubMode.Live)
                {
                    theaterViewer.StartInLiveMode = true;
                }
                else if (subMode == TheaterSubMode.Replay && !string.IsNullOrEmpty(csvFilePath))
                {
                    theaterViewer.StartWithCsvFile = csvFilePath;
                }

                Application.Run(theaterViewer);

                meta.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching Theater Mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                btnLive.Text = "🔴 LIVE";
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