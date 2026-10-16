using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyLifeGame.Models;
using MyLifeGame.Services;

using WinFormsTimer = System.Windows.Forms.Timer;

namespace MyLifeGame.UI
{
    public class MenuForm : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblVersion;
        private Label lblHints;

        private Button btnContinue;
        private Button btnNewGame;
        private Button btnLoadGame;
        private Button btnSettings;
        private Button btnExit;

        private WinFormsTimer animTimer;
        private WinFormsTimer subtitleTimer;

        private float time = 0f;
        private float titleGlow = 0f;

        private int _subtitleIndex = 0;
        private readonly string[] _subtitles =
        {
            "СИМУЛЯТОР ЖИЗНИ",
            "ПУТЬ ОТ БОМЖА ДО МИЛЛИОНЕРА",
            "ТВОЯ ИСТОРИЯ",
            "ЖИЗНЬ — ЭТО ВЫБОР"
        };

        private bool _closing = false;
        private bool _isFullscreen = true;
        private Rectangle _prevBounds;
        private FormBorderStyle _prevBorder;
        private FormWindowState _prevState;

        private readonly Color BgDark = Color.FromArgb(8, 10, 20);
        private readonly Color BgMid = Color.FromArgb(20, 24, 45);
        private readonly Color AccentCyan = Color.FromArgb(0, 220, 255);
        private readonly Color AccentGold = Color.FromArgb(255, 200, 60);

        private readonly SaveService _saveService = new SaveService();
        private readonly List<Star> _stars = new List<Star>();

        private class Star
        {
            public float X, Y;
            public float Speed;
            public float Size;
            public float Phase;
        }

        public MenuForm()
        {
            Text = "🎮 Моя Жизнь";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = BgDark;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            DoubleBuffered = true;

            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint, true);
            UpdateStyles();

            KeyPreview = true;
            KeyDown += MenuForm_KeyDown;

            GenerateStars(150);
            BuildUI();
            StartAnimation();

            Shown += (s, e) => ApplyFullscreen();
            Resize += (s, e) => LayoutControls();
        }

        private void GenerateStars(int count)
        {
            var rnd = new Random(42);
            var screen = Screen.PrimaryScreen.Bounds;

            for (int i = 0; i < count; i++)
            {
                _stars.Add(new Star
                {
                    X = rnd.Next(0, screen.Width),
                    Y = rnd.Next(0, screen.Height),
                    Speed = (float)(rnd.NextDouble() * 0.2 + 0.05),
                    Size = (float)(rnd.NextDouble() * 1.5 + 0.8),
                    Phase = (float)(rnd.NextDouble() * Math.PI * 2)
                });
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_closing) return;
            if (ClientRectangle.Width <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new LinearGradientBrush(ClientRectangle, BgMid, BgDark, 75f))
                g.FillRectangle(brush, ClientRectangle);

            float glowR = 1000 + (float)Math.Sin(titleGlow) * 80;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(ClientRectangle.Width / 2f - glowR / 2, -glowR / 2, glowR, glowR);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(55, 0, 150, 255);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(pgb, path);
                }
            }

            foreach (var star in _stars)
            {
                float flicker = 0.5f + 0.5f * (float)Math.Sin(time * 2f + star.Phase);
                int alpha = (int)(180 * flicker);
                if (alpha < 10) alpha = 10;

                using (var br = new SolidBrush(Color.FromArgb(alpha, 200, 230, 255)))
                    g.FillEllipse(br, star.X, star.Y, star.Size, star.Size);
            }

            DrawCitySilhouette(g);

            using (var path = new GraphicsPath())
            {
                path.AddRectangle(ClientRectangle);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    pgb.SurroundColors = new[] { Color.FromArgb(180, 0, 0, 0) };
                    g.FillRectangle(pgb, ClientRectangle);
                }
            }
        }

        private void DrawCitySilhouette(Graphics g)
        {
            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;
            int baseY = h;

            var rnd = new Random(123);
            int x = 0;

            while (x < w)
            {
                int bw = rnd.Next(40, 110);
                int bh = rnd.Next(80, 260);
                int top = baseY - bh;

                using (var br = new SolidBrush(Color.FromArgb(210, 5, 8, 18)))
                    g.FillRectangle(br, x, top, bw, bh);

                for (int wy = top + 15; wy < baseY - 15; wy += 22)
                {
                    for (int wx = x + 10; wx < x + bw - 10; wx += 18)
                    {
                        if (rnd.Next(100) < 25)
                        {
                            int a = rnd.Next(80, 200);
                            using (var br = new SolidBrush(Color.FromArgb(a, 255, 220, 120)))
                                g.FillRectangle(br, wx, wy, 4, 6);
                        }
                    }
                }

                using (var br = new SolidBrush(Color.FromArgb(120, 0, 220, 255)))
                    g.FillRectangle(br, x, top - 1, bw, 2);

                x += bw + rnd.Next(5, 20);
            }

            using (var br = new LinearGradientBrush(
                new Rectangle(0, baseY - 300, w, 300),
                Color.FromArgb(0, 0, 0, 0),
                Color.FromArgb(220, 8, 10, 20),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(br, 0, baseY - 300, w, 300);
            }
        }

        private void StartAnimation()
        {
            animTimer = new WinFormsTimer { Interval = 33 };
            animTimer.Tick += (s, e) =>
            {
                if (_closing) return;

                time += 0.05f;
                titleGlow += 0.02f;

                foreach (var star in _stars)
                {
                    star.Y -= star.Speed;
                    if (star.Y < -5)
                    {
                        star.Y = ClientRectangle.Height + 5;
                        var rnd = new Random();
                        star.X = rnd.Next(0, Math.Max(1, ClientRectangle.Width));
                    }
                }

                Invalidate();
            };
            animTimer.Start();

            subtitleTimer = new WinFormsTimer { Interval = 3000 };
            subtitleTimer.Tick += (s, e) =>
            {
                if (_closing) return;
                _subtitleIndex = (_subtitleIndex + 1) % _subtitles.Length;
                if (lblSubtitle != null && !lblSubtitle.IsDisposed)
                    lblSubtitle.Text = _subtitles[_subtitleIndex];
            };
            subtitleTimer.Start();
        }

        private void BuildUI()
        {
            lblTitle = new Label
            {
                Text = "МОЯ ЖИЗНЬ",
                Font = new Font("Segoe UI", 58, FontStyle.Bold),
                ForeColor = AccentCyan,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 100
            };

            lblSubtitle = new Label
            {
                Text = _subtitles[0],
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 220, 255),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30
            };

            lblVersion = new Label
            {
                Text = "v1.0",
                Font = new Font("Consolas", 10),
                ForeColor = Color.FromArgb(120, 150, 180),
                BackColor = Color.Transparent,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblHints = new Label
            {
                Text = "F11 — фуллскрин   •   Esc — выход из фуллскрина",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 160, 200),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 25
            };

            int btnW = 380;
            int btnH = 60;

            btnContinue = MakeMenuButton("▶   ПРОДОЛЖИТЬ", btnW, btnH, Color.FromArgb(0, 150, 95));
            btnContinue.Click += (s, e) => ContinueGame();

            btnNewGame = MakeMenuButton("✨   НОВАЯ ИГРА", btnW, btnH, Color.FromArgb(30, 130, 90));
            btnNewGame.Click += (s, e) => NewGame();

            btnLoadGame = MakeMenuButton("💾   ЗАГРУЗИТЬ ИГРУ", btnW, btnH, Color.FromArgb(40, 95, 170));
            btnLoadGame.Click += (s, e) => LoadGame();

            btnSettings = MakeMenuButton("⚙   НАСТРОЙКИ", btnW, btnH, Color.FromArgb(95, 75, 155));
            btnSettings.Click += (s, e) => ShowSettings();

            btnExit = MakeMenuButton("✖   ВЫХОД", btnW, btnH, Color.FromArgb(150, 55, 55));
            btnExit.Click += (s, e) => Close();

            btnContinue.Visible = _saveService.GetSaveSlots().Count > 0;

            Controls.Add(btnContinue);
            Controls.Add(btnNewGame);
            Controls.Add(btnLoadGame);
            Controls.Add(btnSettings);
            Controls.Add(btnExit);
            Controls.Add(lblTitle);
            Controls.Add(lblSubtitle);
            Controls.Add(lblVersion);
            Controls.Add(lblHints);

            LayoutControls();
        }

        private Button MakeMenuButton(string text, int w, int h, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Width = w,
                Height = h,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = color,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(30, 0, 0, 0),
                TabStop = false,
                Tag = color
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += MenuButton_Paint;
            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = Lighten(color, 45);
                btn.Invalidate();
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = color;
                btn.Invalidate();
            };
            return btn;
        }

        private Color Lighten(Color c, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, c.R + amount),
                Math.Min(255, c.G + amount),
                Math.Min(255, c.B + amount));
        }

        private void LayoutControls()
        {
            if (lblTitle == null) return;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            int titleW = 900;
            int titleH = 100;
            lblTitle.Width = titleW;
            lblTitle.Height = titleH;
            lblTitle.Left = (w - titleW) / 2;
            lblTitle.Top = (int)(h * 0.12);

            lblSubtitle.Width = titleW;
            lblSubtitle.Left = (w - titleW) / 2;
            lblSubtitle.Top = lblTitle.Top + titleH + 5;

            lblVersion.Left = w - 100;
            lblVersion.Top = 20;

            lblHints.Width = w;
            lblHints.Left = 0;
            lblHints.Top = h - 40;

            int btnW = 380;
            int btnH = 60;
            int gap = 14;

            var buttons = new List<Button>();
            if (btnContinue.Visible) buttons.Add(btnContinue);
            buttons.Add(btnNewGame);
            buttons.Add(btnLoadGame);
            buttons.Add(btnSettings);
            buttons.Add(btnExit);

            int totalH = buttons.Count * btnH + (buttons.Count - 1) * gap;
            int startY = (int)(h * 0.5) - totalH / 2 + 40;
            int btnX = (w - btnW) / 2;

            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].Left = btnX;
                buttons[i].Top = startY + i * (btnH + gap);
                buttons[i].Width = btnW;
                buttons[i].Height = btnH;
            }
        }

        private void MenuButton_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;

            var b = (Button)sender;
            if (b.Width <= 4 || b.Height <= 4) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, b.Width, b.Height);

            Color c1 = b.BackColor;
            Color c2 = Color.FromArgb(
                Math.Max(0, c1.R - 55),
                Math.Max(0, c1.G - 55),
                Math.Max(0, c1.B - 55));

            using (var br = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical))
                g.FillRectangle(br, rect);

            using (var hl = new LinearGradientBrush(
                new Rectangle(0, 0, rect.Width, rect.Height / 2),
                Color.FromArgb(70, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255), 90f))
                g.FillRectangle(hl, new Rectangle(0, 0, rect.Width, rect.Height / 2));

            using (var pen = new Pen(Color.FromArgb(180, 200, 240, 255), 1.5f))
                g.DrawRectangle(pen, 1, 1, rect.Width - 3, rect.Height - 3);

            using (var br = new SolidBrush(Color.White))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            })
                g.DrawString(b.Text, b.Font, br, new RectangleF(30, 0, b.Width - 40, b.Height), sf);

            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            })
            using (var arrowFont = new Font("Segoe UI", 14))
                g.DrawString("▶", arrowFont, br, new RectangleF(0, 0, b.Width - 25, b.Height), sf);
        }

        private void ContinueGame()
        {
            var slots = _saveService.GetSaveSlots();
            if (slots.Count == 0)
            {
                MessageBox.Show("Нет сохранений.", "Продолжить",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // берём самый свежий
            var latest = slots[0];
            foreach (var s in slots)
                if (s.SavedAt > latest.SavedAt) latest = s;

            var state = _saveService.LoadFromSlot(latest.SlotIndex);
            if (state == null)
            {
                MessageBox.Show("Не удалось загрузить.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool wasTopMost = TopMost;
            TopMost = false;

            var main = new MainForm();
            main.LoadState(state);

            main.FormClosed += (s, e) =>
            {
                Show();
                ApplyFullscreen();
            };

            Hide();
            main.Show();

        }

        private void NewGame()
        {
            bool wasTopMost = TopMost;
            TopMost = false;

            var main = new MainForm();

            main.FormClosed += (s, e) =>
            {
                Show();
                ApplyFullscreen();
            };

            Hide();
            main.Show();
        }

        private void LoadGame()
        {
            var slots = _saveService.GetSaveSlots();
            if (slots.Count == 0)
            {
                MessageBox.Show(
                    "Сохранений не найдено.\n\nНачните новую игру и сохранитесь.",
                    "Загрузить игру",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            bool wasTopMost = TopMost;
            TopMost = false;

            try
            {
                using (var dlg = new LoadGameDialog(slots, _saveService))
                {
                    dlg.TopMost = true;
                    dlg.StartPosition = FormStartPosition.CenterScreen;

                    var result = dlg.ShowDialog();

                    if (result == DialogResult.OK && dlg.SelectedState != null)
                    {
                        var main = new MainForm();
                        main.LoadState(dlg.SelectedState);
                        main.FormClosed += (s, e) =>
                        {
                            Show();
                            ApplyFullscreen();
                        };
                        Hide();
                        main.Show();
                        return;
                    }
                }
            }
            finally
            {
                if (Visible)
                {
                    TopMost = wasTopMost;
                    Activate();
                    BringToFront();
                }
            }
        }

        private void ShowSettings()
        {
            bool wasTopMost = TopMost;
            TopMost = false;

            try
            {
                using (var dlg = new SettingsDialog(_isFullscreen))
                {
                    dlg.TopMost = true;
                    dlg.StartPosition = FormStartPosition.CenterScreen;

                    var result = dlg.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        if (dlg.Fullscreen != _isFullscreen)
                            ToggleFullscreen(dlg.Fullscreen);
                    }
                }
            }
            finally
            {
                TopMost = wasTopMost;
                Activate();
                BringToFront();
            }
        }

        private void ApplyFullscreen()
        {
            _prevBounds = Bounds;
            _prevBorder = FormBorderStyle;
            _prevState = WindowState;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).Bounds;
            TopMost = true;

            _isFullscreen = true;
            LayoutControls();
            Invalidate();
        }

        private void ToggleFullscreen(bool on)
        {
            if (on && !_isFullscreen)
            {
                ApplyFullscreen();
            }
            else if (!on && _isFullscreen)
            {
                TopMost = false;
                FormBorderStyle = _prevBorder;
                WindowState = _prevState;
                Bounds = _prevBounds;
                _isFullscreen = false;
                LayoutControls();
            }
        }

        private void MenuForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen(!_isFullscreen);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && _isFullscreen)
            {
                ToggleFullscreen(false);
                e.Handled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closing = true;
            try { animTimer?.Stop(); } catch { }
            try { subtitleTimer?.Stop(); } catch { }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { animTimer?.Dispose(); } catch { }
                try { subtitleTimer?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }

    public class LoadGameDialog : Form
    {
        public GameState SelectedState { get; private set; }

        private readonly SaveService _saveService;
        private ListBox lstSaves;

        public LoadGameDialog(List<SaveSlotInfo> slots, SaveService saveService)
        {
            _saveService = saveService;
            Text = "Загрузить игру";
            Size = new Size(650, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 24, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;

            var lblTitle = new Label
            {
                Text = "💾 Выберите сохранение",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lstSaves = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 40, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 60,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstSaves.DrawItem += LstSaves_DrawItem;

            foreach (var s in slots)
                lstSaves.Items.Add(s);

            if (lstSaves.Items.Count > 0)
                lstSaves.SelectedIndex = 0;

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(15) };

            var btnLoad = new Button
            {
                Text = "▶  Загрузить",
                Width = 170,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 140, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (s, e) =>
            {
                if (lstSaves.SelectedItem is SaveSlotInfo info)
                {
                    SelectedState = _saveService.LoadFromSlot(info.SlotIndex);
                    if (SelectedState != null)
                        DialogResult = DialogResult.OK;
                    else
                        MessageBox.Show("Не удалось загрузить.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnDelete = new Button
            {
                Text = "🗑  Удалить",
                Width = 150,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(140, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) =>
            {
                if (lstSaves.SelectedItem is SaveSlotInfo info)
                {
                    if (MessageBox.Show($"Удалить сохранение «{info.PlayerName}»?", "Подтверждение",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _saveService.DeleteSlot(info.SlotIndex);
                        lstSaves.Items.Remove(info);
                    }
                }
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                Width = 120,
                Height = 35,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 65, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlButtons.Controls.Add(btnLoad);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnCancel);

            Controls.Add(lstSaves);
            Controls.Add(pnlButtons);
            Controls.Add(lblTitle);
        }

        private void LstSaves_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var info = (SaveSlotInfo)lstSaves.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            var bg = selected ? Color.FromArgb(70, 110, 180) : Color.FromArgb(35, 40, 65);
            using (var br = new SolidBrush(bg))
                e.Graphics.FillRectangle(br, e.Bounds);

            using (var titleFont = new Font("Segoe UI", 12, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 9))
            {
                var titleRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y + 8, e.Bounds.Width - 20, 25);
                var subRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y + 33, e.Bounds.Width - 20, 20);

                using (var br = new SolidBrush(Color.White))
                    e.Graphics.DrawString($"Слот {info.SlotIndex + 1}: {info.PlayerName}", titleFont, br, titleRect);

                using (var br = new SolidBrush(Color.FromArgb(180, 210, 240)))
                    e.Graphics.DrawString(
                        $"Глава {info.Chapter}   |   {info.Money:N0}₽   |   {info.SavedAt:dd.MM.yyyy HH:mm}",
                        subFont, br, subRect);
            }

            using (var pen = new Pen(Color.FromArgb(60, 100, 150), 1))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }
    }

    public class SettingsDialog : Form
    {
        public bool Fullscreen { get; private set; }

        public SettingsDialog(bool currentFullscreen)
        {
            Text = "⚙ Настройки";
            Size = new Size(520, 320);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 24, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;

            var lblTitle = new Label
            {
                Text = "⚙  Настройки",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            var chkFullscreen = new CheckBox
            {
                Text = "🖥  Полноэкранный режим",
                Location = new Point(30, 100),
                Width = 440,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Checked = currentFullscreen
            };

            var lblHint = new Label
            {
                Text = "F11 — вкл/выкл фуллскрин\nEsc — выход из фуллскрина",
                Location = new Point(30, 145),
                Width = 440,
                Height = 40,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 200, 230)
            };

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(15) };

            var btnOk = new Button
            {
                Text = "OK",
                Width = 120,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 140, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                Fullscreen = chkFullscreen.Checked;
                DialogResult = DialogResult.OK;
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                Width = 120,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 65, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlButtons.Controls.Add(btnOk);
            pnlButtons.Controls.Add(btnCancel);

            Controls.Add(lblHint);
            Controls.Add(chkFullscreen);
            Controls.Add(pnlButtons);
            Controls.Add(lblTitle);
        }
    }

    public class SaveSlotDialog : Form
    {
        public int SelectedSlot { get; private set; } = 0;

        private readonly SaveService _saveService;
        private ListBox lstSlots;

        public SaveSlotDialog(SaveService saveService)
        {
            _saveService = saveService;
            Text = "💾 Сохранить игру";
            Size = new Size(520, 440);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(20, 24, 40);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;

            var lblTitle = new Label
            {
                Text = "💾  Выберите слот",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lstSlots = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 40, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 55,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstSlots.DrawItem += LstSlots_DrawItem;

            var existing = _saveService.GetSaveSlots();
            var map = new Dictionary<int, SaveSlotInfo>();
            foreach (var s in existing) map[s.SlotIndex] = s;

            for (int i = 0; i < 5; i++)
            {
                if (map.ContainsKey(i))
                    lstSlots.Items.Add(map[i]);
                else
                    lstSlots.Items.Add(new SaveSlotInfo
                    {
                        SlotIndex = i,
                        PlayerName = "— пусто —",
                        Chapter = 0,
                        Money = 0,
                        SavedAt = DateTime.MinValue
                    });
            }

            lstSlots.SelectedIndex = 0;

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(15) };

            var btnSave = new Button
            {
                Text = "💾  Сохранить",
                Width = 160,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 140, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (lstSlots.SelectedItem is SaveSlotInfo info)
                {
                    if (info.SavedAt != DateTime.MinValue)
                    {
                        var res = MessageBox.Show(
                            $"Перезаписать слот {info.SlotIndex + 1}?\n\nТекущее сохранение «{info.PlayerName}» будет удалено.",
                            "Подтверждение",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (res != DialogResult.Yes) return;
                    }

                    SelectedSlot = info.SlotIndex;
                    DialogResult = DialogResult.OK;
                }
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                Width = 120,
                Height = 35,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 65, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnCancel);

            Controls.Add(lstSlots);
            Controls.Add(pnlButtons);
            Controls.Add(lblTitle);
        }

        private void LstSlots_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var info = (SaveSlotInfo)lstSlots.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool empty = info.SavedAt == DateTime.MinValue;

            var bg = selected ? Color.FromArgb(70, 110, 180) : Color.FromArgb(35, 40, 65);
            using (var br = new SolidBrush(bg))
                e.Graphics.FillRectangle(br, e.Bounds);

            using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 9))
            {
                var titleRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y + 6, e.Bounds.Width - 20, 22);
                var subRect = new Rectangle(e.Bounds.X + 15, e.Bounds.Y + 28, e.Bounds.Width - 20, 20);

                Color titleColor = empty ? Color.FromArgb(140, 150, 170) : Color.White;
                using (var br = new SolidBrush(titleColor))
                    e.Graphics.DrawString($"Слот {info.SlotIndex + 1}: {info.PlayerName}", titleFont, br, titleRect);

                if (!empty)
                {
                    using (var br = new SolidBrush(Color.FromArgb(180, 210, 240)))
                        e.Graphics.DrawString(
                            $"Глава {info.Chapter}   |   {info.Money:N0}₽   |   {info.SavedAt:dd.MM.yyyy HH:mm}",
                            subFont, br, subRect);
                }
                else
                {
                    using (var br = new SolidBrush(Color.FromArgb(120, 130, 150)))
                        e.Graphics.DrawString("(нажмите Сохранить, чтобы записать сюда)", subFont, br, subRect);
                }
            }

            using (var pen = new Pen(Color.FromArgb(60, 100, 150), 1))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }
    }
}
