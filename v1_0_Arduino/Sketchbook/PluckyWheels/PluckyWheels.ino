
#include <Wire.h>
#include <AlfsTechHbridge.h>
#include <Odometry.h>
#include <PID_v1.h>

int ledPin = 13;    // Arduino UNO Yellow LED

const int batteryInPin = A3;  // Analog input pin that the battery 1/3 divider is attached to

#define SLAVE_I2C_ADDRESS 9   // parking sonar sensor, driven by Arduino Pro Mini

const boolean doTRACE = false;

const int mydt = 5;           // 5 milliseconds make for 200 Hz operating cycle
const int pidLoopFactor = 4;  // factor of 4 make for 20ms PID cycle

//-------------------------------------- Variable definitions --------------------------------------------- //

volatile long Ldistance, Rdistance;   // encoders - distance traveled, in ticks

int pwm_R, pwm_L;       // pwm -255..255 sent to H-Bridge pins (will be constrained by set_motor()) 
double dpwm_R, dpwm_L;  // correction output, calculated by PID, -255..255 normally, will be added to the above
double speedMeasured_R, speedMeasured_L;  // percent of max speed for this drive configuration.

// desired speed is set by Comm:
double desiredSpeedR = 0.0;
double desiredSpeedL = 0.0;

// PID Setpoints (desired speed after ema):
double setpointSpeedR = 0.0;
double setpointSpeedL = 0.0;

const int RightMotorChannel = 0;  // index to ema*[] arrays
const int LeftMotorChannel = 1;

// EMA period to smooth wheels movement. 100 is smooth but fast, 300 is slow.
const int EmaPeriod = 100;

// variables to compute exponential moving average:
int emaPeriod[2];
double valuePrev[2];
double multiplier[2];

// Plucky robot parameters:
double wheelBaseMeters = 0.600;
double wheelRadiusMeters = 0.192;
double encoderTicksPerRevolution = 5440;  // one wheel rotation

// current robot pose, updated by odometry:
double X;      // meters
double Y;      // meters
double Theta;  // radians, positive clockwise

DifferentialDriveOdometry *odometry;

// higher Ki causes residual rotation, higher Kd - jerking movement
PID myPID_R(&speedMeasured_R, &dpwm_R, &setpointSpeedR, 1.0, 0, 0.05, DIRECT);    // in, out, setpoint, double Kp, Ki, Kd, DIRECT or REVERSE
PID myPID_L(&speedMeasured_L, &dpwm_L, &setpointSpeedL, 1.0, 0, 0.05, DIRECT);

// received from parking sonar sensor Slave, readings in centimeters:
volatile int rangeFRcm;
volatile int rangeFLcm;
volatile int rangeBRcm;
volatile int rangeBLcm;

// ------------------------------------------------------------------------------------------------------ //

AlfsTechHbridge motors;  	// constructor initializes motors and encoders

long timer = 0;     // general purpose timer
long timer_old;

unsigned int loopCnt = 0;
unsigned int lastLoopCnt = 0;

void setup()
{ 
  //Serial.begin(19200);
  Serial.begin(115200);
  pinMode (ledPin, OUTPUT);  // Status LED

  int PID_SAMPLE_TIME = mydt * pidLoopFactor;  // milliseconds.

  // turn the PID on and set its parameters:
  myPID_R.SetOutputLimits(-250.0, 250.0);  // match to maximum PID outputs in both directions. Motor PWM -255...255 can be too violent.
  myPID_R.SetSampleTime(PID_SAMPLE_TIME);  // milliseconds. Regardless of how frequently Compute() is called, the PID algorithm will be evaluated at a regular interval (no more often than this).
  myPID_R.SetMode(AUTOMATIC);              // AUTOMATIC means the calculations take place, while MANUAL just turns off the PID and lets the man drive

  myPID_L.SetOutputLimits(-250.0, 250.0);
  myPID_L.SetSampleTime(PID_SAMPLE_TIME);
  myPID_L.SetMode(AUTOMATIC);

  InitializeI2c();

  // ======================== init motors and encoders: ===================================

  // uncomment one or both of the following lines if your motors' directions need to be flipped
  //motors.flipLeftMotor(true);
  //motors.flipRightMotor(true);

  EncodersInit();    // attach interrupts

  odometry = new DifferentialDriveOdometry();
  odometry->Init(wheelBaseMeters, wheelRadiusMeters, encoderTicksPerRevolution);

  setEmaPeriod(RightMotorChannel, EmaPeriod);
  setEmaPeriod(LeftMotorChannel, EmaPeriod);
  
  pwm_R = 0;
  pwm_L = 0;
  motors.init();
  set_motors();

  blinkLED(10, 50);

  digitalWrite(ledPin, HIGH);

  timer = millis();
  delay(20);
  loopCnt = 0;
}

void loop() //Main Loop
{
  delay(1); // 1 ms
  
  if((millis() - timer) >= mydt)  // Main loop runs at 200Hz (mydt=5), to allow frequent sampling of AHRS
  {
    loopCnt++;
    timer_old = timer;
    timer = millis();

    if(doTRACE)
    {
      printAll();
    }
    
    readCommCommand();  // reads desiredSpeed
    
    // smooth movement by using ema: take desiredSpeed and produce setpointSpeed
    ema(RightMotorChannel);
    ema(LeftMotorChannel);

    // compute control inputs - increments to current PWM
    myPID_R.Compute();
    myPID_L.Compute();

    boolean isPidLoop = loopCnt % pidLoopFactor == 0;

    if(isPidLoop)  // we do speed PID and odometry calculation on a slower scale, about 20Hz
    {
      // based on PWM increments, calculate speed:
      speed_calculate();
      
      // Calculate the pwm, given the desired speed (pick up values computed by PIDs):
      pwm_calculate();
  
      // test: At both motors set to +80 expect Ldistance and Rdistance to increase
      //pwm_R = 80;
      //pwm_L = 80;
      //pwm_L = 255;  // measure full speed
  
      set_motors();
  
      // odometry calculation takes 28us
      //digitalWrite(10, HIGH);
      // process encoders readings into X, Y, Theta using odometry library:
      odometry->wheelEncoderLeftTicks = Ldistance;
      odometry->wheelEncoderRightTicks = Rdistance;
    
      odometry->Process();
    
      if (odometry->displacement.halfPhi != 0.0 || odometry->displacement.dCenter != 0.0)
      {
        double theta = Theta + odometry->displacement.halfPhi;   // radians
    
        // calculate displacement in the middle of the turn:
        double dX = odometry->displacement.dCenter * cos(theta);      // meters
        double dY = odometry->displacement.dCenter * sin(theta);      // meters
    
        X += dX;
        Y += dY;
        
        Theta += odometry->displacement.halfPhi * 2.0;
      }
      //digitalWrite(10, LOW);
    }
  }
}

void ema(int ch)
{
  // smooth movement by using ema:
  switch (ch)
  {
    case RightMotorChannel:
      setpointSpeedR = (valuePrev[ch] < -1000000.0) ? desiredSpeedR : ((desiredSpeedR - valuePrev[ch]) * multiplier[ch] + valuePrev[ch]);  // ema
      //setpointSpeedR = desiredSpeedR; // no ema
      valuePrev[ch] = setpointSpeedR;
      break;

    case LeftMotorChannel:
      setpointSpeedL = (valuePrev[ch] < -1000000.0) ? desiredSpeedL : ((desiredSpeedL - valuePrev[ch]) * multiplier[ch] + valuePrev[ch]);  // ema
      //setpointSpeedL = desiredSpeedL; // no ema
      valuePrev[ch] = setpointSpeedL;
      break;
  }
}

void setEmaPeriod(int ch, int period)
{
  valuePrev[ch] = -2000000.0;  // signal to use desiredSpeed directly for the first point
  emaPeriod[ch] = period;
  multiplier[ch] = 2.0 / (1.0 + (double)emaPeriod[ch]);
}


