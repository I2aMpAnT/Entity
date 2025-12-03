using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using entity.MapForms;
using Globals;
using HaloMap.Map;

namespace entity.Main
{
    /// <summary>
    /// Theater Mode launcher form - waits for live data or replay file before loading map.
    /// </summary>
    public class TheaterModeLauncher : Form
    {
        private Button btnLive;
        private Button btnLoadReplay;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Form1 mainForm;

        private UdpClient udpClient;
        private TcpListener tcpListener;
        private Thread listenerThread;
        private bool isListening = false;
        private string detectedMapName = null;
        private string replayFilePath = null;

        public TheaterModeLauncher(Form1 main)
        {
            mainForm = main;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Theater Mode";
            this.Size = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Status label
            lblStatus = new Label();
            lblStatus.Text = "Select data source:";
            lblStatus.ForeColor = Color.White;
            lblStatus.Font = new Font("Segoe UI", 12);
            lblStatus.Location = new Point(20, 20);
            lblStatus.Size = new Size(360, 30);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            // Live button
            btnLive = new Button();
            btnLive.Text = "🔴 LIVE";
            btnLive.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnLive.ForeColor = Color.White;
            btnLive.BackColor = Color.FromArgb(180, 40, 40);
            btnLive.FlatStyle = FlatStyle.Flat;
            btnLive.FlatAppearance.BorderColor = Color.Red;
            btnLive.Location = new Point(30, 70);
            btnLive.Size = new Size(150, 50);
            btnLive.Click += BtnLive_Click;
            this.Controls.Add(btnLive);

            // Load Replay button
            btnLoadReplay = new Button();
            btnLoadReplay.Text = "📂 Load Replay";
            btnLoadReplay.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnLoadReplay.ForeColor = Color.White;
            btnLoadReplay.BackColor = Color.FromArgb(60, 60, 60);
            btnLoadReplay.FlatStyle = FlatStyle.Flat;
            btnLoadReplay.FlatAppearance.BorderColor = Color.Gray;
            btnLoadReplay.Location = new Point(210, 70);
            btnLoadReplay.Size = new Size(150, 50);
            btnLoadReplay.Click += BtnLoadReplay_Click;
            this.Controls.Add(btnLoadReplay);

            // Progress bar (hidden initially)
            progressBar = new ProgressBar();
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Location = new Point(30, 140);
            progressBar.Size = new Size(330, 10);
            progressBar.Visible = false;
            this.Controls.Add(progressBar);

            this.FormClosing += TheaterModeLauncher_FormClosing;
        }

        private void BtnLive_Click(object sender, EventArgs e)
        {
            if (!isListening)
            {
                StartListening();
            }
            else
            {
                StopListening();
            }
        }

        private void StartListening()
        {
            try
            {
                isListening = true;
                btnLive.Text = "⏹ STOP";
                btnLive.BackColor = Color.FromArgb(40, 40, 180);
                btnLoadReplay.Enabled = false;
                lblStatus.Text = "Waiting for live data on port 2222...";
                progressBar.Visible = true;

                // Start UDP listener
                udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 2222));

                // Start TCP listener
                tcpListener = new TcpListener(IPAddress.Any, 2222);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start();

                listenerThread = new Thread(ListenerLoop);
                listenerThread.IsBackground = true;
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start listener: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopListening();
            }
        }

        private void StopListening()
        {
            isListening = false;
            btnLive.Text = "🔴 LIVE";
            btnLive.BackColor = Color.FromArgb(180, 40, 40);
            btnLoadReplay.Enabled = true;
            lblStatus.Text = "Select data source:";
            progressBar.Visible = false;

            try
            {
                udpClient?.Close();
                tcpListener?.Stop();
            }
            catch { }
        }

        private void ListenerLoop()
        {
            byte[] buffer = new byte[65536];
            StringBuilder lineBuffer = new StringBuilder();

            while (isListening)
            {
                try
                {
                    // Check UDP
                    if (udpClient != null && udpClient.Available > 0)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref remoteEP);
                        string text = Encoding.UTF8.GetString(data);
                        ProcessIncomingData(text);
                    }

                    // Check TCP
                    if (tcpListener != null && tcpListener.Pending())
                    {
                        TcpClient client = tcpListener.AcceptTcpClient();
                        NetworkStream stream = client.GetStream();
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            ProcessIncomingData(text);
                        }
                        client.Close();
                    }

                    Thread.Sleep(50);
                }
                catch (Exception)
                {
                    if (!isListening) break;
                }
            }
        }

        private void ProcessIncomingData(string data)
        {
            // Look for map name in the data
            // Expected format: mapname in header or as a field
            string[] lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts = line.Split(',');

                // Check if this is a header line with mapname column
                for (int i = 0; i < parts.Length; i++)
                {
                    string field = parts[i].Trim().ToLowerInvariant();
                    if (field == "mapname" || field == "map_name" || field == "map")
                    {
                        // Found mapname column, look for it in data rows
                        continue;
                    }
                }

                // Check for mapname in key:value format
                if (line.ToLowerInvariant().StartsWith("mapname:") ||
                    line.ToLowerInvariant().StartsWith("map:"))
                {
                    string mapName = line.Substring(line.IndexOf(':') + 1).Trim();
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        detectedMapName = mapName;
                        this.BeginInvoke(new Action(() => OnMapDetected(mapName, null)));
                        return;
                    }
                }

                // Also check CSV fields for common map names (lockout, midship, etc.)
                foreach (string part in parts)
                {
                    string trimmed = part.Trim().ToLowerInvariant();
                    if (IsKnownMapName(trimmed))
                    {
                        detectedMapName = trimmed;
                        this.BeginInvoke(new Action(() => OnMapDetected(trimmed, null)));
                        return;
                    }
                }
            }
        }

        private bool IsKnownMapName(string name)
        {
            // Common Halo 2 map names
            string[] knownMaps = {
                "lockout", "midship", "beaver_creek", "zanzibar", "coagulation",
                "ascension", "ivory_tower", "waterworks", "burial_mounds",
                "colossus", "headlong", "terminal", "turf", "foundation",
                "warlock", "sanctuary", "gemini", "elongation", "backwash",
                "relic", "containment", "desolation", "tombstone"
            };

            foreach (string map in knownMaps)
            {
                if (name.Contains(map))
                    return true;
            }
            return false;
        }

        private void BtnLoadReplay_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Load Replay File";
                ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                ofd.InitialDirectory = Prefs.pathExtractsFolder;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    replayFilePath = ofd.FileName;
                    string mapName = ExtractMapNameFromReplay(replayFilePath);
                    OnMapDetected(mapName, replayFilePath);
                }
            }
        }

        private string ExtractMapNameFromReplay(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Read first few lines looking for map name
                    for (int i = 0; i < 10 && !sr.EndOfStream; i++)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;

                        // Check for mapname field
                        if (line.ToLowerInvariant().StartsWith("mapname:") ||
                            line.ToLowerInvariant().StartsWith("map:"))
                        {
                            return line.Substring(line.IndexOf(':') + 1).Trim();
                        }

                        // Check header for mapname column
                        string[] parts = line.Split(',');
                        for (int j = 0; j < parts.Length; j++)
                        {
                            if (parts[j].Trim().ToLowerInvariant() == "mapname")
                            {
                                // Read next line and get the value
                                string dataLine = sr.ReadLine();
                                if (dataLine != null)
                                {
                                    string[] dataParts = dataLine.Split(',');
                                    if (j < dataParts.Length)
                                        return dataParts[j].Trim();
                                }
                            }
                        }

                        // Check for known map names in filename
                        string fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
                        if (IsKnownMapName(fileName))
                        {
                            // Extract just the map name portion
                            string[] knownMaps = { "lockout", "midship", "beaver_creek", "zanzibar", "coagulation",
                                "ascension", "ivory_tower", "waterworks", "burial_mounds", "colossus",
                                "headlong", "terminal", "turf", "foundation", "warlock", "sanctuary",
                                "gemini", "elongation", "backwash", "relic", "containment", "desolation", "tombstone" };
                            foreach (string map in knownMaps)
                            {
                                if (fileName.Contains(map))
                                    return map;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private void OnMapDetected(string mapName, string replayFile)
        {
            StopListening();

            lblStatus.Text = $"Detected: {mapName ?? "Unknown map"}";
            progressBar.Visible = true;
            this.Refresh();

            // Try to find and load the map
            string mapPath = null;

            if (!string.IsNullOrEmpty(mapName))
            {
                mapPath = mainForm.FindMapByName(mapName);
            }

            if (string.IsNullOrEmpty(mapPath))
            {
                // Map not found - prompt user
                lblStatus.Text = "Map not found. Please select:";
                progressBar.Visible = false;

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = $"Select Map File" + (mapName != null ? $" ({mapName})" : "");
                    ofd.Filter = "Map Files (*.map)|*.map";
                    ofd.InitialDirectory = Prefs.pathMapsFolder;

                    if (ofd.ShowDialog() != DialogResult.OK)
                    {
                        lblStatus.Text = "Select data source:";
                        return;
                    }
                    mapPath = ofd.FileName;
                }
            }

            // Load the map and open Theater Mode
            lblStatus.Text = "Loading map...";
            this.Refresh();

            MapForm mapForm = mainForm.TryLoadMapForm(mapPath);
            if (mapForm != null)
            {
                this.Hide();
                mapForm.OpenTheaterMode(replayFile);
                this.Close();
            }
            else
            {
                lblStatus.Text = "Failed to load map. Select data source:";
                progressBar.Visible = false;
            }
        }

        private void TheaterModeLauncher_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopListening();
        }
    }
}
