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

void setup() {
  tft.useFrameBuffer(true);
  tft.begin(72000000);
  tft.setRotation(1);
  Serial.begin(115200);
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

    case 0x41:
      Serial.readBytes(sBuf, BPP24_LEN);
      Serial.write(0x06);
      break;

    case 0x42: //frame data
      Serial.readBytes(sBuf, BPP16_LEN);
      tft.waitUpdateAsyncComplete();

      sP = sBuf + (BPP16_LEN);
      cP = tft.getFrameBuffer() + (UINT16_LEN);

      for (int i = 0; i < UINT16_LEN; i++)
        *--cP = (*--sP | *--sP << 8);

      tft.updateScreenAsync();
      Serial.write(0x06); //acknowledge
      break;

    case 0x43:
      Serial.readBytes(sBuf, UINT16_LEN);
      tft.waitUpdateAsyncComplete();

      cP = tft.getFrameBuffer();
      for (int i = 0; i < UINT16_LEN; i++)
        *cP++ = ((sBuf[i] & B11100000) << 8) | ((sBuf[i] & B00011100) << 6) | ((sBuf[i] & B00000011) << 3);
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

    case 0x44:
      Serial.readBytes(sBuf, UINT16_LEN);
      tft.waitUpdateAsyncComplete();
      cP = tft.getFrameBuffer();
      for (int i = 0; i < UINT16_LEN; i++)
        *cP++ = ((sBuf[i] & B11111000) << 8) | ((sBuf[i] & B11111100) << 3) | ((sBuf[i] & B11111000) >> 3);
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

    case 0x45:
      Serial.readBytes(sBuf, ((width * height) / 8) + ((width * height) % 8));
      tft.waitUpdateAsyncComplete();
      cP = tft.getFrameBuffer();
      for (int i = 0; i < ((width * height) / 8) + ((width * height) % 8); i++) {

        *cP++ = sBuf[i] & B10000000 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B01000000 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B00100000 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B00010000 ? 0xFFFF : 0x0000;

        *cP++ = sBuf[i] & B00001000 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B00000100 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B00000010 ? 0xFFFF : 0x0000;
        *cP++ = sBuf[i] & B00000001 ? 0xFFFF : 0x0000;
        // CL(23, 23, 23);

        // *cP++ = ((sBuf[i] & B11111000) << 8) | ((sBuf[i] & B11111100) << 3) | ((sBuf[i] & B11111000) >> 3);
      }
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

  }
}
