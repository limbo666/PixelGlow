Imports System.Net
Imports System.Net.Sockets
Imports System.Drawing

Public Class Broadcaster
    Private _client As UdpClient
    Private _endpoint As IPEndPoint

    ' The Broadcaster now natively holds the entire physical layout of the target hardware
    Public Property ActivePreset As HardwarePreset

    Public ReadOnly Property EndpointIP As String
        Get
            Return If(_endpoint IsNot Nothing, _endpoint.Address.ToString(), "")
        End Get
    End Property

    Public ReadOnly Property EndpointPort As Integer
        Get
            Return If(_endpoint IsNot Nothing, _endpoint.Port, 0)
        End Get
    End Property

    Public Sub New(preset As HardwarePreset)
        ActivePreset = preset
        _client = New UdpClient() With {.EnableBroadcast = True}

        ' Failsafe against empty IPs crashing the UdpClient
        Dim safeIp As String = If(String.IsNullOrWhiteSpace(preset.IP), "255.255.255.255", preset.IP)
        Dim safePort As Integer = If(preset.Port <= 0, 45045, preset.Port)

        Try
            _endpoint = New IPEndPoint(IPAddress.Parse(safeIp), safePort)
        Catch ex As Exception
            _endpoint = New IPEndPoint(IPAddress.Parse("255.255.255.255"), safePort)
        End Try
    End Sub

    Public Sub SendData(zones(,) As Color)
        _animTick += 1
        Dim leds As New List(Of Color)
        Dim gridW As Integer = zones.GetLength(0)
        Dim gridH As Integer = zones.GetLength(1)

        ' Calculate total physical LEDs from the active preset
        Dim totalLeds As Integer
        If ActivePreset.LayoutMode = "Standard Perimeter" OrElse String.IsNullOrEmpty(ActivePreset.LayoutMode) Then
            totalLeds = ActivePreset.BlankStart + ActivePreset.TopLeds + ActivePreset.BlankAfterTop + ActivePreset.RightLeds + ActivePreset.BlankAfterRight + ActivePreset.BottomLeds + ActivePreset.BlankAfterBottom + ActivePreset.LeftLeds + ActivePreset.BlankAfterLeft
        Else
            totalLeds = ActivePreset.BlankStart + ActivePreset.TopLeds ' TopLeds is repurposed as the total linear count
        End If

        ' --- LINEAR ANIMATIONS (Bypasses Layout Math) ---
        If SettingsManager.Current.DiagSweep OrElse SettingsManager.Current.DiagBullet Then
            For i As Integer = 0 To totalLeds - 1 : leds.Add(Color.Black) : Next
            If SettingsManager.Current.DiagSweep Then
                Dim head As Integer = CInt((_animTick * 0.7) Mod (totalLeds + 10))
                For i As Integer = 0 To 8
                    Dim idx As Integer = head - i
                    If idx >= 0 AndAlso idx < totalLeds Then
                        If i < 3 Then
                            leds(idx) = Color.Red
                        ElseIf i < 6 Then
                            leds(idx) = Color.Green
                        Else
                            leds(idx) = Color.Blue
                        End If
                    End If
                Next
            ElseIf SettingsManager.Current.DiagBullet Then
                Dim head As Integer = CInt((_animTick * 8) Mod (totalLeds + 5))
                For i As Integer = 0 To 3
                    Dim idx As Integer = head - i
                    If idx >= 0 AndAlso idx < totalLeds Then
                        Dim intensity As Integer = Math.Max(0, 255 - (i * 65))
                        leds(idx) = Color.FromArgb(intensity, intensity, intensity)
                    End If
                Next
            End If

            GoTo BuildPacket
        End If

        ' --- LINEAR ROUTING SEQUENCE (Lightbars & Towers) ---
        If ActivePreset.LayoutMode = "Horizontal Center (Lightbar)" OrElse ActivePreset.LayoutMode = "Vertical Center (Towers)" Then
            Dim reverse As Boolean = (ActivePreset.Direction = "Right-to-Left" OrElse ActivePreset.Direction = "Bottom-to-Top")
            Dim linearCount As Integer = Math.Max(1, ActivePreset.LinearZones)
            Dim physicalCount As Integer = Math.Max(1, ActivePreset.TopLeds)

            AddBlankLeds(leds, ActivePreset.BlankStart)

            For i As Integer = 0 To physicalCount - 1
                ' Intelligently map physical LEDs to detection slices
                Dim zoneIdx As Integer = CInt(Math.Floor((i / physicalCount) * linearCount))
                If zoneIdx >= linearCount Then zoneIdx = linearCount - 1
                If reverse Then zoneIdx = (linearCount - 1) - zoneIdx

                If SettingsManager.Current.TestMode Then
                    leds.Add(If(i < physicalCount / 2, Color.Red, Color.Blue))
                ElseIf SettingsManager.Current.DiagGaps Then
                    leds.Add(Color.Black)
                Else
                    If ActivePreset.LayoutMode = "Vertical Center (Towers)" Then
                        leds.Add(zones(0, zoneIdx))
                    Else
                        leds.Add(zones(zoneIdx, 0))
                    End If
                End If
            Next

            GoTo BuildPacket
        End If

        ' --- STANDARD PERIMETER ROUTING SEQUENCE ---
        Dim cw As Boolean = (ActivePreset.Direction = "Clockwise")
        Dim startIdx As Integer = 0

        Select Case ActivePreset.StartEdge
            Case "Right" : startIdx = 1
            Case "Bottom" : startIdx = 2
            Case "Left" : startIdx = 3
            Case Else : startIdx = 0 ' Top
        End Select

        Dim order(3) As Integer
        For i As Integer = 0 To 3
            If cw Then
                order(i) = (startIdx + i) Mod 4
            Else
                order(i) = (startIdx - i + 4) Mod 4
            End If
        Next

        Dim breathVal As Integer = CInt((Math.Sin(_animTick * 0.15) * 127) + 128)
        Dim purpleBreath As Color = Color.FromArgb(breathVal, 0, breathVal)

        AddBlankLeds(leds, ActivePreset.BlankStart)

        For Each sideIdx In order
            Select Case sideIdx
                Case 0 ' Top
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To ActivePreset.TopLeds : leds.Add(Color.Red) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To ActivePreset.TopLeds : leds.Add(If(i = 1 Or i = ActivePreset.TopLeds, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To ActivePreset.TopLeds : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, ActivePreset.TopLeds, True, 0, gridW, Not cw)
                    End If
                    AddBlankLeds(leds, ActivePreset.BlankAfterTop)

                Case 1 ' Right
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To ActivePreset.RightLeds : leds.Add(Color.Magenta) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To ActivePreset.RightLeds : leds.Add(If(i = 1 Or i = ActivePreset.RightLeds, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To ActivePreset.RightLeds : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, ActivePreset.RightLeds, False, gridW - 1, gridH, Not cw)
                    End If
                    AddBlankLeds(leds, ActivePreset.BlankAfterRight)

                Case 2 ' Bottom
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To ActivePreset.BottomLeds : leds.Add(Color.Green) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To ActivePreset.BottomLeds : leds.Add(If(i = 1 Or i = ActivePreset.BottomLeds, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To ActivePreset.BottomLeds : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, ActivePreset.BottomLeds, True, gridH - 1, gridW, cw)
                    End If
                    AddBlankLeds(leds, ActivePreset.BlankAfterBottom)

                Case 3 ' Left
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To ActivePreset.LeftLeds : leds.Add(Color.Blue) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To ActivePreset.LeftLeds : leds.Add(If(i = 1 Or i = ActivePreset.LeftLeds, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To ActivePreset.LeftLeds : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, ActivePreset.LeftLeds, False, 0, gridH, cw)
                    End If
                    AddBlankLeds(leds, ActivePreset.BlankAfterLeft)
            End Select
        Next

BuildPacket:
        ' --- PACKET CONSTRUCTION ---
        Dim isWled As Boolean = (ActivePreset.Protocol = "WLED (DRGB)")
        Dim payload() As Byte
        Dim offset As Integer

        If isWled Then
            ReDim payload(leds.Count * 3 + 1)
            payload(0) = 2 ' DRGB Mode
            payload(1) = 2 ' 2 Second Timeout
            offset = 2
        Else
            ReDim payload(leds.Count * 3 + 2)
            payload(0) = &HFF
            payload(1) = &HAA
            offset = 2
        End If

        For i As Integer = 0 To leds.Count - 1
            Dim c As Color = leds(i)
            Dim idx As Integer = offset + (i * 3)
            Dim activeSeq As String = If(isWled, "RGB", ActivePreset.ColorSequence)

            Select Case activeSeq
                Case "GRB" : payload(idx) = c.G : payload(idx + 1) = c.R : payload(idx + 2) = c.B
                Case "BRG" : payload(idx) = c.B : payload(idx + 1) = c.R : payload(idx + 2) = c.G
                Case "BGR" : payload(idx) = c.B : payload(idx + 1) = c.G : payload(idx + 2) = c.R
                Case "RBG" : payload(idx) = c.R : payload(idx + 1) = c.B : payload(idx + 2) = c.G
                Case "GBR" : payload(idx) = c.G : payload(idx + 1) = c.B : payload(idx + 2) = c.R
                Case Else : payload(idx) = c.R : payload(idx + 1) = c.G : payload(idx + 2) = c.B
            End Select
        Next

        If Not isWled Then payload(payload.Length - 1) = &HBB

        Try
            _client.Send(payload, payload.Length, _endpoint)
        Catch : End Try
    End Sub

    Private Sub MapSide(list As List(Of Color), zones(,) As Color, physicalCount As Integer, isHorizontal As Boolean, fixedIndex As Integer, gridSize As Integer, Optional reverse As Boolean = False)
        For i As Integer = 0 To physicalCount - 1
            Dim idx As Integer = CInt(Math.Floor((i / physicalCount) * gridSize))
            If idx >= gridSize Then idx = gridSize - 1
            If reverse Then idx = (gridSize - 1) - idx

            If isHorizontal Then
                list.Add(zones(idx, fixedIndex))
            Else
                list.Add(zones(fixedIndex, idx))
            End If
        Next
    End Sub

    Private _animTick As Integer = 0

    Private Sub AddBlankLeds(leds As List(Of Color), count As Integer)
        Dim c As Color = If(SettingsManager.Current.DiagGaps, Color.Red, Color.Black)
        For i As Integer = 1 To count
            leds.Add(c)
        Next
    End Sub

    Public Sub ReleaseHardware()
        Try
            If ActivePreset.Protocol = "WLED (DRGB)" Then
                Dim releasePacket() As Byte = {0}
                _client.Send(releasePacket, releasePacket.Length, _endpoint)
            Else
                Dim blackZones(ActivePreset.GridCols - 1, ActivePreset.GridRows - 1) As Color
                For x = 0 To ActivePreset.GridCols - 1
                    For y = 0 To ActivePreset.GridRows - 1
                        blackZones(x, y) = Color.Black
                    Next
                Next
                SendData(blackZones)
            End If
        Catch : End Try
    End Sub

    Public Sub SendSolidColor(targetColor As Color, holdState As Boolean)
        Dim totalLeds As Integer = ActivePreset.TopLeds + ActivePreset.RightLeds + ActivePreset.BottomLeds + ActivePreset.LeftLeds + ActivePreset.BlankStart + ActivePreset.BlankAfterTop + ActivePreset.BlankAfterRight + ActivePreset.BlankAfterBottom + ActivePreset.BlankAfterLeft

        Dim isWled As Boolean = (ActivePreset.Protocol = "WLED (DRGB)")
        Dim payload() As Byte
        Dim offset As Integer

        If isWled Then
            ReDim payload((totalLeds * 3) + 1)
            payload(0) = 2 ' DRGB Mode
            payload(1) = CByte(If(holdState, 255, 2))
            offset = 2
        Else
            ReDim payload((totalLeds * 3) + 2)
            payload(0) = &HFF
            payload(1) = &HAA
            offset = 2
        End If

        Dim activeSeq As String = If(isWled, "RGB", ActivePreset.ColorSequence)

        For i As Integer = 0 To totalLeds - 1
            Dim idx As Integer = offset + (i * 3)
            Select Case activeSeq
                Case "GRB" : payload(idx) = targetColor.G : payload(idx + 1) = targetColor.R : payload(idx + 2) = targetColor.B
                Case "BRG" : payload(idx) = targetColor.B : payload(idx + 1) = targetColor.R : payload(idx + 2) = targetColor.G
                Case "BGR" : payload(idx) = targetColor.B : payload(idx + 1) = targetColor.G : payload(idx + 2) = targetColor.R
                Case "RBG" : payload(idx) = targetColor.R : payload(idx + 1) = targetColor.B : payload(idx + 2) = targetColor.G
                Case "GBR" : payload(idx) = targetColor.G : payload(idx + 1) = targetColor.B : payload(idx + 2) = targetColor.R
                Case Else : payload(idx) = targetColor.R : payload(idx + 1) = targetColor.G : payload(idx + 2) = targetColor.B
            End Select
        Next

        If Not isWled Then payload(payload.Length - 1) = &HBB

        Try
            _client.Send(payload, payload.Length, _endpoint)
        Catch : End Try
    End Sub
End Class