Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class AmbientEngine
    Private _running As Boolean
    Private _workerThread As Thread
    Private ReadOnly _engineLock As New Object()

    Public Property Broadcaster As Broadcaster
    Public Property CurrentZones As Color(,)

    Private _frameCount As Integer = 0
    Private _activeBounds As Rectangle

    Public ReadOnly Property GridCols As Integer
        Get
            Return Broadcaster.Config.GridCols
        End Get
    End Property

    Public ReadOnly Property GridRows As Integer
        Get
            Return Broadcaster.Config.GridRows
        End Get
    End Property

    Public Sub New()
        SettingsManager.Load()
        Dim actIP As String = If(SettingsManager.Current.HardwareProtocol = "WLED (DRGB)", SettingsManager.Current.WledIP, SettingsManager.Current.TargetIP)
        Dim actPort As Integer = If(SettingsManager.Current.HardwareProtocol = "WLED (DRGB)", SettingsManager.Current.WledPort, SettingsManager.Current.TargetPort)
        Broadcaster = New Broadcaster(actIP, actPort)
        ApplyConfigToBroadcaster()
        UpdateGrid()
    End Sub

    Public Sub ReloadSettings()
        SyncLock _engineLock
            Dim actIP As String = If(SettingsManager.Current.HardwareProtocol = "WLED (DRGB)", SettingsManager.Current.WledIP, SettingsManager.Current.TargetIP)
            Dim actPort As Integer = If(SettingsManager.Current.HardwareProtocol = "WLED (DRGB)", SettingsManager.Current.WledPort, SettingsManager.Current.TargetPort)
            Broadcaster = New Broadcaster(actIP, actPort)
            ApplyConfigToBroadcaster()
            UpdateGrid()
            _activeBounds = Rectangle.Empty

            ' ULTIMATE FAIL-SAFE: If the thread died silently, resuscitate it.
            If _workerThread Is Nothing OrElse Not _workerThread.IsAlive Then
                Logger.Info("Background thread was dead. Restarting engine...")
                Start()
            End If
        End SyncLock
        Logger.Info("Settings reloaded and applied to engine.")
    End Sub

    Private Sub ApplyConfigToBroadcaster()
        Broadcaster.Config.TopCount = SettingsManager.Current.TopLeds
        Broadcaster.Config.BottomCount = SettingsManager.Current.BottomLeds
        Broadcaster.Config.LeftCount = SettingsManager.Current.LeftLeds
        Broadcaster.Config.RightCount = SettingsManager.Current.RightLeds
        Broadcaster.Config.GridCols = SettingsManager.Current.GridCols
        Broadcaster.Config.GridRows = SettingsManager.Current.GridRows
    End Sub

    Public Sub UpdateGrid()
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

    Private Sub LoopEngine()
        Logger.Info("Ambient Engine Thread Started.")
        Dim frameCounter As Integer = 0

        While _running
            Try
                frameCounter += 1
                ' Log a heartbeat every 30 frames (~1 second) so we know if the loop is dead or just returning black
                If frameCounter >= 30 Then
                    Logger.Info("Heartbeat: Engine loop is running fine.")
                    frameCounter = 0
                End If

                SyncLock _engineLock
                    CaptureScreen()

                    ' Only broadcast to the network if hardware control is enabled
                    If SettingsManager.Current.ControlHardware Then
                        Broadcaster.SendData(CurrentZones)
                    End If
                End SyncLock

            Catch ex As Exception
                Logger.Error("CRITICAL LOOP FAILURE", ex)
            Finally
                ' Ensure we never burn out the CPU
                Dim sleepTime As Integer = Math.Max(10, SettingsManager.Current.UpdateIntervalMs)
                Thread.Sleep(sleepTime)
            End Try
        End While
        Logger.Info("Ambient Engine Thread Stopped.")
    End Sub

    Private Sub CaptureScreen()
        ' --- NEW: Hardware Alignment Test Mode Bypass ---
        If SettingsManager.Current.TestMode Then
            ' Paint the perimeter with the test colors
            For x As Integer = 0 To GridCols - 1
                CurrentZones(x, 0) = Color.Red            ' Top
                CurrentZones(x, GridRows - 1) = Color.Green ' Bottom
            Next
            For y As Integer = 0 To GridRows - 1
                CurrentZones(0, y) = Color.Blue           ' Left
                CurrentZones(GridCols - 1, y) = Color.Magenta ' Right (Purple)
            Next
            Return ' Exit immediately. Do not capture the screen or apply smoothing.
        End If
        Dim mIndex As Integer = SettingsManager.Current.TargetMonitorIndex
        If mIndex >= Screen.AllScreens.Length OrElse mIndex < 0 Then mIndex = 0
        Dim monitorBounds = Screen.AllScreens(mIndex).Bounds

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

                Dim zW As Integer = _activeBounds.Width \ GridCols
                Dim zH As Integer = _activeBounds.Height \ GridRows
                Dim smoothFactor As Double = SettingsManager.Current.SmoothingSpeed / 100.0

                For y As Integer = 0 To GridRows - 1
                    For x As Integer = 0 To GridCols - 1
                        Dim startX As Integer = _activeBounds.X + (x * zW)
                        Dim startY As Integer = _activeBounds.Y + (y * zH)

                        Dim targetColor = CalculateAverage(data, startX, startY, zW, zH)

                        ' NEW: Apply brightness and anti-washout saturation BEFORE smoothing
                        targetColor = ApplyColorCorrection(targetColor)

                        CurrentZones(x, y) = LerpColor(CurrentZones(x, y), targetColor, smoothFactor)
                    Next
                Next
                bmp.UnlockBits(data)
            End Using
        Catch ex As Exception
            Logger.Error("CAPTURE SCREEN FAILED", ex)
        End Try
    End Sub

    ' --- PREMIUM FEATURES MATH ---

    Private Function LerpColor(current As Color, target As Color, t As Double) As Color
        If t >= 1.0 Then Return target

        ' FIX: Convert to Integer first so negative numbers don't overflow the Byte limit
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
        Dim maxOffset As Integer = (Math.Abs(stride) * h) - 4 ' Hard memory limit

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

        ' Use the dynamic threshold from settings
        Return (CInt(r) + CInt(g) + CInt(b)) > SettingsManager.Current.BlackBarThreshold
    End Function

    Private Function CalculateAverage(data As BitmapData, startX As Integer, startY As Integer, w As Integer, h As Integer) As Color
        Dim totalR As Double = 0, totalG As Double = 0, totalB As Double = 0
        Dim count As Integer = 0
        Dim stride As Integer = data.Stride
        Dim scan0 As IntPtr = data.Scan0
        Dim maxOffset As Integer = (Math.Abs(stride) * data.Height) - 4 ' Hard memory limit

        For y As Integer = 0 To h - 1 Step 8
            For x As Integer = 0 To w - 1 Step 8
                Dim offset As Integer = ((startY + y) * stride) + ((startX + x) * 4)

                ' MEMORY CLAMP: Prevent Access Violation
                If offset < 0 OrElse offset > maxOffset Then Continue For

                Dim b As Byte = Marshal.ReadByte(scan0, offset)
                Dim g As Byte = Marshal.ReadByte(scan0, offset + 1)
                Dim r As Byte = Marshal.ReadByte(scan0, offset + 2)

                ' FIX: Ensure both sides of multiplication are cast to Double to prevent 255*255 Byte overflow
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
        ' 1. Apply Global Brightness
        Dim bri As Single = SettingsManager.Current.MaxBrightness / 100.0F
        Dim r As Single = c.R * bri
        Dim g As Single = c.G * bri
        Dim b As Single = c.B * bri

        ' 2. Apply Luma-Preserving Saturation Boost
        Dim sat As Single = SettingsManager.Current.SaturationBoost / 100.0F
        If sat <> 1.0F Then
            ' Calculate relative luminance (human eye perception)
            Dim luma As Single = 0.299F * r + 0.587F * g + 0.114F * b

            ' Push colors away from the grayscale center
            r = luma + (r - luma) * sat
            g = luma + (g - luma) * sat
            b = luma + (b - luma) * sat
        End If

        ' 3. Clamp values to prevent byte overflow
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