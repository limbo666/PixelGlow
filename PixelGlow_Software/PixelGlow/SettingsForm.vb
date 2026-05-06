Imports System.Windows.Forms
Imports System.Drawing
Imports System.Text.RegularExpressions

Public Class SettingsForm
    Inherits Form

    ' UI Controls
    Private _txtIP As TextBox
    Private _numPort, _numTop, _numBottom, _numLeft, _numRight As NumericUpDown
    Private _numInterval, _numSmooth, _numSat, _numBright As NumericUpDown
    Private _numGapStart, _numGapTop, _numGapRight, _numGapBottom, _numGapLeft As NumericUpDown
    Private _cmbMonitor, _cmbSensitivity, _cmbColorOrder, _cmbGridSize As ComboBox
    Private _chkBlackBar, _chkTestMode, _chkGrid, _chkLogging, _chkControlHw As CheckBox
    Private _chkStartInTray, _chkStartWithWindows As CheckBox
    Private _lblStatus As Label

    ' Virtual Tab Infrastructure
    Private _btnDisp, _btnNet, _btnLeds, _btnEng, _btnGen As Button
    Private _tabDisp, _tabNet, _tabLeds, _tabEng, _tabGen As Panel

    ' NEW: Real-time update event
    Public Event SettingsApplied As EventHandler

    Public Sub New()
        Me.Name = "SettingsForm"
        Me.Text = "PixelGlow Configuration"
        Me.Size = New Size(900, 650)
        Me.Icon = ResourceLoader.GetIcon("ic_PixelGlow1.ico")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        RegistryHelper.LoadWindowBounds(Me)

        ' --- 1. Footer Area ---
        Dim footer As New Panel() With {.Dock = DockStyle.Bottom, .Height = 70, .BackColor = Color.FromArgb(245, 245, 245)}

        Dim btnSave As New Button() With {
            .Text = "OK", .Width = 100, .Height = 40,
            .BackColor = Color.FromArgb(0, 120, 215), .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        Dim btnApply As New Button() With {
            .Text = "Apply", .Width = 100, .Height = 40,
            .BackColor = Color.White, .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnApply.FlatAppearance.BorderColor = Color.LightGray
        btnSave.FlatAppearance.BorderSize = 0

        btnSave.Location = New Point(footer.Width - btnSave.Width - 30, 15)
        btnSave.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnApply.Location = New Point(btnSave.Left - btnApply.Width - 10, 15)
        btnApply.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        AddHandler btnSave.Click, Sub() ValidateAndSave(True)  ' Save & Close
        AddHandler btnApply.Click, Sub() ValidateAndSave(False) ' Save & Keep Open

        _lblStatus = New Label() With {.AutoSize = True, .ForeColor = Color.Firebrick, .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .Location = New Point(230, 25)}

        footer.Controls.Add(_lblStatus)
        footer.Controls.Add(btnApply)
        footer.Controls.Add(btnSave)

        ' --- 2. Sidebar ---
        Dim sidebar As New FlowLayoutPanel() With {
            .Dock = DockStyle.Left, .Width = 220, .BackColor = Color.FromArgb(30, 30, 35),
            .FlowDirection = FlowDirection.TopDown, .Padding = New Padding(0, 30, 0, 0)
        }

        ' --- 3. Main Content Area ---
        Dim contentArea As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}

        Me.Controls.Add(contentArea)
        Me.Controls.Add(sidebar)
        Me.Controls.Add(footer)
        footer.SendToBack()
        sidebar.SendToBack()
        contentArea.BringToFront()

        _tabDisp = CreateTabPanel() : _tabNet = CreateTabPanel() : _tabLeds = CreateTabPanel() : _tabEng = CreateTabPanel() : _tabGen = CreateTabPanel()
        contentArea.Controls.AddRange({_tabDisp, _tabNet, _tabLeds, _tabEng, _tabGen})

        _btnGen = CreateSidebarButton("General", sidebar)
        _btnDisp = CreateSidebarButton("Display", sidebar)
        _btnNet = CreateSidebarButton("Network", sidebar)
        _btnLeds = CreateSidebarButton("Hardware Layout", sidebar)
        _btnEng = CreateSidebarButton("Engine", sidebar)



        ' === TAB 1: Display ===
        AddTabHeader("Display", _tabDisp)
        Dim tblDisp = CreateTable(_tabDisp)
        _cmbMonitor = AddComboBoxRow("Target Display", "Select monitor. Changes apply instantly.", tblDisp)
        PopulateMonitors()

        _cmbGridSize = AddComboBoxRow("Grid Resolution", "How many zones the screen is divided into. Match roughly to your LED counts.", tblDisp)
        _cmbGridSize.Items.AddRange(New String() {"16x9 (Default)", "12x6", "8x3", "4x3"})
        Select Case SettingsManager.Current.GridCols
            Case 16 : _cmbGridSize.SelectedIndex = 0
            Case 12 : _cmbGridSize.SelectedIndex = 1
            Case 8 : _cmbGridSize.SelectedIndex = 2
            Case 4 : _cmbGridSize.SelectedIndex = 3
            Case Else : _cmbGridSize.SelectedIndex = 0
        End Select

        _chkGrid = AddCheckBoxRow("Show Detection Grid", SettingsManager.Current.ShowDetectionGrid, "Displays a transparent overlay showing exactly where the engine is sampling colors.", tblDisp)
        ' === TAB 2: Network ===
        AddTabHeader("Network", _tabNet)
        Dim tblNet = CreateTable(_tabNet)
        _txtIP = AddTextBoxRow("Target IP Address", SettingsManager.Current.TargetIP, "ESP module address. Use '255.255.255.255' to broadcast.", tblNet)
        _numPort = AddNumericRow("UDP Port", 1, 65535, SettingsManager.Current.TargetPort, "Hardware port. Default is 45045.", tblNet)

        ' === TAB 3: Hardware Layout ===
        AddTabHeader("Physical LED Layout", _tabLeds)
        Dim tblLeds = CreateTable(_tabLeds)

        ' Group: Main Config
        AddSectionHeader("Strip Configuration", tblLeds)
        _cmbColorOrder = AddComboBoxRow("Color Sequence", "Matches software to your strip's physical wiring.", tblLeds)
        _cmbColorOrder.Items.AddRange(New String() {"RGB", "GRB", "BRG", "BGR", "RBG", "GBR"})
        _cmbColorOrder.SelectedItem = SettingsManager.Current.ColorSequence
        If _cmbColorOrder.SelectedIndex = -1 Then _cmbColorOrder.SelectedIndex = 0
        _numGapStart = AddNumericRow("Start Offset (Blanks)", 0, 100, SettingsManager.Current.BlankStart, "Hidden LEDs between the controller box and the actual screen start.", tblLeds)

        ' Group: Top
        AddSectionHeader("Top Edge", tblLeds)
        _numTop = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.TopLeds, "LEDs tracking the top screen edge.", tblLeds)
        _numGapTop = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterTop, "Dead LEDs bending around the top-to-right corner.", tblLeds)

        ' Group: Right
        AddSectionHeader("Right Edge", tblLeds)
        _numRight = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.RightLeds, "LEDs tracking the right screen edge.", tblLeds)
        _numGapRight = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterRight, "Dead LEDs bending around the right-to-bottom corner.", tblLeds)

        ' Group: Bottom
        AddSectionHeader("Bottom Edge", tblLeds)
        _numBottom = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.BottomLeds, "LEDs tracking the bottom screen edge.", tblLeds)
        _numGapBottom = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterBottom, "Dead LEDs bending around the bottom-to-left corner.", tblLeds)

        ' Group: Left
        AddSectionHeader("Left Edge", tblLeds)
        _numLeft = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.LeftLeds, "LEDs tracking the left screen edge.", tblLeds)
        _numGapLeft = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterLeft, "Dead LEDs extending past the left edge.", tblLeds)

        ' Group: Diagnostics
        AddSectionHeader("Diagnostics", tblLeds)
        _chkTestMode = AddCheckBoxRow("Alignment Test Mode", SettingsManager.Current.TestMode, "Forces LEDs to Red (Top), Green (Bottom), Blue (Left), Purple (Right).", tblLeds)

        ' === TAB 4: Engine ===
        AddTabHeader("Processing Parameters", _tabEng)
        Dim tblEng = CreateTable(_tabEng)

        _numBright = AddNumericRow("Max Brightness (%)", 1, 100, SettingsManager.Current.MaxBrightness, "Limits the maximum power output of the LEDs.", tblEng)
        _numSat = AddNumericRow("Saturation Boost (%)", 100, 300, SettingsManager.Current.SaturationBoost, "100 = Screen Accurate. 150+ forces vibrant colors and reduces white washout.", tblEng)

        _numInterval = AddNumericRow("Sync Speed (ms)", 10, 1000, SettingsManager.Current.UpdateIntervalMs, "Delay between screen captures. 33ms provides ~30 FPS.", tblEng)
        _numSmooth = AddNumericRow("Temporal Smoothing", 1, 100, SettingsManager.Current.SmoothingSpeed, "1 = Very slow color fade. 100 = Instant flashing.", tblEng)

        _chkBlackBar = AddCheckBoxRow("Auto-Crop Black Bars", SettingsManager.Current.DetectBlackBars, "Automatically crops letterboxing in wide movies.", tblEng)
        _cmbSensitivity = AddComboBoxRow("Crop Sensitivity", "Aggressive bites through streaming compression noise.", tblEng)
        _cmbSensitivity.Items.Add("Standard (Clean Video)")
        _cmbSensitivity.Items.Add("Aggressive (Compressed Streams)")
        _cmbSensitivity.SelectedIndex = If(SettingsManager.Current.BlackBarThreshold <= 40, 0, 1)
        _cmbSensitivity.Enabled = _chkBlackBar.Checked

        AddHandler _chkBlackBar.CheckedChanged, Sub() _cmbSensitivity.Enabled = _chkBlackBar.Checked

        ' --- TAB 5: General ---
        AddTabHeader("General Settings", _tabGen)
        Dim tblGen = CreateTable(_tabGen)

        _chkControlHw = AddCheckBoxRow("Control Hardware", SettingsManager.Current.ControlHardware, "Sends color data over the network. Uncheck to temporarily pause LED lighting.", tblGen)
        _chkStartInTray = AddCheckBoxRow("Start in Tray", SettingsManager.Current.StartInTray, "Launches the application hidden in the system tray instead of showing the mimic screen.", tblGen)
        _chkStartWithWindows = AddCheckBoxRow("Start with Windows", SettingsManager.Current.StartWithWindows, "Automatically launches PixelGlow when you log into your computer.", tblGen)
        _chkLogging = AddCheckBoxRow("Enable Logging", SettingsManager.Current.LoggingEnabled, "Writes background diagnostic information to a log file for troubleshooting.", tblGen)

        ' --- Restore the last selected tab ---
        Select Case SettingsManager.Current.LastSettingsTab
            Case 0 : SidebarClick(_btnGen, EventArgs.Empty)
            Case 1 : SidebarClick(_btnDisp, EventArgs.Empty)
            Case 2 : SidebarClick(_btnNet, EventArgs.Empty)
            Case 3 : SidebarClick(_btnLeds, EventArgs.Empty)
            Case 4 : SidebarClick(_btnEng, EventArgs.Empty)
            Case Else : SidebarClick(_btnGen, EventArgs.Empty)
        End Select
    End Sub


    Private Function CreateTabPanel() As Panel
        Return New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(40, 30, 40, 20), .Visible = False, .AutoScroll = True}
    End Function


    Private Sub AddSectionHeader(title As String, table As TableLayoutPanel)
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ' Text Title
        Dim lbl As New Label() With {
            .Text = title, .Dock = DockStyle.Fill, .AutoSize = True,
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(0, 120, 215), .Padding = New Padding(0, 15, 0, 2)
        }
        table.Controls.Add(lbl, 0, r)
        table.SetColumnSpan(lbl, 2)

        ' Separator Line
        Dim hr As New Label() With {.Height = 1, .BackColor = Color.LightGray, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, 10)}
        table.Controls.Add(hr, 0, r + 1)
        table.SetColumnSpan(hr, 2)
    End Sub

    Private Function CreateSidebarButton(title As String, parent As FlowLayoutPanel) As Button
        Dim btn As New Button() With {
            .Text = title, .Width = 220, .Height = 60, .Margin = New Padding(0),
            .FlatStyle = FlatStyle.Flat, .ForeColor = Color.White, .BackColor = Color.FromArgb(30, 30, 35),
            .Font = New Font("Segoe UI", 10.5F), .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(25, 0, 0, 0), .Cursor = Cursors.Hand
        }
        btn.FlatAppearance.BorderSize = 0
        AddHandler btn.Click, AddressOf SidebarClick
        parent.Controls.Add(btn)
        Return btn
    End Function

    Private Sub SidebarClick(sender As Object, e As EventArgs)
        _tabDisp.Visible = False : _tabNet.Visible = False : _tabLeds.Visible = False : _tabEng.Visible = False : _tabGen.Visible = False
        _btnDisp.BackColor = Color.FromArgb(30, 30, 35) : _btnNet.BackColor = Color.FromArgb(30, 30, 35)
        _btnLeds.BackColor = Color.FromArgb(30, 30, 35) : _btnEng.BackColor = Color.FromArgb(30, 30, 35) : _btnGen.BackColor = Color.FromArgb(30, 30, 35)

        Dim clicked = DirectCast(sender, Button)
        clicked.BackColor = Color.FromArgb(0, 120, 215)

        ' Show the correct tab and update the memory tracker
        If clicked Is _btnGen Then _tabGen.Visible = True : _tabGen.BringToFront() : SettingsManager.Current.LastSettingsTab = 0
        If clicked Is _btnDisp Then _tabDisp.Visible = True : _tabDisp.BringToFront() : SettingsManager.Current.LastSettingsTab = 1
        If clicked Is _btnNet Then _tabNet.Visible = True : _tabNet.BringToFront() : SettingsManager.Current.LastSettingsTab = 2
        If clicked Is _btnLeds Then _tabLeds.Visible = True : _tabLeds.BringToFront() : SettingsManager.Current.LastSettingsTab = 3
        If clicked Is _btnEng Then _tabEng.Visible = True : _tabEng.BringToFront() : SettingsManager.Current.LastSettingsTab = 4
    End Sub

    Private Sub AddTabHeader(title As String, parent As Control)
        Dim lbl As New Label() With {
            .Text = title, .Dock = DockStyle.Top, .Height = 50,
            .Font = New Font("Segoe UI Semilight", 22), .ForeColor = Color.FromArgb(0, 120, 215),
            .TextAlign = ContentAlignment.BottomLeft, .Padding = New Padding(0, 0, 0, 10)
        }
        parent.Controls.Add(lbl)
    End Sub

    Private Function CreateTable(parent As Control) As TableLayoutPanel
        Dim t As New TableLayoutPanel() With {
            .Dock = DockStyle.Top, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 2, .Margin = New Padding(0, 20, 0, 0)
        }
        t.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
        t.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        parent.Controls.Add(t)
        t.BringToFront()
        Return t
    End Function

    Private Function AddComboBoxRow(title As String, desc As String, table As TableLayoutPanel) As ComboBox
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lblTitle As New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}
        Dim cmbInput As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 250, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        Dim lblDesc As New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}
        table.Controls.Add(lblTitle, 0, r) : table.Controls.Add(cmbInput, 1, r) : table.Controls.Add(lblDesc, 1, r + 1)
        Return cmbInput
    End Function

    Private Function AddTextBoxRow(title As String, val As String, desc As String, table As TableLayoutPanel) As TextBox
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lblTitle As New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}
        Dim txtInput As New TextBox() With {.Text = val, .Width = 250, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        Dim lblDesc As New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}
        table.Controls.Add(lblTitle, 0, r) : table.Controls.Add(txtInput, 1, r) : table.Controls.Add(lblDesc, 1, r + 1)
        Return txtInput
    End Function

    Private Function AddNumericRow(title As String, min As Integer, max As Integer, val As Integer, desc As String, table As TableLayoutPanel) As NumericUpDown
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lblTitle As New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}
        Dim numInput As New NumericUpDown() With {.Minimum = min, .Maximum = max, .Value = val, .Width = 100, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        Dim lblDesc As New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}
        table.Controls.Add(lblTitle, 0, r) : table.Controls.Add(numInput, 1, r) : table.Controls.Add(lblDesc, 1, r + 1)
        Return numInput
    End Function

    Private Function AddCheckBoxRow(title As String, chkState As Boolean, desc As String, table As TableLayoutPanel) As CheckBox
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lblTitle As New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}
        Dim chkInput As New CheckBox() With {.Checked = chkState, .AutoSize = True, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        Dim lblDesc As New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}
        table.Controls.Add(lblTitle, 0, r) : table.Controls.Add(chkInput, 1, r) : table.Controls.Add(lblDesc, 1, r + 1)
        Return chkInput
    End Function

    Private Sub PopulateMonitors()
        For i As Integer = 0 To Screen.AllScreens.Length - 1
            Dim s = Screen.AllScreens(i)
            Dim name = $"Display {i + 1} ({s.Bounds.Width}x{s.Bounds.Height})"
            If s.Primary Then name &= " [Primary]"
            _cmbMonitor.Items.Add(New With {.Text = name, .Value = i})
        Next
        _cmbMonitor.DisplayMember = "Text" : _cmbMonitor.ValueMember = "Value"
        _cmbMonitor.SelectedIndex = Math.Max(0, Math.Min(SettingsManager.Current.TargetMonitorIndex, _cmbMonitor.Items.Count - 1))
    End Sub

    ' --- REAL-TIME APPLY LOGIC ---
    Private Sub ValidateAndSave(closeForm As Boolean)
        Dim ipPattern As String = "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"
        If _txtIP.Text <> "255.255.255.255" AndAlso Not Regex.IsMatch(_txtIP.Text, ipPattern) Then
            _lblStatus.Text = "Invalid IP Address format."
            Return
        End If
        _lblStatus.Text = ""

        With SettingsManager.Current
            .TargetMonitorIndex = CInt(Math.Max(0, _cmbMonitor.SelectedIndex))

            Select Case _cmbGridSize.SelectedIndex
                Case 0 : .GridCols = 16 : .GridRows = 9
                Case 1 : .GridCols = 12 : .GridRows = 6
                Case 2 : .GridCols = 8 : .GridRows = 3
                Case 3 : .GridCols = 4 : .GridRows = 3
            End Select

            .TargetIP = _txtIP.Text
            .TargetPort = CInt(_numPort.Value)

            .MaxBrightness = CInt(_numBright.Value)
            .SaturationBoost = CInt(_numSat.Value)

            .UpdateIntervalMs = CInt(_numInterval.Value)

            .SmoothingSpeed = CInt(_numSmooth.Value)
            .DetectBlackBars = _chkBlackBar.Checked
            .BlackBarThreshold = If(_cmbSensitivity.SelectedIndex = 0, 40, 120)

            .ColorSequence = _cmbColorOrder.SelectedItem.ToString()

            .TopLeds = CInt(_numTop.Value)
            .BottomLeds = CInt(_numBottom.Value)
            .LeftLeds = CInt(_numLeft.Value)
            .RightLeds = CInt(_numRight.Value)

            .BlankStart = CInt(_numGapStart.Value)
            .BlankAfterTop = CInt(_numGapTop.Value)
            .BlankAfterRight = CInt(_numGapRight.Value)
            .BlankAfterBottom = CInt(_numGapBottom.Value)
            .BlankAfterLeft = CInt(_numGapLeft.Value)

            .ShowDetectionGrid = _chkGrid.Checked
            .ControlHardware = _chkControlHw.Checked
            .StartInTray = _chkStartInTray.Checked
            .StartWithWindows = _chkStartWithWindows.Checked
            .LoggingEnabled = _chkLogging.Checked
        End With

        ' Apply Windows Startup change immediately
        RegistryHelper.SetStartup(SettingsManager.Current.StartWithWindows)

        SettingsManager.Save()
        RegistryHelper.SaveWindowBounds(Me)

        ' Tell the main application to immediately reload the engine and grid
        RaiseEvent SettingsApplied(Me, EventArgs.Empty)

        If closeForm Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        RegistryHelper.SaveWindowBounds(Me)

        ' FAILSAFE: Only revert TestMode if the user closed the window WITHOUT clicking OK or Apply.
        ' We do NOT touch ShowDetectionGrid here; that is now a permanent setting.
        If Me.DialogResult <> DialogResult.OK Then
            If SettingsManager.Current.TestMode Then
                SettingsManager.Current.TestMode = False
                SettingsManager.Save()
                RaiseEvent SettingsApplied(Me, EventArgs.Empty)
            End If
        End If

        MyBase.OnFormClosing(e)
    End Sub
End Class