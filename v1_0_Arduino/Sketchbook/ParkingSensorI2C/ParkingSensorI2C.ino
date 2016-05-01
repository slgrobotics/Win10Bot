/* Parking Car Assist Ultrasound Sensor
 *------------------
 *
 * Reads values from a 4-sensor ultrasound unit (sold widely for around US $20 on Amazon)
 * and writes the values to the serial port.
 *
 * http://www.trackroamer.com
 * copyleft 2012, 2016 Sergei Grichine
 *
 */

#include <Wire.h>

#define SLAVE_I2C_ADDRESS 0x9  // not to conflict with IMU-9250 addresses 0x68 0x0C

// Car parking sensor decoding port; use pin 3 because it is connected to external interrupt 1 (INT1)
int mParkingSensorPin = 3;

int ledPin = 13;   // LED connected to digital pin 13
//int dbgPin = 8;  // digital pin 8 used for debugging

int ledFRYPin = 12; // LED - Front Right Yellow
int ledFRRPin = A0; // LED - Front Right Red
int ledFLYPin = 11; // LED - Front Left Yellow
int ledFLRPin = 10; // LED - Front Left Red

int ledBRYPin = 2;  // LED - Back Right Yellow
int ledBRRPin = 4;  // LED - Back Right Red
int ledBLYPin = 5;  // LED - Back Left Yellow
int ledBLRPin = 6;  // LED - Back Left Red

boolean runTest = true;           // blink all LEDs at startup
boolean doSerialOutput = false;   // produce serial output (I2C only if false)

// readings in centimeters:
volatile int rangeFRcm;
volatile int rangeFLcm;
volatile int rangeBRcm;
volatile int rangeBLcm;

// these are volatile because they are changed in interrupt handler.
// otherwise the compiler will assume they are unchanged and replace with constants, or put them in registers.
// volatile variables are always fetched from RAM and stored directly in RAM, no optimization applied.
// We only need to declare volatile those variables that are shared between interrupt routine and loop.
volatile boolean psiChanged;

// processed car parking sensors data:
volatile byte psiData[8];	// raw bits from psiRawData converted to distance codes (decimeters in fact) as reported by each of 4 (8?) sensors.

void setup()
{
  Serial.begin(115200);

  InitializeI2c();

  pinMode(mParkingSensorPin, INPUT);  // Set signal pin to input

  pinMode(ledPin, OUTPUT);            // Sets the digital pin as output
  //pinMode(dbgPin, OUTPUT);          // Sets the digital pin as output

  pinMode(ledFRYPin, OUTPUT);         // Set LED pins as output
  pinMode(ledFRRPin, OUTPUT);
  pinMode(ledFLYPin, OUTPUT);
  pinMode(ledFLRPin, OUTPUT);
  
  pinMode(ledBRYPin, OUTPUT);
  pinMode(ledBRRPin, OUTPUT);
  pinMode(ledBLYPin, OUTPUT);
  pinMode(ledBLRPin, OUTPUT);

  // we will get an interrupt on both falling and rising edges:  
  attachInterrupt(1, interrup_isr_onchange, CHANGE);
}

void loop() {
  if(runTest)
  {
    test();
    runTest = false;
  }
  
  if(psiChanged)
  {
    psiChanged = false;
    
    rangeFRcm = psiDataToCentimeters(psiData[0]);  // front right
    rangeFLcm = psiDataToCentimeters(psiData[1]);  // front left
    rangeBRcm = psiDataToCentimeters(psiData[3]);  // back right
    rangeBLcm = psiDataToCentimeters(psiData[2]);  // back left

    if(doSerialOutput)
    {
      printValues();
    }
    
    setLeds();
  }
  delay(50);
}


