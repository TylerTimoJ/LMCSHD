#include <Adafruit_SSD1331.h>
//#include <SPI.h>

Adafruit_SSD1331 display = Adafruit_SSD1331(&SPI, 5, 6, 9); //cs dc rst

char pix[3] = { 0 };

void setup() {
  Serial.begin(1);
  display.begin(38000000);
  display.fillScreen(0);
  delay(2000);
}

void loop() { }

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.printf("%08d\n", 96);
      Serial.printf("%08d\n", 64);
      break;

    case 0x0F: //frame data
      for (int y = 0; y < 64; y++) {
        for (int x = 0; x < 96; x++) {
          Serial.readBytes(pix, 3);
          display.writePixel(x, y, (map(pix[0], 0, 255, 0, 31) << 11) | (map(pix[1], 0, 255, 0, 63) << 5) | map(pix[2], 0, 255, 0, 31));
        }
      }
      display.endWrite();
      //delayMicroseconds(4000);
      Serial.write(0x06); //acknowledge
      break;
  }
}
