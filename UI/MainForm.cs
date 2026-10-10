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
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint, true);
            UpdateStyles();
        }
    }

    public class MainForm : Form
    {
        private readonly GameEngine _engine = new GameEngine();
        private readonly SaveService _saveService = new SaveService();

        private BufferedPanel pnlHeader;
        private BufferedPanel pnlStart;
        private BufferedPanel pnlEventCard;
        private BufferedPanel pnlBars;
        private FlowLayoutPanel pnlChoices;

        private Label lblStats;
        private Label lblChapter;
        private Label lblGoal;
        private Label lblEventTitle;
        private Label lblEventDescription;

        private TextBox txtName;
        private Button btnStart;
        private Button btnSave;
        private Button btnMenu;
        private Button btnShop;
        private Button btnBank;

        private WinFormsTimer animTimer;
        private WinFormsTimer nextTurnTimer;

        private float cardAnim = 1f;
        private float pulse = 0f;
        private int _headerPulseCounter = 0;

        private float animHealth = 100f;
        private float animHappiness = 100f;
        private float animIntel = 100f;
        private float animSocial = 100f;

        private float targetHealth = 100f;
        private float targetHappiness = 100f;
        private float targetIntel = 100f;
        private float targetSocial = 100f;

        private readonly List<Button> _pendingButtons = new List<Button>();
        private int _revealedButtons = 0;
        private float _buttonRevealTimer = 0f;

        private readonly Color BgDark = Color.FromArgb(12, 14, 24);
        private readonly Color BgMid = Color.FromArgb(24, 28, 48);
        private readonly Color AccentCyan = Color.FromArgb(0, 220, 255);
        private readonly Color AccentGold = Color.FromArgb(255, 200, 60);

        private bool _closing = false;
        private bool _suppressDialogs = false;
        private bool _isFullscreen = false;
        private Rectangle _prevBounds;
        private FormBorderStyle _prevBorder;
        private FormWindowState _prevState;

        public MainForm()
        {
            Text = "🎮 Моя Жизнь — Deluxe";
            Size = new Size(1350, 900);
            MinimumSize = new Size(1200, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);

            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint, true);
            UpdateStyles();

            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            _engine.OnChapterCompleted += ch =>
                SafeShowMessage($"🎉 Глава пройдена!\n\n{ch.Title}", "Прогресс");

            _engine.OnChapterStarted += ch =>
                SafeShowMessage($"📖 Новая глава!\n\n{ch.Title}\n\n🎯 {ch.GoalText}", "Новая глава");

            _engine.OnTaxPaid += (amount, success) =>
            {
                if (success)
                    SafeShowMessage(
                        $"🧾 Ежегодный налог оплачен!\n\n" +
                        $"Сумма: {amount:N0}₽\n" +
                        $"Ставка: {_engine.State.Player.TaxRateText}",
                        "💰 Налоговая служба");
                else
                    SafeShowMessage(
                        $"🚨 Ты не смог оплатить налог {amount:N0}₽!\n\n" +
                        $"Штраф: -20 😊, -10 ❤",
                        "💰 Налоговая служба",
                        MessageBoxIcon.Warning);
            };

            _engine.OnMicroEvent += msg => { };

            _engine.OnChainTriggered += msg =>
                SafeShowMessage(msg, "🔗 Событие из прошлого");

            _engine.OnLoanPayment += amount =>
                SafeShowMessage(
                    $"🏦 Ежемесячный платёж по кредиту\n\n" +
                    $"Оплачено: {amount:N0}₽\n" +
                    $"Осталось: {_engine.State.Player.LoanRemaining:N0}₽\n" +
                    $"Месяцев: {_engine.State.Player.LoanMonthsLeft}",
                    "🏦 Банк");

            _engine.OnLoanDefault += amount =>
                SafeShowMessage(
                    $"🚨 ПРОСРОЧКА ПО КРЕДИТУ!\n\n" +
                    $"Не оплачено: {amount:N0}₽\n" +
                    $"Штраф: -5 ❤, -10 😊\n" +
                    $"Баланс: {_engine.State.Player.Money:N0}₽",
                    "🚨 Банк",
                    MessageBoxIcon.Warning);

            _engine.OnLoanFullyPaid += () =>
                SafeShowMessage(
                    $"🎉 Кредит полностью погашен!\n\n+15 😊",
                    "✅ Банк");

            _engine.OnBankrupt += debt =>
                SafeShowMessage(
                    $"💀 БАНКРОТ!\n\n" +
                    $"Твой долг: {debt:N0}₽\n\n" +
                    $"Бандиты нашли тебя. Они забрали часть денег\n" +
                    $"и хорошенько проучили.\n\n" +
                    $"Штраф: -15 ❤, -20 😊",
                    "💀 Бандиты",
                    MessageBoxIcon.Error);

            BuildUI();
            StartAnimation();
            InitNextTurnTimer();
        }

        private void SafeShowMessage(string text, string caption, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            if (_suppressDialogs) return;
            if (_closing || IsDisposed) return;

            if (IsHandleCreated)
            {
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (_closing || IsDisposed) return;
                        if (_suppressDialogs) return;
                        MessageBox.Show(this, text, caption, MessageBoxButtons.OK, icon);
                    }));
                }
                catch (InvalidOperationException) { }
            }
            else
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    Load -= handler;
                    if (_closing || IsDisposed) return;
                    if (_suppressDialogs) return;
                    try
                    {
                        BeginInvoke(new Action(() =>
                        {
                            if (_closing || IsDisposed) return;
                            if (_suppressDialogs) return;
                            MessageBox.Show(this, text, caption, MessageBoxButtons.OK, icon);
                        }));
                    }
                    catch (InvalidOperationException) { }
                };
                Load += handler;
            }
        }

        public void LoadState(GameState state)
        {
            if (state == null) return;

            _engine.State = state;

            var p = _engine.State.Player;
            p.OnChanged -= UpdateStats;
            p.OnChanged += UpdateStats;

            animHealth = targetHealth = p.Health;
            animHappiness = targetHappiness = p.Happiness;
            animIntel = targetIntel = p.Intelligence;
            animSocial = targetSocial = p.Social;

            txtName.Text = p.Name;

            UpdateChapter();
            UpdateStats(p);

            if (IsHandleCreated)
            {
                NextTurn();
            }
            else
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    Load -= handler;
                    NextTurn();
                };
                Load += handler;
            }
        }

        private void InitNextTurnTimer()
        {
            // ⚡ 250ms — быстрые ходы
            nextTurnTimer = new WinFormsTimer { Interval = 250 };
            nextTurnTimer.Tick += (s, e) =>
            {
                StopNextTurnTimer();
                NextTurn();
            };
        }

        private void StartNextTurnTimer()
        {
            if (nextTurnTimer == null) return;
            try { nextTurnTimer.Stop(); nextTurnTimer.Start(); } catch { }
        }

        private void StopNextTurnTimer()
        {
            if (nextTurnTimer == null) return;
            try { nextTurnTimer.Stop(); } catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_closing) return;
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new LinearGradientBrush(ClientRectangle, BgMid, BgDark, 45f))
                g.FillRectangle(brush, ClientRectangle);

            var rnd = new Random(42);
            for (int i = 0; i < 120; i++)
            {
                int x = rnd.Next(Math.Max(1, ClientRectangle.Width));
                int y = rnd.Next(Math.Max(1, ClientRectangle.Height));
                int a = rnd.Next(30, 120);
                using (var b = new SolidBrush(Color.FromArgb(a, 180, 220, 255)))
                    g.FillEllipse(b, x, y, 2, 2);
            }
        }

        private void StartAnimation()
        {
            animTimer = new WinFormsTimer { Interval = 33 };
            animTimer.Tick += (s, e) =>
            {
                if (_closing) return;

                bool barsChanged = false;
                barsChanged |= Approach(ref animHealth, targetHealth, 1.5f);
                barsChanged |= Approach(ref animHappiness, targetHappiness, 1.5f);
                barsChanged |= Approach(ref animIntel, targetIntel, 1.5f);
                barsChanged |= Approach(ref animSocial, targetSocial, 1.5f);
                if (barsChanged) SafeInvalidate(pnlBars);

                if (cardAnim < 1f)
                {
                    cardAnim += 0.12f;
                    if (cardAnim > 1f) cardAnim = 1f;
                    SafeInvalidate(pnlEventCard);
                }

                if (_revealedButtons < _pendingButtons.Count)
                {
                    _buttonRevealTimer += 1f;
                    if (_buttonRevealTimer >= 2f)
                    {
                        _buttonRevealTimer = 0f;
                        _pendingButtons[_revealedButtons].Visible = true;
                        _revealedButtons++;
                    }
                }

                _headerPulseCounter++;
                if (_headerPulseCounter >= 3)
                {
                    _headerPulseCounter = 0;
                    pulse += 0.05f;
                    if (pulse > Math.PI * 2) pulse -= (float)(Math.PI * 2);
                    SafeInvalidate(pnlHeader);
                }
            };
            animTimer.Start();
        }

        private bool Approach(ref float current, float target, float speed)
        {
            float diff = target - current;
            if (Math.Abs(diff) < 0.5f)
            {
                if (current != target) { current = target; return true; }
                return false;
            }
            float step = Math.Sign(diff) * Math.Min(speed, Math.Abs(diff));
            current += step;
            return true;
        }

        private void SafeInvalidate(Control c)
        {
            if (c == null) return;
            if (c.IsDisposed) return;
            if (!c.IsHandleCreated) return;
            try { c.Invalidate(); } catch { }
        }

        private void BuildUI()
        {
            pnlHeader = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.Transparent
            };
            pnlHeader.Paint += PnlHeader_Paint;

            lblChapter = new Label
            {
                Location = new Point(30, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = AccentCyan,
                BackColor = Color.Transparent,
                Text = "🎬 Нажмите 'Начать жизнь'"
            };

            lblGoal = new Label
            {
                Location = new Point(32, 62),
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = AccentGold,
                BackColor = Color.Transparent,
                Text = ""
            };

            pnlHeader.Controls.Add(lblChapter);
            pnlHeader.Controls.Add(lblGoal);

            pnlStart = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.Transparent
            };

            txtName = new TextBox
            {
                Text = "Игрок",
                Width = 150,
                Location = new Point(30, 22),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(40, 45, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnStart = new Button
            {
                Text = "▶  НАЧАТЬ",
                Location = new Point(190, 20),
                Width = 120,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 140, 90),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Paint += GlowButton_Paint;
            btnStart.MouseEnter += (s, e) => { btnStart.BackColor = Color.FromArgb(0, 180, 110); btnStart.Invalidate(); };
            btnStart.MouseLeave += (s, e) => { btnStart.BackColor = Color.FromArgb(0, 140, 90); btnStart.Invalidate(); };
            btnStart.Click += (s, e) => StartGame();

            btnShop = new Button
            {
                Text = "🏪  МАГАЗИН",
                Location = new Point(320, 20),
                Width = 160,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 130, 40),
                Cursor = Cursors.Hand
            };
            btnShop.FlatAppearance.BorderSize = 0;
            btnShop.Paint += GlowButton_Paint;
            btnShop.MouseEnter += (s, e) => { btnShop.BackColor = Color.FromArgb(220, 170, 60); btnShop.Invalidate(); };
            btnShop.MouseLeave += (s, e) => { btnShop.BackColor = Color.FromArgb(180, 130, 40); btnShop.Invalidate(); };
            btnShop.Click += (s, e) => OpenShop();

            btnBank = new Button
            {
                Text = "🏦  БАНК",
                Location = new Point(490, 20),
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 130, 80),
                Cursor = Cursors.Hand
            };
            btnBank.FlatAppearance.BorderSize = 0;
            btnBank.Paint += GlowButton_Paint;
            btnBank.MouseEnter += (s, e) => { btnBank.BackColor = Color.FromArgb(70, 170, 110); btnBank.Invalidate(); };
            btnBank.MouseLeave += (s, e) => { btnBank.BackColor = Color.FromArgb(50, 130, 80); btnBank.Invalidate(); };
            btnBank.Click += (s, e) => OpenBank();

            btnSave = new Button
            {
                Text = "💾  СОХРАНИТЬ",
                Location = new Point(650, 20),
                Width = 160,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 90, 160),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Paint += GlowButton_Paint;
            btnSave.MouseEnter += (s, e) => { btnSave.BackColor = Color.FromArgb(60, 120, 200); btnSave.Invalidate(); };
            btnSave.MouseLeave += (s, e) => { btnSave.BackColor = Color.FromArgb(40, 90, 160); btnSave.Invalidate(); };
            btnSave.Click += (s, e) => SaveCurrentGame();

            btnMenu = new Button
            {
                Text = "☰  В МЕНЮ",
                Location = new Point(820, 20),
                Width = 150,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(90, 70, 140),
                Cursor = Cursors.Hand
            };
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.Paint += GlowButton_Paint;
            btnMenu.MouseEnter += (s, e) => { btnMenu.BackColor = Color.FromArgb(120, 100, 180); btnMenu.Invalidate(); };
            btnMenu.MouseLeave += (s, e) => { btnMenu.BackColor = Color.FromArgb(90, 70, 140); btnMenu.Invalidate(); };
            btnMenu.Click += (s, e) => ReturnToMenu();

            pnlStart.Controls.Add(txtName);
            pnlStart.Controls.Add(btnStart);
            pnlStart.Controls.Add(btnShop);
            pnlStart.Controls.Add(btnBank);
            pnlStart.Controls.Add(btnSave);
            pnlStart.Controls.Add(btnMenu);

            pnlBars = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.Transparent
            };
            pnlBars.Paint += PnlBars_Paint;

            lblStats = new Label
            {
                Location = new Point(30, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text = ""
            };
            pnlBars.Controls.Add(lblStats);

            pnlEventCard = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.Transparent
            };
            pnlEventCard.Paint += PnlEventCard_Paint;

            lblEventTitle = new Label
            {
                Location = new Point(60, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = AccentGold,
                BackColor = Color.Transparent,
                Text = ""
            };

            lblEventDescription = new Label
            {
                Location = new Point(62, 75),
                MaximumSize = new Size(1200, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(220, 230, 255),
                BackColor = Color.Transparent,
                Text = ""
            };

            pnlEventCard.Controls.Add(lblEventTitle);
            pnlEventCard.Controls.Add(lblEventDescription);

            pnlChoices = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(40, 20, 40, 30),
                BackColor = Color.Transparent
            };

            Controls.Add(pnlChoices);
            Controls.Add(pnlEventCard);
            Controls.Add(pnlBars);
            Controls.Add(pnlStart);
            Controls.Add(pnlHeader);
        }

        private void PnlHeader_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;
            if (pnlHeader.Width <= 60) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.FromArgb(180, AccentCyan), 2))
                g.DrawLine(pen, 30, pnlHeader.Height - 3, pnlHeader.Width - 30, pnlHeader.Height - 3);
        }

        private void PnlEventCard_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;
            if (pnlEventCard.Width <= 120 || pnlEventCard.Height <= 30) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float ease = EaseOutCubic(cardAnim);
            int slideX = (int)((1f - ease) * -120);
            int alphaMul = Math.Clamp((int)(255 * ease), 0, 255);

            var rect = new Rectangle(
                40 + slideX,
                15,
                pnlEventCard.Width - 80,
                pnlEventCard.Height - 30);

            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (var brush = new LinearGradientBrush(rect,
                Color.FromArgb(alphaMul, 45, 55, 90),
                Color.FromArgb(alphaMul, 25, 30, 55), 90f))
                g.FillRectangle(brush, rect);

            int borderAlpha = Math.Clamp((int)(180 * ease), 0, 255);
            using (var pen = new Pen(Color.FromArgb(borderAlpha, AccentCyan), 2))
                g.DrawRectangle(pen, rect);

            int hlAlpha = Math.Clamp((int)(60 * ease), 0, 255);
            int hlH = Math.Min(30, rect.Height);
            if (hlH > 0)
            {
                using (var hl = new LinearGradientBrush(
                    new Rectangle(rect.X, rect.Y, rect.Width, hlH),
                    Color.FromArgb(hlAlpha, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255), 90f))
                    g.FillRectangle(hl, new Rectangle(rect.X, rect.Y, rect.Width, hlH));
            }
        }

        private float EaseOutCubic(float t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return 1f - (float)Math.Pow(1f - t, 3);
        }

        private void PnlBars_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int y = 65;
            int blockW = 250;
            int gap = 20;
            int startX = 30;

            DrawNeonBar(g, startX + 0 * (blockW + gap), y, "❤ ЗДОРОВЬЕ", animHealth, 100, Color.FromArgb(255, 60, 90));
            DrawNeonBar(g, startX + 1 * (blockW + gap), y, "😊 СЧАСТЬЕ", animHappiness, 100, Color.FromArgb(255, 200, 60));
            DrawNeonBar(g, startX + 2 * (blockW + gap), y, "🧠 ИНТЕЛЛЕКТ", animIntel, 100, Color.FromArgb(80, 180, 255));
            DrawNeonBar(g, startX + 3 * (blockW + gap), y, "👥 СОЦИУМ", animSocial, 100, Color.FromArgb(180, 100, 255));
        }

        private void DrawNeonBar(Graphics g, int x, int y, string label, float value, int max, Color color)
        {
            if (max <= 0) max = 1;
            int w = 250, h = 28;

            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(220, 230, 250)))
                g.DrawString(label, font, br, x + 5, y - 22);

            using (var br = new SolidBrush(Color.FromArgb(90, 10, 15, 30)))
                g.FillRectangle(br, x, y, w, h);

            int fill = (int)(w * Math.Clamp(value / max, 0, 1));
            if (fill > 0)
            {
                using (var br = new LinearGradientBrush(
                    new Rectangle(x, y, fill, h),
                    Color.FromArgb(200, color), color, LinearGradientMode.Vertical))
                    g.FillRectangle(br, x, y, fill, h);

                using (var hl = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                    g.FillRectangle(hl, x, y, fill, h / 3);
            }

            using (var pen = new Pen(Color.FromArgb(160, color), 1.5f))
                g.DrawRectangle(pen, x, y, w, h);

            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var br = new SolidBrush(Color.White))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString($"{(int)value} / {max}", font, br, new RectangleF(x, y, w - 8, h), sf);
            }
        }

        private void GlowButton_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;

            var btn = (Button)sender;
            if (btn.Width <= 4 || btn.Height <= 4) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, btn.Width, btn.Height);

            Color c1 = btn.BackColor;
            Color c2 = Color.FromArgb(
                Math.Max(0, c1.R - 60),
                Math.Max(0, c1.G - 60),
                Math.Max(0, c1.B - 60));

            using (var br = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical))
                g.FillRectangle(br, rect);

            if (btn.Width > 6 && btn.Height > 6)
            {
                using (var pen = new Pen(Color.FromArgb(180, 120, 255, 180), 2))
                    g.DrawRectangle(pen, 1, 1, btn.Width - 3, btn.Height - 3);
            }

            if (btn.Font != null && !string.IsNullOrEmpty(btn.Text))
            {
                using (var br = new SolidBrush(Color.White))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                    g.DrawString(btn.Text, btn.Font, br, new RectangleF(0, 0, btn.Width, btn.Height), sf);
            }
        }

        private void StartGame()
        {
            _engine.Reset();
            _engine.State.Player.Name = string.IsNullOrWhiteSpace(txtName.Text) ? "Игрок" : txtName.Text;

            _engine.State.Player.OnChanged -= UpdateStats;
            _engine.State.Player.OnChanged += UpdateStats;

            var p = _engine.State.Player;
            animHealth = targetHealth = p.Health;
            animHappiness = targetHappiness = p.Happiness;
            animIntel = targetIntel = p.Intelligence;
            animSocial = targetSocial = p.Social;

            UpdateChapter();
            UpdateStats(p);
            NextTurn();
        }

        private void OpenShop()
        {
            if (_engine.State.Turn == 0)
            {
                MessageBox.Show("Сначала начните игру.", "Магазин",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _suppressDialogs = true;
            try
            {
                using (var dlg = new ShopDialog(_engine))
                    dlg.ShowDialog(this);
            }
            finally { _suppressDialogs = false; }

            UpdateChapter();
            UpdateStats(_engine.State.Player);
            SafeInvalidate(pnlBars);
        }

        private void OpenBank()
        {
            if (_engine.State.Turn == 0)
            {
                MessageBox.Show("Сначала начните игру.", "Банк",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _suppressDialogs = true;
            try
            {
                using (var dlg = new BankDialog(_engine))
                    dlg.ShowDialog(this);
            }
            finally { _suppressDialogs = false; }

            UpdateChapter();
            UpdateStats(_engine.State.Player);
            SafeInvalidate(pnlBars);
        }

        private void SaveCurrentGame()
        {
            if (_engine.State.Turn == 0)
            {
                MessageBox.Show("Сначала начните игру.", "Сохранение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _suppressDialogs = true;
            try
            {
                using (var dlg = new SaveSlotDialog(_saveService))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _saveService.SaveToSlot(_engine.State, dlg.SelectedSlot);
                        _suppressDialogs = false;
                        MessageBox.Show($"Игра сохранена в слот {dlg.SelectedSlot + 1}.", "Сохранение",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            finally { _suppressDialogs = false; }
        }

        private void ReturnToMenu()
        {
            var res = MessageBox.Show(
                "Выйти в главное меню?\n\nНесохранённый прогресс будет потерян.",
                "Выход в меню",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                _closing = true;
                Close();
            }
        }

        private void UpdateChapter()
        {
            var ch = _engine.State.GetCurrentChapter();
            if (ch != null)
            {
                lblChapter.Text = $"🎬 Глава {ch.Number}: {ch.Title}";
                lblGoal.Text = $"🎯 Цель: {ch.GoalText}";
            }
        }

        private void UpdateStats(Player p)
        {
            if (_closing) return;

            int passive = p.GetAssetsIncome();
            string family = "";
            if (p.IsMarried) family += $"     👰 {p.SpouseName}";
            if (p.ChildrenCount > 0) family += $"     👶 {p.ChildrenCount}";

            string education = "";
            if (p.HasEducation)
            {
                string[] levels = { "", "🎓", "📚", "🏆" };
                education = $"     {levels[Math.Min(p.EducationLevel, 3)]} {p.Education}";
            }

            string trips = p.TripsCount > 0 ? $"     ✈ {p.TripsCount}" : "";
            string passiveText = passive > 0 ? $"     📊 {passive:N0}₽/мес" : "";

            string loanText = "";
            if (p.HasLoan)
            {
                string color = p.Money < 0 ? "🚨" : "🏦";
                loanText = $"     {color} Долг: {p.LoanRemaining:N0}₽ ({p.LoanMonthsLeft} мес)";
            }

            // ⚠️ Показываем неделю и месяц
            string dateText = $"📅 {p.Season} {p.MonthName} {p.Age} лет (неделя {p.Week}/4)";

            lblStats.Text =
                $"👤 {p.Name}     " +
                dateText + "     " +
                $"💰 {p.Money:N0}₽     " +
                $"💼 {p.Job}     " +
                $"📈 +{p.MonthlyIncome:N0}₽/мес     " +
                $"🧾 {p.TaxRateText}" +
                family + education + trips + passiveText + loanText;

            targetHealth = p.Health;
            targetHappiness = p.Happiness;
            targetIntel = p.Intelligence;
            targetSocial = p.Social;
        }

        private void NextTurn()
        {
            if (_closing) return;

            if (!_engine.State.Player.IsAlive)
            {
                MessageBox.Show(
                    $"Жизнь оборвалась...\n\n" +
                    $"Возраст: {_engine.State.Player.Age}\n" +
                    $"Глава: {_engine.State.Player.Chapter}\n" +
                    $"Деньги: {_engine.State.Player.Money:N0}₽\n" +
                    $"Налогов уплачено: {_engine.State.Player.TotalTaxPaid:N0}₽",
                    "🎬 Конец", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnStart.Text = "▶  НАЧАТЬ ЗАНОВО";
                return;
            }

            UpdateChapter();
            var ev = _engine.NextTurn();
            ShowEvent(ev);
        }

        private void ShowEvent(GameEvent ev)
        {
            if (_closing) return;

            lblEventTitle.Text = ev.Title;
            lblEventDescription.Text = ev.Description;

            pnlChoices.SuspendLayout();
            pnlChoices.Controls.Clear();

            _pendingButtons.Clear();
            _revealedButtons = 0;
            _buttonRevealTimer = 0f;
            cardAnim = 0f;

            var player = _engine.State.Player;

            foreach (var choice in ev.Choices)
            {
                int btnWidth = Math.Max(500, pnlChoices.ClientSize.Width - 90);
                bool available = choice.CanChoose(player);

                var btn = new Button
                {
                    Text = available ? "  " + choice.Text : "  " + choice.NotAvailableReason,
                    Width = btnWidth,
                    Height = 60,
                    BackColor = available ? Color.FromArgb(40, 70, 130) : Color.FromArgb(50, 50, 60),
                    ForeColor = available ? Color.White : Color.FromArgb(150, 150, 160),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Margin = new Padding(0, 8, 0, 0),
                    Cursor = available ? Cursors.Hand : Cursors.No,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(25, 0, 0, 0),
                    Visible = false,
                    Enabled = available
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Paint += ChoiceButton_Paint;

                if (available)
                {
                    btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(70, 120, 200); btn.Invalidate(); };
                    btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(40, 70, 130); btn.Invalidate(); };
                }

                var c = choice;
                btn.Click += (s, e) =>
                {
                    if (_closing) return;
                    if (!c.CanChoose(_engine.State.Player)) return;

                    _engine.ApplyChoice(c);

                    if (!string.IsNullOrEmpty(c.ResultMessage))
                        lblEventDescription.Text = c.ResultMessage;

                    foreach (Control ctrl in pnlChoices.Controls)
                        ctrl.Enabled = false;

                    StartNextTurnTimer();
                };

                _pendingButtons.Add(btn);
                pnlChoices.Controls.Add(btn);
            }

            pnlChoices.ResumeLayout();
        }

        private void ChoiceButton_Paint(object sender, PaintEventArgs e)
        {
            if (_closing) return;

            var b = (Button)sender;
            if (b.Width <= 4 || b.Height <= 4) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, b.Width, b.Height);
            bool disabled = !b.Enabled;

            Color c1 = b.BackColor;
            Color c2 = Color.FromArgb(
                Math.Max(0, c1.R - 55),
                Math.Max(0, c1.G - 55),
                Math.Max(0, c1.B - 55));

            using (var br = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical))
                g.FillRectangle(br, rect);

            if (!disabled)
            {
                using (var hl = new LinearGradientBrush(
                    new Rectangle(0, 0, rect.Width, rect.Height / 2),
                    Color.FromArgb(50, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255), 90f))
                    g.FillRectangle(hl, new Rectangle(0, 0, rect.Width, rect.Height / 2));
            }

            Color borderColor = disabled
                ? Color.FromArgb(80, 100, 100, 110)
                : Color.FromArgb(150, 100, 200, 255);

            using (var pen = new Pen(borderColor, 1.5f))
                g.DrawRectangle(pen, rect.X + 1, rect.Y + 1, rect.Width - 3, rect.Height - 3);

            using (var br = new SolidBrush(b.ForeColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            })
                g.DrawString(b.Text, b.Font, br, new RectangleF(25, 0, b.Width - 60, b.Height), sf);

            if (!disabled)
            {
                using (var br = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                })
                using (var arrowFont = new Font("Segoe UI", 12))
                    g.DrawString("▶", arrowFont, br, new RectangleF(0, 0, b.Width - 25, b.Height), sf);
            }
            else
            {
                using (var br = new SolidBrush(Color.FromArgb(120, 200, 200, 210)))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                })
                using (var lockFont = new Font("Segoe UI", 12))
                    g.DrawString("🔒", lockFont, br, new RectangleF(0, 0, b.Width - 25, b.Height), sf);
            }
        }

        private void ApplyFullscreen()
        {
            if (_isFullscreen) return;

            _prevBounds = Bounds;
            _prevBorder = FormBorderStyle;
            _prevState = WindowState;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).Bounds;
            TopMost = true;

            _isFullscreen = true;
            Invalidate();
        }

        private void ToggleFullscreen(bool on)
        {
            if (on && !_isFullscreen) ApplyFullscreen();
            else if (!on && _isFullscreen)
            {
                TopMost = false;
                FormBorderStyle = _prevBorder;
                WindowState = _prevState;
                Bounds = _prevBounds;
                _isFullscreen = false;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
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
            try { nextTurnTimer?.Stop(); } catch { }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { animTimer?.Dispose(); } catch { }
                try { nextTurnTimer?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //   🏦 БАНК
    // ═══════════════════════════════════════════════════════════════
    public class BankDialog : Form
    {
        private readonly GameEngine _engine;

        private Label lblMoney;
        private Label lblStatus;
        private Panel pnlContent;
        private NumericUpDown numAmount;
        private NumericUpDown numMonths;
        private Label lblPayment;
        private Button btnTake;
        private Button btnPayOff;

        public BankDialog(GameEngine engine)
        {
            _engine = engine;

            Text = "🏦 Банк";
            Size = new Size(820, 640);
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
                Text = "🏦  Государственный Банк",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 150),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lblMoney = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 220, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(15, 18, 30)
            };

            btnTake = new Button
            {
                Text = "✅ ВЗЯТЬ КРЕДИТ",
                Location = new Point(20, 20),
                Width = 260,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 140, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnTake.FlatAppearance.BorderSize = 0;
            btnTake.MouseEnter += (s, e) => { btnTake.BackColor = Color.FromArgb(0, 180, 110); };
            btnTake.MouseLeave += (s, e) => { btnTake.BackColor = Color.FromArgb(0, 140, 90); };
            btnTake.Click += (s, e) => TakeLoan();

            btnPayOff = new Button
            {
                Text = "💵 ПОГАСИТЬ ДОСРОЧНО",
                Location = new Point(20, 20),
                Width = 320,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 140, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnPayOff.FlatAppearance.BorderSize = 0;
            btnPayOff.MouseEnter += (s, e) => { if (btnPayOff.Enabled) btnPayOff.BackColor = Color.FromArgb(0, 180, 110); };
            btnPayOff.MouseLeave += (s, e) => { if (btnPayOff.Enabled) btnPayOff.BackColor = Color.FromArgb(0, 140, 90); };
            btnPayOff.Click += (s, e) => PayOffLoan();

            var btnClose = new Button
            {
                Text = "ЗАКРЫТЬ",
                Width = 150,
                Height = 60,
                Location = new Point(820 - 30 - 150, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 80, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.FromArgb(90, 100, 130); };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.FromArgb(70, 80, 110); };
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.Add(btnTake);
            pnlBottom.Controls.Add(btnPayOff);
            pnlBottom.Controls.Add(btnClose);

            Controls.Add(pnlContent);
            Controls.Add(pnlBottom);
            Controls.Add(lblStatus);
            Controls.Add(lblMoney);
            Controls.Add(lblTitle);

            RefreshUI();
        }

        private void RefreshUI()
        {
            var p = _engine.State.Player;
            lblMoney.Text = $"💰 Твой баланс: {p.Money:N0}₽     |     📈 Доход: +{p.MonthlyIncome:N0}₽/мес";

            pnlContent.Controls.Clear();
            btnTake.Visible = false;
            btnPayOff.Visible = false;

            if (p.HasLoan)
            {
                BuildLoanInfo();
                btnPayOff.Visible = true;
                btnPayOff.Enabled = p.Money >= p.LoanRemaining;
                btnPayOff.BackColor = btnPayOff.Enabled
                    ? Color.FromArgb(0, 140, 90)
                    : Color.FromArgb(70, 70, 80);
                btnPayOff.Text = $"💵 ПОГАСИТЬ ({p.LoanRemaining:N0}₽)";
            }
            else
            {
                BuildLoanForm();
                btnTake.Visible = true;
            }
        }

        private void BuildLoanForm()
        {
            int y = 0;

            var lblHeader = new Label
            {
                Text = "📋 Возьми кредит",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 45;

            var lblInfo = new Label
            {
                Text = "Процентная ставка:\n" +
                       "  • До 50 000₽       → 20%\n" +
                       "  • До 200 000₽      → 30%\n" +
                       "  • До 1 000 000₽   → 45%\n" +
                       "  • Больше 1 млн    → 60%\n\n" +
                       "⚠️ Если не платишь — деньги уходят в минус.\n" +
                       "⚠️ При долге > 50 000₽ — придут бандиты.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(200, 220, 250),
                Location = new Point(0, y),
                Size = new Size(720, 200)
            };
            y += 200;

            var lblAmount = new Label
            {
                Text = "💰 Сумма кредита:",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(0, y + 5),
                AutoSize = true
            };

            numAmount = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 5000000,
                Value = 10000,
                Increment = 1000,
                Location = new Point(220, y),
                Width = 200,
                Height = 35,
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(40, 45, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ThousandsSeparator = true
            };
            y += 55;

            var lblMonths = new Label
            {
                Text = "📅 Срок (мес):",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(0, y + 5),
                AutoSize = true
            };

            numMonths = new NumericUpDown
            {
                Minimum = 3,
                Maximum = 60,
                Value = 12,
                Increment = 1,
                Location = new Point(220, y),
                Width = 200,
                Height = 35,
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(40, 45, 70),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            y += 55;

            lblPayment = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 100),
                Location = new Point(0, y),
                Size = new Size(720, 60)
            };

            numAmount.ValueChanged += (s, e) => UpdatePayment();
            numMonths.ValueChanged += (s, e) => UpdatePayment();
            UpdatePayment();

            pnlContent.Controls.Add(lblHeader);
            pnlContent.Controls.Add(lblInfo);
            pnlContent.Controls.Add(lblAmount);
            pnlContent.Controls.Add(numAmount);
            pnlContent.Controls.Add(lblMonths);
            pnlContent.Controls.Add(numMonths);
            pnlContent.Controls.Add(lblPayment);
        }

        private void UpdatePayment()
        {
            if (numAmount == null || numMonths == null || lblPayment == null) return;

            int amount = (int)numAmount.Value;
            int months = (int)numMonths.Value;

            double rate = amount <= 50000 ? 1.20
                        : amount <= 200000 ? 1.30
                        : amount <= 1000000 ? 1.45
                        : 1.60;

            int total = (int)(amount * rate);
            int monthly = total / months;
            int overpay = total - amount;

            lblPayment.Text =
                $"💵 К возврату: {total:N0}₽ (переплата: {overpay:N0}₽)\n" +
                $"📆 Платёж: {monthly:N0}₽ × {months} мес";
        }

        private void TakeLoan()
        {
            if (numAmount == null || numMonths == null) return;

            int amount = (int)numAmount.Value;
            int months = (int)numMonths.Value;

            string error;
            if (_engine.TakeLoan(amount, months, out error))
            {
                lblStatus.Text = $"✅ Кредит на {amount:N0}₽ взят!";
                RefreshUI();
            }
            else
            {
                lblStatus.Text = $"❌ {error}";
            }
        }

        private void BuildLoanInfo()
        {
            var p = _engine.State.Player;
            int y = 0;

            var lblHeader = new Label
            {
                Text = "📋 Твой активный кредит",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 150),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 45;

            int progress = p.LoanAmount > 0
                ? (int)((1f - (float)p.LoanRemaining / (p.LoanAmount * 1.2f)) * 100)
                : 0;
            if (progress < 0) progress = 0;
            if (progress > 100) progress = 100;

            var lblInfo = new Label
            {
                Text =
                    $"💰 Взято:             {p.LoanAmount:N0}₽\n" +
                    $"📉 Осталось:        {p.LoanRemaining:N0}₽\n" +
                    $"📆 Платёж/мес:    {p.LoanMonthlyPayment:N0}₽\n" +
                    $"⏳ Месяцев:         {p.LoanMonthsLeft}",
                Font = new Font("Segoe UI", 13),
                ForeColor = Color.White,
                Location = new Point(0, y),
                Size = new Size(720, 150)
            };
            y += 140;

            var progressBar = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(720, 30),
                BackColor = Color.FromArgb(40, 50, 70)
            };
            progressBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                int w = (int)(progressBar.Width * (progress / 100f));
                if (w > 0)
                {
                    using (var br = new LinearGradientBrush(
                        new Rectangle(0, 0, w, progressBar.Height),
                        Color.FromArgb(0, 200, 120),
                        Color.FromArgb(0, 255, 150),
                        LinearGradientMode.Horizontal))
                    {
                        g.FillRectangle(br, 0, 0, w, progressBar.Height);
                    }
                }
                using (var pen = new Pen(Color.FromArgb(150, 100, 200, 255), 2))
                    g.DrawRectangle(pen, 0, 0, progressBar.Width - 1, progressBar.Height - 1);
            };
            y += 40;

            var lblProgress = new Label
            {
                Text = $"Прогресс: {progress}%",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 220, 250),
                Location = new Point(0, y),
                AutoSize = true
            };
            y += 40;

            var lblHint = new Label
            {
                Text = "💡 Совет: погашение досрочно экономит проценты.\n" +
                       "   Кнопка «ПОГАСИТЬ» внизу окна.",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 200, 230),
                Location = new Point(0, y),
                Size = new Size(720, 40)
            };

            pnlContent.Controls.Add(lblHeader);
            pnlContent.Controls.Add(lblInfo);
            pnlContent.Controls.Add(progressBar);
            pnlContent.Controls.Add(lblProgress);
            pnlContent.Controls.Add(lblHint);
        }

        private void PayOffLoan()
        {
            if (_engine.PayOffLoan())
            {
                lblStatus.Text = "🎉 Кредит полностью погашен!";
                RefreshUI();
            }
            else
            {
                lblStatus.Text = "❌ Недостаточно денег для погашения.";
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //   МАГАЗИН АКТИВОВ
    // ═══════════════════════════════════════════════════════════════
    public class ShopDialog : Form
    {
        private readonly GameEngine _engine;
        private FlowLayoutPanel pnlItems;
        private Label lblMoney;
        private Label lblStatus;

        public ShopDialog(GameEngine engine)
        {
            _engine = engine;

            Text = "🏪 Магазин активов";
            Size = new Size(950, 720);
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
                Text = "🏪  Магазин активов",
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 60),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lblMoney = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 150),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0)
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 220, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(25, 0, 0, 0),
                Text = ""
            };

            pnlItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(25, 30, 50)
            };

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(15) };

            var btnClose = new Button
            {
                Text = "Закрыть",
                Width = 150,
                Height = 35,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 80, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.Add(btnClose);

            Controls.Add(pnlItems);
            Controls.Add(pnlBottom);
            Controls.Add(lblStatus);
            Controls.Add(lblMoney);
            Controls.Add(lblTitle);

            RefreshShop();
        }

        private void RefreshShop()
        {
            pnlItems.SuspendLayout();
            pnlItems.Controls.Clear();

            var p = _engine.State.Player;
            lblMoney.Text = $"💰 У тебя: {p.Money:N0}₽     |     📈 Доход: +{p.MonthlyIncome:N0}₽/мес";

            foreach (var asset in AssetCatalog.All)
            {
                bool owned = p.HasAsset(asset.Id);
                bool available = p.Chapter >= asset.MinChapter;
                bool canAfford = p.Money >= asset.Price;

                if (!available && !owned) continue;

                var card = BuildAssetCard(asset, owned, canAfford);
                pnlItems.Controls.Add(card);
            }

            pnlItems.ResumeLayout();
        }

        private Control BuildAssetCard(Asset asset, bool owned, bool canAfford)
        {
            var card = new Panel
            {
                Width = pnlItems.ClientSize.Width - 60,
                Height = 100,
                BackColor = owned
                    ? Color.FromArgb(35, 90, 55)
                    : (canAfford ? Color.FromArgb(35, 55, 100) : Color.FromArgb(45, 45, 55)),
                Margin = new Padding(10, 6, 10, 6)
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var font = new Font("Segoe UI Emoji", 30))
                using (var br = new SolidBrush(Color.White))
                    g.DrawString(asset.Icon, font, br, 15, 25);
            };

            var lblName = new Label
            {
                Text = asset.Name,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(95, 15),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var lblDesc = new Label
            {
                Text = asset.Description,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                Location = new Point(95, 45),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 220, 250),
                BackColor = Color.Transparent
            };

            var lblPrice = new Label
            {
                Text = $"💰 {asset.Price:N0}₽",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(95, 70),
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 220, 100),
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblName);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblPrice);

            var btnBuy = new Button
            {
                Text = owned ? "✅ Куплено" : (canAfford ? "🛒 Купить" : "❌ Не хватает"),
                Width = 180,
                Height = 45,
                Location = new Point(card.Width - 200, 27),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = owned
                    ? Color.FromArgb(40, 120, 70)
                    : (canAfford ? Color.FromArgb(0, 140, 90) : Color.FromArgb(70, 70, 80)),
                Enabled = !owned && canAfford,
                Cursor = (!owned && canAfford) ? Cursors.Hand : Cursors.No
            };
            btnBuy.FlatAppearance.BorderSize = 0;

            var a = asset;
            btnBuy.Click += (s, e) =>
            {
                if (_engine.BuyAsset(a))
                {
                    lblStatus.Text = $"✅ {a.Name} куплен!";
                    RefreshShop();
                }
                else
                {
                    lblStatus.Text = $"❌ Не удалось купить {a.Name}.";
                }
            };

            card.Controls.Add(btnBuy);
            card.Resize += (s, e) => { btnBuy.Left = card.Width - 200; };

            return card;
        }
    }
}