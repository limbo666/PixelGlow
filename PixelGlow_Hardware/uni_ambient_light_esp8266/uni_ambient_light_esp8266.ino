/* ==============================================================================
 * ESP8266 Universal Ambient Backlight Firmware
 * 
 * QUICK CONFIGURATION GUIDE:
 * Modify the definitions below to match your specific hardware.
 * 
 * Recommended DATA_PIN for ESP8266 (NodeMCU / Wemos):
 * - Pin 2 (D4)   : BEST. Uses internal UART1 hardware for flicker-free LEDs.
 * - Pin 3 (RX)   : GOOD. Uses DMA hardware, but prevents using standard Serial RX.
 * - Pin 1 / 4    : AVOID. Relies on software bit-banging. Will cause flickering 
 *                  when the WiFi module communicates with the router.
 * 
 * IMPORTANT HARDWARE NOTE: 
 * The ESP8266 disables core interrupts to push WS2812B data. If your strip is 
 * very long (e.g., >300 LEDs), the WiFi might drop under heavy load.
 * ============================================================================== */

// --- Required Libraries ---
#include <ESP8266WiFi.h>   // Core ESP8266 WiFi library
#include <WiFiUdp.h>       // Standard non-blocking UDP library for ESP8266
#include <WiFiManager.h>   // Captive portal library (Install via Library Manager)
#include <FastLED.h>       // LED library (Install via Library Manager)

// --- Hardware & Protocol Settings ---
#define DATA_PIN            2       // GPIO 2 corresponds to pin D4 on NodeMCU/Wemos boards
#define MAX_LEDS            300     // Your exact LED count
#define COLOR_ORDER         RGB     // Color channel order (Check your specific WS2812B strip)
#define CHIPSET             WS2812B // LED chipset protocol
#define UDP_PORT            45045   // Listening port for PC software
#define MAX_MILLIAMPS       2000    // 5V power supply limit in mA (e.g., 2000mA = 2A)

// --- Performance & Timing Constraints ---
#define MAX_FPS             60      // Framerate cap
#define TIMEOUT_MS          3000    // Milliseconds without data before fading out
#define FADE_DURATION_MS    2000    // Milliseconds to complete fade to black
#define BUFFER_SIZE         1024    // Max UDP packet size (1024 bytes safely holds ~340 LEDs)

// --- Globals ---
CRGB leds[MAX_LEDS];
WiFiUDP udp;                        // Standard ESP8266 UDP object
uint8_t packetBuffer[BUFFER_SIZE];  // Global buffer to store incoming network data

unsigned long lastPacketTime = 0;
bool newPacketReceived = false;

// State Machine definitions
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
    bool connected = wm.autoConnect("ESP8266-Backlight-Setup");
    if (!connected) {
        Serial.println("Failed to connect and hit timeout. Rebooting...");
        ESP.restart();
    }
    Serial.print("WiFi Connected. IP: ");
    Serial.println(WiFi.localIP());

    // 3. Initialize UDP Listener
    if (udp.begin(UDP_PORT)) {
        Serial.printf("Listening on UDP port %d\n", UDP_PORT);
    } else {
        Serial.println("Error initializing UDP listener!");
    }
}

void loop() {
    unsigned long now = millis();
    static unsigned long lastFrameTime = 0;

    // --- 1. Non-Blocking UDP Polling ---
    // Unlike the ESP32 Async library, we manually check the UDP buffer every loop.
    // Because there are no delays in this loop, this runs thousands of times a second.
    int packetSize = udp.parsePacket();
    if (packetSize) {
        // Read the packet into our global buffer
        udp.read(packetBuffer, BUFFER_SIZE);

        // Packet Validation: Check length and header (FF AA)
        if (packetSize >= 3 && packetBuffer[0] == 0xFF && packetBuffer[1] == 0xAA) {
            
            // Subtract 3 framing bytes (2 header, 1 footer)
            int ledsToUpdate = (packetSize - 3) / 3; 
            if (ledsToUpdate > MAX_LEDS) ledsToUpdate = MAX_LEDS;

            // Direct mapping with software Gamma Correction
            for (int i = 0; i < ledsToUpdate; i++) {
                int offset = 2 + (i * 3); // Skip the 2-byte header
                leds[i] = CRGB(
                    dim8_video(packetBuffer[offset]),     
                    dim8_video(packetBuffer[offset + 1]), 
                    dim8_video(packetBuffer[offset + 2])  
                );
            }
            lastPacketTime = now;
            newPacketReceived = true;
        }
    }

    // --- 2. Strict Framerate Limiter ---
    if (now - lastFrameTime < (1000 / MAX_FPS)) {
        return; // Exit loop early if we are trying to render too fast
    }
    lastFrameTime = now;

    // --- 3. State Machine ---
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