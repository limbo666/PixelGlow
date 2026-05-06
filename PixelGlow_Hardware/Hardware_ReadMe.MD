# PixelGlow ESP Ambient Backlight Hardware

This project contains production-grade, non-blocking firmware to drive WS2812B ambient monitor backlights via UDP over WiFi. It is heavily optimized to maintain a strict 60fps refresh rate while keeping network listening perfectly uninterrupted.

## How it works
**Flash the firmware**  
**Use Captive Portal to set WiFi**  
**Use PixelGlow software to control**  
-


## Features
* **Zero-Blocking Architecture:** Network listeners and LED rendering run asynchronously.
* **Captive Portal:** Uses WiFiManager. No hardcoded SSIDs or passwords; connect to the `ESP-Backlight-Setup` AP on first boot to configure your network.
* **Smart Timeout:** Smoothly fades the LEDs to black over 2 seconds if the PC stops sending data for 3 seconds.
* **Hardware Safety:** Integrates FastLED power management to cap maximum amperage.
* **Gamma Correction:** Software-level gamma adjustment (`dim8_video`) for accurate PC-to-LED color mapping.

## File Selection
Choose the correct `.ino` file based on your microcontroller:

* **`uni_ambient_light_esp32.ino`**: For all ESP32 variants (Classic, S2, S3, C3, C6). Uses the FreeRTOS-backed `AsyncUDP` library.
* **`uni_ambient_light_esp8266.ino`**: For all ESP8266 variants (NodeMCU, Wemos D1). Uses high-speed polling with the standard `WiFiUDP` library.

## Dependencies
Install the following via the Arduino IDE Library Manager:
* **FastLED** (v3.6.0 or newer recommended)
* **WiFiManager** (by tzapu)

*(Note: The UDP libraries are built natively into the ESP board cores; no external download is required).*

## Configuration & Setup
Before flashing, open the `.ino` file and adjust the **Hardware & Protocol Settings** block at the top of the code to match your physical setup:

1. **`DATA_PIN`**: **CRITICAL.** Read the comments at the top of the script to select a safe pin for your specific board. (e.g., Pin 10 for ESP32-C3, Pin 2 for ESP8266).
2. **`MAX_LEDS`**: Set to the exact number of LEDs on your monitor.
3. **`COLOR_ORDER`**: Typically `RGB` or `GRB` for WS2812B strips.
4. **`MAX_MILLIAMPS`**: Set to match your 5V power supply capacity (e.g., `2000` for a 2A supply).

## Protocol Definition
The firmware listens on **UDP Port 45045**.
It expects a raw byte array formatted as follows:
* **Header:** `0xFF`, `0xAA`
* **Payload:** `[R, G, B]` bytes for LED 0, `[R, G, B]` bytes for LED 1, etc.
* **Footer:** Any 1 byte (ignored by parser).