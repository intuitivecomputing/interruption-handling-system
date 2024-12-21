#include <SPI.h>
#include <Arduino.h>
#include "ServoEasing.hpp"
#define NUM_MOTORS 4
#define userPosition 60
#define screenPosition 100

int motorPositions[4];  // Array to track each motor's position in degrees
ServoEasing motorH;
ServoEasing motorL;
ServoEasing motorR;
ServoEasing motorB;
void setup() {
  Serial.begin(9600);
  Serial.println(F("Attach servo at pin " STR(5)));
  motorL.attach(3, 55);
  motorR.attach(4, 70);
  motorH.attach(5, 60);
}
void loop() {
  if (Serial.available() > 0) {
    String req = Serial.readStringUntil('\n');
    int result = handleBehaviorRequest(req);
    if (result == 0) {
      Serial.println("Success: Motor moved to requested angle.");
    } else {
      Serial.println("Error: Invalid command format.");
    }
  }
}
void lookAtUser() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(userPosition, 40);
}

void lookAtUserAndLeftNod() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(userPosition, 40);
  delay(500);
  leftDoubleNod();
}

void lookAtUserAndRightNod() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(userPosition, 40);
  delay(500);
  rightDoubleNod();
}

void lookAtScreen() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(screenPosition, 40);
}
void lookAwayLeft() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(40, 20);
}
void lookAwayRight() {
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  motorH.easeTo(80, 20);
}
void lookAwayUp() {
  motorH.easeTo(60, 40);
  motorL.easeTo(62, 40); 
  motorR.easeTo(58, 20); 
}
void lookAwayUpLeft() {
  motorH.easeTo(60, 40);
  // motorL.easeTo(65, 40);
  // motorR.easeTo(70, 20); 
  motorL.easeTo(30, 40);
  motorR.easeTo(70, 20);
}
void lookAwayUpRight() {
  motorH.easeTo(60, 40);
  motorL.easeTo(55, 40); 
  motorR.easeTo(60, 20); 
}
void leftNod() {
  motorH.easeTo(60, 40);
  // motorL.easeTo(65, 40);
  motorL.easeTo(38, 40);
  motorR.easeTo(70, 20);
  delay(250);
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  delay(250);
}
void leftDoubleNod() {
  motorH.easeTo(60, 40);
  // motorL.easeTo(65, 40);
  motorL.easeTo(38, 40);
  motorR.easeTo(70, 20);
  delay(250);
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
  delay(250);
  // motorL.easeTo(65, 40);
  motorL.easeTo(38, 40);
  motorR.easeTo(70, 20);
  delay(250);
  motorL.easeTo(55, 40);
  motorR.easeTo(70, 40);
}
void rightNod() {
  motorH.easeTo(60, 40);
  motorL.easeTo(55, 40); 
  // motorR.easeTo(60, 20); 
  motorR.easeTo(82, 40);
  delay(250);
  motorL.easeTo(55, 40); 
  motorR.easeTo(70, 40); 
  delay(250);
}
void rightDoubleNod() {
  motorH.easeTo(60, 40);
  motorL.easeTo(55, 40); 
  // motorR.easeTo(60, 20); 
  motorR.easeTo(82, 20);
  delay(250);
  motorL.easeTo(55, 40); 
  motorR.easeTo(70, 40); 
  delay(250);
  motorL.easeTo(55, 40); 
  // motorR.easeTo(60, 20); 
  motorR.easeTo(82, 20);
  delay(250);
  motorL.easeTo(55, 40); 
  motorR.easeTo(70, 40); 
}

const int LookAtUser = 1;
const int LookAtScreen = 2;
const int LookAwayLeft = 3;
const int LookAwayRight = 4;
const int LookAwayUp = 5; 
const int LeftNod = 6;
const int RightNod = 7;
const int LeftDoubleNod = 8;
const int RightDoubleNod = 9;
const int LookAwayUpLeft = 10; 
const int LookAwayUpRight = 11; 
const int LookAtUserAndLeftNod = 17;
const int LookAtUserAndRightNod = 18;

int handleBehaviorRequest(String command) {
  Serial.print("Received: ");
  Serial.println(command);
  int separatorIndex = command.indexOf(',');
  if (separatorIndex == -1) {
    Serial.println("Invalid command format.");
    return -1;
  }
  String motorIndexString = command.substring(0, separatorIndex);
  String targetAngleString = command.substring(separatorIndex + 1);
  int motorIndex = motorIndexString.toInt();
  int targetAngle = targetAngleString.toInt();
  Serial.println("recieved move code: " + targetAngleString);
  switch (targetAngle) {
    case LookAtUser:
      Serial.println("Looking at User | Request Executed");
      lookAtUser();
      break;
    case LookAtScreen:
      Serial.println("Looking at Screen | Request Executed");
      lookAtScreen();
      break;
     case LookAwayLeft:
      Serial.println("Looking Away Left | Request Executed");
      lookAwayLeft();
      break;
     case LookAwayRight:
      Serial.println("Looking Away Right | Request Executed");
      lookAwayRight();
      break;
    case LookAwayUp:
      Serial.println("Looking Away Up | Request Executed");
      lookAwayUp();
      break;
    case LookAwayUpRight:
      Serial.println("Looking Away Up Right | Request Executed");
      lookAwayUpRight();
      break;
    case LookAwayUpLeft:
      Serial.println("Looking Away Up Left | Request Executed");
      lookAwayUpLeft();
      break;
    case LeftNod:
      Serial.println("Nodding w/ Left Tilt | Request Executed");
      leftNod();
      break;
    case LeftDoubleNod:
      Serial.println("Double Nodding w/ Left Tilt | Request Executed");
      leftDoubleNod();
      break;
    case RightNod:
      Serial.println("Nodding w/ Right Tilt | Request Executed");
      rightNod();
      break;
    case RightDoubleNod:
      Serial.println("Double Nodding w/ Right Tilt | Request Executed");
      rightDoubleNod();
      break;
    case LookAtUserAndLeftNod:
      Serial.println("Double Nodding After Looking At User | Request Executed");
      lookAtUserAndLeftNod();
      break;
    case LookAtUserAndRightNod:
      Serial.println("Double Nodding After Looking At User | Request Executed");
      lookAtUserAndRightNod();
      break;
    default:
      Serial.println("Invalid Behaviour Code Recieved: "+ targetAngleString);
      break;
  }
  return 0;
}