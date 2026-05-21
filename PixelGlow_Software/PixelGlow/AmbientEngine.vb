Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class AmbientEngine
    Private _running As Boolean
    Private _isSuspended As Boolean = False
    Private _workerThread As Thread
    Private ReadOnly _engineLock As New Object()

    ' State Trackers
    Private _currentProfileName As String = "BASE"
    Private _currentBrightness As Integer = 100
    Public Property MimicOverrideColor As Color = Color.Empty

    Public Property Broadcaster As Broadcaster
    Public Property CurrentZones As Color(,)

    Private _frameCount As Integer = 0
    Private _activeBounds As Rectangle

    Public ReadOnly Property LayoutMode As String
        Get
            Return If(Broadcaster IsNot Nothing AndAlso Broadcaster.ActivePreset IsNot Nothing, If(String.IsNullOrEmpty(Broadcaster.ActivePreset.LayoutMode), "Standard Perimeter", Broadcaster.ActivePreset.LayoutMode), "Standard Perimeter")
        End Get
    End Property

    Public ReadOnly Property LinearZones As Integer
        Get
            Return If(Broadcaster IsNot Nothing AndAlso Broadcaster.ActivePreset IsNot Nothing, Math.Max(1, Broadcaster.ActivePreset.LinearZones), 32)
        End Get
    End Property

    Public ReadOnly Property CaptureThickness As Double
        Get
            Return If(Broadcaster IsNot Nothing AndAlso Broadcaster.ActivePreset IsNot Nothing, Math.Max(1, Broadcaster.ActivePreset.CaptureThickness) / 100.0, 0.2)
        End Get
    End Property

    Public ReadOnly Property GridCols As Integer
        Get
            If LayoutMode = "Horizontal Center (Lightbar)" Then Return LinearZones
            If LayoutMode = "Vertical Center (Towers)" Then Return 1
            Return If(Broadcaster IsNot Nothing AndAlso Broadcaster.ActivePreset IsNot Nothing, Broadcaster.ActivePreset.GridCols, 16)
        End Get
    End Property

    Public ReadOnly Property GridRows As Integer
        Get
            If LayoutMode = "Vertical Center (Towers)" Then Return LinearZones
            If LayoutMode = "Horizontal Center (Lightbar)" Then Return 1
            Return If(Broadcaster IsNot Nothing AndAlso Broadcaster.ActivePreset IsNot Nothing, Broadcaster.ActivePreset.GridRows, 9)
        End Get
    End Property

    ' Helper to safely grab the hardware preset the user currently has selected in the UI
    Public Function GetActivePreset() As HardwarePreset
        If SettingsManager.Current.HardwarePresets Is Nothing OrElse SettingsManager.Current.HardwarePresets.Count = 0 Then
            Return New HardwarePreset() ' Failsafe
        End If
        Dim p = SettingsManager.Current.HardwarePresets.FirstOrDefault(Function(x) x.PresetName = SettingsManager.Current.ActivePresetName)
        Return If(p IsNot Nothing, p, SettingsManager.Current.HardwarePresets(0))
    End Function

    Public Sub New()
        SettingsManager.Load()
        Broadcaster = New Broadcaster(GetActivePreset())
        UpdateGrid()
    End Sub

    Public Sub ReloadSettings()
        SyncLock _engineLock
            Broadcaster = New Broadcaster(GetActivePreset())
            UpdateGrid()
            _activeBounds = Rectangle.Empty

            If _workerThread Is Nothing OrElse Not _workerThread.IsAlive Then
                Logger.Info("Background thread was dead. Restarting engine...")
                Start()
            End If
        End SyncLock
        Logger.Info("Settings reloaded and applied to engine.")
    End Sub

    Public Sub UpdateGrid()
        ' Dynamically builds the exact 1D or 2D array size needed based on layout
        CurrentZones = New Color(GridCols - 1, GridRows - 1) {}
        For y As Integer = 0 To GridRows - 1
            For x As Integer = 0 To GridCols - 1
                CurrentZones(x, y) = Color.Black
            Next
        Next
    End Sub

    Public Sub Start()
        If _running AndAlso _workerThread IsNot Nothing AndAlso _workerThread.IsAlive Then Return
        _running = True
        _workerThread = New Thread(AddressOf LoopEngine) With {.IsBackground = True, .Priority = ThreadPriority.AboveNormal}
        _workerThread.Start()
    End Sub

    Public Sub [Stop]()
        _running = False
    End Sub

    Public Sub Suspend()
        If SettingsManager.Current.FollowPowerState Then _isSuspended = True
    End Sub

    Public Sub [Resume]()
        If SettingsManager.Current.FollowPowerState Then _isSuspended = False
    End Sub

    Private Sub LoopEngine()
        Logger.Info("Ambient Engine Thread Started.")
        Dim frameCounter As Integer = 0

        While _running
            Try
                frameCounter += 1
                If frameCounter >= 30 Then
                    Logger.Info("Heartbeat: Engine loop is running fine.")
                    frameCounter = 0
                End If

                ' --- PROFILE EVALUATOR ---
                SyncLock _engineLock
                    CheckAndApplyProfileSwitches()
                End SyncLock

                ' --- MIMIC HOTKEY BYPASS ---
                If MimicOverrideColor <> Color.Empty Then
                    SyncLock _engineLock
                        If SettingsManager.Current.ControlHardware Then
                            Broadcaster.SendSolidColor(MimicOverrideColor, False)
                        End If
                    End SyncLock
                    Thread.Sleep(33)
                    Continue While ' Skip screen capture
                End If

                ' --- SUSPEND / DIM STATE BYPASS ---
                If _isSuspended Then
                    If SettingsManager.Current.DimOnPowerState Then
                        Dim baseIntensity As Integer = 15
                        If SettingsManager.Current.DimBreathing Then
                            ' Use TickCount for microsecond-smooth time progression independent of system clock
                            Dim timeSec As Double = Environment.TickCount / 1000.0
                            ' Create a 0.0 to 1.0 normalized wave
                            Dim wave As Double = (Math.Sin(timeSec * 0.5) + 1.0) / 2.0
                            ' Square the wave for human-eye Gamma Correction (makes low-light fades look perfectly linear)
                            wave = wave * wave
                            ' Expand the range (3 to 55) for high-resolution integer stepping
                            baseIntensity = CInt((52.0 * wave) + 10.0)
                        End If

                        Dim dimColor As Color
                        Select Case SettingsManager.Current.DimColor
                            Case "Red" : dimColor = Color.FromArgb(baseIntensity, 0, 0)
                            Case "Green" : dimColor = Color.FromArgb(0, baseIntensity, 0)
                            Case "Blue" : dimColor = Color.FromArgb(0, 0, baseIntensity)
                            Case Else : dimColor = Color.FromArgb(baseIntensity, CInt(baseIntensity * 0.85), CInt(baseIntensity * 0.7)) ' Warm Cinematic White
                        End Select

                        SyncLock _engineLock
                            If SettingsManager.Current.ControlHardware Then
                                Broadcaster.SendSolidColor(dimColor, True)
                            End If
                        End SyncLock

                        ' 33ms (30 FPS) refresh for buttery smooth animation, 500ms for static
                        Thread.Sleep(If(SettingsManager.Current.DimBreathing, 33, 500))
                    Else
                        Thread.Sleep(1000)
                    End If
                    Continue While
                End If

                SyncLock _engineLock
                    CaptureScreen()
                    If SettingsManager.Current.ControlHardware Then
                        Broadcaster.SendData(CurrentZones)
                    End If
                End SyncLock

            Catch ex As Exception
                Logger.Error("CRITICAL LOOP FAILURE", ex)
            Finally
                Dim sleepTime As Integer = Math.Max(10, SettingsManager.Current.UpdateIntervalMs)
                Thread.Sleep(sleepTime)
            End Try
        End While
        Logger.Info("Ambient Engine Thread Stopped.")
    End Sub

    ' --- PROFILE EVALUATOR ---
    Private Function GetActiveProfile() As PixelProfile
        If SettingsManager.Current.Profiles Is Nothing OrElse SettingsManager.Current.Profiles.Count = 0 Then Return Nothing

        Dim nowT As TimeSpan = DateTime.Now.TimeOfDay

        For Each p As PixelProfile In SettingsManager.Current.Profiles
            If Not p.IsEnabled Then Continue For

            Dim conditionsMet As Boolean = True

            If p.EnableTimeRule Then
                Dim startT As TimeSpan
                Dim endT As TimeSpan

                If TimeSpan.TryParse(p.StartTime, startT) AndAlso TimeSpan.TryParse(p.EndTime, endT) Then
                    Dim isNightTime As Boolean = False
                    If startT < endT Then
                        isNightTime = (nowT >= startT AndAlso nowT <= endT)
                    Else ' Spans across midnight
                        isNightTime = (nowT >= startT OrElse nowT <= endT)
                    End If

                    If Not isNightTime Then conditionsMet = False
                Else
                    conditionsMet = False
                End If
            End If

            If conditionsMet Then Return p
        Next

        Return Nothing
    End Function

    Private Sub CheckAndApplyProfileSwitches()
        Dim activeProf As PixelProfile = GetActiveProfile()
        Dim targetProfileName As String = If(activeProf IsNot Nothing, activeProf.ProfileName, "BASE")

        If targetProfileName = _currentProfileName Then Return

        Logger.Info($"Profile Switch Detected: Switching from '{_currentProfileName}' to '{targetProfileName}'")
        _currentProfileName = targetProfileName

        ' 1. Apply Brightness
        _currentBrightness = If(activeProf IsNot Nothing AndAlso activeProf.OverrideMaxBrightness <> -1,
                                activeProf.OverrideMaxBrightness,
                                SettingsManager.Current.MaxBrightness)

        ' 2. Apply Hardware Preset Switch
        Dim targetPreset As HardwarePreset = GetActivePreset() ' Start with base UI preset

        If activeProf IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(activeProf.OverridePresetName) Then
            Dim overrideP = SettingsManager.Current.HardwarePresets.FirstOrDefault(Function(p) p.PresetName = activeProf.OverridePresetName)
            If overrideP IsNot Nothing Then targetPreset = overrideP
        End If

        ' If the preset IP, Protocol, or Grid Size is different, reboot the Broadcaster.
        Dim requiresSwap As Boolean = False
        If Broadcaster IsNot Nothing Then
            If Broadcaster.ActivePreset.PresetName <> targetPreset.PresetName OrElse Broadcaster.EndpointIP <> targetPreset.IP OrElse Broadcaster.EndpointPort <> targetPreset.Port Then
                requiresSwap = True
            End If
        End If

        If requiresSwap Then
            Logger.Info($"Rebooting Broadcaster for Hardware Preset Switch: {targetPreset.PresetName} ({targetPreset.IP}:{targetPreset.Port})")
            Broadcaster.ReleaseHardware()
            Broadcaster = New Broadcaster(targetPreset)
            UpdateGrid() ' Rebuild memory grid in case the new preset has different LED zone counts
        End If
    End Sub

    Private Sub CaptureScreen()
        If SettingsManager.Current.TestMode Then
            For x As Integer = 0 To GridCols - 1
                CurrentZones(x, 0) = Color.Red
                CurrentZones(x, GridRows - 1) = Color.Green
            Next
            For y As Integer = 0 To GridRows - 1
                CurrentZones(0, y) = Color.Blue
                CurrentZones(GridCols - 1, y) = Color.Magenta
            Next
            Return
        End If

        Dim mIndex As Integer = SettingsManager.Current.TargetMonitorIndex
        If mIndex >= Screen.AllScreens.Length OrElse mIndex < 0 Then mIndex = 0
        Dim rawBounds = Screen.AllScreens(mIndex).Bounds

        Dim cropPct As Double = SettingsManager.Current.ScreenCropPercent / 100.0
        Dim cropX As Integer = CInt(rawBounds.Width * cropPct)
        Dim cropY As Integer = CInt(rawBounds.Height * cropPct)

        Dim monitorBounds As New Rectangle(
            rawBounds.X + cropX,
            rawBounds.Y + cropY,
            rawBounds.Width - (cropX * 2),
            rawBounds.Height - (cropY * 2)
        )

        Try
            Using bmp As New Bitmap(monitorBounds.Width, monitorBounds.Height)
                Using g = Graphics.FromImage(bmp)
                    g.CopyFromScreen(monitorBounds.Location, Point.Empty, monitorBounds.Size)
                End Using

                Dim data = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)

                If SettingsManager.Current.DetectBlackBars Then
                    _frameCount += 1
                    If _frameCount > 60 Then
                        _frameCount = 0
                        RecalculateBounds(data, bmp.Width, bmp.Height)
                    End If
                Else
                    _activeBounds = New Rectangle(0, 0, bmp.Width, bmp.Height)
                End If

                If _activeBounds = Rectangle.Empty OrElse _activeBounds.Width <= 0 OrElse _activeBounds.Height <= 0 Then
                    _activeBounds = New Rectangle(0, 0, bmp.Width, bmp.Height)
                End If

                Dim smoothFactor As Double = SettingsManager.Current.SmoothingSpeed / 100.0

                If LayoutMode = "Horizontal Center (Lightbar)" Then
                    Dim zW As Integer = _activeBounds.Width \ LinearZones
                    Dim zH As Integer = CInt(_activeBounds.Height * CaptureThickness)
                    Dim startY As Integer = _activeBounds.Y + (_activeBounds.Height \ 2) - (zH \ 2)

                    For x As Integer = 0 To LinearZones - 1
                        Dim startX As Integer = _activeBounds.X + (x * zW)
                        Dim targetColor = CalculateAverage(data, startX, startY, zW, zH)
                        targetColor = ApplyColorCorrection(targetColor)
                        CurrentZones(x, 0) = LerpColor(CurrentZones(x, 0), targetColor, smoothFactor)
                    Next

                ElseIf LayoutMode = "Vertical Center (Towers)" Then
                    Dim zH As Integer = _activeBounds.Height \ LinearZones
                    Dim zW As Integer = CInt(_activeBounds.Width * CaptureThickness)
                    Dim startX As Integer = _activeBounds.X + (_activeBounds.Width \ 2) - (zW \ 2)

                    For y As Integer = 0 To LinearZones - 1
                        Dim startY As Integer = _activeBounds.Y + (y * zH)
                        Dim targetColor = CalculateAverage(data, startX, startY, zW, zH)
                        targetColor = ApplyColorCorrection(targetColor)
                        CurrentZones(0, y) = LerpColor(CurrentZones(0, y), targetColor, smoothFactor)
                    Next

                Else ' Standard Perimeter
                    Dim zW As Integer = _activeBounds.Width \ GridCols
                    Dim zH As Integer = _activeBounds.Height \ GridRows

                    For y As Integer = 0 To GridRows - 1
                        For x As Integer = 0 To GridCols - 1
                            Dim startX As Integer = _activeBounds.X + (x * zW)
                            Dim startY As Integer = _activeBounds.Y + (y * zH)

                            Dim targetColor = CalculateAverage(data, startX, startY, zW, zH)
                            targetColor = ApplyColorCorrection(targetColor)

                            CurrentZones(x, y) = LerpColor(CurrentZones(x, y), targetColor, smoothFactor)
                        Next
                    Next
                End If
                bmp.UnlockBits(data)
            End Using
        Catch ex As Exception
            Logger.Error("CAPTURE SCREEN FAILED", ex)
        End Try
    End Sub

    ' --- PREMIUM FEATURES MATH ---
    Private Function LerpColor(current As Color, target As Color, t As Double) As Color
        If t >= 1.0 Then Return target

        Dim diffR As Integer = CInt(target.R) - CInt(current.R)
        Dim diffG As Integer = CInt(target.G) - CInt(current.G)
        Dim diffB As Integer = CInt(target.B) - CInt(current.B)

        If Math.Abs(diffR) <= 3 AndAlso Math.Abs(diffG) <= 3 AndAlso Math.Abs(diffB) <= 3 Then
            Return target
        End If

        Dim r As Integer = CInt(current.R) + CInt(diffR * t)
        Dim g As Integer = CInt(current.G) + CInt(diffG * t)
        Dim b As Integer = CInt(current.B) + CInt(diffB * t)

        r = Math.Max(0, Math.Min(255, r))
        g = Math.Max(0, Math.Min(255, g))
        b = Math.Max(0, Math.Min(255, b))

        Return Color.FromArgb(255, r, g, b)
    End Function

    Private Sub RecalculateBounds(data As BitmapData, w As Integer, h As Integer)
        Dim topOffset As Integer = 0
        Dim leftOffset As Integer = 0
        Dim stride As Integer = data.Stride
        Dim scan0 As IntPtr = data.Scan0
        Dim maxOffset As Integer = (Math.Abs(stride) * h) - 4
        Dim midX As Integer = w \ 2
        Dim midY As Integer = h \ 2

        For y As Integer = 0 To midY Step 10
            If IsPixelBright(scan0, stride, maxOffset, midX, y) Then : topOffset = y : Exit For : End If
        Next

        For x As Integer = 0 To midX Step 10
            If IsPixelBright(scan0, stride, maxOffset, x, midY) Then : leftOffset = x : Exit For : End If
        Next

        If topOffset > (h * 0.25) Then topOffset = 0
        If leftOffset > (w * 0.25) Then leftOffset = 0

        If topOffset > 20 OrElse leftOffset > 20 Then
            _activeBounds = New Rectangle(leftOffset, topOffset, w - (leftOffset * 2), h - (topOffset * 2))
        Else
            _activeBounds = New Rectangle(0, 0, w, h)
        End If
    End Sub

    Private Function IsPixelBright(scan0 As IntPtr, stride As Integer, maxOffset As Integer, x As Integer, y As Integer) As Boolean
        Dim offset As Integer = (y * stride) + (x * 4)
        If offset < 0 OrElse offset > maxOffset Then Return False
        Dim b As Byte = Marshal.ReadByte(scan0, offset)
        Dim g As Byte = Marshal.ReadByte(scan0, offset + 1)
        Dim r As Byte = Marshal.ReadByte(scan0, offset + 2)
        Return (CInt(r) + CInt(g) + CInt(b)) > SettingsManager.Current.BlackBarThreshold
    End Function

    Private Function CalculateAverage(data As BitmapData, startX As Integer, startY As Integer, w As Integer, h As Integer) As Color
        Dim totalR As Double = 0, totalG As Double = 0, totalB As Double = 0
        Dim count As Integer = 0
        Dim stride As Integer = data.Stride
        Dim scan0 As IntPtr = data.Scan0
        Dim maxOffset As Integer = (Math.Abs(stride) * data.Height) - 4

        For y As Integer = 0 To h - 1 Step 8
            For x As Integer = 0 To w - 1 Step 8
                Dim offset As Integer = ((startY + y) * stride) + ((startX + x) * 4)
                If offset < 0 OrElse offset > maxOffset Then Continue For

                Dim b As Byte = Marshal.ReadByte(scan0, offset)
                Dim g As Byte = Marshal.ReadByte(scan0, offset + 1)
                Dim r As Byte = Marshal.ReadByte(scan0, offset + 2)

                totalB += (CDbl(b) * CDbl(b))
                totalG += (CDbl(g) * CDbl(g))
                totalR += (CDbl(r) * CDbl(r))
                count += 1
            Next
        Next

        If count = 0 Then Return Color.Black
        Dim avgR As Integer = CInt(Math.Sqrt(totalR / count))
        Dim avgG As Integer = CInt(Math.Sqrt(totalG / count))
        Dim avgB As Integer = CInt(Math.Sqrt(totalB / count))

        If avgR < 20 AndAlso avgG < 20 AndAlso avgB < 20 Then Return Color.Black
        Return Color.FromArgb(255, avgR, avgG, avgB)
    End Function

    Private Function ApplyColorCorrection(c As Color) As Color
        Dim bri As Single = _currentBrightness / 100.0F
        Dim r As Single = c.R * bri
        Dim g As Single = c.G * bri
        Dim b As Single = c.B * bri

        Dim sat As Single = SettingsManager.Current.SaturationBoost / 100.0F
        If sat <> 1.0F Then
            Dim luma As Single = 0.299F * r + 0.587F * g + 0.114F * b
            r = luma + (r - luma) * sat
            g = luma + (g - luma) * sat
            b = luma + (b - luma) * sat
        End If

        r = Math.Max(0, Math.Min(255, r))
        g = Math.Max(0, Math.Min(255, g))
        b = Math.Max(0, Math.Min(255, b))

        Return Color.FromArgb(255, CInt(r), CInt(g), CInt(b))
    End Function

    Public Sub ReleaseAndStop()
        _running = False
        If Broadcaster IsNot Nothing Then
            Broadcaster.ReleaseHardware()
        End If
    End Sub
End Class