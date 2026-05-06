' Broadcaster.vb
Imports System.Net
Imports System.Net.Sockets
Imports System.Drawing

Public Class Broadcaster
    Private _client As UdpClient
    Private _endpoint As IPEndPoint
    Public Property Config As New LedConfiguration()

    Public Sub New(ip As String, port As Integer)
        _client = New UdpClient() With {.EnableBroadcast = True}
        _endpoint = New IPEndPoint(IPAddress.Parse(ip), port)
    End Sub

    Public Sub SendData(zones(,) As Color)
        Dim leds As New List(Of Color)
        Dim gridW As Integer = zones.GetLength(0)
        Dim gridH As Integer = zones.GetLength(1)

        ' Mapping Sequence with Physical Corner Gaps
        AddBlankLeds(leds, SettingsManager.Current.BlankStart)

        MapSide(leds, zones, Config.TopCount, True, 0, gridW)
        AddBlankLeds(leds, SettingsManager.Current.BlankAfterTop)

        MapSide(leds, zones, Config.RightCount, False, gridW - 1, gridH)
        AddBlankLeds(leds, SettingsManager.Current.BlankAfterRight)

        MapSide(leds, zones, Config.BottomCount, True, gridH - 1, gridW, True)
        AddBlankLeds(leds, SettingsManager.Current.BlankAfterBottom)

        MapSide(leds, zones, Config.LeftCount, False, 0, gridH, True)
        AddBlankLeds(leds, SettingsManager.Current.BlankAfterLeft)

        ' Packet: [Header 2 bytes] + [RGB Data] + [Footer 1 byte]
        Dim payload(leds.Count * 3 + 2) As Byte
        payload(0) = &HFF ' Sync Header
        payload(1) = &HAA

        For i As Integer = 0 To leds.Count - 1
            Dim offset As Integer = 2 + (i * 3)
            Dim c As Color = leds(i)

            ' Dynamically sort the bytes based on your Settings dropdown
            Select Case SettingsManager.Current.ColorSequence
                Case "GRB"
                    payload(offset) = c.G : payload(offset + 1) = c.R : payload(offset + 2) = c.B
                Case "BRG"
                    payload(offset) = c.B : payload(offset + 1) = c.R : payload(offset + 2) = c.G
                Case "BGR"
                    payload(offset) = c.B : payload(offset + 1) = c.G : payload(offset + 2) = c.R
                Case "RBG"
                    payload(offset) = c.R : payload(offset + 1) = c.B : payload(offset + 2) = c.G
                Case "GBR"
                    payload(offset) = c.G : payload(offset + 1) = c.B : payload(offset + 2) = c.R
                Case Else ' RGB (Default fallback)
                    payload(offset) = c.R : payload(offset + 1) = c.G : payload(offset + 2) = c.B
            End Select
        Next
        payload(payload.Length - 1) = &HBB ' Footer

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

    Private Sub AddBlankLeds(list As List(Of Color), count As Integer)
        For i As Integer = 1 To count
            list.Add(Color.Black) ' Forces the LED to remain off
        Next
    End Sub
End Class