' Broadcaster.vb
Imports System.Net
Imports System.Net.Sockets
Imports System.Drawing

Public Class Broadcaster
    Private _client As UdpClient
    Private _endpoint As IPEndPoint
    Public Property Config As New LedConfiguration()

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

    Private _activeProtocol As String

    Public Sub New(ip As String, port As Integer, protocol As String)
        _client = New UdpClient() With {.EnableBroadcast = True}
        _endpoint = New IPEndPoint(IPAddress.Parse(ip), port)
        _activeProtocol = protocol
    End Sub

    Public Sub SendData(zones(,) As Color)
        _animTick += 1
        Dim leds As New List(Of Color)
        Dim gridW As Integer = zones.GetLength(0)
        Dim gridH As Integer = zones.GetLength(1)

        ' Calculate total physical LEDs for the linear animations
        Dim totalLeds As Integer = SettingsManager.Current.BlankStart + Config.TopCount + SettingsManager.Current.BlankAfterTop + Config.RightCount + SettingsManager.Current.BlankAfterRight + Config.BottomCount + SettingsManager.Current.BlankAfterBottom + Config.LeftCount + SettingsManager.Current.BlankAfterLeft

        ' --- LINEAR ANIMATIONS (Bypasses Layout Math) ---
        If SettingsManager.Current.DiagSweep OrElse SettingsManager.Current.DiagBullet Then
            For i As Integer = 0 To totalLeds - 1 : leds.Add(Color.Black) : Next
            If SettingsManager.Current.DiagSweep Then
                ' Sweep: RRRGGGBBB moving linearly (Slower)
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
                ' Bullet: Rapid white comet (Faster, short tail)
                Dim head As Integer = CInt((_animTick * 8) Mod (totalLeds + 5))
            For i As Integer = 0 To 3 ' Only 4 LEDs total
                Dim idx As Integer = head - i
                If idx >= 0 AndAlso idx < totalLeds Then
                    Dim intensity As Integer = Math.Max(0, 255 - (i * 65)) ' Drops fast: 255, 190, 125, 60
                    leds(idx) = Color.FromArgb(intensity, intensity, intensity)
                End If
            Next
        End If

        GoTo BuildPacket
        End If

        ' --- DYNAMIC ROUTING SEQUENCE ---
        Dim cw As Boolean = (SettingsManager.Current.Direction = "Clockwise")
        Dim startIdx As Integer = 0

        Select Case SettingsManager.Current.StartEdge
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

        ' Breathing Math for Segments (Generates a sine wave pulse between 0 and 255)
        Dim breathVal As Integer = CInt((Math.Sin(_animTick * 0.15) * 127) + 128)
        Dim purpleBreath As Color = Color.FromArgb(breathVal, 0, breathVal)

        AddBlankLeds(leds, SettingsManager.Current.BlankStart)

        For Each sideIdx In order
            Select Case sideIdx
                Case 0 ' Top
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To Config.TopCount : leds.Add(Color.Red) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To Config.TopCount : leds.Add(If(i = 1 Or i = Config.TopCount, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To Config.TopCount : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, Config.TopCount, True, 0, gridW, Not cw)
                    End If
                    AddBlankLeds(leds, SettingsManager.Current.BlankAfterTop)

                Case 1 ' Right
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To Config.RightCount : leds.Add(Color.Magenta) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To Config.RightCount : leds.Add(If(i = 1 Or i = Config.RightCount, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To Config.RightCount : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, Config.RightCount, False, gridW - 1, gridH, Not cw)
                    End If
                    AddBlankLeds(leds, SettingsManager.Current.BlankAfterRight)

                Case 2 ' Bottom
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To Config.BottomCount : leds.Add(Color.Green) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To Config.BottomCount : leds.Add(If(i = 1 Or i = Config.BottomCount, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To Config.BottomCount : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, Config.BottomCount, True, gridH - 1, gridW, cw)
                    End If
                    AddBlankLeds(leds, SettingsManager.Current.BlankAfterBottom)

                Case 3 ' Left
                    If SettingsManager.Current.TestMode Then
                        For i As Integer = 1 To Config.LeftCount : leds.Add(Color.Blue) : Next
                    ElseIf SettingsManager.Current.DiagSegments Then
                        For i As Integer = 1 To Config.LeftCount : leds.Add(If(i = 1 Or i = Config.LeftCount, purpleBreath, Color.Black)) : Next
                    ElseIf SettingsManager.Current.DiagGaps Then
                        For i As Integer = 1 To Config.LeftCount : leds.Add(Color.Black) : Next
                    Else
                        MapSide(leds, zones, Config.LeftCount, False, 0, gridH, cw)
                    End If
                    AddBlankLeds(leds, SettingsManager.Current.BlankAfterLeft)
            End Select
        Next

BuildPacket:
        ' --- PACKET CONSTRUCTION ---
        Dim isWled As Boolean = (_activeProtocol = "WLED (DRGB)")
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
            Dim activeSeq As String = If(isWled, "RGB", SettingsManager.Current.ColorSequence)

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

    Private _animTick As Integer = 0 ' Master animation frame counter

    Private Sub AddBlankLeds(leds As List(Of Color), count As Integer)
        ' Inject Red if the Gap Diagnostic is active, otherwise keep them Black
        Dim c As Color = If(SettingsManager.Current.DiagGaps, Color.Red, Color.Black)
        For i As Integer = 1 To count
            leds.Add(c)
        Next
    End Sub



    Public Sub ReleaseHardware()
        Try
            If _activeProtocol = "WLED (DRGB)" Then
                ' Sending a 1-byte packet containing '0' tells WLED to immediately exit real-time mode
                Dim releasePacket() As Byte = {0}
                _client.Send(releasePacket, releasePacket.Length, _endpoint)
            Else
                ' For PixelGlow Native, send a pure black packet to turn off the LEDs
                Dim blackZones(Config.GridCols - 1, Config.GridRows - 1) As Color
                For x = 0 To Config.GridCols - 1
                    For y = 0 To Config.GridRows - 1
                        blackZones(x, y) = Color.Black
                    Next
                Next
                SendData(blackZones)
            End If
        Catch : End Try
    End Sub

    Public Sub SendSolidColor(targetColor As Color, holdState As Boolean)
        Dim totalLeds As Integer = Config.TopCount + Config.RightCount + Config.BottomCount + Config.LeftCount + SettingsManager.Current.BlankStart + SettingsManager.Current.BlankAfterTop + SettingsManager.Current.BlankAfterRight + SettingsManager.Current.BlankAfterBottom + SettingsManager.Current.BlankAfterLeft

        Dim isWled As Boolean = (_activeProtocol = "WLED (DRGB)")
        Dim payload() As Byte
        Dim offset As Integer

        If isWled Then
            ReDim payload((totalLeds * 3) + 1)
            payload(0) = 2 ' DRGB Mode
            ' If holding state (dim), set WLED timeout to 255 (infinite). Otherwise, standard 2 seconds.
            payload(1) = CByte(If(holdState, 255, 2))
            offset = 2
        Else
            ReDim payload((totalLeds * 3) + 2)
            payload(0) = &HFF
            payload(1) = &HAA
            offset = 2
        End If

        Dim activeSeq As String = If(isWled, "RGB", SettingsManager.Current.ColorSequence)

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