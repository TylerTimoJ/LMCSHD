#include "SPI.h"
#include "ILI9341_t3n.h"
#define SPI0_DISP1

#define BPP16_LEN 153600
#define BPP24_LEN 230400
#define UINT16_LEN 76800

#define width 320
#define height 240

DMAMEM uint16_t fb1[320 * 240];
DMAMEM uint16_t fb2[320 * 240];

ILI9341_t3n tft = ILI9341_t3n(9, 10, 8);



char sBuf[width * height * 3];

uint8_t *sP;
uint16_t *cP;

void DelayFrameInterval(unsigned long waitTime = 33000);

void setup() {
  tft.setFrameBuffer(fb1);
  tft.useFrameBuffer(true);
  tft.begin(72000000);
  tft.setRotation(1);
  Serial.begin(115200);

  tft.fillScreen(ILI9341_BLACK);
  unsigned long waitTime = 30000, startTime = micros();
  tft.updateScreen();
  while (micros() - startTime < waitTime);

  tft.fillScreen(ILI9341_RED);
  startTime = micros();
  tft.updateScreen();
  while (micros() - startTime < waitTime);

  tft.fillScreen(ILI9341_GREEN);
  startTime = micros();
  tft.updateScreen();
  while (micros() - startTime < waitTime);

  tft.fillScreen(ILI9341_BLUE);
  startTime = micros();
  tft.updateScreen();
  while (micros() - startTime < waitTime);

  tft.fillScreen(ILI9341_BLACK);
  startTime = micros();
  tft.updateScreen();
  while (micros() - startTime < waitTime);

  PrintFPS();
  delay(30);
  tft.updateScreen();
}

void loop(void) {}


bool usingFB1 = true;

void DelayFrameInterval(unsigned long waitTime = 33000) {
  static unsigned long lastTime = 0;
  while (micros() - lastTime < waitTime);
  lastTime = micros();
}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(width);
      Serial.println(height);
      break;

    case 0x41:
      Serial.readBytes(sBuf, width * height * 3);
      DelayFrameInterval(99500);
      tft.fillScreen(ILI9341_BLACK);
      tft.setCursor(random(0, 300), random(0, 200));
      tft.setTextColor(ILI9341_PINK);
      tft.print("not supported");
      PrintFPS();
      tft.updateScreen();
      Serial.write(0x06);
      break;

    case 0x42: //16BPP RGB FRAME

      Serial.readBytes(sBuf, width * height * 2);

      tft.waitUpdateAsyncComplete();

      int i = 0;

      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          tft.fillRect(x * 320 / width, y * 240 / height, 320 / width, 240 / height, sBuf[i * 2] << 8 | sBuf[i * 2 + 1]);
          i++;
        }
      }



      /*
            int offset = 0;
            if (usingFB1) {
              usingFB1 = false;
              for (int i = 0; i < width * height; i++) {
                fb2[i * 10 + offset] = sBuf[i * 2] << 8 | sBuf[i * 2 + 1];
                offset += 10;
              }
            }
            else {
              usingFB1 = true;
              for (int i = 0; i < width * height; i++) {
                fb1[i * 10 + offset] = sBuf[i * 2] << 8 | sBuf[i * 2 + 1];
                offset += 10;
              }
            }
      */


      // tft.setFrameBuffer(usingFB1 ? fb1 : fb2);

      PrintFPS();
      tft.updateScreenAsync(false);
      Serial.write(0x06); //acknowledge
      break;


    case 0x43: // 8BPP RGB FRAME
      Serial.readBytes(sBuf, width * height);

      if (usingFB1) {
        usingFB1 = false;
        for (int i = 0; i < 320 * 240; i++) {
          fb2[i] = ((sBuf[i] & B11100000) << 8) | ((sBuf[i] & B00011100) << 6) | ((sBuf[i] & B00000011) << 3);
        }
      }
      else {
        usingFB1 = true;
        for (int i = 0; i < 320 * 240; i++) {
          fb1[i] = ((sBuf[i] & B11100000) << 8) | ((sBuf[i] & B00011100) << 6) | ((sBuf[i] & B00000011) << 3);
        }
      }
      tft.waitUpdateAsyncComplete();
      PrintFPS();
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

    case 0x44: // 8BPP GREYSCALE FRAME
      Serial.readBytes(sBuf, width * height);

      if (usingFB1) {
        usingFB1 = false;
        for (int i = 0; i < 320 * 240; i++) {
          fb2[i] = ((sBuf[i] & B11111000) << 8) | ((sBuf[i] & B11111100) << 3) | ((sBuf[i] & B11111000) >> 3);
        }
      }
      else {
        usingFB1 = true;
        for (int i = 0; i < 320 * 240; i++) {
          fb1[i] = ((sBuf[i] & B11111000) << 8) | ((sBuf[i] & B11111100) << 3) | ((sBuf[i] & B11111000) >> 3);
        }
      }

      tft.waitUpdateAsyncComplete();
      PrintFPS();
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

    case 0x45: // 1BPP MONOCHROME FRAME
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
      }
      PrintFPS();
      tft.updateScreenAsync();
      Serial.write(0x06);
      break;

  }
}



void PrintFPS() {

  static unsigned long lastTime = 0;
  unsigned long elapsedTime = micros() - lastTime;
  lastTime = micros();

  uint16_t colors[39 * 11];

  tft.readRect(0, 0, 39, 11, colors);

  uint32_t r = 0, g = 0, b = 0;

  for (int i = 0; i < 39 * 11; i++) {

    r += (uint8_t)(colors[i] >> 11);        //1111 1000 0000 0000
    g += (uint8_t)((colors[i] >> 5) & 0x3F); //0000 0111 1110 0000
    b += (uint8_t)(colors[i] & 0x1F);  //0000 0000 0001 1111
  }

  r /= (39 * 11); // 0 - 31
  g /= (39 * 11); // 0 - 63
  b /= (39 * 11); // 0 - 31

  uint16_t compColor = (((~r) & 0x1F) << 11) | (((~g) & 0x3F) << 5) | ((~b) & 0x1F);

  tft.setCursor(2, 2);
  tft.setTextColor(compColor);
  tft.print(1000000 / elapsedTime);
  tft.print(" FPS");

}
