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
7. [Credits](#credits)
8. [License](#license)

---

## Overview

PixelGlow is a Windows-based ambient lighting application that runs silently from your system tray, turning your monitor setup into an immersive RGB backlight experience. It requires no gaming ecosystem, no subscriptions, and introduces zero bloat to your system.

At its core, PixelGlow features a high-performance screen color detection engine that continuously samples the edges of any selected monitor and translates the dominant colors into real-time LED lighting commands. A built-in mimic preview window lets you visualize exactly what your LED strips will display before the data reaches the hardware, making setup intuitive and precise.

All intelligence lives on the Windows side. The software handles every decision, while the hardware node acts purely as an executor.

---

## Key Features

### Software (Windows Client)
* **Per-Segment LED Strip Layout:** Independently configure the number of active LEDs and blank (gap) LEDs for each screen edge: top, bottom, left, and right.
* **Selectable Color Byte Order:** Full support for RGB, GBR, BRG, BGR, and other WS2812-compatible schemes to perfectly match your specific LED strip wiring.
* **Transition Smoothing Engine:** Introduces a configurable delay on color changes, eliminating harsh flickers and producing fluid, cinematic light transitions.
* **Brightness & Color Intensity Controls:** Fine-tune the power output and color saturation to match your room's ambiance without touching the hardware.
* **Alignment Test Mode:** A diagnostic mode that forces specific colors to specific screen edges (Red for Top, Green for Bottom, Blue for Left, Purple for Right) to easily verify physical LED installation.
* **Selectable Grid Resolution:** Adjust the screen division grid to best match the physical density of your LED strips.
* **Black Bar Detection:** Automatically adapts when displaying letterboxed video content, ignoring the black areas to capture the actual media colors.
* **Multi-Monitor Support:** Select which display drives the ambient engine on any multi-screen setup.
* **UDP-Based Communication:** Fast, low-latency command delivery to ESP devices over your local network.
* **System Tray Integration:** Runs silently in the background with options to start with Windows.

### Hardware (ESP Node)
* **Plug-and-Play Wi-Fi:** Built for ESP32 or ESP8266 microcontrollers.
* **Captive Portal Setup:** On first power-up, the device broadcasts its own Wi-Fi network. Connect from any device to enter your home Wi-Fi credentials—no serial cables or terminal windows required after the initial flash.
* **Zero-Config Execution:** The node operates purely as a receiver. It takes UDP color packets and drives the WS2812 strip instantly. No LED layout logic is required on the device.
* **Auto Fade-Out Safety:** A built-in safety feature smoothly dims and turns off the LED strip after 2 seconds of signal inactivity. This protects LEDs during PC sleep, shutdown, or network drops, eliminating the "frozen rainbow" problem.

---

## Architecture

PixelGlow operates on a decoupled architecture:
1.  **The Analyzer (Windows PC):** Captures the screen, calculates average edge colors, applies smoothing and color correction, and broadcasts a lightweight UDP packet containing the exact RGB values for the entire strip.
2.  **The Executor (ESP Microcontroller):** Listens on a specific UDP port, receives the byte array, and pushes the data directly to the WS2812 LED strip.

---

## Hardware Setup

### Prerequisites
* An ESP32 board or an ESP8266 (e.g., Wemos D1 Mini, NodeMCU).
* A WS2812B (or compatible) addressable LED strip.
* An appropriate 5V power supply for your LED strip.

### Installation Steps
1.  **Flash the Firmware:** Flash the provided PixelGlow firmware to your ESP board using your preferred flashing tool (Arduino IDE, PlatformIO, or ESP Flasher).
2.  **First Boot (Captive Portal):** * Power on the ESP.
    * Using your phone or laptop, search for a new Wi-Fi network named `PixelGlow_Setup` (or `ESP32-Backlight-Setup`, or `ESP8266-Backlight-Setup` depending on firmware defaults).
    * Connect to it. A captive portal page should automatically open.
    * Select your home Wi-Fi network, enter the password, and save.
3.  **Find the IP Address:** Once connected to your home network, check your router's DHCP client list to find the IP address assigned to the ESP node. You will need this for the Windows software.
4.  **Wiring:** Connect the data pin of your LED strip to the designated data pin on the ESP (refer to the firmware configuration), and ensure common grounds are connected.

---

## Software Setup & Usage

### Running PixelGlow
1.  Download the latest release of the PixelGlow Windows application.
2.  Extract the files to a standard directory (e.g., `C:\Program Files\PixelGlow` or your Documents folder).
3.  Run `PixelGlow.exe`.
4.  The application will start. You will see a tray icon (a glowing monitor) appear in your system tray. Double-clicking the tray icon opens the Mimic Window.
5.  Right-click the tray icon and select **Settings** to configure the application.

---

## Configuration Guide

The Settings menu is divided into intuitive tabs:

### General Settings
* **Control Hardware:** Check to send data over the network. Uncheck to pause physical lighting while keeping the software running.
* **Start in Tray / Start with Windows:** Configure startup behavior for seamless daily use.

### Display
* **Target Display:** Choose which monitor the engine analyzes.
* **Grid Resolution:** Set how many zones the screen is divided into. Match this roughly to the number of LEDs you have.
* **Show Detection Grid:** Displays a transparent overlay on the mimic screen to show where colors are being sampled.

### Network
* **Target IP Address:** Enter the IP address of your ESP node (found during Hardware Setup). Use `255.255.255.255` to broadcast to the entire network.
* **UDP Port:** Default is `45045`. Ensure this matches your ESP firmware.

### Hardware Layout
* **Color Sequence:** Adjust this if your colors look swapped (e.g., red shows as green). Common formats are RGB, GRB, or BGR.
* **Start Offset (Blanks):** Number of LEDs hidden between the controller and the start of the screen.
* **Edge Configuration:** For each edge (Top, Right, Bottom, Left), specify the number of **Active LEDs** tracking that edge, and the **Corner Gap (Blanks)** for dead LEDs bending around corners.
* **Alignment Test Mode:** Enable this to force diagnostic colors to the edges. This helps ensure your physical starting point and direction match the software mapping.

### Processing Parameters (Engine)
* **Max Brightness:** Limits the overall power output.
* **Saturation Boost:** Increase this to prevent bright colors from washing out into pure white.
* **Sync Speed (ms):** The delay between screen captures. ~33ms provides approximately 30 frames per second.
* **Temporal Smoothing:** Controls the fade speed between colors. Higher values mean faster, sharper transitions; lower values mean slow, cinematic fades.
* **Auto-Crop Black Bars:** Enable to ignore letterboxing in movies. You can select standard or aggressive sensitivity.


---

## Credits

Nikos Georgousis  
Hand Water Pump  


---

## License

**Apache 2.0 License + Commons Clause**

You may freely use, modify, and distribute this software for non-commercial purposes. Selling or commercializing this software, or its derivatives, for financial gain is strictly prohibited. All forks, distributions, or mentions must explicitly credit the original creator, Nikos Georgousis.
