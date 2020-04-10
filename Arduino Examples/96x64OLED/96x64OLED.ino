#include <Adafruit_SSD1331.h>
#include <SPI.h>

Adafruit_SSD1331 display = Adafruit_SSD1331(&SPI, 10, 8, 9);
#define LED_RED_HIGH     (31 << 11)
#define LED_GREEN_HIGH     (63 << 5)
#define LED_BLUE_HIGH     31
#define LED_WHITE_HIGH    (LED_RED_HIGH    + LED_GREEN_HIGH    + LED_BLUE_HIGH)

#define CL(_r,_g,_b) ((((_r)&0xF8)<<8)|(((_g)&0xFC)<<3)|((_b)>>3))

char frame[2] = {0};
uint16_t colorData;

void setup() {
  Serial.begin(115200);
  display.begin();
  display.fillScreen(LED_RED_HIGH);
}

void loop() { }

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(96);
      Serial.println(64);
      break;

    case 0x12: //frame data
      for (int y = 0; y < 64; y++) {
        for (int x = 0; x < 96; x++) {
          Serial.readBytes(frame, 2);
          display.drawPixel(x, y, (frame[0] << 8) | frame[1]);
        }
      }
      Serial.write(0x06); //acknowledge
      break;

    case 0x11:
      for (int y = 0; y < 64; y++) {
        for (int x = 0; x < 96; x++) {
          Serial.readBytes(frame, 3);
          uint16_t color = CL(frame[0], frame[1], frame[2]);
          display.drawPixel(x, y, color);
        }
      }


      Serial.write(0x06); //acknowledge
      break;
    case 0x13:
      Serial.readBytes(frame, 96 * 64);
      Serial.write(0x06); //acknowledge
      break;
  }
}
