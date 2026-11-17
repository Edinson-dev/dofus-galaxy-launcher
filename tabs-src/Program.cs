using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DofusGalaxyTabs
{
    internal static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_CHILD = 0x40000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_BORDER = 0x00800000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_CAPTION = 0x00C00000;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        public const int WM_HOTKEY = 0x0312;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int HT_CAPTION = 0x2;

        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        public const int HSHELL_FLASH = 0x8006;
        public const int HSHELL_REDRAW = 6;
        public const int HSHELL_RUDEAPPACTIVATED = 0x8004;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool DeregisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }

    public class DofusAccountTab
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public bool IsCustomName { get; set; }
        public IntPtr Hwnd { get; set; }
        public int OriginalStyle { get; set; }
        public int ProcessId { get; set; }
        public Button TabButton { get; set; }
        public Panel HostPanel { get; set; }
        public bool IsMyTurn { get; set; }
    }

    public class MainForm : Form
    {
        private const int HOTKEY_NEXT = 101;
        private const int HOTKEY_PREV = 102;
        private int shellHookMessage = 0;
        private readonly string hotkeyConfigFile;

        // Paleta Dofus Galaxy (Piedra, Pergamino y Oro Cósmico)
        private readonly Color colStoneDarkest = Color.FromArgb(22, 15, 12);
        private readonly Color colStoneDark = Color.FromArgb(36, 26, 20);
        private readonly Color colStoneMid = Color.FromArgb(64, 48, 38);
        private readonly Color colStoneLight = Color.FromArgb(100, 80, 68);
        private readonly Color colStoneHighlight = Color.FromArgb(135, 115, 100);
        private readonly Color colParchment = Color.FromArgb(244, 235, 208);
        private readonly Color colParchmentDark = Color.FromArgb(216, 200, 158);
        private readonly Color colGoldBright = Color.FromArgb(241, 196, 15);
        private readonly Color colGoldOrange = Color.FromArgb(230, 126, 34);
        private readonly Color colGoldDark = Color.FromArgb(184, 134, 11);
        private readonly Color colEmeraldGreen = Color.FromArgb(46, 204, 113);

        private Panel titleBar;
        private Panel tabContainer;
        private FlowLayoutPanel tabsFlow;
        private Panel contentArea;
        private Panel emptyPanel;
        private Button btnAutoTurn;
        private Button btnHotkeyConfig;

        private System.Windows.Forms.Timer autoDetectTimer;
        private readonly List<DofusAccountTab> accounts = new List<DofusAccountTab>();
        private DofusAccountTab activeAccount = null;

        private bool autoTurnEnabled = true;
        private string nextKeyName = "F1";
        private string prevKeyName = "F2";
        private Image appIconImage = null;

        public MainForm()
        {
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);

            hotkeyConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DofusGalaxyTabs", "hotkeys.cfg");
            LoadHotkeyConfig();

            InitializeUi();
            RegisterAllHotkeys();

            // Hook para capturar eventos de turno en Dofus (FlashWindow)
            try
            {
                shellHookMessage = NativeMethods.RegisterWindowMessage("SHELLHOOK");
                NativeMethods.RegisterShellHookWindow(this.Handle);
            }
            catch { }

            autoDetectTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            autoDetectTimer.Tick += (s, e) => ScanAndAutoAttachDofusWindows();
            autoDetectTimer.Start();

            this.Shown += (s, e) => ScanAndAutoAttachDofusWindows();
        }

        private void InitializeUi()
        {
            this.Text = "Dofus Galaxy — WinTabs";
            this.Size = new Size(1300, 850);
            this.MinimumSize = new Size(850, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = colStoneDarkest;
            this.Padding = new Padding(6);

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    this.Icon = new Icon(iconPath);
                    appIconImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png"));
                }
                catch { }
            }

            // ==========================================
            // 1. BARRA DE TÍTULO MEDIEVAL (32px)
            // ==========================================
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = colStoneDark,
                Padding = new Padding(8, 2, 4, 2)
            };
            titleBar.Paint += PaintTitleBarBorder;
            titleBar.MouseDown += DragWindowMouseDown;

            if (appIconImage != null)
            {
                PictureBox pbIcon = new PictureBox
                {
                    Image = appIconImage,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(20, 20),
                    Location = new Point(8, 6),
                    BackColor = Color.Transparent
                };
                pbIcon.MouseDown += DragWindowMouseDown;
                titleBar.Controls.Add(pbIcon);
            }

            Label lblTitle = new Label
            {
                Text = "⚔ DOFUS GALAXY — WINTABS MULTICUENTA ⚔",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = colGoldBright,
                AutoSize = true,
                Location = new Point(appIconImage != null ? 34 : 10, 7),
                BackColor = Color.Transparent
            };
            lblTitle.MouseDown += DragWindowMouseDown;
            titleBar.Controls.Add(lblTitle);

            Button btnClose = CreateTitleBarBtn("✕", Color.FromArgb(160, 40, 40));
            btnClose.Click += (s, e) => this.Close();

            Button btnMax = CreateTitleBarBtn("□", colStoneMid);
            btnMax.Click += (s, e) => ToggleMaximize();

            Button btnMin = CreateTitleBarBtn("─", colStoneMid);
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            Panel pnlWindowControls = new Panel
            {
                Dock = DockStyle.Right,
                Width = 105,
                BackColor = Color.Transparent
            };
            pnlWindowControls.Controls.Add(btnMin);
            pnlWindowControls.Controls.Add(btnMax);
            pnlWindowControls.Controls.Add(btnClose);
            titleBar.Controls.Add(pnlWindowControls);

            this.Controls.Add(titleBar);

            // ==========================================
            // 2. BARRA DE PESTAÑAS DOFUS RETRO (38px)
            // ==========================================
            tabContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(26, 18, 14),
                Padding = new Padding(4, 3, 4, 3)
            };
            tabContainer.Paint += PaintTabContainerBorder;

            tabsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                WrapContents = false,
                AutoScroll = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tabContainer.Controls.Add(tabsFlow);

            // Panel derecho de controles (Auto-Turno, Teclas Siguiente/Atrás, Desacoplar)
            Panel tabControlsRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = Color.Transparent
            };

            btnAutoTurn = new Button
            {
                Text = "⚡ Auto-Turno: ON",
                Size = new Size(120, 30),
                Location = new Point(2, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 80, 50),
                ForeColor = colEmeraldGreen,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAutoTurn.FlatAppearance.BorderColor = colEmeraldGreen;
            btnAutoTurn.FlatAppearance.BorderSize = 1;
            btnAutoTurn.Click += (s, e) => ToggleAutoTurn();
            tabControlsRight.Controls.Add(btnAutoTurn);

            // Botón Configurar Teclas (Siguiente y Atrás)
            btnHotkeyConfig = new Button
            {
                Text = string.Format("⌨ [{0} ➔] | [⬅ {1}] ⚙", nextKeyName, prevKeyName),
                Size = new Size(190, 30),
                Location = new Point(126, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = colStoneDark,
                ForeColor = colGoldBright,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHotkeyConfig.FlatAppearance.BorderColor = colStoneMid;
            btnHotkeyConfig.FlatAppearance.BorderSize = 1;
            btnHotkeyConfig.Click += (s, e) => ShowHotkeyConfigDialog();
            tabControlsRight.Controls.Add(btnHotkeyConfig);

            Button btnDetach = new Button
            {
                Text = "↩",
                Size = new Size(34, 30),
                Location = new Point(320, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = colStoneDark,
                ForeColor = colParchmentDark,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDetach.FlatAppearance.BorderColor = colStoneMid;
            btnDetach.FlatAppearance.BorderSize = 1;
            btnDetach.Click += (s, e) => DetachAllAccounts();
            tabControlsRight.Controls.Add(btnDetach);

            tabContainer.Controls.Add(tabControlsRight);
            this.Controls.Add(tabContainer);

            // ==========================================
            // 3. ÁREA DE JUEGO (100% de la pantalla)
            // ==========================================
            contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            contentArea.Resize += (s, e) => ResizeActiveWindow();
            this.Controls.Add(contentArea);

            CreateEmptyStateView();

            this.FormClosing += (s, e) =>
            {
                autoDetectTimer.Stop();
                try { NativeMethods.DeregisterShellHookWindow(this.Handle); } catch { }
                UnregisterGlobalHotkeys();
                DetachAllAccounts();
            };
        }

        private void CreateEmptyStateView()
        {
            emptyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 9, 20)
            };
            emptyPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                using (LinearGradientBrush bgBrush = new LinearGradientBrush(emptyPanel.ClientRectangle,
                       Color.FromArgb(20, 10, 30), Color.FromArgb(8, 4, 15), 90f))
                {
                    g.FillRectangle(bgBrush, emptyPanel.ClientRectangle);
                }

                int boxW = 560;
                int boxH = 270;
                int boxX = (emptyPanel.Width - boxW) / 2;
                int boxY = (emptyPanel.Height - boxH) / 2;
                Rectangle boxRect = new Rectangle(boxX, boxY, boxW, boxH);

                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                {
                    g.FillRectangle(shadow, new Rectangle(boxX + 5, boxY + 5, boxW, boxH));
                }

                using (LinearGradientBrush parchBrush = new LinearGradientBrush(boxRect,
                       Color.FromArgb(245, 238, 215), Color.FromArgb(215, 198, 160), 90f))
                {
                    g.FillRectangle(parchBrush, boxRect);
                }

                using (Pen borderPen = new Pen(colStoneMid, 3))
                {
                    g.DrawRectangle(borderPen, boxRect);
                }
                using (Pen innerPen = new Pen(colGoldDark, 1))
                {
                    g.DrawRectangle(innerPen, new Rectangle(boxX + 4, boxY + 4, boxW - 8, boxH - 8));
                }

                using (Font fTitle = new Font("Segoe UI", 15, FontStyle.Bold))
                using (SolidBrush bTitle = new SolidBrush(Color.FromArgb(40, 25, 15)))
                {
                    string txtTitle = "🌌 Esperando ventanas de Dofus...";
                    SizeF sz = g.MeasureString(txtTitle, fTitle);
                    g.DrawString(txtTitle, fTitle, bTitle, boxX + (boxW - sz.Width) / 2, boxY + 25);
                }

                using (Font fBody = new Font("Segoe UI", 10, FontStyle.Regular))
                using (SolidBrush bBody = new SolidBrush(Color.FromArgb(70, 50, 40)))
                {
                    string desc = "Abre tus cuentas de Dofus normalmente desde tu cliente o launcher.\n\n" +
                                  "• WinTabs las unificará aquí automáticamente en pestañas.\n" +
                                  string.Format("• Atajos configurados: [{0}] Siguiente Cuenta | [{1}] Cuenta Anterior.\n", nextKeyName, prevKeyName) +
                                  "• Al entrar en combate, cambiará automáticamente a la cuenta en turno.";
                    g.DrawString(desc, fBody, bBody, new RectangleF(boxX + 35, boxY + 75, boxW - 70, 175));
                }
            };

            contentArea.Controls.Add(emptyPanel);
        }

        private Button CreateTitleBarBtn(string text, Color hoverColor)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Right,
                Width = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = colStoneDark,
                ForeColor = colParchment,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            return btn;
        }

        private void PaintTitleBarBorder(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(colStoneLight, 1))
            {
                e.Graphics.DrawLine(p, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            }
        }

        private void PaintTabContainerBorder(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(colStoneLight, 1))
            {
                e.Graphics.DrawLine(p, 0, tabContainer.Height - 1, tabContainer.Width, tabContainer.Height - 1);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            Rectangle rect = this.ClientRectangle;

            using (Pen p = new Pen(colStoneDarkest, 2))
                g.DrawRectangle(p, 0, 0, rect.Width - 1, rect.Height - 1);

            using (Pen p = new Pen(colStoneMid, 2))
                g.DrawRectangle(p, 2, 2, rect.Width - 5, rect.Height - 5);

            using (Pen p = new Pen(colStoneLight, 1))
                g.DrawRectangle(p, 4, 4, rect.Width - 9, rect.Height - 9);

            DrawRivet(g, 8, 8);
            DrawRivet(g, rect.Width - 16, 8);
            DrawRivet(g, 8, rect.Height - 16);
            DrawRivet(g, rect.Width - 16, rect.Height - 16);
        }

        private void DrawRivet(Graphics g, int x, int y)
        {
            using (SolidBrush bShadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                g.FillEllipse(bShadow, x + 1, y + 1, 8, 8);
            using (SolidBrush bRivet = new SolidBrush(colStoneHighlight))
                g.FillEllipse(bRivet, x, y, 7, 7);
            using (SolidBrush bCenter = new SolidBrush(colGoldDark))
                g.FillEllipse(bCenter, x + 2, y + 2, 3, 3);
        }

        // ==========================================
        // CONFIGURACIÓN DE TECLAS (SIGUIENTE Y ATRÁS)
        // ==========================================
        private void ShowHotkeyConfigDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Configurar Teclas";
                dlg.Size = new Size(440, 260);
                dlg.FormBorderStyle = FormBorderStyle.None;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = colStoneDarkest;
                dlg.Padding = new Padding(3);

                dlg.Paint += (s, e) =>
                {
                    using (Pen p = new Pen(colGoldDark, 2))
                        e.Graphics.DrawRectangle(p, 1, 1, dlg.Width - 3, dlg.Height - 3);
                };

                // Título
                Label lblHeader = new Label
                {
                    Text = "⌨ CONFIGURAR TECLAS DE TURNO",
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = colGoldBright,
                    Location = new Point(20, 18),
                    AutoSize = true
                };
                dlg.Controls.Add(lblHeader);

                string[] keyOptions = new string[] {
                    "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8",
                    "Tab", "Ctrl+Tab", "Shift+Tab", "Espacio", "Bloq Mayús",
                    "Q", "E", "D", "A", "Flecha Derecha", "Flecha Izquierda"
                };

                // Tecla Siguiente
                Label lblNext = new Label
                {
                    Text = "Tecla: Siguiente Cuenta (Adelante):",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = colParchment,
                    Location = new Point(25, 60),
                    AutoSize = true
                };
                dlg.Controls.Add(lblNext);

                ComboBox comboNext = new ComboBox
                {
                    Location = new Point(265, 57),
                    Width = 145,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = colStoneDark,
                    ForeColor = colGoldBright,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                comboNext.Items.AddRange(keyOptions);
                comboNext.SelectedItem = nextKeyName;
                if (comboNext.SelectedIndex < 0) comboNext.SelectedIndex = 0;
                dlg.Controls.Add(comboNext);

                // Tecla Anterior
                Label lblPrev = new Label
                {
                    Text = "Tecla: Cuenta Anterior (Atrás):",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = colParchment,
                    Location = new Point(25, 105),
                    AutoSize = true
                };
                dlg.Controls.Add(lblPrev);

                ComboBox comboPrev = new ComboBox
                {
                    Location = new Point(265, 102),
                    Width = 145,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = colStoneDark,
                    ForeColor = colGoldBright,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                comboPrev.Items.AddRange(keyOptions);
                comboPrev.SelectedItem = prevKeyName;
                if (comboPrev.SelectedIndex < 0) comboPrev.SelectedIndex = 1;
                dlg.Controls.Add(comboPrev);

                // Botones
                Button btnSave = new Button
                {
                    Text = "Guardar Atajos",
                    Location = new Point(190, 175),
                    Size = new Size(125, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = colGoldOrange,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.OK
                };
                btnSave.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnSave);

                Button btnCancel = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(325, 175),
                    Size = new Size(85, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = colStoneMid,
                    ForeColor = colParchment,
                    Font = new Font("Segoe UI", 9),
                    Cursor = Cursors.Hand,
                    DialogResult = DialogResult.Cancel
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(btnCancel);

                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    nextKeyName = comboNext.SelectedItem.ToString();
                    prevKeyName = comboPrev.SelectedItem.ToString();

                    SaveHotkeyConfig();
                    RegisterAllHotkeys();

                    btnHotkeyConfig.Text = string.Format("⌨ [{0} ➔] | [⬅ {1}] ⚙", nextKeyName, prevKeyName);
                    emptyPanel.Invalidate();
                }
            }
        }

        private void RegisterAllHotkeys()
        {
            UnregisterGlobalHotkeys();

            RegisterKeyHelper(HOTKEY_NEXT, nextKeyName);
            RegisterKeyHelper(HOTKEY_PREV, prevKeyName);

            // Ctrl + 1..8
            for (int i = 0; i < 8; i++)
            {
                NativeMethods.RegisterHotKey(this.Handle, 2000 + i, NativeMethods.MOD_CONTROL, (uint)(Keys.D1 + i));
            }
        }

        private void RegisterKeyHelper(int id, string keyStr)
        {
            uint mod = NativeMethods.MOD_NONE;
            Keys k = Keys.None;

            if (keyStr == "Ctrl+Tab")
            {
                mod = NativeMethods.MOD_CONTROL;
                k = Keys.Tab;
            }
            else if (keyStr == "Shift+Tab")
            {
                mod = NativeMethods.MOD_SHIFT;
                k = Keys.Tab;
            }
            else if (keyStr == "Espacio")
            {
                k = Keys.Space;
            }
            else if (keyStr == "Bloq Mayús")
            {
                k = Keys.Capital;
            }
            else if (keyStr == "Flecha Derecha")
            {
                k = Keys.Right;
            }
            else if (keyStr == "Flecha Izquierda")
            {
                k = Keys.Left;
            }
            else
            {
                try { k = (Keys)Enum.Parse(typeof(Keys), keyStr, true); }
                catch { k = (id == HOTKEY_NEXT ? Keys.F1 : Keys.F2); }
            }

            if (k != Keys.None)
            {
                NativeMethods.RegisterHotKey(this.Handle, id, mod, (uint)k);
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_NEXT);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_PREV);
            for (int i = 0; i < 8; i++)
                NativeMethods.UnregisterHotKey(this.Handle, 2000 + i);
        }

        private void LoadHotkeyConfig()
        {
            try
            {
                if (File.Exists(hotkeyConfigFile))
                {
                    string[] lines = File.ReadAllLines(hotkeyConfigFile);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Next=")) nextKeyName = line.Substring(5).Trim();
                        if (line.StartsWith("Prev=")) prevKeyName = line.Substring(5).Trim();
                    }
                }
            }
            catch { }
        }

        private void SaveHotkeyConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(hotkeyConfigFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(hotkeyConfigFile, new string[] {
                    "Next=" + nextKeyName,
                    "Prev=" + prevKeyName
                });
            }
            catch { }
        }

        // ==========================================
        // AUTO-ATTACH & PESTAÑAS
        // ==========================================
        private void ScanAndAutoAttachDofusWindows()
        {
            for (int i = accounts.Count - 1; i >= 0; i--)
            {
                var acc = accounts[i];
                if (!NativeMethods.IsWindow(acc.Hwnd))
                {
                    RemoveTab(acc);
                }
                else if (!acc.IsCustomName)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                    NativeMethods.GetWindowText(acc.Hwnd, sb, 256);
                    string currentTitle = sb.ToString().Trim();

                    string cleaned = CleanDofusTitle(currentTitle);
                    if (!string.IsNullOrEmpty(cleaned) && cleaned != acc.Title)
                    {
                        acc.Title = cleaned;
                        acc.TabButton.Text = string.Format("[{0}] {1}", acc.Index + 1, cleaned);
                    }
                }
            }

            IntPtr myHwnd = this.Handle;
            NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd) || hwnd == myHwnd)
                    return true;

                uint procId;
                NativeMethods.GetWindowThreadProcessId(hwnd, out procId);

                string pName = "";
                try
                {
                    Process p = Process.GetProcessById((int)procId);
                    pName = p.ProcessName.ToLower();
                }
                catch { return true; }

                if (pName.Contains("dofus"))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                    NativeMethods.GetWindowText(hwnd, sb, 256);
                    string title = sb.ToString().Trim();

                    if (!string.IsNullOrEmpty(title) && !title.Contains("Launcher") && !title.Contains("WinTabs") && !IsWindowAlreadyCaptured(hwnd))
                    {
                        AttachDofusWindow(hwnd, (int)procId, title);
                    }
                }

                return true;
            }, IntPtr.Zero);

            emptyPanel.Visible = (accounts.Count == 0);
        }

        private string CleanDofusTitle(string rawTitle)
        {
            if (string.IsNullOrEmpty(rawTitle)) return "Dofus";

            string t = rawTitle;
            t = t.Replace("Dofus Retro v1.39.8", "");
            t = t.Replace("Dofus Retro", "");
            t = t.Replace("Dofus", "");
            t = t.Trim(' ', '-', '—', ':', '|', '[', ']');

            if (string.IsNullOrEmpty(t)) return "Dofus Retro";
            if (t.Length > 16) t = t.Substring(0, 16);
            return t;
        }

        private bool IsWindowAlreadyCaptured(IntPtr hwnd)
        {
            foreach (var acc in accounts)
            {
                if (acc.Hwnd == hwnd) return true;
            }
            return false;
        }

        private void AttachDofusWindow(IntPtr hwnd, int processId, string windowTitle)
        {
            int originalStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);

            int style = originalStyle;
            style &= ~NativeMethods.WS_CAPTION;
            style &= ~NativeMethods.WS_THICKFRAME;
            style &= ~NativeMethods.WS_BORDER;
            style |= NativeMethods.WS_CHILD;
            style |= NativeMethods.WS_CLIPCHILDREN;
            style |= NativeMethods.WS_CLIPSIBLINGS;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);

            Panel hostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Visible = false
            };
            contentArea.Controls.Add(hostPanel);

            NativeMethods.SetParent(hwnd, hostPanel.Handle);

            string shortTitle = CleanDofusTitle(windowTitle);

            DofusAccountTab account = new DofusAccountTab
            {
                Index = accounts.Count,
                Title = shortTitle,
                IsCustomName = false,
                Hwnd = hwnd,
                OriginalStyle = originalStyle,
                ProcessId = processId,
                HostPanel = hostPanel,
                IsMyTurn = false
            };

            Button tabBtn = CreateTabButton(account);

            account.TabButton = tabBtn;
            accounts.Add(account);
            tabsFlow.Controls.Add(tabBtn);

            emptyPanel.Visible = false;

            if (activeAccount == null)
            {
                SelectAccount(account);
            }
        }

        private Button CreateTabButton(DofusAccountTab account)
        {
            Button tabBtn = new Button
            {
                Text = string.Format("[{0}] {1}", account.Index + 1, account.Title),
                Size = new Size(140, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 0, 2, 0),
                BackColor = colStoneDark,
                ForeColor = colParchment
            };
            tabBtn.FlatAppearance.BorderSize = 0;

            tabBtn.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = tabBtn.ClientRectangle;
                bool isActive = (activeAccount == account);

                Color topCol = isActive ? Color.FromArgb(243, 156, 18) : colStoneMid;
                Color btmCol = isActive ? Color.FromArgb(211, 84, 0) : colStoneDark;

                if (account.IsMyTurn && !isActive)
                {
                    topCol = Color.FromArgb(46, 204, 113);
                    btmCol = Color.FromArgb(39, 174, 96);
                }

                using (LinearGradientBrush lgb = new LinearGradientBrush(r, topCol, btmCol, 90f))
                {
                    g.FillRectangle(lgb, r);
                }

                Color bdrCol = isActive ? Color.FromArgb(255, 230, 100) : (account.IsMyTurn ? Color.FromArgb(100, 255, 150) : colStoneLight);
                using (Pen p = new Pen(bdrCol, isActive ? 2 : 1))
                {
                    g.DrawRectangle(p, 0, 0, r.Width - 1, r.Height - 1);
                }

                string txt = tabBtn.Text;
                if (account.IsMyTurn && !isActive) txt = "⚡ " + txt;

                using (Font f = new Font("Segoe UI", 9, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(txt, f);
                    float txtX = (r.Width - sz.Width) / 2;
                    float txtY = (r.Height - sz.Height) / 2;

                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                        g.DrawString(txt, f, shadow, txtX + 1, txtY + 1);

                    Color textCol = isActive ? Color.White : (account.IsMyTurn ? Color.White : colParchment);
                    using (SolidBrush brush = new SolidBrush(textCol))
                        g.DrawString(txt, f, brush, txtX, txtY);
                }
            };

            tabBtn.Click += (s, e) => SelectAccount(account);
            tabBtn.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    ShowTabContextMenu(account, tabBtn);
            };

            return tabBtn;
        }

        public void SelectAccount(DofusAccountTab acc)
        {
            if (acc == null) return;

            if (activeAccount != null && activeAccount != acc)
            {
                activeAccount.HostPanel.Visible = false;
                NativeMethods.ShowWindow(activeAccount.Hwnd, NativeMethods.SW_HIDE);
                activeAccount.TabButton.Invalidate();
            }

            activeAccount = acc;
            acc.IsMyTurn = false;

            acc.HostPanel.Visible = true;
            acc.HostPanel.BringToFront();

            NativeMethods.ShowWindow(acc.Hwnd, NativeMethods.SW_SHOW);
            NativeMethods.MoveWindow(acc.Hwnd, 0, 0, contentArea.Width, contentArea.Height, true);
            NativeMethods.SetForegroundWindow(acc.Hwnd);
            NativeMethods.SetFocus(acc.Hwnd);

            acc.TabButton.Invalidate();
        }

        public void NextAccount()
        {
            if (accounts.Count <= 1 || activeAccount == null) return;
            int nextIdx = (accounts.IndexOf(activeAccount) + 1) % accounts.Count;
            SelectAccount(accounts[nextIdx]);
        }

        public void PreviousAccount()
        {
            if (accounts.Count <= 1 || activeAccount == null) return;
            int prevIdx = accounts.IndexOf(activeAccount) - 1;
            if (prevIdx < 0) prevIdx = accounts.Count - 1;
            SelectAccount(accounts[prevIdx]);
        }

        public void SelectAccountByIndex(int index)
        {
            if (index >= 0 && index < accounts.Count)
                SelectAccount(accounts[index]);
        }

        // AUTO-TURNO (FlashWindow / ShellHook)
        private void OnDofusTurnNotification(IntPtr flashingHwnd)
        {
            DofusAccountTab target = null;
            foreach (var acc in accounts)
            {
                if (acc.Hwnd == flashingHwnd)
                {
                    target = acc;
                    break;
                }
            }

            if (target != null && target != activeAccount)
            {
                target.IsMyTurn = true;
                target.TabButton.Invalidate();

                if (autoTurnEnabled)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        SelectAccount(target);
                    }));
                }
            }
        }

        private void ToggleAutoTurn()
        {
            autoTurnEnabled = !autoTurnEnabled;
            if (autoTurnEnabled)
            {
                btnAutoTurn.Text = "⚡ Auto-Turno: ON";
                btnAutoTurn.BackColor = Color.FromArgb(30, 80, 50);
                btnAutoTurn.ForeColor = colEmeraldGreen;
                btnAutoTurn.FlatAppearance.BorderColor = colEmeraldGreen;
            }
            else
            {
                btnAutoTurn.Text = "⚡ Auto-Turno: OFF";
                btnAutoTurn.BackColor = colStoneDark;
                btnAutoTurn.ForeColor = colParchmentDark;
                btnAutoTurn.FlatAppearance.BorderColor = colStoneMid;
            }
        }

        private void ResizeActiveWindow()
        {
            if (activeAccount != null && NativeMethods.IsWindow(activeAccount.Hwnd))
            {
                NativeMethods.MoveWindow(activeAccount.Hwnd, 0, 0, contentArea.Width, contentArea.Height, true);
            }
        }

        private void ShowTabContextMenu(DofusAccountTab acc, Control source)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("✏ Renombrar Personaje o Clase", null, (s, e) => RenameTab(acc));
            menu.Items.Add("🔝 Mover a la Izquierda", null, (s, e) => MoveTab(acc, -1));
            menu.Items.Add("🔜 Mover a la Derecha", null, (s, e) => MoveTab(acc, 1));
            menu.Items.Add("↩ Desacoplar a Ventana Normal", null, (s, e) => DetachSingleAccount(acc));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("✕ Cerrar este Cliente", null, (s, e) => CloseAccount(acc));

            menu.Show(source, new Point(0, source.Height));
        }

        private void RenameTab(DofusAccountTab acc)
        {
            string newName = PromptInput("Nombre del Personaje o Clase (ej. Yopuka):", "Renombrar Pestaña", acc.Title);
            if (!string.IsNullOrEmpty(newName))
            {
                acc.Title = newName;
                acc.IsCustomName = true;
                acc.TabButton.Text = string.Format("[{0}] {1}", acc.Index + 1, acc.Title);
                acc.TabButton.Invalidate();
            }
        }

        private void MoveTab(DofusAccountTab acc, int direction)
        {
            int oldIdx = accounts.IndexOf(acc);
            int newIdx = oldIdx + direction;
            if (newIdx >= 0 && newIdx < accounts.Count)
            {
                accounts.RemoveAt(oldIdx);
                accounts.Insert(newIdx, acc);
                RefreshTabButtons();
            }
        }

        private void RefreshTabButtons()
        {
            tabsFlow.Controls.Clear();
            for (int i = 0; i < accounts.Count; i++)
            {
                accounts[i].Index = i;
                accounts[i].TabButton.Text = string.Format("[{0}] {1}", i + 1, accounts[i].Title);
                tabsFlow.Controls.Add(accounts[i].TabButton);
                accounts[i].TabButton.Invalidate();
            }
        }

        private void DetachSingleAccount(DofusAccountTab acc)
        {
            if (NativeMethods.IsWindow(acc.Hwnd))
            {
                NativeMethods.SetParent(acc.Hwnd, IntPtr.Zero);
                NativeMethods.SetWindowLong(acc.Hwnd, NativeMethods.GWL_STYLE, acc.OriginalStyle != 0 ? acc.OriginalStyle : NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME | NativeMethods.WS_VISIBLE);
                NativeMethods.ShowWindow(acc.Hwnd, NativeMethods.SW_SHOW);
                NativeMethods.SetForegroundWindow(acc.Hwnd);
            }

            RemoveTab(acc);
        }

        private void DetachAllAccounts()
        {
            while (accounts.Count > 0)
                DetachSingleAccount(accounts[0]);
            emptyPanel.Visible = true;
        }

        private void CloseAccount(DofusAccountTab acc)
        {
            try
            {
                Process p = Process.GetProcessById(acc.ProcessId);
                if (!p.HasExited) p.Kill();
            }
            catch { }
            RemoveTab(acc);
        }

        private void RemoveTab(DofusAccountTab acc)
        {
            contentArea.Controls.Remove(acc.HostPanel);
            tabsFlow.Controls.Remove(acc.TabButton);
            accounts.Remove(acc);

            RefreshTabButtons();
            if (activeAccount == acc)
            {
                activeAccount = null;
                if (accounts.Count > 0) SelectAccount(accounts[0]);
            }

            emptyPanel.Visible = (accounts.Count == 0);
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void DragWindowMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_NCHITTEST)
            {
                base.WndProc(ref m);
                Point pt = this.PointToClient(new Point(m.LParam.ToInt32()));
                int border = 8;

                if (pt.X <= border && pt.Y <= border) m.Result = (IntPtr)NativeMethods.HTTOPLEFT;
                else if (pt.X >= this.ClientSize.Width - border && pt.Y <= border) m.Result = (IntPtr)NativeMethods.HTTOPRIGHT;
                else if (pt.X <= border && pt.Y >= this.ClientSize.Height - border) m.Result = (IntPtr)NativeMethods.HTBOTTOMLEFT;
                else if (pt.X >= this.ClientSize.Width - border && pt.Y >= this.ClientSize.Height - border) m.Result = (IntPtr)NativeMethods.HTBOTTOMRIGHT;
                else if (pt.X <= border) m.Result = (IntPtr)NativeMethods.HTLEFT;
                else if (pt.X >= this.ClientSize.Width - border) m.Result = (IntPtr)NativeMethods.HTRIGHT;
                else if (pt.Y <= border) m.Result = (IntPtr)NativeMethods.HTTOP;
                else if (pt.Y >= this.ClientSize.Height - border) m.Result = (IntPtr)NativeMethods.HTBOTTOM;
                return;
            }

            if (shellHookMessage != 0 && m.Msg == shellHookMessage)
            {
                int nCode = m.WParam.ToInt32();
                if (nCode == NativeMethods.HSHELL_FLASH || nCode == 33 || nCode == NativeMethods.HSHELL_REDRAW)
                {
                    IntPtr targetHwnd = m.LParam;
                    OnDofusTurnNotification(targetHwnd);
                }
            }

            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_NEXT)
                {
                    NextAccount();
                }
                else if (id == HOTKEY_PREV)
                {
                    PreviousAccount();
                }
                else if (id >= 2000 && id < 2008)
                {
                    int idx = id - 2000;
                    if (idx < accounts.Count) SelectAccount(accounts[idx]);
                }
            }

            base.WndProc(ref m);
        }

        private string PromptInput(string text, string caption, string defaultValue)
        {
            Form prompt = new Form()
            {
                Width = 380,
                Height = 160,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = colStoneDarkest,
                Padding = new Padding(3)
            };
            prompt.Paint += (s, e) =>
            {
                using (Pen p = new Pen(colGoldDark, 2))
                    e.Graphics.DrawRectangle(p, 1, 1, prompt.Width - 3, prompt.Height - 3);
            };

            Label lbl = new Label() { Left = 20, Top = 18, Text = text, AutoSize = true, ForeColor = colGoldBright, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 330, Text = defaultValue, BackColor = colStoneDark, ForeColor = colParchment, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            Button confirmation = new Button() { Text = "Guardar", Left = 165, Width = 90, Top = 85, DialogResult = DialogResult.OK, BackColor = colGoldOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            confirmation.FlatAppearance.BorderSize = 0;
            Button cancel = new Button() { Text = "Cancelar", Left = 265, Width = 85, Top = 85, DialogResult = DialogResult.Cancel, BackColor = colStoneMid, ForeColor = colParchment, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            cancel.FlatAppearance.BorderSize = 0;

            prompt.Controls.Add(lbl);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text.Trim() : defaultValue;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
