/* ==============================================================================
 * ESP32 Universal Ambient Backlight Firmware
 * 
 * QUICK CONFIGURATION GUIDE:
 * Modify the definitions below to match your specific hardware.
 * 
 * Recommended DATA_PIN by Board Type:
 * - ESP32-C3 Zero / SuperMini : Pin 10, 8, or 2. (Avoid 11-17: used by SPI flash)
 * - Classic ESP32 (WROOM)     : Pin 2, 4, 13, 16, or 33. (Avoid 6-11: used by SPI flash)
 * - ESP32-S3                  : Pin 5, 6, 7, 15, or 21. 
 * - ESP32-S2                  : Pin 5, 7, 9, or 11.
 * 
 * Note: If using a Classic ESP32 and LEDs flicker, you may need a 5V level shifter.
 * ============================================================================== */

// --- Required Libraries ---
#include <WiFi.h>
#include <AsyncUDP.h>
#include <WiFiManager.h>
#include <FastLED.h>

// --- Hardware & Protocol Settings ---
#define DATA_PIN            2      // CHANGE THIS based on the board guide above
#define MAX_LEDS            300     // Your exact LED count
#define COLOR_ORDER         RGB     // Color channel order (Standard WS2812B uses Green-Red-Blue, but check your strip)
#define CHIPSET             WS2812B // LED chipset protocol
#define UDP_PORT            45045   // Listening port for PC software
#define MAX_MILLIAMPS       2000    // 5V power supply limit in mA (e.g., 2000mA = 2A)

// --- Performance & Timing Constraints ---
#define MAX_FPS             60      // Framerate cap
#define TIMEOUT_MS          3000    // Milliseconds without data before fading out
#define FADE_DURATION_MS    2000    // Milliseconds to complete fade to black

// --- Globals ---
CRGB leds[MAX_LEDS];
AsyncUDP udp;

volatile unsigned long lastPacketTime = 0;
volatile bool newPacketReceived = false;

enum Mode { ACTIVE, TIMEOUT_FADING, IDLE };
Mode currentMode = IDLE;
unsigned long fadeStartTime = 0;
const uint8_t BASE_BRIGHTNESS = 255;

void setup() {
    Serial.begin(115200);
    delay(1000); 

    // 1. Initialize FastLED
    FastLED.addLeds<CHIPSET, DATA_PIN, COLOR_ORDER>(leds, MAX_LEDS)
           .setCorrection(TypicalLEDStrip);
    FastLED.setMaxPowerInVoltsAndMilliamps(5, MAX_MILLIAMPS); 
    FastLED.setBrightness(BASE_BRIGHTNESS);
    FastLED.clear();
    FastLED.show();

    // 2. Initialize WiFiManager
    WiFiManager wm;
    bool connected = wm.autoConnect("ESP32-Backlight-Setup");
    if (!connected) {
        Serial.println("Failed to connect and hit timeout. Rebooting...");
        ESP.restart();
    }
    Serial.print("WiFi Connected. IP: ");
    Serial.println(WiFi.localIP());

    // 3. Initialize AsyncUDP
    if (udp.listen(UDP_PORT)) {
        Serial.printf("Listening on UDP port %d\n", UDP_PORT);
        
        udp.onPacket([](AsyncUDPPacket packet) {
            size_t len = packet.length();
            uint8_t* data = packet.data();

            // 1. Packet Validation: Check header (FF AA)
            if (len < 3 || data[0] != 0xFF || data[1] != 0xAA) {
                return;
            }

            // 2. Safety Guard: Subtract 3 framing bytes (2 header, 1 footer)
            int ledsToUpdate = (len - 3) / 3; 
            if (ledsToUpdate > MAX_LEDS) ledsToUpdate = MAX_LEDS;

            // 3. Direct mapping with software Gamma Correction
            for (int i = 0; i < ledsToUpdate; i++) {
                int offset = 2 + (i * 3); // Skip the 2-byte header
                leds[i] = CRGB(
                    dim8_video(data[offset]),     
                    dim8_video(data[offset + 1]), 
                    dim8_video(data[offset + 2])  
                );
            }

            lastPacketTime = millis();
            newPacketReceived = true;
        });
    } else {
        Serial.println("Error initializing UDP listener!");
    }
}

void loop() {
    unsigned long now = millis();
    static unsigned long lastFrameTime = 0;

    // Strict Framerate Limiter
    if (now - lastFrameTime < (1000 / MAX_FPS)) {
        return;
    }
    lastFrameTime = now;

    // --- State Machine ---
    if (newPacketReceived) {
        currentMode = ACTIVE;
        FastLED.setBrightness(BASE_BRIGHTNESS);
        FastLED.show();
        newPacketReceived = false;
        
    } else if (currentMode == ACTIVE) {
        if (now - lastPacketTime >= TIMEOUT_MS) {
            currentMode = TIMEOUT_FADING;
            fadeStartTime = now;
        }
        
    } else if (currentMode == TIMEOUT_FADING) {
        unsigned long elapsedFade = now - fadeStartTime;
        
        if (elapsedFade >= FADE_DURATION_MS) {
            FastLED.clear();
            FastLED.show();
            currentMode = IDLE;
        } else {
            uint8_t targetBrightness = map(elapsedFade, 0, FADE_DURATION_MS, BASE_BRIGHTNESS, 0);
            FastLED.setBrightness(targetBrightness);
            FastLED.show();
        }
    }
}