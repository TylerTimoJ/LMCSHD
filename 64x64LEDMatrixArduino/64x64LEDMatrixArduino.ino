
#include <SmartMatrix3.h>


#define COLOR_DEPTH 24                  // known working: 24, 48 - If the sketch uses type `rgb24` directly, COLOR_DEPTH must be 24
const uint8_t kMatrixWidth = 64;        // known working: 16, 32, 48, 64
const uint8_t kMatrixHeight = 64;       // known working: 32, 64, 96, 128
const uint8_t kRefreshDepth = 36;       // known working: 24, 36, 48
const uint8_t kDmaBufferRows = 4;       // known working: 2-4, use 2 to save memory, more to keep from dropping frames and automatically lowering refresh rate
const uint8_t kPanelType = SMARTMATRIX_HUB75_32ROW_MOD16SCAN; // use SMARTMATRIX_HUB75_16ROW_MOD8SCAN for common 16x32 panels, or use SMARTMATRIX_HUB75_64ROW_MOD32SCAN for common 64x64 panels
const uint8_t kMatrixOptions = (SMARTMATRIX_OPTIONS_NONE);      // see http://docs.pixelmatix.com/SmartMatrix for options
const uint8_t kBackgroundLayerOptions = (SM_BACKGROUND_OPTIONS_NONE);

SMARTMATRIX_ALLOCATE_BUFFERS(matrix, kMatrixWidth, kMatrixHeight, kRefreshDepth, kDmaBufferRows, kPanelType, kMatrixOptions);
SMARTMATRIX_ALLOCATE_BACKGROUND_LAYER(backgroundLayer, kMatrixWidth, kMatrixHeight, COLOR_DEPTH, kBackgroundLayerOptions);

char serialFrame[kMatrixWidth * kMatrixHeight * 3];
const int serialFrameLength = kMatrixWidth * kMatrixHeight * 3;
char frameDefData[2] = {(char)(byte)kMatrixWidth - 1, (char)(byte)kMatrixHeight - 1};
void setup() {
  matrix.addLayer(&backgroundLayer);
  matrix.begin();
  matrix.setBrightness(254);
  Serial.begin(1000000);
}

void loop() {}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05:
      Serial.write(frameDefData, 2);
      break;

    case 0x0F:
      Serial.readBytes(serialFrame, serialFrameLength);
      int index = 0;
      for (int x = 0; x < kMatrixWidth; x++) {
        for (int y = 0; y < kMatrixHeight; y++) {
          backgroundLayer.drawPixel(x, y, {serialFrame[index * 3], serialFrame[index * 3 + 1], serialFrame[index * 3 + 2]});
          index++;
        }
      }
      backgroundLayer.swapBuffers(false);
      Serial.write(6);
      break;
  }
}
