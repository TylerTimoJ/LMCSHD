
#include <SmartMatrix3.h>


#define COLOR_DEPTH 24                  // known working: 24, 48 - If the sketch uses type `rgb24` directly, COLOR_DEPTH must be 24
const uint8_t kMatrixWidth = 128;        // known working: 16, 32, 48, 64
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
void setup() {
  matrix.addLayer(&backgroundLayer);
  matrix.begin();
  matrix.setBrightness(200);
  Serial.begin(12000000);
}

void SpeedTest() {

  byte randomR = (byte)random(0, 255);
  byte randomG = (byte)random(0, 255);
  byte randomB = (byte)random(0, 255);

  for (int i = 0; i < 2000; i++) {
    int index = 0;
    for (int y = 0; y < kMatrixHeight; y++) {
      for (int x = 0; x < kMatrixWidth; x++) {
        backgroundLayer.drawPixel(x, y, {randomR, randomG, randomB}); //update each pixel with data from serial
        index++;
      }
    }
    backgroundLayer.swapBuffers();
  }
}

void loop() {}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(kMatrixWidth);
      Serial.println(kMatrixHeight);
      break;

    case 0x0F: //frame data
      Serial.readBytes(serialFrame, serialFrameLength); //read all incomming data from serial connection
      int index = 0;
      for (int y = 0; y < kMatrixHeight; y++) {
        for (int x = 0; x < kMatrixWidth; x++) {
          backgroundLayer.drawPixel(x, y, {serialFrame[index * 3], serialFrame[index * 3 + 1], serialFrame[index * 3 + 2]}); //update each pixel with data from serial
          index++;
        }
      }
      backgroundLayer.swapBuffers();
      Serial.write(0x06); //acknkowledge
      break;
  }
}







/*
  void serialEvent()
  {
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(<matrix width>);
      Serial.println(<matrix height>);
      break;

    case 0x0F: //frame data
      Serial.readBytes(serialFrame, serialFrameLength); //read all incomming data from serial connection
      <use data in serialFrame to update display>
      Serial.write(0x06); //acknkowledge
      break;
  }
  }
*/
