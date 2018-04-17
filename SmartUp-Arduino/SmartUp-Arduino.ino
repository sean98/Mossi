#include <Servo.h>
#include <Adafruit_Sensor.h>
#include <DHT.h>
#include <DHT_U.h>

#define DHT_PIN   11      // Pin which is connected to the DHT sensor.
#define SERVO_PIN 9       //Servo motor pin
#define RELAY_PIN 10      //Ralay pin

#define DHT_TYPE   DHT22  // DHT 22 (AM2302)
#define DHT_DELAY 2000    // DHT recomended time between measerments

#define MAX_ANGLE     180 //max angle of the motor
#define MIN_ANGLE     0   //min angle of the motor
#define DEFAULT_ANGLE 90  //default angle of the motor
#define SERVO_DEALY 1000  //duration of every motor action (defined by kinect frequency)

#define INIT "init" //const for init
#define SCAN "scan" //const for scan

#define RELAY_TEMPERTURE 20 //temperure to turn on the relay
#define RELAY_HUMIDITY   50 //humidity to turn on the relay

int curAngle=DEFAULT_ANGLE, scan_speed=80; //define current angle and scanning speed
DHT_Unified dht(DHT_PIN, DHT_TYPE); //create dht object
Servo servo;//create servo motor object
int DhtTimeElapsed = 0; //represent the time elapsed from last dht check

void setup() {
  //Initalize servo
  servo.attach(SERVO_PIN);
  servo.write(DEFAULT_ANGLE);
   
  //Initialize device.
  dht.begin();

  //Open serial cammunication
  Serial.begin(9600);
}

void loop() {
  float t=0, h=0;
  String msg = readLine();  //read message
  if (msg.equals(INIT))  Serial.print((String)INIT + "\n"); //connect to program from PC
  else if(isNumeric(msg))  moveMotorTo(curAngle + msg.toInt());
  else if (msg.equals(SCAN))  scan();
  else  //no valid message, read DHT with delay
  {
    sensors_event_t event;
    t = getTemperture(event);
    h = getHumidity(event);
    delay(DHT_DELAY);
  }

  if (DhtTimeElapsed>=DHT_DELAY) //can measure DHT without delay
  {
    DhtTimeElapsed = 0;
    sensors_event_t event;
    t = getTemperture(event);
    h = getHumidity(event);
  }
  
  if (t>RELAY_TEMPERTURE && h>RELAY_HUMIDITY)
      analogWrite(RELAY_PIN, 255);
  else
      analogWrite(RELAY_PIN,0);
}
float getTemperture(sensors_event_t event)
{  
  dht.temperature().getEvent(&event);
  if (isnan(event.temperature)) return 0;  //Error reading temperture
  else return event.temperature;
}

float getHumidity(sensors_event_t event)
{
    dht.humidity().getEvent(&event);
    if (isnan(event.relative_humidity)) return 0;  //Error reading humidity
    else  return event.relative_humidity;
}

void scan()
{
  if (curAngle == MAX_ANGLE || curAngle == MIN_ANGLE) scan_speed*=-1;
  moveMotorTo(curAngle + scan_speed);
}

boolean isNumeric(String str) {
    int stringLength = str.length();
 
    if (stringLength == 0) return false;
    if (str.charAt(0) == '-') return isNumeric(str.substring(1));

    for(int i = 0; i < stringLength; i++) {
        if (!isDigit(str.charAt(i)))
            return false;
    }
    return true;
}

String readLine()
{
  String msg1 = "";
  while (Serial.available() > 0)
  {
    char inChar = Serial.read();
    
    if (inChar == '\n')
    {
        String msg2 = readLine();
        if (msg2.equals(""))
            return msg1;
        return msg2;
    }
    msg1 += (String)inChar;
    delay(2);
  }
  return msg1;
}

void moveMotorTo(int newAngle)
{
  DhtTimeElapsed += SERVO_DEALY;
  
  if (newAngle > MAX_ANGLE) newAngle = MAX_ANGLE;
  if (newAngle < MIN_ANGLE) newAngle = MIN_ANGLE;

  int delayTime = SERVO_DEALY/abs(newAngle-curAngle) +1;
  
  for (;curAngle<newAngle;curAngle++)
  {
    servo.write(curAngle);
    delay(delayTime);
  }
  for (;curAngle>newAngle;curAngle--)
  {
    servo.write(curAngle);
    delay(delayTime);
  }
}
