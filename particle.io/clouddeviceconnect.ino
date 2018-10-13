// -----------------------------------
// Azure IoT demo
// Sample code from Particle.io
// Board - Particle Photon with built in wifi
//
// Reading from photoresister then read out on LED
// Turn on/off messages to Cloud
// -----------------------------------

// Setup
// photoresistor - pin A0, 3v3 (power)
// resister 10k - pin A0, Gnd
// LED indicator - pin D0, resistor 10k
// LED resistor 10k - LED, Gnd
// Switch - power(left), resistor(mid), Gnd(right)
// Switch resistor - Switch, D6

int boardLED = D7;
int photoresistor = A0;
int indicatorLED = D0;
int sendSwitch = D6;
int controlLED = D1;
int controlLamp = D4;
int cloudSwitch = D5;

int offPhoto = 2500;
int onPhoto = 3100;

//flash LED
int flashDuration = 2000;  // blue led flash duration
int flashElasped = 0;
bool blueOn = false;
bool flashInvoke = false;
int flashStart;

// publish variables
int publishTime = 2000;   // publish status every x milliseconds
int elaspedTime = 0;
bool publishNow = false;
int startTime;

// variables
bool sendToCloud = false;
int photoRead;
bool lampStatus;
bool blueStatus;
bool redStatus;
bool cloudSwitchStatus;
bool manSwitchStatus;

// functions
int cloudSwitchOn(String command);
int cloudSwitchOff(String command);
int flashControlLed(String command);
int toggleLamp(String command);
bool publishTimer(String command);
bool flashBlue(int holdTime, int thisTime);

void setup() {
    //startup vars
    startTime = millis();
    
    // Send digital read and publish to Azure IoT Hub
    photoRead = analogRead(photoresistor);
    String photoReadStr = String(photoRead);
    String dataOut = String( "{ \"lightLevel\":" + photoReadStr +"}");
    Particle.publish("monkeyhunterdevicestartup", dataOut, PRIVATE);
    
    // Set pin modes
    pinMode(photoresistor, INPUT);
    pinMode(sendSwitch, INPUT);
    pinMode(boardLED, OUTPUT);
    pinMode(indicatorLED, OUTPUT);
    pinMode(controlLED, OUTPUT);
    pinMode(controlLamp, OUTPUT);
    pinMode(cloudSwitch, OUTPUT);

    // Register the cloud functions
    // POST https://api.particle.io/v1/devices/{DEVICE_ID}/{function}/?access_token=<token>
    Particle.function("flashBlue",flashControlLed);
    Particle.function("toggleLamp",toggleLamp);
    Particle.function("toCloudON",cloudSwitchOn);
    Particle.function("toCloudOFF",cloudSwitchOff);
    
    // Register variables for retrieval
    // variable name max length is 12 characters long
    Particle.variable("onLevel", onPhoto);
    Particle.variable("offLevel", offPhoto);
    Particle.variable("sendToCloud", sendToCloud);
    Particle.variable("photoLevel", photoRead);
    Particle.variable("lampStatus", lampStatus);
    Particle.variable("blueStatus", blueStatus);
    Particle.variable("redStatus", redStatus);
    Particle.variable("cloudStatus", cloudSwitchStatus);
    Particle.variable("manualStatus", manSwitchStatus);
    Particle.variable("flashTime", flashDuration);
    
    // Reset pins
    digitalWrite(boardLED, LOW);
    digitalWrite(indicatorLED, LOW);
    digitalWrite(controlLED, LOW);
    digitalWrite(controlLamp, LOW);
    digitalWrite(cloudSwitch, LOW);

    // Flash indicate at setup ending
    digitalWrite(indicatorLED, HIGH);
    delay(800);
    digitalWrite(indicatorLED, LOW);
    delay(800);
    digitalWrite(indicatorLED, HIGH);
    delay(800);
    digitalWrite(indicatorLED, LOW);
    delay(800);
    digitalWrite(indicatorLED, HIGH);
    delay(2000);
    digitalWrite(indicatorLED, LOW);
    delay(2000);
    
}

void loop() {

    // Read from photoresistor, and to variables
    photoRead = analogRead(photoresistor);
    lampStatus = digitalRead(controlLamp);
    blueStatus = digitalRead(controlLED);
    redStatus = digitalRead(indicatorLED);
    cloudSwitchStatus = digitalRead(cloudSwitch);
    manSwitchStatus = digitalRead(sendSwitch);

    // Turn on LED when photoresister is low
    if(photoRead >= onPhoto){
        digitalWrite(indicatorLED, LOW);
    }
    // Turn off LED when photoresister is high
    if(photoRead < offPhoto){
        digitalWrite(indicatorLED, HIGH);
    }

    // Read manual switch status if cloud switch is OFF
    if(digitalRead(cloudSwitch) == HIGH){
        sendToCloud = true;
    }
    else{
        sendToCloud = false;
        if (digitalRead(sendSwitch) == HIGH){
            sendToCloud = true;  
        }
    }
    
    blueOn = flashBlue(flashDuration);
    if (blueOn){
        digitalWrite(controlLED, HIGH);
    }else{
        digitalWrite(controlLED, LOW);
    }

    // check timer
    publishNow = publishTimer(String(publishNow));

    if (publishNow){
        // Publish to event log    
        Particle.publish("lightReading", String(photoRead), 60, PRIVATE);
        
        // Send message to cloud if true, turn on board led
        if (sendToCloud){
            // Turn on cloud log status LED
            digitalWrite(boardLED,HIGH);
            String dataOut = "";
    
            dataOut = String::format("{ \"lightLevel\":%d, \"logStatus\":%i, \"lampStatus\":%i, \"blueStatus\":%i, \"redStatus\":%i, \"logSwitchStatus\":%i, \"manSwitchStatus\":%i, \"log\":\"asb\" }",photoRead,sendToCloud,lampStatus,blueStatus,redStatus,cloudSwitchStatus,manSwitchStatus);
            //dataOut = String::format("{ \"lightLevel\":%d }",photoRead);
            Particle.publish("monkeyhunterstatus", dataOut, PRIVATE);
    
            dataOut = "";
        }else{
            // Turn off cloud log status LED
            digitalWrite(boardLED,LOW);
        }
    }

    delay(500);

}

// publish timer
bool publishTimer(String command){
    int nowTime = millis();
    elaspedTime = nowTime - startTime;
    if (elaspedTime >= publishTime){
        startTime = millis();
        return true;
    }else{
        return false;
    }
}

// suspend
bool flashBlue(int dur){
    //if flash invoke, start timer and set flash invoke to false
    //if within timer, turn on led
    if(flashInvoke){
        flashInvoke = false;
        flashStart = millis();
        flashElasped = 0;
        //reset start time
    }else{
        int nowTime = millis();
        flashElasped = nowTime - flashStart;
        if (flashElasped <= flashDuration){
            return true;
        }else{
            return false;
        }
    }
}

// Start sending data to cloud
int cloudSwitchOn(String command){
    Particle.publish("monkeyhunterAction", "send data to Cloud = ON", PRIVATE);
    digitalWrite(cloudSwitch, HIGH);
    return digitalRead(cloudSwitch);
}

// Stop sending data to cloud
int cloudSwitchOff(String command){
    Particle.publish("monkeyhunterAction", "send data to Cloud = OFF", PRIVATE);
    digitalWrite(cloudSwitch, LOW);
    return digitalRead(cloudSwitch);
}

// Flash blue LED for x seconds
int flashControlLed(String command){
    int commandDuration = command.toInt();
    int flashSec = flashDuration;    // default to 2 seconds flash
    if (commandDuration > 0){
        flashSec = commandDuration;
    }
    Particle.publish("monkeyhunterAction", "flash LED at " + String(flashSec) + " ms", PRIVATE);
    //digitalWrite(controlLED, HIGH);
    //delay(flashSec);
    flashInvoke = true;
    flashDuration = flashSec;
    return flashDuration;
}

// Turn on/off bright lamp
int toggleLamp(String command){
    Particle.publish("monkeyhunterAction", "toggle Lamp", PRIVATE);
    if (digitalRead(controlLamp) == HIGH){
        digitalWrite(controlLamp, LOW);
        return digitalRead(controlLamp);
    }
    if (digitalRead(controlLamp) == LOW){
        digitalWrite(controlLamp, HIGH);
        return digitalRead(controlLamp);
    }
}

