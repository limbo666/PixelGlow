# PixelGlow

![Banner](https://github.com/limbo666/PixelGlow/blob/main/PixelGlow_Images_Icons/Im_PixelGlow_Banner.png?raw=true)  
A simple ambient light system for windows.
## Description
### Program Description  
PixelGlow is a lightweight Windows ambient lighting application that runs silently from the system tray, turning your monitor setup into an immersive RGB backlight experience — no gaming ecosystem required, no subscriptions, no bloat.   
At its core, PixelGlow features a high-performance screen color detection engine that continuously samples the edges of any selected monitor and translates the dominant colors into real-time LED lighting commands. A built-in mimic preview window lets you visualize exactly what your LED strips will display before it reaches the hardware — making setup intuitive and precise.  

All control stays on the Windows side. The software handles every decision:  
 - **Per-segment LED strip layout** : Independently configure the number of active LEDs and blank (gap) LEDs for each screen edge: top, bottom, left and right  
 - **Selectable color byte order** : Full support for RGB, GBR, BRG and other WS2812-compatible schemes to match any strip wiring  
 - **Transition smoothing engine** : Introduces configurable delay on color changes, eliminating harsh flicker and producing cinematic, fluid light transitions  
 - **Brightness and color intensity controls** : Fine-tune the output to match your room ambiance without touching the hardware
 - **Test mode**: Easy identify the LEDs side for installation
 - **Selectable window division** : To change the detection mechanism
 - **Black band detection** : Adapt when video displayed is in a different ratio that the screen and produces black areas  
 - **Multi-monitor support** : Select which display drives the ambient engine on any multi-screen setup  
 - **UDP-based communication** : Fast, low-latency command delivery to ESP devices over your local network  

### Hardware Description
The PixelGlow ESP node is a plug-and-play Wi-Fi LED controller built on ESP32 or ESP8266, designed to require zero configuration on the LED side and minimal setup from the user.  
On first power-up, the device launches a captive portal — connect from any phone or laptop, enter your Wi-Fi credentials, and the node is online in under a minute. No serial cables, no flashing tools, no terminal windows.  
Once connected, the ESP node operates purely as an executor — it receives UDP color packets from PixelGlow and drives the WS2812 strip instantly. No logic, no processing, no LED layout knowledge needed on the device side.   All intelligence lives in the Windows application.  
A built-in auto fade-out safety feature smoothly dims and cuts the strip after 2 seconds of signal inactivity — protecting your LEDs from stalled colors during sleep, shutdown or network interruptions, and eliminating the classic "frozen rainbow" problem when the PC goes idle.  
