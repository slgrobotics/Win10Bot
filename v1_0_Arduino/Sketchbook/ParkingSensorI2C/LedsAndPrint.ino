
int redTreshold = 40;     // cm
int yellowTreshold = 80;

void setLeds()
{
  digitalWrite(ledFRYPin, LOW);
  digitalWrite(ledFRRPin, LOW);
  digitalWrite(ledFLYPin, LOW);
  digitalWrite(ledFLRPin, LOW);
  digitalWrite(ledBRYPin, LOW);
  digitalWrite(ledBRRPin, LOW);
  digitalWrite(ledBLYPin, LOW);
  digitalWrite(ledBLRPin, LOW);

  if(rangeFRcm >= 0)
  {
    if(rangeFRcm < redTreshold)
    {
      digitalWrite(ledFRRPin, HIGH);
    }
    else if(rangeFRcm < yellowTreshold)
    {
      digitalWrite(ledFRYPin, HIGH);
    }
  }
  
  if(rangeFLcm >= 0)
  {
    if(rangeFLcm < redTreshold)
    {
      digitalWrite(ledFLRPin, HIGH);
    }
    else if(rangeFLcm < yellowTreshold)
    {
      digitalWrite(ledFLYPin, HIGH);
    }
  }
    
  if(rangeBRcm >= 0)
  {
    if(rangeBRcm < redTreshold)
    {
      digitalWrite(ledBRRPin, HIGH);
    }
    else if(rangeBRcm < yellowTreshold)
    {
      digitalWrite(ledBRYPin, HIGH);
    }
  }
    
  if(rangeBLcm >= 0)
  {
    if(rangeBLcm < redTreshold)
    {
      digitalWrite(ledBLRPin, HIGH);
    }
    else if(rangeBLcm < yellowTreshold)
    {
      digitalWrite(ledBLYPin, HIGH);
    }
  }
}

// ===============================================================================
void test()
{
  for(int i=0; i < 5 ;i++)
  {
    testOne(ledFRYPin);
    testOne(ledFRRPin);
    testOne(ledFLYPin);
    testOne(ledFLRPin);
    
    testOne(ledBRYPin);
    testOne(ledBRRPin);
    testOne(ledBLYPin);
    testOne(ledBLRPin);
  }
}

void testOne(int led)
{
  digitalWrite(led, HIGH);
  digitalWrite(ledPin, HIGH);
  delay(100);
  digitalWrite(led, LOW);
  digitalWrite(ledPin, LOW);
  //delay(100);
}

// ===============================================================================
// nice to have a LED blinking when signal is captured OK. Good for debugging too.
void mLED_Red_On()
{
  digitalWrite(ledPin, HIGH);   // turn the LED on (HIGH is the voltage level)
}

void mLED_Red_Off()
{
  digitalWrite(ledPin, LOW);    // turn the LED off by making the voltage LOW
}

/*
// some debug related stuff:
void mLED_Dbg_On()
{
 digitalWrite(dbgPin, HIGH);   // turn the LED on (HIGH is the voltage level)
}
 
void mLED_Dbg_Off()
{
 digitalWrite(dbgPin, LOW);    // turn the LED off by making the voltage LOW
}
*/

void printValues() 
{
  Serial.print("!SNR:");
  Serial.print(rangeFRcm);  // front right
  Serial.print(",");
  Serial.print(rangeFLcm);  // front left
  Serial.print(",");
  Serial.print(rangeBLcm);  // back left
  Serial.print(",");
  Serial.print(rangeBRcm);  // back right
  /*     Serial.print("\t");
   Serial.print(psiData[4]);  // raw byte
   Serial.print("\t");
   Serial.print(psiData[5]);  // raw byte
   Serial.print("\t");
   Serial.print(psiData[6]);  // raw byte
   Serial.print("\t");
   Serial.print(psiData[7]);  // raw byte
   */
  Serial.print("\n");
  /*
   Serial.print(psiRawData[0]);
   Serial.print("***");
   Serial.print(psiRawData[1]);
   Serial.print("***");
   Serial.print(psiRawData[2]);
   Serial.print("***");
   Serial.print(psiRawData[3]);
   Serial.print("\n");
   */
}

