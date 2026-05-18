' AppSettings.vb
Public Class AppSettings
    ' Network Settings
    Public Property HardwareProtocol As String = "PixelGlow Native"
    Public Property TargetIP As String = "255.255.255.255"
    Public Property TargetPort As Integer = 45045
    Public Property WledIP As String = "255.255.255.255"
    Public Property WledPort As Integer = 21324

    ' Performance & Display Settings
    Public Property UpdateIntervalMs As Integer = 30
    Public Property LoggingEnabled As Boolean = False
    Public Property StartInTray As Boolean = False
    Public Property StartWithWindows As Boolean = False
    Public Property FollowPowerState As Boolean = True
    Public Property DimOnPowerState As Boolean = False

    Public Property StartEdge As String = "Top"
    Public Property Direction As String = "Clockwise"


    Public Property TargetMonitorIndex As Integer = 0
    Public Property ScreenCropPercent As Integer = 0 ' 0 to 25% edge cropping

    ' --- Premium Processing ---
    Public Property MaxBrightness As Integer = 100   ' 1 to 100%
    Public Property SaturationBoost As Integer = 150 ' 100 = Normal, 150 = Vibrant, 200 = Aggressive

    Public Property SmoothingSpeed As Integer = 30
    Public Property DetectBlackBars As Boolean = False
    Public Property BlackBarThreshold As Integer = 120 ' NEW: 40 = Standard, 120 = Aggressive

    Public Property TestMode As Boolean = False
    Public Property DiagSegments As Boolean = False
    Public Property DiagGaps As Boolean = False
    Public Property DiagSweep As Boolean = False
    Public Property DiagBullet As Boolean = False
    Public Property ShowDetectionGrid As Boolean = False
    Public Property ControlHardware As Boolean = True
    Public Property LastSettingsTab As Integer = 0


    ' Physical LED/Grid Configuration
    Public Property GridCols As Integer = 16
    Public Property GridRows As Integer = 9
    Public Property TopLeds As Integer = 16
    Public Property BottomLeds As Integer = 16
    Public Property LeftLeds As Integer = 9
    Public Property RightLeds As Integer = 9

    Public Property BlankStart As Integer = 0
    Public Property BlankAfterTop As Integer = 0
    Public Property BlankAfterRight As Integer = 0
    Public Property BlankAfterBottom As Integer = 0
    Public Property BlankAfterLeft As Integer = 0

    Public Property ColorSequence As String = "RGB"

    ' --- Dynamic Profiles ---
    Public Property Profiles As New List(Of PixelProfile)()
End Class


' --- NEW: Dynamic Profile System ---
Public Class PixelProfile
    Public Property ProfileName As String = "New Profile"
    Public Property IsEnabled As Boolean = True

    ' --- The Overrides (What changes when this profile is active) ---
    Public Property OverrideHardwareProtocol As String = "" ' Leave blank to keep Base setting
    Public Property OverrideTargetIP As String = ""
    Public Property OverrideTargetPort As Integer = 0
    Public Property OverrideMaxBrightness As Integer = -1 ' -1 means do not override

    ' --- The Conditions (When does this activate?) ---
    ' Condition 1: Time of Day
    Public Property EnableTimeRule As Boolean = False
    Public Property StartTime As String = "22:00"
    Public Property EndTime As String = "07:00"

    ' Condition 2: Active Application (Future-proofing)
    Public Property EnableAppRule As Boolean = False
    Public Property TargetExe As String = "netflix.exe"

    ' Condition 3: Fullscreen (Future-proofing)
    Public Property EnableFullscreenRule As Boolean = False
End Class