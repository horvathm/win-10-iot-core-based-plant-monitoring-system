#include<Wire.h>

#define VIN 3.3
#define R2 10000
#define ADC_RES 1023
#define I2C_SLAVE_ADDRESS 0x63
#define NUMBER_OF_TRIES 10
#define SAMPLES 5

// Pin macros
#define PIN_ULS_TRIG 3
#define PIN_ULS_ECHO 2
#define PIN_LED 13
#define PIN_THERMISTOR A0


byte command;
int measurementRegister;
byte outputBuffer[2];
int term[SAMPLES];

/*
  Reads the thermistor from A0 pin, calculates it's resistance and returns that.
*/
double readThermistorResistance(){ 
  for(int i = 0; i < SAMPLES; i++)
  {
    term[i] = analogRead(PIN_THERMISTOR);
    delayMicroseconds(10);
  }
  
  double temp;
  
  for(int i = 0; i < SAMPLES; i++)
  {
    temp += term[i];
  }

  temp /= SAMPLES;
  temp *= (double)VIN/(double)ADC_RES;
  return (R2*((VIN/temp) - 1));
}

/*
  Reads the ultrasonic distance. 
*/
double readUltrasonicDistance(){
  double distance;
  unsigned long duration;
  digitalWrite(PIN_ULS_TRIG,LOW);
  delayMicroseconds(10);
  for(int i = 0; i < NUMBER_OF_TRIES; ++i)
  {
    /*
      Tries NUMBER_OF_TRIES times to read the ultrasonic distance, by triggering 
      the PIN_ULS_TRIG and checking whether the soundwave hasa arrived back on 
      the PIN_ULS_ECHO pin.
     */
    digitalWrite(PIN_ULS_TRIG,HIGH);
    delayMicroseconds(20);
    digitalWrite(PIN_ULS_TRIG,LOW);
    duration = pulseIn(PIN_ULS_ECHO, HIGH, 75000);
    if(duration != 0){
      distance = (duration / 2) * 0.03434;
      return distance;
    }
  }
  return -1;
}

// This method executes when the device is beeing read on the I2C bus by the master.
void requestEvent(){
  switch(command){
    case 0x01:
      // On read request it writes the last measurement.
      Wire.write(outputBuffer,2);
    break;
    case 0x02:
      Wire.write(outputBuffer,2);
    break;
    default:
    break;
  }
}

// This method executes when it's beeing written on the I2C bus by the master.
void receiveEvent(int numberOfBytes){
  while(1 <= Wire.available()){
    command = Wire.read();
    switch(command){
      case 0x01: 
        // Executes a measurement and store it.
        measurementRegister = (int)readThermistorResistance();
        outputBuffer[0] = highByte(measurementRegister);
        outputBuffer[1] = lowByte(measurementRegister);
        break;
      case 0x02:      
        measurementRegister = (int)readUltrasonicDistance();
        outputBuffer[0] = highByte(measurementRegister);
        outputBuffer[1] = lowByte(measurementRegister);
        break;
      default:
      break;
    }
  }
}

void setup() {
  // Setting the device as an I2C slave with address of I2C_SLAVE_ADDRESS.
  Wire.begin(I2C_SLAVE_ADDRESS);

  // Assigning event handlers for I2C read and write.
  Wire.onRequest(requestEvent);
  Wire.onReceive(receiveEvent);

  // Setting the desired pin modes.
  pinMode(PIN_LED, OUTPUT);
  pinMode(PIN_ULS_TRIG, OUTPUT);
  pinMode(PIN_ULS_ECHO, INPUT);
  pinMode(PIN_THERMISTOR, INPUT);

  // Writing initial values on output pins.
  digitalWrite(PIN_ULS_TRIG, LOW);
  digitalWrite(PIN_LED, LOW);
}

void loop() {
  // Loop section has nothing to do, just blinking the built in LED.
  digitalWrite(PIN_LED, HIGH);
  delay(250);
  digitalWrite(PIN_LED, LOW);
  delay(2000);
}
