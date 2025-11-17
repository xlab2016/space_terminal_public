using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpaceTerminal.Windows;

public class DesktopViewer : Form
{
    private PictureBox _pictureBox = null!;
    private Label _statusLabel = null!;
    private Label _fpsLabel = null!;
    private int _frameCount = 0;
    private DateTime _lastFpsUpdate = DateTime.Now;

    public DesktopViewer(string clientId)
    {
        InitializeComponents(clientId);
    }

    private void InitializeComponents(string clientId)
    {
        // Form settings
        Text = $"Desktop Sharing - {clientId}";
        Size = new Size(1280, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        // Status panel
        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(45, 45, 48)
        };

        _statusLabel = new Label
        {
            Text = "Connected - Receiving stream...",
            ForeColor = Color.LightGreen,
            AutoSize = true,
            Location = new Point(10, 7)
        };

        _fpsLabel = new Label
        {
            Text = "FPS: 0",
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(250, 7)
        };

        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(_fpsLabel);

        // Picture box for video display
        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        // Add controls
        Controls.Add(_pictureBox);
        Controls.Add(statusPanel);

        // Handle close event
        FormClosing += (s, e) =>
        {
            _statusLabel.Text = "Disconnected";
            _statusLabel.ForeColor = Color.Red;
        };
    }

    public void UpdateFrame(byte[] imageData)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<byte[]>(UpdateFrame), imageData);
            return;
        }

        try
        {
            using var ms = new System.IO.MemoryStream(imageData);
            var oldImage = _pictureBox.Image;
            _pictureBox.Image = Image.FromStream(ms);
            oldImage?.Dispose();

            // Update FPS
            _frameCount++;
            var elapsed = (DateTime.Now - _lastFpsUpdate).TotalSeconds;
            if (elapsed >= 1.0)
            {
                var fps = _frameCount / elapsed;
                _fpsLabel.Text = $"FPS: {fps:F1}";
                _frameCount = 0;
                _lastFpsUpdate = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _statusLabel.ForeColor = Color.Red;
        }
    }

    public void UpdateStatus(string message, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string, Color>(UpdateStatus), message, color);
            return;
        }

        _statusLabel.Text = message;
        _statusLabel.ForeColor = color;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pictureBox?.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
