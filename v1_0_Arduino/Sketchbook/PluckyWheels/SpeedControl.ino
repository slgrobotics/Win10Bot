


long LdistancePrev, RdistancePrev;   // last encoders values - distance traveled, in ticks

void speed_calculate()
{
  long distR = Rdistance - RdistancePrev; // around 270- 300 with 20Hz cycle and wheels in the air
  long distL = Ldistance - LdistancePrev;

  speedMeasured_R = map(distR,-110, 110, -100, 100);  // first pair is number of ticks at full speed, we map it to 100%
  speedMeasured_L = map(distL,-110, 110, -100, 100);

  RdistancePrev = Rdistance;
  LdistancePrev = Ldistance;
}

// **********************************************
// Calculate the pwm, given the desired speed
// **********************************************
void pwm_calculate()
{
      // "desiredSpeed" comes in the range -100...100 - it has a meaning of "percent of max speed"

      //dpwm_R = map(desiredSpeedR, -100, 100, -255, 255);
      //dpwm_L = map(desiredSpeedL, -100, 100, -255, 255);

      // pick up values computed by PIDs:
      pwm_R += dpwm_R;
      pwm_L += dpwm_L;

      /*
      // we let set_motor() do the following:
      if(pwm_R > 255)      // Maximum / Minimum Limitations
        pwm_R = 255;
    
      if(pwm_R < -255)
        pwm_R = -255;
    
      if(pwm_L > 255)
        pwm_L = 255;
        
      if(pwm_L < -255)
        pwm_L = -255;
      */
}

