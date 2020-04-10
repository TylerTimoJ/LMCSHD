#include "FastLED.h"
#define WIDTH 32
#define HEIGHT 16

const int NUM_LEDS = WIDTH * HEIGHT;

CRGB leds[NUM_LEDS];

void setup() {
  FastLED.addLeds<WS2812B, 6, GRB>(leds, NUM_LEDS);
  Serial.begin(4608000);
}

void loop() {}

void serialEvent() {
  switch (Serial.read()) //read header
  {
    case 0x05: //request for matrix definition
      Serial.println(WIDTH);
      Serial.println(HEIGHT);
      break;

    case 0x11: //frame data
      Serial.readBytes((char*)leds, NUM_LEDS * 3);
      FastLED.show();
      Serial.write(0x06); //acknowledge
      break;
  }
}
