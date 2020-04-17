#include <LCDWIKI_SPI.h> //Hardware-specific library
#include <SPI.h>
LCDWIKI_SPI my_lcd(SSD1283A, 10, 9, 8, A3); //hardware spi,cs,cd,reset

#define scale 130

unsigned char sBuf[50700] = { 0 };

SPISettings settingsB(36000000, MSBFIRST, SPI_MODE0);

void setup()
{
  SPI.begin();
  SPI.beginTransaction(settingsB);
  my_lcd.Init_LCD();
  my_lcd.Fill_Screen(0);
  Serial.begin(115200);
  SPI.endTransaction();
  SPI.beginTransaction(settingsB);
}

void loop() {}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(130);
      Serial.println(130);
      break;


    case 0x12: //frame data
      Serial.readBytes(sBuf, 33800);
      SPI.transfer(sBuf, 0, 33800);
      Serial.write(0x06); //acknowledge
      break;
      
    case 0x11:
      Serial.readBytes(sBuf, 50700);
      Serial.write(0x06);
      break;
    case 0x13:
      Serial.readBytes(sBuf, 16900);
      Serial.write(0x06);
      break;
  }
}
