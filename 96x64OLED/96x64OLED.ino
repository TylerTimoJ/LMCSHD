#include <Adafruit_SSD1331.h>

Adafruit_SSD1331 display = Adafruit_SSD1331(10, 8, 17, 18, 9);
#define LED_RED_HIGH     (31 << 11)
#define LED_GREEN_HIGH     (63 << 5)
#define LED_BLUE_HIGH     31
#define LED_WHITE_HIGH    (LED_RED_HIGH    + LED_GREEN_HIGH    + LED_BLUE_HIGH)
char pix[3] = { 0 };

char frame[96 * 64 * 3] = {0};
uint16_t colorData[96 * 64] = {0};

void setup() {
  Serial.begin(1);
  display.begin(38000000);
  display.fillScreen(LED_WHITE_HIGH);
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

    case 0x12: //frame data
      Serial.readBytes(frame, 96 * 64 * 2);
      Serial.write(0x06); //acknowledge
      for (int i = 0; i < 96 * 64; i++) {
        colorData[i] = frame[i * 2] << 8 | frame[i * 2 + 1];
      }
      display.drawRGBBitmap(0, 0, colorData, 96, 64);
      break;

    case 0x11:
      Serial.readBytes(frame, 96 * 64 * 3);
      Serial.write(0x06); //acknowledge
      break;
    case 0x13:
      Serial.readBytes(frame, 96 * 64);
      Serial.write(0x06); //acknowledge
      break;
  }
}
