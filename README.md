# PixelGlow

![Banner](https://github.com/limbo666/PixelGlow/blob/main/PixelGlow_Images_Icons/Base_Led.png?raw=true)

A high-performance, lightweight Windows ambient lighting system that harmonizes your digital environment with physical light.

## Table of Contents
1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Architecture](#architecture)
4. [Hardware Setup](#hardware-setup)
5. [Software Setup & Usage](#software-setup--usage)
6. [Configuration Guide](#configuration-guide)
7. [Known Limitations](#known-limitations)
8. [Credits](#credits)
9. [License](#license)

---

## Overview

PixelGlow is a Windows-based ambient lighting application that runs silently from your system tray, turning your monitor setup into an immersive RGB backlight experience. It requires no gaming ecosystem, no subscriptions, and introduces zero bloat to your system.

At its core, PixelGlow features a high-performance screen color detection engine that continuously samples the edges of any selected monitor and translates the dominant colors into real-time LED lighting commands. It can drive its own dedicated, zero-setup ESP firmware, or act as a universal ambient engine streaming seamlessly to any existing **WLED** installation.

A built-in mimic preview window lets you visualize exactly what your LED strips will display before the data reaches the hardware. All intelligence lives on the Windows side. The software handles every decision, while the hardware node acts purely as an executor.

---

## Key Features

### Software (Windows Client)
* **Universal WLED Support:** Natively streams to any WLED device using the DRGB Real-Time UDP protocol. Includes a seamless "release" command that instantly returns control to your WLED presets the moment PixelGlow closes.
* **Per-Segment LED Strip Layout:** Independently configure the number of active LEDs and blank (gap) LEDs for each screen edge: top, bottom, left, and right.
* **Selectable Color Byte Order:** Full support for RGB, GBR, BRG, BGR, and other WS2812-compatible schemes to perfectly match your specific LED strip wiring. *(Auto-disabled when using WLED to prevent color scrambling).*
* **Transition Smoothing Engine:** Introduces a configurable delay on color changes, eliminating harsh flickers and producing fluid, cinematic light transitions.
* **Advanced Diagnostic Suite:** Features a dedicated testing tab with built-in animated effects (Sweep, Bullet, Breathing Segments) and Mimic Window hotkeys to effortlessly verify physical LED installation.
* **Brightness & Color Intensity Controls:** Fine-tune the power output and color saturation to match your room's ambiance without touching the hardware.
* **Black Bar Detection:** Automatically adapts when displaying letterboxed video content, ignoring the black areas to capture the actual media colors.
* **Multi-Monitor Support:** Select which display drives the ambient engine on any multi-screen setup.
* **System Tray Integration:** Runs silently in the background with options to start with Windows. Includes a quick-access toggle in the tray menu to instantly enable/disable hardware control.

### Hardware (PixelGlow Native ESP Node)
* **Plug-and-Play Wi-Fi:** Built for ESP32 or ESP8266 microcontrollers.
* **Captive Portal Setup:** On first power-up, the device broadcasts its own Wi-Fi network. Connect from any device to enter your home Wi-Fi credentials—no serial cables required after the initial flash.
* **Zero-Config Execution:** The node operates purely as a receiver. It takes UDP color packets and drives the WS2812 strip instantly.
* **Auto Fade-Out Safety:** A built-in safety feature smoothly dims and turns off the LED strip after 2 seconds of signal inactivity.

---

## Architecture

PixelGlow operates on a decoupled architecture:
1.  **The Analyzer (Windows PC):** Captures the screen, calculates average edge colors, applies smoothing and color correction, calculates the exact physical routing, and broadcasts a lightweight UDP packet.
2.  **The Executor (ESP Microcontroller with dedicated firmware or ESP Microcontroller running WLED):** Listens on a specific UDP port (45045 natively, or 21324 for WLED), receives the byte array, and pushes the data directly to the LED strip.

---

## Hardware Setup

You have two options for the hardware receiver: using the custom PixelGlow firmware, or using standard WLED.

### Option A: PixelGlow Native Firmware (Recommended for dedicated setups)
1.  **Prerequisites:** An ESP32 or ESP8266, a WS2812B LED strip, and a 5V power supply.
2.  **Flash the Firmware:** Flash the provided PixelGlow firmware to your ESP board.
3.  **First Boot (Captive Portal):** Power on the ESP. Search for a new Wi-Fi network named `PixelGlow_Setup` (or `ESP32-Backlight-Setup`). Connect to it, enter your home Wi-Fi credentials, and save.
4.  **Find the IP Address:** Check your router's DHCP client list to find the IP address assigned to the ESP node.

### Option B: WLED Integration
If you already have a WLED strip behind your monitor, no flashing is required!
1.  Open your WLED web interface.
2.  Go to **Config -> LED Preferences** and ensure your total LED count matches the physical length of your strip.
3.  Go to **Config -> Sync Interfaces** and ensure **"Receive UDP realtime"** is checked (enabled by default).
4.  Note the IP address of your WLED controller.

---

## Software Setup & Usage

### Running PixelGlow
1.  Download the latest release of the PixelGlow Windows application.
2.  Extract the files to a standard directory (e.g., `C:\Program Files\PixelGlow`).
3.  Run `PixelGlow.exe`.
4.  The application will start, and a tray icon (a glowing monitor) will appear in your system tray. 
5.  Right-click the tray icon and select **Settings** to configure the application, or use the **Control Hardware** toggle to quickly pause/resume physical lighting.

### 💡 Pro Tip: Mimic Window Hotkeys
Double-clicking the tray icon opens the Mimic Window. While looking at the Mimic Window, **hold CTRL and Double-Click anywhere** to instantly cycle your physical LEDs through solid Red, Green, Blue, and White diagnostic colors. A standard Double-Click exits color mode.

---

## Configuration Guide

The Settings menu is divided into intuitive tabs:

### General Settings
* **Control Hardware:** Check to send data over the network. Uncheck to pause physical lighting while keeping the software running. (Also accessible via the System Tray right-click menu).
* **Start in Tray / Start with Windows:** Configure startup behavior for seamless daily use.

### Display
* **Target Display:** Choose which monitor the engine analyzes.
* **Grid Resolution:** Set how many zones the screen is divided into. Match this roughly to the number of LEDs you have.
* **Show Detection Grid:** Displays a transparent overlay on the mimic screen to show where colors are being sampled.

### Network
* **Hardware Protocol:** Select either **PixelGlow Native** or **WLED (DRGB)**. *Note: PixelGlow automatically saves separate IP addresses and ports for each protocol, allowing you to switch between testing environments instantly.*
* **Target IP Address:** Enter the IP address of your ESP node or WLED controller. Use `255.255.255.255` to broadcast to the entire network.
* **UDP Port:** Default is `45045` for Native, and automatically switches to `21324` for WLED.

### Physical LED Layout
* **Color Sequence:** Adjust this if your colors look swapped (e.g., red shows as green). *Note: This is automatically disabled when using WLED, as WLED handles the hardware color sequence internally.*
* **Start Offset (Blanks):** Number of LEDs hidden between the controller and the start of the screen.
* **Edge Configuration:** For each edge (Top, Right, Bottom, Left), specify the number of **Active LEDs** tracking that edge, and the **Corner Gap (Blanks)** for dead LEDs bending around corners.

### System Diagnostics
A dedicated suite of mutually-exclusive hardware tests that bypass the screen engine to help you verify your physical layout:
* **Alignment Test Mode:** Forces specific colors to specific screen edges (Red Top, Green Bottom, Blue Left, Purple Right).
* **Indicate Segments:** Sends a purple breathing beacon to the exact start and end LEDs of every screen edge.
* **Indicate Gaps:** Lights up all hidden Start Offset and Corner Gap LEDs in steady Red.
* **Sweep Effect:** A linear RGB wave that slowly sweeps the entire length of the strip.
* **Bullet Effect:** A rapid white comet effect with a fading tail shooting down the strip.

### Processing Parameters (Engine)
* **Max Brightness:** Limits the overall power output.
* **Saturation Boost:** Increase this to prevent bright colors from washing out into pure white.
* **Sync Speed (ms):** The delay between screen captures. ~33ms provides approximately 30 frames per second.
* **Temporal Smoothing:** Controls the fade speed between colors. Higher values mean faster, sharper transitions; lower values mean slow, cinematic fades.
* **Auto-Crop Black Bars:** Enable to ignore letterboxing in movies. You can select standard or aggressive sensitivity.

---

## Known Limitations
DRM & Hardware Acceleration (Netflix, Disney+, etc.)

If you attempt to watch DRM-protected content on streaming platforms like Netflix, Disney+, Amazon Prime, or Hulu, you may notice that PixelGlow's Mimic Window shows a completely black screen where the video should be, and your LEDs will turn off. 

**This is not a bug in PixelGlow.** It is an intentional, industry-wide restriction caused by Digital Rights Management (DRM). 

To prevent movie piracy, modern browsers and native streaming apps use **Hardware Acceleration**. This sends the encrypted video stream directly to your graphics card (GPU) to be decoded, completely bypassing the standard Windows desktop environment. Because the video never actually renders on the Windows software layer, screen-capture engines like PixelGlow cannot see it.

### How to Fix It (The Workarounds)

If you want your ambient lighting to react to streaming services, you must force the video to render through software rather than your GPU.

* **Method 1: Disable Hardware Acceleration in your Browser (Recommended)**
  If you use Chrome, Edge, or Brave, go to your browser's **Settings > System**, and toggle **"Use graphics acceleration when available"** to **OFF**. Restart the browser. PixelGlow will now be able to see and react to the video. *(Note: Streaming services may restrict software-decoded video to 1080p).*
* **Method 2: Try Firefox**
  Firefox handles the Windows display pipeline differently than Chromium browsers. Often, simply watching your content in Firefox is enough to allow PixelGlow to capture the colors without changing any settings.
* **Method 3: Avoid Native Windows Apps**
  Dedicated streaming apps downloaded from the Microsoft Store (like the native Netflix app) are completely locked down by the operating system. You cannot disable hardware acceleration in them. You must watch your content in a web browser for PixelGlow to interact with it.

---

## Credits

Nikos Georgousis  
Hand Water Pump  

---

## License

**Apache 2.0 License + Commons Clause**

You may freely use, modify, and distribute this software for non-commercial purposes. Selling or commercializing this software, or its derivatives, for financial gain is strictly prohibited. All forks, distributions, or mentions must explicitly credit the original creator, Nikos Georgousis.
