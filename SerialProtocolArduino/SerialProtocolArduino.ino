//LMCSHD Serial Protocol v1.0
//This is an example of how the LMCSHD serial data protocol works.

#define WIDTH 1920
#define HEIGHT 1080

//byte frameData[WIDTH * HEIGHT * 3];
byte frameData[3];
void setup() {
  Serial.begin(1);
}

void loop() {}

void serialEvent()
{
  switch (Serial.read())
  {
    case 0x05: //request for matrix definition
      Serial.println(WIDTH);
      Serial.println(HEIGHT);
      break;

    case 0x11: //24bpp frame data
      for (long i = 0; i < WIDTH * HEIGHT * 3; i++)
        Serial.readBytes(frameData, 3);
      Serial.write(0x06); //acknowledge
      break;

    case 0x12: //16bpp frame data
            for (long i = 0; i < WIDTH * HEIGHT * 2; i++)
        Serial.readBytes(frameData, 2);
      Serial.write(0x06); //acknowledge
      break;

    case 0x13: //8bpp frame data
            for (long i = 0; i < WIDTH * HEIGHT; i++)
        Serial.readBytes(frameData, 1);
      Serial.write(0x06); //acknowledge
      break;
  }
}
