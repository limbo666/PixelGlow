Imports System.Windows.Forms
Imports System.Drawing
Imports System.Text.RegularExpressions

Public Class SettingsForm
    Inherits Form

    ' UI Controls
    Private _txtIP As TextBox
    Private _numPort, _numTop, _numBottom, _numLeft, _numRight As NumericUpDown
    Private _numInterval, _numSmooth, _numSat, _numBright, _numCrop As NumericUpDown
    Private _numGapStart, _numGapTop, _numGapRight, _numGapBottom, _numGapLeft As NumericUpDown
    Private _cmbMonitor, _cmbSensitivity, _cmbColorOrder, _cmbGridSize As ComboBox
    Private _cmbStartEdge, _cmbDirection As ComboBox
    Private _cmbProtocol, _cmbLayoutMode As ComboBox
    Private _numLinearZones, _numThickness As NumericUpDown
    Private _chkBlackBar, _chkTestMode, _chkGrid, _chkLogging, _chkControlHw As CheckBox
    Private _chkDiagSegments, _chkDiagGaps, _chkDiagSweep, _chkDiagBullet As CheckBox
    Private _chkStartInTray, _chkStartWithWindows As CheckBox
    Private _chkFollowPower, _chkDimOnPower, _chkDimBreathing As CheckBox
    Private _cmbDimColor As ComboBox
    Private _lblStatus As Label

    ' Virtual Tab Infrastructure
    Private _btnDisp, _btnHw, _btnEng, _btnGen, _btnDiag, _btnProf As Button
    Private _tabDisp, _tabHw, _tabEng, _tabGen, _tabDiag, _tabProf As Panel

    ' Preset UI Variables
    Private _cmbPresetSelect As ComboBox
    Private _txtPresetName As TextBox
    Private _isLoadingPreset As Boolean = False
    Private _lastPresetIndex As Integer = -1

    ' Hardware Tracking
    Private _hwIsDirty As Boolean = False
    Private _btnSavePreset As Button

    ' Profile UI Variables
    Private _cmbProfileSelect, _cmbProfPreset As ComboBox
    Private _txtProfName, _txtProfStart, _txtProfEnd As TextBox
    Private _chkProfEnabled, _chkProfTime As CheckBox
    Private _numProfBri As NumericUpDown
    Private _isLoadingProfile As Boolean = False
    Private _lastSelectedProfileIndex As Integer = -1

    Public Event SettingsApplied As EventHandler

    Public Sub New()
        Me.Name = "SettingsForm"
        Me.Text = "PixelGlow Configuration"
        Me.Size = New Size(900, 720)
        Me.Icon = ResourceLoader.GetIcon("ic_PixelGlow1.ico")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        RegistryHelper.LoadWindowBounds(Me)

        ' --- 1. Footer Area ---
        Dim footer As New Panel() With {.Dock = DockStyle.Bottom, .Height = 70, .BackColor = Color.FromArgb(245, 245, 245)}
        Dim btnSave As New Button() With {.Text = "OK", .Width = 100, .Height = 40, .BackColor = Color.FromArgb(0, 120, 215), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .Cursor = Cursors.Hand}
        Dim btnApply As New Button() With {.Text = "Apply", .Width = 100, .Height = 40, .BackColor = Color.White, .ForeColor = Color.Black, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .Cursor = Cursors.Hand}
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
        Dim sidebar As New FlowLayoutPanel() With {.Dock = DockStyle.Left, .Width = 220, .BackColor = Color.FromArgb(30, 30, 35), .FlowDirection = FlowDirection.TopDown, .Padding = New Padding(0, 30, 0, 0)}

        ' --- 3. Main Content Area ---
        Dim contentArea As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}
        Me.Controls.Add(contentArea)
        Me.Controls.Add(sidebar)
        Me.Controls.Add(footer)
        footer.SendToBack()
        sidebar.SendToBack()
        contentArea.BringToFront()

        _tabDisp = CreateTabPanel() : _tabHw = CreateTabPanel() : _tabEng = CreateTabPanel() : _tabGen = CreateTabPanel() : _tabDiag = CreateTabPanel() : _tabProf = CreateTabPanel()
        contentArea.Controls.AddRange({_tabDisp, _tabHw, _tabEng, _tabGen, _tabDiag, _tabProf})

        _btnGen = CreateSidebarButton("General", sidebar)
        _btnDisp = CreateSidebarButton("Display", sidebar)
        _btnHw = CreateSidebarButton("Hardware Devices", sidebar)
        _btnEng = CreateSidebarButton("Engine", sidebar)
        _btnDiag = CreateSidebarButton("Diagnostics", sidebar)
        _btnProf = CreateSidebarButton("Automated Profiles", sidebar)

        ' === Ensure Base Data Exists ===
        If SettingsManager.Current.HardwarePresets Is Nothing Then SettingsManager.Current.HardwarePresets = New List(Of HardwarePreset)()
        If SettingsManager.Current.HardwarePresets.Count = 0 Then SettingsManager.Current.HardwarePresets.Add(New HardwarePreset() With {.PresetName = "Desktop Monitor"})
        If SettingsManager.Current.Profiles Is Nothing Then SettingsManager.Current.Profiles = New List(Of PixelProfile)()

        ' === TAB 1: Display ===
        AddTabHeader("Display", _tabDisp)
        Dim tblDisp = CreateTable(_tabDisp)
        _cmbMonitor = AddComboBoxRow("Target Display", "Select the target monitor. Visual changes apply instantly to the Mimic Window.", tblDisp)
        PopulateMonitors()
        _numCrop = AddNumericRow("Edge Crop (Zoom) %", 0, 25, SettingsManager.Current.ScreenCropPercent, "Crops the outer edges of the screen to ignore taskbars and window borders. 0 = Full Screen.", tblDisp)
        _chkGrid = AddCheckBoxRow("Show Detection Grid", SettingsManager.Current.ShowDetectionGrid, "Displays a transparent overlay showing exactly where the engine is sampling colors.", tblDisp)


        ' === TAB 2: HARDWARE DEVICES (Merged Network & Layout) ===
        AddTabHeader("Hardware Devices", _tabHw)
        Dim tblHw = CreateTable(_tabHw)

        ' Preset Header
        Dim pnlHwHeader As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        _cmbPresetSelect = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 200}

        _btnSavePreset = New Button() With {.Text = "Save Edits", .Width = 90, .Height = 26, .FlatStyle = FlatStyle.Flat, .ForeColor = Color.DarkGreen}
        Dim btnAddPreset As New Button() With {.Text = "New", .Width = 60, .Height = 26, .FlatStyle = FlatStyle.Flat}
        Dim btnDelPreset As New Button() With {.Text = "Delete", .Width = 60, .Height = 26, .FlatStyle = FlatStyle.Flat, .ForeColor = Color.Firebrick}

        pnlHwHeader.Controls.AddRange({_cmbPresetSelect, _btnSavePreset, btnAddPreset, btnDelPreset})

        ' Force all buttons to the next line under the dropdown
        pnlHwHeader.SetFlowBreak(_cmbPresetSelect, True)
        Dim rHw = tblHw.RowCount
        tblHw.RowCount += 1
        tblHw.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblHw.Controls.Add(New Label() With {.Text = "Select Target:", .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0)}, 0, rHw)
        tblHw.Controls.Add(pnlHwHeader, 1, rHw)

        _txtPresetName = AddTextBoxRow("Device Name", "", "A friendly display name for this specific hardware device and physical layout.", tblHw)

        AddSectionHeader("Network Connection", tblHw)
        _cmbProtocol = AddComboBoxRow("Protocol", "Select the firmware protocol of your LED receiver.", tblHw)
        _cmbProtocol.Items.AddRange(New String() {"PixelGlow Native", "WLED (DRGB)"})
        _txtIP = AddTextBoxRow("IP Address", "", "Target IP. Use '255.255.255.255' to broadcast to all devices.", tblHw)
        _numPort = AddNumericRow("UDP Port", 1, 65535, 45045, "Hardware UDP port. Native defaults to 45045, WLED defaults to 21324.", tblHw)

        AddSectionHeader("Physical Ledger (Front-Facing Perspective)", tblHw)

        _cmbLayoutMode = AddComboBoxRow("Hardware Layout Mode", "Defines the physical shape of your lighting setup.", tblHw)
        _cmbLayoutMode.Items.AddRange(New String() {"Standard Perimeter", "Horizontal Center (Lightbar)", "Vertical Center (Towers)"})

        _numLinearZones = AddNumericRow("Detection Zones (Linear)", 1, 250, 32, "How many segments to slice the center screen into.", tblHw)
        _numThickness = AddNumericRow("Capture Thickness (%)", 1, 100, 20, "How thick the detection slice is relative to the screen.", tblHw)

        _cmbGridSize = AddComboBoxRow("Grid Resolution", "How many zones the screen is divided into. Match roughly to your LED counts.", tblHw)
        _cmbGridSize.Items.AddRange(New String() {"16x9 (Default)", "12x6", "8x3", "4x3"})
        _cmbStartEdge = AddComboBoxRow("Starting Edge", "Where does the strip start? (Imagine looking at the FRONT of your monitor).", tblHw)
        _cmbStartEdge.Items.AddRange(New String() {"Top", "Right", "Bottom", "Left"})
        _cmbDirection = AddComboBoxRow("Routing Direction", "Direction from the FRONT of the screen. (⚠️ If you are looking at the BACK of the monitor, select the opposite!)", tblHw)
        _cmbDirection.Items.AddRange(New String() {"Clockwise", "Counter-Clockwise"})
        _cmbColorOrder = AddComboBoxRow("Color Sequence", "Matches software to your strip's physical wiring (e.g., GRB for WS2812B).", tblHw)
        _cmbColorOrder.Items.AddRange(New String() {"RGB", "GRB", "BRG", "BGR", "RBG", "GBR"})

        _numGapStart = AddNumericRow("Start Offset (Blanks)", 0, 100, 0, "Hidden LEDs between the controller box and the actual screen start.", tblHw)

        AddSectionHeader("Screen Edges", tblHw)
        _numTop = AddNumericRow("Top Active LEDs", 0, 1000, 16, "LEDs tracking the top screen edge.", tblHw)
        _numGapTop = AddNumericRow("Corner Gap (Blanks)", 0, 50, 0, "Dead/Hidden LEDs in the corner following the Top edge.", tblHw)
        _numRight = AddNumericRow("Right Active LEDs", 0, 1000, 9, "LEDs tracking the right screen edge.", tblHw)
        _numGapRight = AddNumericRow("Corner Gap (Blanks)", 0, 50, 0, "Dead/Hidden LEDs in the corner following the Right edge.", tblHw)
        _numBottom = AddNumericRow("Bottom Active LEDs", 0, 1000, 16, "LEDs tracking the bottom screen edge.", tblHw)
        _numGapBottom = AddNumericRow("Corner Gap (Blanks)", 0, 50, 0, "Dead/Hidden LEDs in the corner following the Bottom edge.", tblHw)
        _numLeft = AddNumericRow("Left Active LEDs", 0, 1000, 9, "LEDs tracking the left screen edge.", tblHw)
        _numGapLeft = AddNumericRow("Corner Gap (Blanks)", 0, 50, 0, "Dead/Hidden LEDs in the corner following the Left edge.", tblHw)

        ' --- INSTANT SWITCH WIRING ---
        AddHandler _btnSavePreset.Click, Sub()
                                             SaveActivePresetUI()
                                             SettingsManager.Save()
                                             RaiseEvent SettingsApplied(Me, EventArgs.Empty)
                                             ClearHwDirty()

                                             _lblStatus.Text = "Saved & Applied to Engine!"
                                             _lblStatus.ForeColor = Color.DarkGreen
                                             Dim t As New Timer() With {.Interval = 3000}
                                             AddHandler t.Tick, Sub(senderTimer, eTimer)
                                                                    _lblStatus.Text = ""
                                                                    t.Stop()
                                                                End Sub
                                             t.Start()
                                         End Sub

        AddHandler btnAddPreset.Click, Sub()
                                           SaveActivePresetUI()
                                           SettingsManager.Current.HardwarePresets.Add(New HardwarePreset() With {.PresetName = "New Device"})
                                           RefreshPresetList(SettingsManager.Current.HardwarePresets.Count - 1)
                                       End Sub

        AddHandler btnDelPreset.Click, Sub()
                                           If _cmbPresetSelect.SelectedIndex >= 0 AndAlso SettingsManager.Current.HardwarePresets.Count > 1 Then
                                               SettingsManager.Current.HardwarePresets.RemoveAt(_cmbPresetSelect.SelectedIndex)
                                               RefreshPresetList(0)
                                           Else
                                               MessageBox.Show("You must have at least one hardware device.")
                                           End If
                                       End Sub

        AddHandler _cmbPresetSelect.SelectedIndexChanged, Sub()
                                                              If Not _isLoadingPreset Then
                                                                  ' Save outgoing edits
                                                                  SaveActivePresetUI()

                                                                  ' Instantly apply new selection to engine
                                                                  SettingsManager.Current.ActivePresetName = _cmbPresetSelect.SelectedItem.ToString()
                                                                  SettingsManager.Save()
                                                                  RaiseEvent SettingsApplied(Me, EventArgs.Empty)

                                                                  ' Load UI
                                                                  _lastPresetIndex = _cmbPresetSelect.SelectedIndex
                                                                  LoadSelectedPreset()
                                                                  ClearHwDirty()
                                                              End If
                                                          End Sub

        AddHandler _cmbPresetSelect.SelectedIndexChanged, Sub()
                                                              If Not _isLoadingPreset Then
                                                                  SaveActivePresetUI()
                                                                  _lastPresetIndex = _cmbPresetSelect.SelectedIndex
                                                                  LoadSelectedPreset()
                                                                  ClearHwDirty() ' Reset dirty flag on new load
                                                              End If
                                                          End Sub

        ' Port Autocomplete
        AddHandler _cmbProtocol.SelectedIndexChanged, Sub()
                                                          Dim isWled As Boolean = (_cmbProtocol.SelectedItem IsNot Nothing AndAlso _cmbProtocol.SelectedItem.ToString() = "WLED (DRGB)")
                                                          _cmbColorOrder.Enabled = Not isWled
                                                          If isWled AndAlso _numPort.Value = 45045 Then
                                                              _numPort.Value = 21324
                                                          ElseIf Not isWled AndAlso _numPort.Value = 21324 Then
                                                              _numPort.Value = 45045
                                                          End If
                                                      End Sub

        ' Attach Dirty Trackers to every input control in the Hardware Table
        For Each ctrl As Control In tblHw.Controls
            If TypeOf ctrl Is NumericUpDown Then AddHandler DirectCast(ctrl, NumericUpDown).ValueChanged, Sub() MarkHwDirty()
            If TypeOf ctrl Is TextBox Then AddHandler DirectCast(ctrl, TextBox).TextChanged, Sub() MarkHwDirty()
            If TypeOf ctrl Is ComboBox AndAlso ctrl IsNot _cmbPresetSelect Then AddHandler DirectCast(ctrl, ComboBox).SelectedIndexChanged, Sub() MarkHwDirty()
        Next


        ' === TAB 3: Engine ===
        AddTabHeader("Processing Parameters", _tabEng)
        Dim tblEng = CreateTable(_tabEng)
        _numBright = AddNumericRow("Max Brightness (%)", 1, 100, SettingsManager.Current.MaxBrightness, "Limits the maximum power output of the LEDs.", tblEng)
        _numSat = AddNumericRow("Saturation Boost (%)", 100, 300, SettingsManager.Current.SaturationBoost, "100 = Screen Accurate. 150+ forces vibrant colors and reduces white washout.", tblEng)
        _numInterval = AddNumericRow("Sync Speed (ms)", 10, 1000, SettingsManager.Current.UpdateIntervalMs, "Delay between screen captures. 33ms provides ~30 FPS.", tblEng)
        _numSmooth = AddNumericRow("Temporal Smoothing", 1, 100, SettingsManager.Current.SmoothingSpeed, "1 = Very slow cinematic color fade. 100 = Instant flashing.", tblEng)

        _chkBlackBar = AddCheckBoxRow("Auto-Crop Black Bars", SettingsManager.Current.DetectBlackBars, "Automatically detects and crops letterboxing (black bars) in wide movies.", tblEng)
        _cmbSensitivity = AddComboBoxRow("Crop Sensitivity", "Aggressive bites through streaming compression noise and dark artifacts.", tblEng)
        _cmbSensitivity.Items.AddRange(New String() {"Standard (Clean Video)", "Aggressive (Compressed Streams)"})
        _cmbSensitivity.SelectedIndex = If(SettingsManager.Current.BlackBarThreshold <= 40, 0, 1)
        _cmbSensitivity.Enabled = _chkBlackBar.Checked
        AddHandler _chkBlackBar.CheckedChanged, Sub() _cmbSensitivity.Enabled = _chkBlackBar.Checked


        ' === TAB 4: Diagnostics ===
        AddTabHeader("System Diagnostics", _tabDiag)
        Dim tblDiag = CreateTable(_tabDiag)
        _chkTestMode = AddCheckBoxRow("Alignment Test Mode", SettingsManager.Current.TestMode, "Forces LEDs to Red (Top), Green (Bottom), Blue (Left), Purple (Right).", tblDiag)
        _chkDiagSegments = AddCheckBoxRow("Indicate Segments", SettingsManager.Current.DiagSegments, "Sends a purple breathing beacon to the start and end LEDs (2 each) of every screen edge.", tblDiag)
        _chkDiagGaps = AddCheckBoxRow("Indicate Gaps", SettingsManager.Current.DiagGaps, "Lights up all hidden Start Offset and Corner Gap LEDs in steady Red.", tblDiag)
        _chkDiagSweep = AddCheckBoxRow("Sweep Effect", SettingsManager.Current.DiagSweep, "Sweeps Red, Green, and Blue (3 LEDs each) from start to end continuously.", tblDiag)
        _chkDiagBullet = AddCheckBoxRow("Bullet Effect", SettingsManager.Current.DiagBullet, "Rapid white comet effect with a fading tail shooting from start to end.", tblDiag)

        Dim diagBoxes() As CheckBox = {_chkTestMode, _chkDiagSegments, _chkDiagGaps, _chkDiagSweep, _chkDiagBullet}
        For Each cb In diagBoxes
            AddHandler cb.Click, Sub(sender As Object, e As EventArgs)
                                     Dim clickedBox = DirectCast(sender, CheckBox)
                                     If clickedBox.Checked Then
                                         For Each otherBox In diagBoxes
                                             If otherBox IsNot clickedBox Then otherBox.Checked = False
                                         Next
                                     End If
                                 End Sub
        Next

        Dim lblNote As New Label() With {
            .Text = "💡 Pro Tip: On the Mimic Window, hold CTRL and Double-Click anywhere to instantly cycle through solid Red, Green, Blue, and White diagnostic colors. A standard Double-Click returns to normal operation.",
            .Dock = DockStyle.Fill, .AutoSize = True, .ForeColor = Color.DimGray,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Italic), .Padding = New Padding(10, 25, 10, 0)
        }
        Dim rDiag = tblDiag.RowCount
        tblDiag.RowCount += 1
        tblDiag.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblDiag.Controls.Add(lblNote, 0, rDiag)
        tblDiag.SetColumnSpan(lblNote, 2)


        ' === TAB 5: General ===
        AddTabHeader("General Settings", _tabGen)
        Dim tblGen = CreateTable(_tabGen)
        AddSectionHeader("General", tblGen)
        _chkControlHw = AddCheckBoxRow("Control Hardware", SettingsManager.Current.ControlHardware, "Sends color data over the network. Uncheck to temporarily pause LED lighting.", tblGen)
        _chkStartInTray = AddCheckBoxRow("Start in Tray", SettingsManager.Current.StartInTray, "Launches the application hidden in the system tray instead of showing the mimic screen.", tblGen)
        _chkStartWithWindows = AddCheckBoxRow("Start with Windows", SettingsManager.Current.StartWithWindows, "Automatically launches PixelGlow when you log into your computer.", tblGen)
        AddSectionHeader("Power Management", tblGen)
        _chkFollowPower = AddCheckBoxRow("Follow OS Power State", SettingsManager.Current.FollowPowerState, "Automatically pauses the ambient lighting when Windows goes to sleep or the screen is locked.", tblGen)
        _chkDimOnPower = AddCheckBoxRow("Dim Lights on Lock/Sleep", SettingsManager.Current.DimOnPowerState, "Instead of turning off completely, fade the LEDs to a very dim glow.", tblGen)

        _cmbDimColor = AddComboBoxRow("Dimming Color", "Select the color to use during standby.", tblGen)
        _cmbDimColor.Items.AddRange(New String() {"White", "Red", "Green", "Blue"})
        _cmbDimColor.SelectedItem = If(String.IsNullOrEmpty(SettingsManager.Current.DimColor), "White", SettingsManager.Current.DimColor)

        _chkDimBreathing = AddCheckBoxRow("Breathing Effect", SettingsManager.Current.DimBreathing, "Slowly pulse the dim color while the system is locked.", tblGen)

        AddSectionHeader("Tools", tblGen)
        _chkLogging = AddCheckBoxRow("Enable Logging", SettingsManager.Current.LoggingEnabled, "Writes background diagnostic information to a log file for troubleshooting.", tblGen)

        Dim syncPowerUi = Sub()
                              _chkDimOnPower.Enabled = _chkFollowPower.Checked
                              _cmbDimColor.Enabled = _chkFollowPower.Checked AndAlso _chkDimOnPower.Checked
                              _chkDimBreathing.Enabled = _chkFollowPower.Checked AndAlso _chkDimOnPower.Checked
                          End Sub
        AddHandler _chkFollowPower.CheckedChanged, Sub() syncPowerUi()
        AddHandler _chkDimOnPower.CheckedChanged, Sub() syncPowerUi()
        syncPowerUi()


        ' === TAB 6: Profiles ===
        AddTabHeader("Automated Profiles", _tabProf)
        Dim tblProf = CreateTable(_tabProf)

        Dim pnlProfHeader As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        _cmbProfileSelect = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 200}
        Dim btnAddProf As New Button() With {.Text = "New Profile", .Width = 100, .Height = 26, .FlatStyle = FlatStyle.Flat}
        Dim btnDelProf As New Button() With {.Text = "Delete", .Width = 70, .Height = 26, .FlatStyle = FlatStyle.Flat, .ForeColor = Color.Firebrick}
        pnlProfHeader.Controls.AddRange({_cmbProfileSelect, btnAddProf, btnDelProf})

        Dim rProf = tblProf.RowCount
        tblProf.RowCount += 1
        tblProf.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblProf.Controls.Add(New Label() With {.Text = "Select Profile:", .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0)}, 0, rProf)
        tblProf.Controls.Add(pnlProfHeader, 1, rProf)

        _txtProfName = AddTextBoxRow("Profile Name", "", "Display name for this automation rule.", tblProf)
        _chkProfEnabled = AddCheckBoxRow("Enable Profile", False, "Allow this profile to activate when conditions are met.", tblProf)

        AddSectionHeader("Activation Conditions", tblProf)
        _chkProfTime = AddCheckBoxRow("Time of Day", False, "Activate this profile automatically during a specific time window.", tblProf)
        Dim pnlTime As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        _txtProfStart = New TextBox() With {.Width = 60}
        _txtProfEnd = New TextBox() With {.Width = 60}
        pnlTime.Controls.AddRange({New Label() With {.Text = "Start (HH:MM):", .AutoSize = True, .Padding = New Padding(0, 4, 0, 0)}, _txtProfStart, New Label() With {.Text = "End (HH:MM):", .AutoSize = True, .Padding = New Padding(10, 4, 0, 0)}, _txtProfEnd})
        rProf = tblProf.RowCount
        tblProf.RowCount += 1
        tblProf.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblProf.Controls.Add(pnlTime, 1, rProf)

        AddSectionHeader("Overrides (Leave blank to use Base Configuration)", tblProf)
        _numProfBri = AddNumericRow("Brightness Override (%)", -1, 100, -1, "Set to -1 to ignore and use your device's base brightness setting.", tblProf)
        _cmbProfPreset = AddComboBoxRow("Target Hardware Override", "Select which physical device to stream to when this profile is active.", tblProf)

        AddHandler btnAddProf.Click, Sub()
                                         SaveActiveProfileUI()
                                         SettingsManager.Current.Profiles.Add(New PixelProfile() With {.ProfileName = "New Profile"})
                                         RefreshProfileList(SettingsManager.Current.Profiles.Count - 1)
                                     End Sub
        AddHandler btnDelProf.Click, Sub()
                                         If _cmbProfileSelect.SelectedIndex >= 0 Then
                                             SettingsManager.Current.Profiles.RemoveAt(_cmbProfileSelect.SelectedIndex)
                                             RefreshProfileList(0)
                                         End If
                                     End Sub
        AddHandler _cmbProfileSelect.SelectedIndexChanged, Sub()
                                                               If Not _isLoadingProfile Then
                                                                   SaveActiveProfileUI()
                                                                   _lastSelectedProfileIndex = _cmbProfileSelect.SelectedIndex
                                                                   LoadSelectedProfile()
                                                               End If
                                                           End Sub


        ' === INITIALIZATION LOADS ===
        RefreshPresetList(0)
        For i As Integer = 0 To _cmbPresetSelect.Items.Count - 1
            If _cmbPresetSelect.Items(i).ToString() = SettingsManager.Current.ActivePresetName Then
                _cmbPresetSelect.SelectedIndex = i
                _lastPresetIndex = i
                LoadSelectedPreset()
                Exit For
            End If
        Next

        RefreshProfileList(0)

        ' --- Dynamic Layout UI Wiring ---
        Dim syncLayoutUi = Sub()
                               Dim mode As String = If(_cmbLayoutMode.SelectedItem IsNot Nothing, _cmbLayoutMode.SelectedItem.ToString(), "Standard Perimeter")
                               Dim isPerimeter As Boolean = (mode = "Standard Perimeter")
                               Dim isLinear As Boolean = Not isPerimeter

                               ' Toggle Perimeter vs Linear controls
                               ToggleRow(_cmbGridSize, isPerimeter)
                               ToggleRow(_cmbStartEdge, isPerimeter)
                               ToggleRow(_numGapStart, isPerimeter)
                               ToggleRow(_numGapTop, isPerimeter)
                               ToggleRow(_numRight, isPerimeter)
                               ToggleRow(_numGapRight, isPerimeter)
                               ToggleRow(_numBottom, isPerimeter)
                               ToggleRow(_numGapBottom, isPerimeter)
                               ToggleRow(_numLeft, isPerimeter)
                               ToggleRow(_numGapLeft, isPerimeter)

                               ToggleRow(_numLinearZones, isLinear)
                               ToggleRow(_numThickness, isLinear)

                               ' Dynamically rename shared controls based on the mode
                               Dim tbl = TryCast(_numTop.Parent, TableLayoutPanel)
                               If tbl IsNot Nothing Then
                                   ' Recycle Top LEDs into "Physical LED Count"
                                   Dim rTop = tbl.GetRow(_numTop)
                                   If rTop >= 0 Then
                                       Dim lblTitle = TryCast(tbl.GetControlFromPosition(0, rTop), Label)
                                       Dim lblDesc = TryCast(tbl.GetControlFromPosition(1, rTop + 1), Label)
                                       If lblTitle IsNot Nothing AndAlso lblDesc IsNot Nothing Then
                                           If isLinear Then
                                               lblTitle.Text = "Physical LED Count:"
                                               lblDesc.Text = "Total number of LEDs on this straight strip."
                                           Else
                                               lblTitle.Text = "Top Active LEDs:"
                                               lblDesc.Text = "LEDs tracking the top screen edge."
                                           End If
                                       End If
                                   End If

                                   ' Update Direction items and text contextually
                                   Dim rDir = tbl.GetRow(_cmbDirection)
                                   If rDir >= 0 Then
                                       Dim lblDescDir = TryCast(tbl.GetControlFromPosition(1, rDir + 1), Label)

                                       ' Store current selection to prevent UI reset jumps
                                       Dim currentSelection As String = If(_cmbDirection.SelectedItem IsNot Nothing, _cmbDirection.SelectedItem.ToString(), "")
                                       _cmbDirection.Items.Clear()

                                       If mode = "Horizontal Center (Lightbar)" Then
                                           _cmbDirection.Items.AddRange(New String() {"Left-to-Right", "Right-to-Left"})
                                           If lblDescDir IsNot Nothing Then lblDescDir.Text = "Physical data flow direction of the lightbar."
                                       ElseIf mode = "Vertical Center (Towers)" Then
                                           _cmbDirection.Items.AddRange(New String() {"Top-to-Bottom", "Bottom-to-Top"})
                                           If lblDescDir IsNot Nothing Then lblDescDir.Text = "Physical data flow direction of the towers."
                                       Else
                                           _cmbDirection.Items.AddRange(New String() {"Clockwise", "Counter-Clockwise"})
                                           If lblDescDir IsNot Nothing Then lblDescDir.Text = "Direction from the FRONT of the screen. (⚠️ If looking at BACK, select opposite!)"
                                       End If

                                       ' Try to restore selection, otherwise default to 0
                                       If _cmbDirection.Items.Contains(currentSelection) Then
                                           _cmbDirection.SelectedItem = currentSelection
                                       Else
                                           _cmbDirection.SelectedIndex = 0
                                       End If
                                   End If
                               End If
                           End Sub

        AddHandler _cmbLayoutMode.SelectedIndexChanged, Sub() syncLayoutUi()
        syncLayoutUi() ' Run once on load to establish the correct initial UI state


        ' --- Restore the last selected tab ---
        Select Case SettingsManager.Current.LastSettingsTab
            Case 0 : SidebarClick(_btnGen, EventArgs.Empty)
            Case 1 : SidebarClick(_btnDisp, EventArgs.Empty)
            Case 2 : SidebarClick(_btnHw, EventArgs.Empty)
            Case 3 : SidebarClick(_btnEng, EventArgs.Empty)
            Case 4 : SidebarClick(_btnDiag, EventArgs.Empty)
            Case 5 : SidebarClick(_btnProf, EventArgs.Empty)
            Case Else : SidebarClick(_btnGen, EventArgs.Empty)
        End Select
    End Sub

    ' --- UI GENERATOR HELPERS ---
    Private Function CreateTabPanel() As Panel
        Return New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(40, 30, 40, 20), .Visible = False, .AutoScroll = True}
    End Function

    Private Sub AddSectionHeader(title As String, table As TableLayoutPanel)
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lbl As New Label() With {.Text = title, .Dock = DockStyle.Fill, .AutoSize = True, .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold), .ForeColor = Color.FromArgb(0, 120, 215), .Padding = New Padding(0, 15, 0, 2)}
        table.Controls.Add(lbl, 0, r)
        table.SetColumnSpan(lbl, 2)
        Dim hr As New Label() With {.Height = 1, .BackColor = Color.LightGray, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, 10)}
        table.Controls.Add(hr, 0, r + 1)
        table.SetColumnSpan(hr, 2)
    End Sub

    Private Function CreateSidebarButton(title As String, parent As FlowLayoutPanel) As Button
        Dim btn As New Button() With {.Text = title, .Width = 220, .Height = 60, .Margin = New Padding(0), .FlatStyle = FlatStyle.Flat, .ForeColor = Color.White, .BackColor = Color.FromArgb(30, 30, 35), .Font = New Font("Segoe UI", 10.5F), .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(25, 0, 0, 0), .Cursor = Cursors.Hand}
        btn.FlatAppearance.BorderSize = 0
        AddHandler btn.Click, AddressOf SidebarClick
        parent.Controls.Add(btn)
        Return btn
    End Function

    Private Sub SidebarClick(sender As Object, e As EventArgs)
        _tabDisp.Visible = False : _tabHw.Visible = False : _tabEng.Visible = False : _tabGen.Visible = False : _tabDiag.Visible = False : _tabProf.Visible = False
        _btnDisp.BackColor = Color.FromArgb(30, 30, 35) : _btnHw.BackColor = Color.FromArgb(30, 30, 35) : _btnEng.BackColor = Color.FromArgb(30, 30, 35) : _btnGen.BackColor = Color.FromArgb(30, 30, 35) : _btnDiag.BackColor = Color.FromArgb(30, 30, 35) : _btnProf.BackColor = Color.FromArgb(30, 30, 35)

        Dim clicked = DirectCast(sender, Button)
        clicked.BackColor = Color.FromArgb(0, 120, 215)

        If clicked Is _btnGen Then _tabGen.Visible = True : _tabGen.BringToFront() : SettingsManager.Current.LastSettingsTab = 0
        If clicked Is _btnDisp Then _tabDisp.Visible = True : _tabDisp.BringToFront() : SettingsManager.Current.LastSettingsTab = 1
        If clicked Is _btnHw Then _tabHw.Visible = True : _tabHw.BringToFront() : SettingsManager.Current.LastSettingsTab = 2
        If clicked Is _btnEng Then _tabEng.Visible = True : _tabEng.BringToFront() : SettingsManager.Current.LastSettingsTab = 3
        If clicked Is _btnDiag Then _tabDiag.Visible = True : _tabDiag.BringToFront() : SettingsManager.Current.LastSettingsTab = 4
        If clicked Is _btnProf Then _tabProf.Visible = True : _tabProf.BringToFront() : SettingsManager.Current.LastSettingsTab = 5


    End Sub

    Private Sub AddTabHeader(title As String, parent As Control)
        parent.Controls.Add(New Label() With {.Text = title, .Dock = DockStyle.Top, .Height = 50, .Font = New Font("Segoe UI Semilight", 22), .ForeColor = Color.FromArgb(0, 120, 215), .TextAlign = ContentAlignment.BottomLeft, .Padding = New Padding(0, 0, 0, 10)})
    End Sub

    Private Function CreateTable(parent As Control) As TableLayoutPanel
        Dim t As New TableLayoutPanel() With {.Dock = DockStyle.Top, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .ColumnCount = 2, .Margin = New Padding(0, 20, 0, 0)}
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
        Dim cmbInput As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 250, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        table.Controls.Add(New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}, 0, r)
        table.Controls.Add(cmbInput, 1, r)
        table.Controls.Add(New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}, 1, r + 1)
        Return cmbInput
    End Function

    Private Function AddTextBoxRow(title As String, val As String, desc As String, table As TableLayoutPanel) As TextBox
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim txtInput As New TextBox() With {.Text = val, .Width = 250, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        table.Controls.Add(New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}, 0, r)
        table.Controls.Add(txtInput, 1, r)
        table.Controls.Add(New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}, 1, r + 1)
        Return txtInput
    End Function

    Private Function AddNumericRow(title As String, min As Integer, max As Integer, val As Integer, desc As String, table As TableLayoutPanel) As NumericUpDown
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim numInput As New NumericUpDown() With {.Minimum = min, .Maximum = max, .Value = val, .Width = 100, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        table.Controls.Add(New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}, 0, r)
        table.Controls.Add(numInput, 1, r)
        table.Controls.Add(New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}, 1, r + 1)
        Return numInput
    End Function

    Private Function AddCheckBoxRow(title As String, chkState As Boolean, desc As String, table As TableLayoutPanel) As CheckBox
        Dim r = table.RowCount
        table.RowCount += 2
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim chkInput As New CheckBox() With {.Checked = chkState, .AutoSize = True, .Anchor = AnchorStyles.Left Or AnchorStyles.Top}
        table.Controls.Add(New Label() With {.Text = title & ":", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0), .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}, 0, r)
        table.Controls.Add(chkInput, 1, r)
        table.Controls.Add(New Label() With {.Text = desc, .Dock = DockStyle.Fill, .ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .AutoSize = True, .Padding = New Padding(0, 2, 0, 25)}, 1, r + 1)
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
        SaveActivePresetUI()
        SaveActiveProfileUI()

        With SettingsManager.Current
            .ActivePresetName = If(_cmbPresetSelect.SelectedItem IsNot Nothing, _cmbPresetSelect.SelectedItem.ToString(), "Desktop Monitor")
            .TargetMonitorIndex = CInt(Math.Max(0, _cmbMonitor.SelectedIndex))
            .ScreenCropPercent = CInt(_numCrop.Value)
            .MaxBrightness = CInt(_numBright.Value)
            .SaturationBoost = CInt(_numSat.Value)
            .UpdateIntervalMs = CInt(_numInterval.Value)
            .SmoothingSpeed = CInt(_numSmooth.Value)
            .DetectBlackBars = _chkBlackBar.Checked
            .BlackBarThreshold = If(_cmbSensitivity.SelectedIndex = 0, 40, 120)
            .TestMode = _chkTestMode.Checked
            .DiagSegments = _chkDiagSegments.Checked
            .DiagGaps = _chkDiagGaps.Checked
            .DiagSweep = _chkDiagSweep.Checked
            .DiagBullet = _chkDiagBullet.Checked
            .ShowDetectionGrid = _chkGrid.Checked
            .ControlHardware = _chkControlHw.Checked
            .StartInTray = _chkStartInTray.Checked
            .StartWithWindows = _chkStartWithWindows.Checked
            .FollowPowerState = _chkFollowPower.Checked
            .DimOnPowerState = _chkDimOnPower.Checked
            .DimColor = If(_cmbDimColor.SelectedItem IsNot Nothing, _cmbDimColor.SelectedItem.ToString(), "White")
            .DimBreathing = _chkDimBreathing.Checked
            .LoggingEnabled = _chkLogging.Checked
        End With

        RegistryHelper.SetStartup(SettingsManager.Current.StartWithWindows)
        SettingsManager.Save()
        RegistryHelper.SaveWindowBounds(Me)

        RaiseEvent SettingsApplied(Me, EventArgs.Empty)

        If closeForm Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        RegistryHelper.SaveWindowBounds(Me)
        If Me.DialogResult <> DialogResult.OK Then
            If SettingsManager.Current.TestMode OrElse SettingsManager.Current.DiagSegments OrElse SettingsManager.Current.DiagGaps OrElse SettingsManager.Current.DiagSweep OrElse SettingsManager.Current.DiagBullet Then
                SettingsManager.Current.TestMode = False : SettingsManager.Current.DiagSegments = False : SettingsManager.Current.DiagGaps = False : SettingsManager.Current.DiagSweep = False : SettingsManager.Current.DiagBullet = False
                SettingsManager.Save()
                RaiseEvent SettingsApplied(Me, EventArgs.Empty)
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ' --- HARDWARE PRESET LOGIC ---
    Private Sub RefreshPresetList(selectedIndex As Integer)
        _isLoadingPreset = True
        _cmbPresetSelect.Items.Clear()

        Dim oldProfSelection As String = If(_cmbProfPreset.SelectedItem IsNot Nothing, _cmbProfPreset.SelectedItem.ToString(), "")
        _cmbProfPreset.Items.Clear()
        _cmbProfPreset.Items.Add("") ' Blank for no override

        For Each p In SettingsManager.Current.HardwarePresets
            _cmbPresetSelect.Items.Add(p.PresetName)
            _cmbProfPreset.Items.Add(p.PresetName)
        Next

        If _cmbProfPreset.Items.Contains(oldProfSelection) Then _cmbProfPreset.SelectedItem = oldProfSelection

        If _cmbPresetSelect.Items.Count > 0 Then
            Dim targetIndex As Integer = If(selectedIndex >= 0 AndAlso selectedIndex < _cmbPresetSelect.Items.Count, selectedIndex, 0)
            _cmbPresetSelect.SelectedIndex = targetIndex
            _lastPresetIndex = targetIndex
            LoadSelectedPreset()
        Else
            _lastPresetIndex = -1
        End If
        _isLoadingPreset = False
    End Sub

    Private Sub LoadSelectedPreset()
        If _cmbPresetSelect.SelectedIndex < 0 Then Return
        _isLoadingPreset = True
        Dim p = SettingsManager.Current.HardwarePresets(_cmbPresetSelect.SelectedIndex)

        _txtPresetName.Text = p.PresetName
        _cmbProtocol.SelectedItem = If(String.IsNullOrEmpty(p.Protocol), "PixelGlow Native", p.Protocol)
        _txtIP.Text = p.IP
        _numPort.Value = p.Port

        _cmbLayoutMode.SelectedItem = If(String.IsNullOrEmpty(p.LayoutMode), "Standard Perimeter", p.LayoutMode)
        If _cmbLayoutMode.SelectedIndex = -1 Then _cmbLayoutMode.SelectedIndex = 0

        _numLinearZones.Value = Math.Max(1, Math.Min(250, If(p.LinearZones > 0, p.LinearZones, 32)))
        _numThickness.Value = Math.Max(1, Math.Min(100, If(p.CaptureThickness > 0, p.CaptureThickness, 20)))

        Select Case p.GridCols
            Case 16 : _cmbGridSize.SelectedIndex = 0
            Case 12 : _cmbGridSize.SelectedIndex = 1
            Case 8 : _cmbGridSize.SelectedIndex = 2
            Case 4 : _cmbGridSize.SelectedIndex = 3
            Case Else : _cmbGridSize.SelectedIndex = 0
        End Select

        _cmbStartEdge.SelectedItem = p.StartEdge
        If _cmbStartEdge.SelectedIndex = -1 Then _cmbStartEdge.SelectedIndex = 0
        _cmbDirection.SelectedItem = p.Direction
        If _cmbDirection.SelectedIndex = -1 Then _cmbDirection.SelectedIndex = 0
        _cmbColorOrder.SelectedItem = p.ColorSequence
        If _cmbColorOrder.SelectedIndex = -1 Then _cmbColorOrder.SelectedIndex = 0

        _numTop.Value = p.TopLeds
        _numBottom.Value = p.BottomLeds
        _numLeft.Value = p.LeftLeds
        _numRight.Value = p.RightLeds

        _numGapStart.Value = p.BlankStart
        _numGapTop.Value = p.BlankAfterTop
        _numGapRight.Value = p.BlankAfterRight
        _numGapBottom.Value = p.BlankAfterBottom
        _numGapLeft.Value = p.BlankAfterLeft

        _isLoadingPreset = False
    End Sub

    Private Sub SaveActivePresetUI()
        If _isLoadingPreset OrElse _lastPresetIndex < 0 OrElse _lastPresetIndex >= SettingsManager.Current.HardwarePresets.Count Then Return
        Dim p = SettingsManager.Current.HardwarePresets(_lastPresetIndex)

        p.PresetName = _txtPresetName.Text
        p.Protocol = If(_cmbProtocol.SelectedItem IsNot Nothing, _cmbProtocol.SelectedItem.ToString(), "PixelGlow Native")
        p.IP = _txtIP.Text
        p.Port = CInt(_numPort.Value)

        p.LayoutMode = If(_cmbLayoutMode.SelectedItem IsNot Nothing, _cmbLayoutMode.SelectedItem.ToString(), "Standard Perimeter")
        p.LinearZones = CInt(_numLinearZones.Value)
        p.CaptureThickness = CInt(_numThickness.Value)

        Select Case _cmbGridSize.SelectedIndex
            Case 0 : p.GridCols = 16 : p.GridRows = 9
            Case 1 : p.GridCols = 12 : p.GridRows = 6
            Case 2 : p.GridCols = 8 : p.GridRows = 3
            Case 3 : p.GridCols = 4 : p.GridRows = 3
        End Select

        p.StartEdge = If(_cmbStartEdge.SelectedItem IsNot Nothing, _cmbStartEdge.SelectedItem.ToString(), "Top")
        p.Direction = If(_cmbDirection.SelectedItem IsNot Nothing, _cmbDirection.SelectedItem.ToString(), "Clockwise")
        p.ColorSequence = If(_cmbColorOrder.SelectedItem IsNot Nothing, _cmbColorOrder.SelectedItem.ToString(), "RGB")

        p.TopLeds = CInt(_numTop.Value)
        p.BottomLeds = CInt(_numBottom.Value)
        p.LeftLeds = CInt(_numLeft.Value)
        p.RightLeds = CInt(_numRight.Value)

        p.BlankStart = CInt(_numGapStart.Value)
        p.BlankAfterTop = CInt(_numGapTop.Value)
        p.BlankAfterRight = CInt(_numGapRight.Value)
        p.BlankAfterBottom = CInt(_numGapBottom.Value)
        p.BlankAfterLeft = CInt(_numGapLeft.Value)

        If _cmbPresetSelect.Items(_lastPresetIndex).ToString() <> p.PresetName Then
            _isLoadingPreset = True
            _cmbPresetSelect.Items(_lastPresetIndex) = p.PresetName

            Dim oldProfSelect = _cmbProfPreset.SelectedItem
            _cmbProfPreset.Items.Clear()
            _cmbProfPreset.Items.Add("")
            For Each pr In SettingsManager.Current.HardwarePresets
                _cmbProfPreset.Items.Add(pr.PresetName)
            Next
            If _cmbProfPreset.Items.Contains(oldProfSelect) Then _cmbProfPreset.SelectedItem = oldProfSelect
            _isLoadingPreset = False
        End If
    End Sub

    ' --- PROFILE UI LOGIC ---
    Private Sub RefreshProfileList(selectedIndex As Integer)
        _isLoadingProfile = True
        _cmbProfileSelect.Items.Clear()
        For Each p In SettingsManager.Current.Profiles
            _cmbProfileSelect.Items.Add(p.ProfileName)
        Next
        If _cmbProfileSelect.Items.Count > 0 Then
            Dim targetIndex As Integer = If(selectedIndex >= 0 AndAlso selectedIndex < _cmbProfileSelect.Items.Count, selectedIndex, 0)
            _cmbProfileSelect.SelectedIndex = targetIndex
            _lastSelectedProfileIndex = targetIndex
            LoadSelectedProfile()
        Else
            _lastSelectedProfileIndex = -1
            ClearProfileUI()
        End If
        _isLoadingProfile = False
    End Sub

    Private Sub LoadSelectedProfile()
        If _cmbProfileSelect.SelectedIndex < 0 Then Return
        _isLoadingProfile = True
        Dim p = SettingsManager.Current.Profiles(_cmbProfileSelect.SelectedIndex)

        _txtProfName.Text = p.ProfileName
        _chkProfEnabled.Checked = p.IsEnabled
        _chkProfTime.Checked = p.EnableTimeRule
        _txtProfStart.Text = p.StartTime
        _txtProfEnd.Text = p.EndTime
        _numProfBri.Value = p.OverrideMaxBrightness

        _cmbProfPreset.SelectedItem = If(String.IsNullOrEmpty(p.OverridePresetName), "", p.OverridePresetName)

        _isLoadingProfile = False
    End Sub

    Private Sub ClearProfileUI()
        _isLoadingProfile = True
        _txtProfName.Text = ""
        _chkProfEnabled.Checked = False
        _chkProfTime.Checked = False
        _txtProfStart.Text = ""
        _txtProfEnd.Text = ""
        _numProfBri.Value = -1
        _cmbProfPreset.SelectedIndex = 0
        _isLoadingProfile = False
    End Sub

    Private Sub SaveActiveProfileUI()
        If _isLoadingProfile OrElse _lastSelectedProfileIndex < 0 OrElse _lastSelectedProfileIndex >= SettingsManager.Current.Profiles.Count Then Return
        Dim p = SettingsManager.Current.Profiles(_lastSelectedProfileIndex)

        p.ProfileName = _txtProfName.Text
        p.IsEnabled = _chkProfEnabled.Checked
        p.EnableTimeRule = _chkProfTime.Checked
        p.StartTime = _txtProfStart.Text
        p.EndTime = _txtProfEnd.Text
        p.OverrideMaxBrightness = CInt(_numProfBri.Value)
        p.OverridePresetName = If(_cmbProfPreset.SelectedItem IsNot Nothing, _cmbProfPreset.SelectedItem.ToString(), "")

        If _cmbProfileSelect.Items(_lastSelectedProfileIndex).ToString() <> p.ProfileName Then
            _isLoadingProfile = True
            _cmbProfileSelect.Items(_lastSelectedProfileIndex) = p.ProfileName
            _isLoadingProfile = False
        End If
    End Sub


    Private Sub MarkHwDirty()
        If _isLoadingPreset OrElse _hwIsDirty Then Return
        _hwIsDirty = True
        _btnSavePreset.Text = "Save Edits *"
        _btnSavePreset.Font = New Font(_btnSavePreset.Font, FontStyle.Bold)
        _btnSavePreset.BackColor = Color.LightGoldenrodYellow
    End Sub

    Private Sub ClearHwDirty()
        _hwIsDirty = False
        _btnSavePreset.Text = "Save Edits"
        _btnSavePreset.Font = New Font("Segoe UI", 9.5F)
        _btnSavePreset.BackColor = SystemColors.Control
    End Sub



    ' --- DYNAMIC UI HELPERS ---
    Private Sub ToggleRow(ctrl As Control, isVisible As Boolean)
        If ctrl Is Nothing OrElse ctrl.Parent Is Nothing Then Return
        Dim tbl = TryCast(ctrl.Parent, TableLayoutPanel)
        If tbl Is Nothing Then Return

        ctrl.Visible = isVisible
        Dim r = tbl.GetRow(ctrl)
        If r >= 0 Then
            Dim lblTitle = tbl.GetControlFromPosition(0, r)
            Dim lblDesc = tbl.GetControlFromPosition(1, r + 1)
            If lblTitle IsNot Nothing Then lblTitle.Visible = isVisible
            If lblDesc IsNot Nothing Then lblDesc.Visible = isVisible
        End If
    End Sub
End Class