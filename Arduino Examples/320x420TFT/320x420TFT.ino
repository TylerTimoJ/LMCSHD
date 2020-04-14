#include "SPI.h"
#include "ILI9341_t3n.h"
#define SPI0_DISP1

#define BPP16_LEN 153600
#define BPP24_LEN 230400
#define UINT16_LEN 76800


ILI9341_t3n tft = ILI9341_t3n(9, 10, 8);

const int width = 320;
const int height = 240;

unsigned volatile char sBuf[width * height * 3] = {0};

uint8_t *sP;
uint16_t *cP;

long start = 0;
long end = 0;

void setup() {
  tft.useFrameBuffer(true);
  tft.begin(72000000);
  tft.setRotation(1);
  Serial.begin(1);
}

void loop(void) {}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(width);
      Serial.println(height);
      break;

    case 'A':
      Serial.println(end - start);
      break;

    case 0x12: //frame data
      Serial.readBytes(sBuf, BPP16_LEN);
      tft.waitUpdateAsyncComplete();

      sP = sBuf + (BPP16_LEN);
      cP = tft.getFrameBuffer() + (UINT16_LEN);

      for (int i = 0; i < UINT16_LEN; i++)
        *--cP = (*--sP | *--sP << 8);

      tft.updateScreenAsync();
      Serial.write(0x06); //acknowledge
      break;

    case 0x11:
      Serial.readBytes(sBuf, BPP24_LEN);
      Serial.write(0x06);
      break;
    case 0x13:
      Serial.readBytes(sBuf, UINT16_LEN);
      tft.waitUpdateAsyncComplete();

      cP = tft.getFrameBuffer();
      for (int i = 0; i < UINT16_LEN; i++)
        *cP++ = ((sBuf[i] & B11100000) << 8) | ((sBuf[i] & B00011100) << 6) | ((sBuf[i] & B00000011) << 3);
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;
  }
}
