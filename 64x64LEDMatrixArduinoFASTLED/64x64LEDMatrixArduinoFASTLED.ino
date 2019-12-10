#include "FastLED.h"

//You must include a reference to the FastLED library to use this code. http://fastled.io/

const int width = 32; 
const int height = 16;
const int DATA_PIN = 6;


const int NUM_LEDS = width * height;
int drawIndex = 0;
int x;
int y;
byte pixelType = 0;
char drawIn[5];
char frameIn[NUM_LEDS * 3];


// Define the array of leds
CRGB leds[NUM_LEDS];

void setup() {

  FastLED.addLeds<WS2812B, DATA_PIN, GRB>(leds, NUM_LEDS);

  for (int i = 0; i < NUM_LEDS; i++)
  {
    leds[i] = CRGB::Black;
  }
  FastLED.show();

  Serial.begin(1000000);
}

void loop() {}

void serialEvent() {

switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(width);
      Serial.println(height);
      break;

    case 0x0F: //frame data
      Serial.readBytes((char*)leds, NUM_LEDS * 3);
      FastLED.show();
      Serial.write(0x06); //acknowledge
      break;
  }




/*
  switch (Serial.read()) {
    case 0:
      //draw mode

      Serial.readBytes(drawIn, 5);
      x = (int)drawIn[0];
      y = (int)drawIn[1];
      drawIndex = (int)(y * width) + x;
      leds[drawIndex] = CRGB((byte)drawIn[2], (byte)drawIn[3], (byte)drawIn[4]);
      FastLED.show();
      //delay(1);

      break;

    case 1:

      //clear mode
      for (int i = 0; i < NUM_LEDS; i++)
      {
        leds[i] = CRGB::Black;
      }

      FastLED.show();
      break;

    case 2:

      //frame in mode
      Serial.readBytes((char*)leds, NUM_LEDS * 3);
      FastLED.show();
      break;
  }
  Serial.write(16);

  */
}
