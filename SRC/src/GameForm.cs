using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Clawbyrinth
{
    public partial class GameForm : Form
    {
        private GameEngine gameEngine;
        private System.Windows.Forms.Timer gameTimer;
        private const int WINDOW_WIDTH = 800;
        private const int WINDOW_HEIGHT = 600;

        public GameForm()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
            this.Text = "Clawbyrinth";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Set background color
            this.BackColor = Color.Black;
        }

        private void InitializeGame()
        {
            gameEngine = new GameEngine(WINDOW_WIDTH, WINDOW_HEIGHT);
            
            // Setup game timer for 60 FPS
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16; // ~60 FPS
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
            
            // Hook up events
            this.KeyDown += GameForm_KeyDown;
            this.Paint += GameForm_Paint;
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            gameEngine.Update();
            this.Invalidate(); // Trigger repaint
        }

        private void GameForm_KeyDown(object? sender, KeyEventArgs e)
        {
            gameEngine.HandleInput(e.KeyCode);
        }

        private void GameForm_Paint(object? sender, PaintEventArgs e)
        {
            // Enable anti-aliasing for smoother graphics
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            
            gameEngine.Render(e.Graphics);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            gameTimer?.Stop();
            gameTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}