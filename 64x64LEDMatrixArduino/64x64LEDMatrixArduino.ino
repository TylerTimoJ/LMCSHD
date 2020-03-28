
#include <SmartMatrix3.h>


#define COLOR_DEPTH 24                  // known working: 24, 48 - If the sketch uses type `rgb24` directly, COLOR_DEPTH must be 24
const uint8_t kMatrixWidth = 128;        // known working: 16, 32, 48, 64
const uint8_t kMatrixHeight = 64;       // known working: 32, 64, 96, 128
const uint8_t kRefreshDepth = 24;       // known working: 24, 36, 48
const uint8_t kDmaBufferRows = 32;       // known working: 2-4, use 2 to save memory, more to keep from dropping frames and automatically lowering refresh rate
const uint8_t kPanelType = SMARTMATRIX_HUB75_32ROW_MOD16SCAN; // use SMARTMATRIX_HUB75_16ROW_MOD8SCAN for common 16x32 panels, or use SMARTMATRIX_HUB75_64ROW_MOD32SCAN for common 64x64 panels
const uint8_t kMatrixOptions = (SMARTMATRIX_OPTIONS_NONE);      // see http://docs.pixelmatix.com/SmartMatrix for options
const uint8_t kBackgroundLayerOptions = (SM_BACKGROUND_OPTIONS_NONE);

SMARTMATRIX_ALLOCATE_BUFFERS(matrix, kMatrixWidth, kMatrixHeight, kRefreshDepth, kDmaBufferRows, kPanelType, kMatrixOptions);
SMARTMATRIX_ALLOCATE_BACKGROUND_LAYER(backgroundLayer, kMatrixWidth, kMatrixHeight, COLOR_DEPTH, kBackgroundLayerOptions);

void setup() {
  matrix.addLayer(&backgroundLayer);
  matrix.setBrightness(254);
  matrix.begin();

  Serial.begin(2000000);
}

unsigned char inc = 0;
long mil[256] = {0};
long prevMillis = 0;

unsigned char pix[3] = { 0 };
rgb24 *buffer;

void loop() {}
#define BLK 28

void serialEvent()
{
  switch (Serial.read())
  {
    case 'A':
      for (int i = 0; i < 256; i++) {
        Serial.println((String)mil[i]);
      }
      break;
    case 0x05: //request for matrix definition
      Serial.printf("%08d\n", kMatrixWidth);
      Serial.printf("%08d\n", kMatrixHeight);
      break;

    case 0x11: //24bpp frame data
      mil[inc] = micros() - prevMillis;
      prevMillis = micros();
      inc++;
      while (backgroundLayer.isSwapPending());
      buffer = backgroundLayer.backBuffer();
      for (int i = 0; i < kMatrixWidth * kMatrixHeight; i++) {
        Serial.readBytes(pix, 3);
        *buffer++ = rgb24{map(pix[0], 0, 255, BLK, 255), map(pix[1], 0, 255, BLK, 255), map(pix[2], 0, 255, BLK, 255)};
      }
      backgroundLayer.swapBuffers(false);
      Serial.write(0x06); //acknowledge
      break;

    case 0x12: //16bpp frame data
      mil[inc] = micros() - prevMillis;
      prevMillis = micros();
      inc++;
      while (backgroundLayer.isSwapPending());
      buffer = backgroundLayer.backBuffer();
      for (int i = 0; i < kMatrixWidth * kMatrixHeight; i++) {
        Serial.readBytes(pix, 2);
        *buffer++ = rgb24{map(((pix[0] & B11111000) >> 3), 0, 31, BLK, 255), map((((pix[0] & B00000111) << 3) | ((pix[1] & B11100000) >> 5)), 0, 63, BLK, 255), map((pix[1] & B00011111), 0, 31, BLK, 255)}; //,
      }
      backgroundLayer.swapBuffers(false);
      Serial.write(0x06); //acknowledge
      break;

    case 0x13: //8bpp frame data
      mil[inc] = micros() - prevMillis;
      prevMillis = micros();
      inc++;
      while (backgroundLayer.isSwapPending());
      buffer = backgroundLayer.backBuffer();
      for (int i = 0; i < kMatrixWidth * kMatrixHeight; i++) {
        Serial.readBytes(pix, 1);
        *buffer++ = rgb24{map(((pix[0] & B11000000) >> 6), 0, 3, BLK, 255), map(((pix[0] & B00110000) >> 4), 0, 3, BLK, 255), map(((pix[0] & B00001100) >> 2), 0, 3, BLK, 255)}; //,
      }
      backgroundLayer.swapBuffers(false);
      Serial.write(0x06); //acknowledge
      break;
  }
}
