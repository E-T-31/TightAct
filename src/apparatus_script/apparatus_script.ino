#include <WiFi.h>
#include <WiFiMulti.h>
#include <WebServer.h>

//-// ESP32 Pins //-//
int internalLED = 2;  // blue
// Pressure Cuff #1
int airPump_1        = 26;
int solenoidValve_1  = 25;
int pressureSensor_1 = 34;
// Pressure Cuff #2
int airPump_2        = 19;
int solenoidValve_2  = 21;
int pressureSensor_2 = 35; // 4;

//-// Miscellaneous Variables //-//
// Pressure Cuff #1
int sensor_idle_1         = 60;     // in analogRead Units (0-4095)
int max_pressure_1        = 1000;
int current_pressure_1    = 0;
float intended_pressure_1 = 0;      // Percentage
bool reached_pressure_1   = false;
// Pressure Cuff #2
int sensor_idle_2         = 60;     // in analogRead Units (0-4095) ???
int max_pressure_2        = 1000;
int current_pressure_2    = 0;
float intended_pressure_2 = 0;      // Percentage
bool reached_pressure_2   = false;

float tolerance     = 50;           // in analogRead Units (±)
int adjustment_rate = 20;           // in ms (≤ 25)
bool readjust      = true;

String msg;                         // received via the SerialMonitor
String cmd;

const char *ssid[]      = {"<stationary_ssid>", "<mobile_ssid>"};
const char *password[]  = {"<stationary_password>", "<mobile_password>"};

WiFiMulti wifiMulti;

WebServer server(80);


void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);

  pinMode(internalLED, OUTPUT);

  pinMode(airPump_1, OUTPUT);
  pinMode(solenoidValve_1, OUTPUT);
  pinMode(airPump_2, OUTPUT);
  pinMode(solenoidValve_2, OUTPUT);

  // Start-up Sequence
  delay(1000);
  digitalWrite(solenoidValve_1, HIGH);
  Serial.println();

  connect_to_WiFi();
  server.on("/", handle_HTTP_main);
  server.on("/get", handle_HTTP_get);
  server.on("/set", handle_HTTP_set);
  server.begin();

  Serial.println();
  digitalWrite(solenoidValve_1, LOW);
}


void loop() {
  // put your main code here, to run repeatedly:
  server.handleClient();

  // Check for new Values
  if (Serial.available()) {
    msg = Serial.readString();
    handle_msg(msg);
  }

  // Read Pressure Values
  current_pressure_1 = analogRead(pressureSensor_1);
  current_pressure_2 = analogRead(pressureSensor_2);

  // Print Pressure Cuff #1
  Serial.print("Cuff_1.Pressure = " + String(current_pressure_1));
  Serial.print(" (" + String(to_mV(current_pressure_1)) + " mV");
  current_pressure_1 -= sensor_idle_1;
  Serial.print(" → " + printable_pressure(current_pressure_1) + ")");

  // Handle Pressure Cuff #1
  if ((intended_pressure_1 > 0) && (current_pressure_1 < (intended_pressure_1*max_pressure_1 - tolerance)) && (readjust || !reached_pressure_1)) {   // Inflate Cuff
    digitalWrite(airPump_1, HIGH);
    digitalWrite(solenoidValve_1, LOW);   // *(thud)*
    digitalWrite(internalLED, HIGH);
    Serial.println(" --> +");
  } else if (current_pressure_1 > (intended_pressure_1*max_pressure_1 + tolerance) && (readjust || !reached_pressure_1)) {                           // Deflate Cuff
    digitalWrite(airPump_1, LOW);
    digitalWrite(solenoidValve_1, HIGH);  // *click!*
    digitalWrite(internalLED, HIGH);
    Serial.println(" --> -");
  } else {                                                                                                                                                // Maintain current Pressure
    digitalWrite(airPump_1, LOW);
    if (intended_pressure_1 > 0)          // Stop Valve flickering when no Pressure to be sustained
      digitalWrite(solenoidValve_1, LOW);
    digitalWrite(internalLED, LOW);
    Serial.println(" --> =");
    reached_pressure_1 = true;
  }

  // Print Pressure Cuff #2
  Serial.print("Cuff_2.Pressure = " + String(current_pressure_2));
  Serial.print(" (" + String(to_mV(current_pressure_2)) + " mV");
  current_pressure_2 -= sensor_idle_2;
  Serial.print(" → " + printable_pressure(current_pressure_2) + ")");

  // Handle Pressure Cuff #2
  if ((intended_pressure_2 > 0) && (current_pressure_2 < (intended_pressure_2*max_pressure_2 - tolerance)) && (readjust || !reached_pressure_2)) {   // Inflate Cuff
    digitalWrite(airPump_2, HIGH);
    digitalWrite(solenoidValve_2, LOW);   // *(thud)*
    digitalWrite(internalLED, HIGH);
    Serial.println(" --> +");
  } else if (current_pressure_2 > (intended_pressure_2*max_pressure_2 + tolerance) && (readjust || !reached_pressure_2)) {                           // Deflate Cuff
    digitalWrite(airPump_2, LOW);
    digitalWrite(solenoidValve_2, HIGH);  // *click!*
    digitalWrite(internalLED, HIGH);
    Serial.println(" --> -");
  } else {                                                                                                      // Maintain current Pressure
    digitalWrite(airPump_2, LOW);
    if (intended_pressure_2 > 0)          // Stop Valve flickering when no Pressure to be sustained
      digitalWrite(solenoidValve_2, LOW);
    digitalWrite(internalLED, LOW);
    Serial.println(" --> =");
    reached_pressure_2 = true;
  }

  delay(adjustment_rate);
}


//-// Helper Functions //-//
float to_mV(int value) {
  return 3300*value/4095;
}

float to_kPa(int value) {
  return to_mV(value)/45;
}

String printable_pressure(int pressure) {
  float kPa = to_kPa(pressure);
  float mmHg = 7.50062*kPa;
  return (String(pressure) + " ≙ " + String(kPa) + " kPa = " + String(mmHg) + " mmHg");
}

void handle_msg(String msg) {
  cmd = msg.substring(0, 3);
  Serial.println();
  if (cmd == "max") {                   // max_pressure_1       (max)
    max_pressure_1 = msg.substring(3).toInt();
    Serial.println("max_pressure_1 <-- " + String(max_pressure_1));
  } else if (cmd == "lvl") {            // intended_pressure_1  (lvl)
    intended_pressure_1 = msg.substring(3).toFloat();
    Serial.println("intended_pressure_1 <-- " + String(intended_pressure_1) + " (≙ " + printable_pressure(intended_pressure_1*max_pressure_1) + ")");
  } else if (cmd == "adj") {            // adjustment_rate      (adj)
    adjustment_rate = msg.substring(3).toInt();
    Serial.println("adjustment_rate <-- " + String(adjustment_rate) + " ms");
  } else if (cmd == "tol") {            // tolerance            (tol)
    tolerance = msg.substring(3).toInt();
    Serial.println("tolerance <-- ±" + String(tolerance));
  } else if (cmd == "rea") {            // tolerance            (tol)
    if (msg.substring(3).toInt() >= 1) {
      readjust = true;
      Serial.println("readjust <-- true");
    } else {
      readjust = false;
      Serial.println("readjust <-- false");
    }
  } else {
    Serial.println("Unknown msg...");
  }
  Serial.println();
  reached_pressure_1 = false;
  reached_pressure_2 = false;
}

void connect_to_WiFi() {
  int i = 0;
  while (WiFi.status() != WL_CONNECTED) {
    WiFi.disconnect(true);  // Delete old Config

    Serial.println();
    Serial.print("Connecting to \"" + String(ssid[i]) + "\"");
    WiFi.begin(ssid[i], password[i]);

    int j = 0;
    while (WiFi.status() != WL_CONNECTED && j < 10) {
      delay(500);
      Serial.print(".");
      j++;
    }

    Serial.println();
    if (WiFi.status() == WL_CONNECTED) {
      Serial.print("Connection established under IP address: ");
      Serial.println(WiFi.localIP());
      break;
    }
    Serial.println("Couldn't establish a connection in time.");
    i = (i + 1) % 2;
  }
}

void handle_HTTP_main() {
  server.send(200, "text/html", "<html><title>ESP32</title><body><a>Click </a><a href=""/set"">here</a><a> to control the Pressure Cuffs or </a><a href=""/get"">here</a><a> to get their readings</a></body></html>");
}

void handle_HTTP_get() {
  server.send(200, "text/html", String(current_pressure_1) + " " + String(current_pressure_2));  // Alternately: "text/plain"
}

void handle_HTTP_set() {
  Serial.println();
  String html = "<html><head><meta charset=\"UTF-8\"><title>ESP32 Control Page</title></head><body>";

  html       += "<h1>Pressure Cuff Controls</h1>";
  html       += "<form action=""/set"" method=""get"">";
  html       += "  <input type=""text"" id=""rea"" name=""rea"">";
  html       += "  <label for=""rea"">readjust?";

  if (server.hasArg("rea") && !server.arg("rea").isEmpty()) {
    if (server.arg("rea").toInt() >= 1) {
      readjust = true;
      Serial.println("readjust? <-- true");
    } else {
      readjust = false;
      Serial.println("readjust? <-- false");
    }
    html     += " → ";
    reached_pressure_1 = false;
    reached_pressure_2 = false;
  } else {
    html     += " = ";
  }
  html       += String(readjust) + "</label><br><br>";

  html       += "  <input type=""text"" id=""adj"" name=""adj"">";
  html       += "  <label for=""adj"">adjustment_rate";

  if (server.hasArg("adj") && !server.arg("adj").isEmpty()) {
    adjustment_rate = server.arg("adj").toInt();
    Serial.println("adjustment_rate <-- " + String(adjustment_rate) + " ms");
    html     += " → ";
    reached_pressure_1 = false;
    reached_pressure_2 = false;
  } else {
    html     += " = ";
  }
  html       += String(adjustment_rate) + " ms</label><br><br>";

  html       += "  <input type=""text"" id=""tol"" name=""tol"">";
  html       += "  <label for=""tol"">tolerance";

  if (server.hasArg("tol") && !server.arg("tol").isEmpty()) {
    tolerance = server.arg("tol").toFloat();
    Serial.println("tolerance <-- ±" + printable_pressure(tolerance));
    html     += " → ";
    reached_pressure_1 = false;
    reached_pressure_2 = false;
  } else {
    html     += " = ";
  }
  html       += "±" + printable_pressure(tolerance) + "</label><br><br>";

  html       += "  <label for=""cuff_1"">Pressure Cuff #1 [<a href=""/set?lvl_1=0"">Deflate</a>]</label><br><br>";
  html       += "  <input type=""text"" id=""max_1"" name=""max_1"">";
  html       += "  <label for=""max_1"">max_pressure_1";

  if (server.hasArg("max_1") && !server.arg("max_1").isEmpty()) {
    max_pressure_1 = server.arg("max_1").toInt();
    if (max_pressure_1 > 4000)
      max_pressure_1 = 4000;
    Serial.println("max_pressure_1 <-- " + printable_pressure(max_pressure_1));
    html     += " → ";
    reached_pressure_1 = false;
  } else {
    html     += " = ";
  }
  html       += printable_pressure(max_pressure_1) + "</label><br><br>";

  html       += "  <input type=""text"" id=""lvl_1"" name=""lvl_1"">";
  html       += "  <label for=""lvl_1"">intended_pressure_1";

  if (server.hasArg("lvl_1") && !server.arg("lvl_1").isEmpty()) {
    intended_pressure_1 = server.arg("lvl_1").toFloat();
    if (intended_pressure_1 > 1.0)
      intended_pressure_1 = 1.0;
    Serial.println("intended_pressure_1 <-- " + String(intended_pressure_1) + " (≙ " + printable_pressure(intended_pressure_1*max_pressure_1) + ")");
    html     += " → ";
    reached_pressure_1 = false;
  } else {
    html     += " = ";
  }
  html       += String(intended_pressure_1) + " (= " + printable_pressure(intended_pressure_1*max_pressure_1) + ")</label><br><br>";

  html       += "  <label for=""cuff_1"">Pressure Cuff #2 [<a href=""/set?lvl_2=0"">Deflate</a>]</label><br><br>";
  html       += "  <input type=""text"" id=""max_2"" name=""max_2"">";
  html       += "  <label for=""max_2"">max_pressure_2";

  if (server.hasArg("max_2") && !server.arg("max_2").isEmpty()) {
    max_pressure_2 = server.arg("max_2").toInt();
    if (max_pressure_2 > 4000)
      max_pressure_2 = 4000;
    Serial.println("max_pressure_2 <-- " + printable_pressure(max_pressure_2));
    html     += " → ";
    reached_pressure_2 = false;
  } else {
    html     += " = ";
  }
  html       += printable_pressure(max_pressure_2) + "</label><br><br>";

  html       += "  <input type=""text"" id=""lvl_2"" name=""lvl_2"">";
  html       += "  <label for=""lvl_2"">intended_pressure_2";

  if (server.hasArg("lvl_2") && !server.arg("lvl_2").isEmpty()) {
    intended_pressure_2 = server.arg("lvl_2").toFloat();
    if (intended_pressure_2 > 1.0)
      intended_pressure_2 = 1.0;
    Serial.println("intended_pressure_2 <-- " + String(intended_pressure_2) + " (≙ " + printable_pressure(intended_pressure_2*max_pressure_2) + ")");
    html     += " → ";
    reached_pressure_2 = false;
  } else {
    html     += " = ";
  }
  html       += String(intended_pressure_2) + " (= " + printable_pressure(intended_pressure_2*max_pressure_2) + ")</label><br><br>";

  html       += "  <label for=""lvl_2"">Adjust the Pressure Cuffs' parameters as needed by clicking </label><input type=""submit"" value=""Apply"">";
  html       += "</form>";
  
  html       += "<a>Quick Action Buttons: <a href=""/set?lvl_1=0""><button>Deflate Cuff #1</button></a> <a href=""/set?lvl_2=0""><button>Deflate Cuff #2</button></a> <a href=""/set?lvl_1=0&lvl_2=0""><button>Deflate both Cuffs</button></a></a>";
  html       += "</body></html>";

  server.send(200, "text/html; charset=UTF-8", html);
  Serial.println();
}