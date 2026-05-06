' AppSettings.vb
Public Class AppSettings
    ' Network Settings
    Public Property TargetIP As String = "255.255.255.255"
    Public Property TargetPort As Integer = 45045

    ' Performance & Display Settings
    Public Property UpdateIntervalMs As Integer = 30
    Public Property LoggingEnabled As Boolean = False
    Public Property StartInTray As Boolean = False
    Public Property StartWithWindows As Boolean = False

    Public Property TargetMonitorIndex As Integer = 0

    ' --- Premium Processing ---
    Public Property MaxBrightness As Integer = 100   ' 1 to 100%
    Public Property SaturationBoost As Integer = 150 ' 100 = Normal, 150 = Vibrant, 200 = Aggressive

    Public Property SmoothingSpeed As Integer = 30
    Public Property DetectBlackBars As Boolean = False
    Public Property BlackBarThreshold As Integer = 120 ' NEW: 40 = Standard, 120 = Aggressive

    Public Property TestMode As Boolean = False
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
End Class