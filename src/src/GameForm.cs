using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace Clawbyrinth
{
    public partial class GameForm : Form
    {
        private GameEngine gameEngine = null!;
        private System.Threading.Timer? precisionTimer;
        private const int WINDOW_WIDTH = 1280;
        private const int WINDOW_HEIGHT = 720;
        private const int TARGET_FPS = 120;
        private const int TIMER_INTERVAL = 1000 / TARGET_FPS; // ~8ms
        
        private volatile bool isUpdating = false;

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
            
            // Set background color to #342132
            this.BackColor = Color.FromArgb(0x34, 0x21, 0x32);
        }

        private void InitializeGame()
        {
            gameEngine = new GameEngine(WINDOW_WIDTH, WINDOW_HEIGHT);
            
            // Hook up events
            this.KeyDown += GameForm_KeyDown;
            this.Paint += GameForm_Paint;
            this.Load += GameForm_Load; // Start timer when form is loaded
        }

        private void GameForm_Load(object? sender, EventArgs e)
        {
            // Setup high-precision timer for 120 FPS after window handle is created
            precisionTimer = new System.Threading.Timer(
                callback: TimerCallback,
                state: null,
                dueTime: 100, // Small delay to ensure everything is ready
                period: TIMER_INTERVAL
            );
        }

        private void TimerCallback(object? state)
        {
            // Prevent overlapping updates
            if (isUpdating) return;
            
            // Don't update if form isn't ready
            if (!this.IsHandleCreated || this.IsDisposed) return;
            
            try
            {
                isUpdating = true;
                
                // Update game logic
                gameEngine.Update();
                
                // Request redraw on UI thread
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(() => this.Invalidate());
                }
            }
            finally
            {
                isUpdating = false;
            }
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
            precisionTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}