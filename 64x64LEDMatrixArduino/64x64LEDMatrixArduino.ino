
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
      Serial.println(kMatrixWidth);
      Serial.println(kMatrixHeight);
      break;

    case 0x0F: //frame data
      
        mil[inc] = micros() - prevMillis;
        prevMillis = micros();
        inc++;
        unsigned char pix[3] = { 0 };
        while (backgroundLayer.isSwapPending());
        rgb24 *buffer = backgroundLayer.backBuffer();
        for (int i = 0; i < kMatrixWidth * kMatrixHeight; i++) {
          Serial.readBytes(pix, 3);
           *buffer++ = rgb24{constrain(pix[0], BLK, 255), constrain(pix[1], BLK, 255), constrain(pix[2], BLK, 255)};
        }
        backgroundLayer.swapBuffers(false);
        Serial.write(0x06); //acknowledge
        break;
  }
}
