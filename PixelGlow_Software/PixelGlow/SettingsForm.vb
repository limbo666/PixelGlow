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
    Private _cmbProtocol As ComboBox
    Private _chkBlackBar, _chkTestMode, _chkGrid, _chkLogging, _chkControlHw As CheckBox
    Private _chkDiagSegments, _chkDiagGaps, _chkDiagSweep, _chkDiagBullet As CheckBox
    Private _chkStartInTray, _chkStartWithWindows As CheckBox
    Private _chkFollowPower, _chkDimOnPower As CheckBox
    Private _lblStatus As Label

    ' Virtual Tab Infrastructure
    Private _btnDisp, _btnNet, _btnLeds, _btnEng, _btnGen, _btnDiag As Button
    Private _tabDisp, _tabNet, _tabLeds, _tabEng, _tabGen, _tabDiag As Panel
    ' Profile UI Variables
    Private _btnProf As Button
    Private _tabProf As Panel
    Private _cmbProfileSelect, _cmbProfProto As ComboBox
    Private _txtProfName, _txtProfStart, _txtProfEnd, _txtProfIP As TextBox
    Private _chkProfEnabled, _chkProfTime As CheckBox
    Private _numProfBri, _numProfPort As NumericUpDown
    Private _isLoadingProfile As Boolean = False
    Private _lastSelectedProfileIndex As Integer = -1 ' Tracks the correct profile memory slot


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

        _tabDisp = CreateTabPanel() : _tabNet = CreateTabPanel() : _tabLeds = CreateTabPanel() : _tabEng = CreateTabPanel() : _tabGen = CreateTabPanel() : _tabDiag = CreateTabPanel()
        contentArea.Controls.AddRange({_tabDisp, _tabNet, _tabLeds, _tabEng, _tabGen, _tabDiag})

        _btnGen = CreateSidebarButton("General", sidebar)
        _btnDisp = CreateSidebarButton("Display", sidebar)
        _btnNet = CreateSidebarButton("Network", sidebar)
        _btnLeds = CreateSidebarButton("Hardware Layout", sidebar)
        _btnEng = CreateSidebarButton("Engine", sidebar)
        _btnDiag = CreateSidebarButton("Diagnostics", sidebar)
        _tabProf = CreateTabPanel()
        contentArea.Controls.Add(_tabProf)
        _btnProf = CreateSidebarButton("Profiles", sidebar)


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
        _numCrop = AddNumericRow("Edge Crop (Zoom) %", 0, 25, SettingsManager.Current.ScreenCropPercent, "Crops the outer edges of the screen to ignore taskbars and window borders. 0 = Full Screen.", tblDisp)

        ' === TAB 2: Network ===
        AddTabHeader("Network", _tabNet)
        Dim tblNet = CreateTable(_tabNet)

        _cmbProtocol = AddComboBoxRow("Hardware Protocol", "Select the type of receiver.", tblNet)
        _cmbProtocol.Items.AddRange(New String() {"PixelGlow Native", "WLED (DRGB)"})
        _cmbProtocol.SelectedItem = SettingsManager.Current.HardwareProtocol
        If _cmbProtocol.SelectedIndex = -1 Then _cmbProtocol.SelectedIndex = 0
        ' Load initial UI based on current protocol
        Dim isWled As Boolean = (SettingsManager.Current.HardwareProtocol = "WLED (DRGB)")
        Dim initIP As String = If(isWled, SettingsManager.Current.WledIP, SettingsManager.Current.TargetIP)
        Dim initPort As Integer = If(isWled, SettingsManager.Current.WledPort, SettingsManager.Current.TargetPort)

        _txtIP = AddTextBoxRow("Target IP Address", initIP, "", tblNet)
        _numPort = AddNumericRow("UDP Port", 1, 65535, initPort, "", tblNet)

        ' Dynamically grab the description labels from the table layout
        Dim lblIpDesc As Label = DirectCast(tblNet.GetControlFromPosition(1, tblNet.GetRow(_txtIP) + 1), Label)
        Dim lblPortDesc As Label = DirectCast(tblNet.GetControlFromPosition(1, tblNet.GetRow(_numPort) + 1), Label)

        ' Inline helper to swap the text dynamically
        Dim updateDescriptions = Sub(wledMode As Boolean)
                                     If wledMode Then
                                         lblIpDesc.Text = "IP Address of your WLED controller. (Static IP recommended in router)."
                                         lblPortDesc.Text = "Hardware port. WLED DRGB stream defaults to 21324."
                                     Else
                                         lblIpDesc.Text = "ESP module address. Use '255.255.255.255' to broadcast to network."
                                         lblPortDesc.Text = "Hardware port. Native firmware defaults to 45045."
                                     End If
                                 End Sub

        ' Set the text correctly on initial load
        updateDescriptions(isWled)

        ' Keep track of the dropdown state to save before swapping
        Dim lastSelectedProtocol As String = _cmbProtocol.SelectedItem.ToString()

        AddHandler _cmbProtocol.SelectedIndexChanged, Sub()
                                                          ' 1. Save the currently typed values to memory before switching
                                                          If lastSelectedProtocol = "WLED (DRGB)" Then
                                                              SettingsManager.Current.WledIP = _txtIP.Text
                                                              SettingsManager.Current.WledPort = CInt(_numPort.Value)
                                                          Else
                                                              SettingsManager.Current.TargetIP = _txtIP.Text
                                                              SettingsManager.Current.TargetPort = CInt(_numPort.Value)
                                                          End If

                                                          ' 2. Update tracker
                                                          lastSelectedProtocol = _cmbProtocol.SelectedItem.ToString()
                                                          Dim isNowWled As Boolean = (lastSelectedProtocol = "WLED (DRGB)")

                                                          ' 3. Swap the UI text and values to the newly selected protocol
                                                          updateDescriptions(isNowWled)

                                                          If isNowWled Then
                                                              _txtIP.Text = SettingsManager.Current.WledIP
                                                              _numPort.Value = SettingsManager.Current.WledPort
                                                          Else
                                                              _txtIP.Text = SettingsManager.Current.TargetIP
                                                              _numPort.Value = SettingsManager.Current.TargetPort
                                                          End If
                                                      End Sub

        ' === TAB 3: Hardware Layout ===
        AddTabHeader("Physical LED Layout", _tabLeds)
        Dim tblLeds = CreateTable(_tabLeds)

        ' Group: Main Config
        AddSectionHeader("Strip Configuration (Front-Facing Perspective)", tblLeds)

        _cmbStartEdge = AddComboBoxRow("Starting Edge", "Where does the strip start? (Imagine looking at the FRONT of your monitor).", tblLeds)
        _cmbStartEdge.Items.AddRange(New String() {"Top", "Right", "Bottom", "Left"})
        _cmbStartEdge.SelectedItem = SettingsManager.Current.StartEdge
        If _cmbStartEdge.SelectedIndex = -1 Then _cmbStartEdge.SelectedIndex = 0

        _cmbDirection = AddComboBoxRow("Routing Direction", "Direction from the FRONT of the screen. (⚠️ If you are looking at the BACK of the monitor, select the opposite!)", tblLeds)
        _cmbDirection.Items.AddRange(New String() {"Clockwise", "Counter-Clockwise"})
        _cmbDirection.SelectedItem = SettingsManager.Current.Direction
        If _cmbDirection.SelectedIndex = -1 Then _cmbDirection.SelectedIndex = 0

        _cmbColorOrder = AddComboBoxRow("Color Sequence", "Matches software to your strip's physical wiring.", tblLeds)
        _cmbColorOrder.Items.AddRange(New String() {"RGB", "GRB", "BRG", "BGR", "RBG", "GBR"})
        _cmbColorOrder.SelectedItem = SettingsManager.Current.ColorSequence
        If _cmbColorOrder.SelectedIndex = -1 Then _cmbColorOrder.SelectedIndex = 0

        _numGapStart = AddNumericRow("Start Offset (Blanks)", 0, 100, SettingsManager.Current.BlankStart, "Hidden LEDs between the controller box and the actual screen start.", tblLeds)

        ' Group: Top
        AddSectionHeader("Top Edge", tblLeds)
        _numTop = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.TopLeds, "LEDs tracking the top screen edge.", tblLeds)
        _numGapTop = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterTop, "Dead LEDs in the corner following the Top edge.", tblLeds)

        ' Group: Right
        AddSectionHeader("Right Edge", tblLeds)
        _numRight = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.RightLeds, "LEDs tracking the right screen edge.", tblLeds)
        _numGapRight = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterRight, "Dead LEDs in the corner following the Right edge.", tblLeds)

        ' Group: Bottom
        AddSectionHeader("Bottom Edge", tblLeds)
        _numBottom = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.BottomLeds, "LEDs tracking the bottom screen edge.", tblLeds)
        _numGapBottom = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterBottom, "Dead LEDs in the corner following the Bottom edge.", tblLeds)

        ' Group: Left
        AddSectionHeader("Left Edge", tblLeds)
        _numLeft = AddNumericRow("Active LEDs", 0, 1000, SettingsManager.Current.LeftLeds, "LEDs tracking the left screen edge.", tblLeds)
        _numGapLeft = AddNumericRow("Corner Gap (Blanks)", 0, 50, SettingsManager.Current.BlankAfterLeft, "Dead LEDs in the corner following the Left edge.", tblLeds)

        ' === NEW TAB: Diagnostics ===
        AddTabHeader("System Diagnostics", _tabDiag)
        Dim tblDiag = CreateTable(_tabDiag)

        AddSectionHeader("Hardware Testing", tblDiag)
        _chkTestMode = AddCheckBoxRow("Alignment Test Mode", SettingsManager.Current.TestMode, "Forces LEDs to Red (Top), Green (Bottom), Blue (Left), Purple (Right).", tblDiag)
        _chkDiagSegments = AddCheckBoxRow("Indicate Segments", SettingsManager.Current.DiagSegments, "Sends a purple breathing beacon to the start and end LEDs (2 each) of every screen edge.", tblDiag)
        _chkDiagGaps = AddCheckBoxRow("Indicate Gaps", SettingsManager.Current.DiagGaps, "Lights up all hidden Start Offset and Corner Gap LEDs in steady Red.", tblDiag)
        _chkDiagSweep = AddCheckBoxRow("Sweep Effect", SettingsManager.Current.DiagSweep, "Sweeps Red, Green, and Blue (3 LEDs each) from start to end continuously.", tblDiag)
        _chkDiagBullet = AddCheckBoxRow("Bullet Effect", SettingsManager.Current.DiagBullet, "Rapid white comet effect with a fading tail shooting from start to end.", tblDiag)

        ' --- Mutually Exclusive Logic (Only one test mode at a time) ---
        Dim diagBoxes() As CheckBox = {_chkTestMode, _chkDiagSegments, _chkDiagGaps, _chkDiagSweep, _chkDiagBullet}
        For Each cb In diagBoxes
            ' We use 'Click' instead of 'CheckedChanged' so our code unchecking them doesn't trigger an infinite loop
            AddHandler cb.Click, Sub(sender As Object, e As EventArgs)
                                     Dim clickedBox = DirectCast(sender, CheckBox)
                                     If clickedBox.Checked Then
                                         For Each otherBox In diagBoxes
                                             If otherBox IsNot clickedBox Then otherBox.Checked = False
                                         Next
                                     End If
                                 End Sub
        Next

        ' Add the Pro-Tip label manually to span the table
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

        AddSectionHeader("General", tblGen)
        _chkControlHw = AddCheckBoxRow("Control Hardware", SettingsManager.Current.ControlHardware, "Sends color data over the network. Uncheck to temporarily pause LED lighting.", tblGen)
        _chkStartInTray = AddCheckBoxRow("Start in Tray", SettingsManager.Current.StartInTray, "Launches the application hidden in the system tray instead of showing the mimic screen.", tblGen)
        _chkStartWithWindows = AddCheckBoxRow("Start with Windows", SettingsManager.Current.StartWithWindows, "Automatically launches PixelGlow when you log into your computer.", tblGen)

        AddSectionHeader("Power Management", tblGen)
        _chkFollowPower = AddCheckBoxRow("Follow OS Power State", SettingsManager.Current.FollowPowerState, "Automatically pauses the ambient lighting when Windows goes to sleep or the screen is locked.", tblGen)
        _chkDimOnPower = AddCheckBoxRow("Dim Lights on Lock/Sleep", SettingsManager.Current.DimOnPowerState, "Instead of turning off completely, fade the LEDs to a very dim white glow.", tblGen)

        AddSectionHeader("Tools", tblGen)
        _chkLogging = AddCheckBoxRow("Enable Logging", SettingsManager.Current.LoggingEnabled, "Writes background diagnostic information to a log file for troubleshooting.", tblGen)

        ' Link the Power checkboxes
        _chkDimOnPower.Enabled = _chkFollowPower.Checked
        AddHandler _chkFollowPower.CheckedChanged, Sub() _chkDimOnPower.Enabled = _chkFollowPower.Checked

        ' --- Cross-Tab UI Sync for WLED ---
        ' Grab the description label for the Color Sequence dropdown from the Layout table
        Dim lblColorDesc As Label = DirectCast(tblLeds.GetControlFromPosition(1, tblLeds.GetRow(_cmbColorOrder) + 1), Label)

        Dim syncCrossTabUi = Sub()
                                 Dim isNowWledMode As Boolean = (_cmbProtocol.SelectedItem.ToString() = "WLED (DRGB)")
                                 _cmbColorOrder.Enabled = Not isNowWledMode

                                 If isNowWledMode Then
                                     lblColorDesc.Text = "Disabled. Configure your physical color sequence (GRB, RGB, etc.) inside the WLED web interface."
                                 Else
                                     lblColorDesc.Text = "Matches software to your strip's physical wiring."
                                 End If
                             End Sub

        ' Attach the listener and run it once to set the initial state
        AddHandler _cmbProtocol.SelectedIndexChanged, Sub() syncCrossTabUi()
        syncCrossTabUi()


        ' === TAB 6: Profiles ===
        AddTabHeader("Automated Profiles", _tabProf)
        Dim tblProf = CreateTable(_tabProf)

        ' Profile Header & Buttons
        Dim pnlHeader As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        _cmbProfileSelect = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 200}
        Dim btnAddProf As New Button() With {.Text = "New Profile", .Width = 100, .Height = 26, .FlatStyle = FlatStyle.Flat}
        Dim btnDelProf As New Button() With {.Text = "Delete", .Width = 70, .Height = 26, .FlatStyle = FlatStyle.Flat, .ForeColor = Color.Firebrick}
        pnlHeader.Controls.AddRange({_cmbProfileSelect, btnAddProf, btnDelProf})

        Dim rProf = tblProf.RowCount
        tblProf.RowCount += 1
        tblProf.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblProf.Controls.Add(New Label() With {.Text = "Select Profile:", .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.TopRight, .Padding = New Padding(0, 4, 10, 0)}, 0, rProf)
        tblProf.Controls.Add(pnlHeader, 1, rProf)

        AddSectionHeader("Profile Settings", tblProf)
        _txtProfName = AddTextBoxRow("Profile Name", "", "Display name for this rule.", tblProf)
        _chkProfEnabled = AddCheckBoxRow("Enable Profile", False, "Allow this profile to activate when conditions are met.", tblProf)

        AddSectionHeader("Activation Conditions", tblProf)
        _chkProfTime = AddCheckBoxRow("Time of Day", False, "Activate during a specific time window.", tblProf)

        Dim pnlTime As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True}
        _txtProfStart = New TextBox() With {.Width = 60}
        _txtProfEnd = New TextBox() With {.Width = 60}
        pnlTime.Controls.AddRange({New Label() With {.Text = "Start (HH:MM):", .AutoSize = True, .Padding = New Padding(0, 4, 0, 0)}, _txtProfStart,
                                   New Label() With {.Text = "End (HH:MM):", .AutoSize = True, .Padding = New Padding(10, 4, 0, 0)}, _txtProfEnd})
        rProf = tblProf.RowCount
        tblProf.RowCount += 1
        tblProf.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblProf.Controls.Add(pnlTime, 1, rProf)

        AddSectionHeader("Overrides (Leave blank to use Base Settings)", tblProf)
        _numProfBri = AddNumericRow("Brightness Override (%)", -1, 100, -1, "Set to -1 to ignore and use Base Brightness.", tblProf)

        _cmbProfProto = AddComboBoxRow("Hardware Protocol", "Override receiver type.", tblProf)
        _cmbProfProto.Items.AddRange(New String() {"", "PixelGlow Native", "WLED (DRGB)"})

        _txtProfIP = AddTextBoxRow("Target IP Override", "", "Leave blank to use Base IP.", tblProf)
        _numProfPort = AddNumericRow("UDP Port Override", 0, 65535, 0, "Set to 0 to use Base Port.", tblProf)

        ' Profile Logic Wiring
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
                                                                   ' Save current screen text to the OLD profile slot
                                                                   SaveActiveProfileUI()
                                                                   ' Update our tracker to the NEW profile slot
                                                                   _lastSelectedProfileIndex = _cmbProfileSelect.SelectedIndex
                                                                   ' Load the NEW profile data to the screen
                                                                   LoadSelectedProfile()
                                                               End If
                                                           End Sub
        RefreshProfileList(0)

        ' --- Restore the last selected tab ---
        Select Case SettingsManager.Current.LastSettingsTab
            Case 0 : SidebarClick(_btnGen, EventArgs.Empty)
            Case 1 : SidebarClick(_btnDisp, EventArgs.Empty)
            Case 2 : SidebarClick(_btnNet, EventArgs.Empty)
            Case 3 : SidebarClick(_btnLeds, EventArgs.Empty)
            Case 4 : SidebarClick(_btnEng, EventArgs.Empty)
            Case 5 : SidebarClick(_btnDiag, EventArgs.Empty)
            Case 6 : SidebarClick(_btnProf, EventArgs.Empty)
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
        _tabDisp.Visible = False : _tabNet.Visible = False : _tabLeds.Visible = False : _tabEng.Visible = False : _tabGen.Visible = False : _tabDiag.Visible = False : _tabProf.Visible = False
        _btnDisp.BackColor = Color.FromArgb(30, 30, 35) : _btnNet.BackColor = Color.FromArgb(30, 30, 35)
        _btnLeds.BackColor = Color.FromArgb(30, 30, 35) : _btnEng.BackColor = Color.FromArgb(30, 30, 35) : _btnGen.BackColor = Color.FromArgb(30, 30, 35) : _btnDiag.BackColor = Color.FromArgb(30, 30, 35) : _btnProf.BackColor = Color.FromArgb(30, 30, 35)
        Dim clicked = DirectCast(sender, Button)
        clicked.BackColor = Color.FromArgb(0, 120, 215)

        ' Show the correct tab and update the memory tracker
        If clicked Is _btnGen Then _tabGen.Visible = True : _tabGen.BringToFront() : SettingsManager.Current.LastSettingsTab = 0
        If clicked Is _btnDisp Then _tabDisp.Visible = True : _tabDisp.BringToFront() : SettingsManager.Current.LastSettingsTab = 1
        If clicked Is _btnNet Then _tabNet.Visible = True : _tabNet.BringToFront() : SettingsManager.Current.LastSettingsTab = 2
        If clicked Is _btnLeds Then _tabLeds.Visible = True : _tabLeds.BringToFront() : SettingsManager.Current.LastSettingsTab = 3
        If clicked Is _btnEng Then _tabEng.Visible = True : _tabEng.BringToFront() : SettingsManager.Current.LastSettingsTab = 4
        If clicked Is _btnProf Then _tabProf.Visible = True : _tabProf.BringToFront() : SettingsManager.Current.LastSettingsTab = 6
        If clicked Is _btnDiag Then _tabDiag.Visible = True : _tabDiag.BringToFront() : SettingsManager.Current.LastSettingsTab = 5
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
        ' Commit any profile edits currently on the screen to memory before saving
        SaveActiveProfileUI()

        If _txtIP.Text <> "255.255.255.255" AndAlso Not Regex.IsMatch(_txtIP.Text, ipPattern) Then
            _lblStatus.Text = "Invalid IP Address format."
            Return
        End If
        _lblStatus.Text = ""

        With SettingsManager.Current
            .TargetMonitorIndex = CInt(Math.Max(0, _cmbMonitor.SelectedIndex))
            .ScreenCropPercent = CInt(_numCrop.Value)

            Select Case _cmbGridSize.SelectedIndex
                Case 0 : .GridCols = 16 : .GridRows = 9
                Case 1 : .GridCols = 12 : .GridRows = 6
                Case 2 : .GridCols = 8 : .GridRows = 3
                Case 3 : .GridCols = 4 : .GridRows = 3
            End Select

            .HardwareProtocol = _cmbProtocol.SelectedItem.ToString()
            If .HardwareProtocol = "WLED (DRGB)" Then
                .WledIP = _txtIP.Text
                .WledPort = CInt(_numPort.Value)
            Else
                .TargetIP = _txtIP.Text
                .TargetPort = CInt(_numPort.Value)
            End If

            .MaxBrightness = CInt(_numBright.Value)
            .SaturationBoost = CInt(_numSat.Value)

            .UpdateIntervalMs = CInt(_numInterval.Value)

            .SmoothingSpeed = CInt(_numSmooth.Value)
            .DetectBlackBars = _chkBlackBar.Checked
            .BlackBarThreshold = If(_cmbSensitivity.SelectedIndex = 0, 40, 120)

            .StartEdge = _cmbStartEdge.SelectedItem.ToString()
            .Direction = _cmbDirection.SelectedItem.ToString()
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

        ' FAILSAFE: Revert all hardware testing if the user closed the window WITHOUT clicking OK or Apply.
        If Me.DialogResult <> DialogResult.OK Then
            If SettingsManager.Current.TestMode OrElse SettingsManager.Current.DiagSegments OrElse SettingsManager.Current.DiagGaps OrElse SettingsManager.Current.DiagSweep OrElse SettingsManager.Current.DiagBullet Then
                SettingsManager.Current.TestMode = False
                SettingsManager.Current.DiagSegments = False
                SettingsManager.Current.DiagGaps = False
                SettingsManager.Current.DiagSweep = False
                SettingsManager.Current.DiagBullet = False
                SettingsManager.Save()
                RaiseEvent SettingsApplied(Me, EventArgs.Empty)
            End If
        End If

        MyBase.OnFormClosing(e)
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

        _cmbProfProto.SelectedItem = If(String.IsNullOrEmpty(p.OverrideHardwareProtocol), "", p.OverrideHardwareProtocol)
        If _cmbProfProto.SelectedIndex = -1 Then _cmbProfProto.SelectedIndex = 0
        _txtProfIP.Text = p.OverrideTargetIP
        _numProfPort.Value = p.OverrideTargetPort

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
        _cmbProfProto.SelectedIndex = 0
        _txtProfIP.Text = ""
        _numProfPort.Value = 0
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

        p.OverrideHardwareProtocol = _cmbProfProto.SelectedItem.ToString()
        p.OverrideTargetIP = _txtProfIP.Text
        p.OverrideTargetPort = CInt(_numProfPort.Value)

        ' Update listbox text gracefully without firing index-changed loops
        If _cmbProfileSelect.Items(_lastSelectedProfileIndex).ToString() <> p.ProfileName Then
            _isLoadingProfile = True
            _cmbProfileSelect.Items(_lastSelectedProfileIndex) = p.ProfileName
            _isLoadingProfile = False
        End If
    End Sub

End Class