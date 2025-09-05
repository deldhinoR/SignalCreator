// Arduino Phase B + C Code (Supports PHASE_A, PHASE_B, PHASE_C with Full Sine Wave)

#define MAX_PULSES 8
#define SINE_RESOLUTION 256

float amplitude = 2.77;
float frequency = 10.0;
float dutyPct = 70.0;

float periodMs;
float pulseWidthMs;
int dacValue;

float periodMsB[MAX_PULSES];
float pulseWidthMsB[MAX_PULSES];
int dacValuesB[MAX_PULSES];

float sineAmplitude = 2.0;
float sineFrequency = 10.0;
int sineTable[SINE_RESOLUTION];

struct Pulse {
  float amplitude;
  float frequency;
  float dutyPct;
  float leadlagFrac;
  float angle;
};

Pulse pulses[MAX_PULSES] = {
  { 2.0, 20.0, 40.0,  0.0, 10.0 },
  { 2.0, 20.0, 40.0,  0.0, 10.0 },
  { 2.0, 20.0, 40.0,  0.0, 10.0 },
  { 2.0, 20.0, 40.0,  0.0, 10.0 },
  { 0, 0, 0, 0, 0 },
  { 0, 0, 0, 0, 0 },
  { 0, 0, 0, 0, 0 },
  { 0, 0, 0, 0, 0 }
};

int pulseCount = 4;

enum Mode { PHASE_A, PHASE_B, PHASE_C };
Mode currentMode = PHASE_A;

enum isInvert { on, off };
isInvert currentInvert = off;

const char* SKETCH_VERSION = "1.1.0";

int calculateVoltage(float voltage) {
  voltage = constrain(voltage, 0.0, 2.77);
  return (int)((voltage / 2.77) * 4095.0);
}

void computeA() {
  amplitude = constrain(amplitude, 0.0, 2.77);
  dutyPct = constrain(dutyPct, 0.0, 100.0);
  frequency = max(frequency, 0.01);
  periodMs = 1000.0 / frequency;
  pulseWidthMs = (dutyPct / 100.0) * periodMs;
  dacValue = (amplitude / 2.77) * 4095;
}

void computeB() {
  for (int i = 0; i < pulseCount; i++) {
    pulses[i].amplitude = constrain(pulses[i].amplitude, 0.0, 2.77);
    pulses[i].dutyPct = constrain(pulses[i].dutyPct, 0.0, 100.0);
    pulses[i].frequency = max(pulses[i].frequency, 0.01);

    periodMsB[i] = 1000.0 / pulses[i].frequency;
    pulseWidthMsB[i] = (pulses[i].dutyPct / 100.0) * periodMsB[i];

    if (pulseWidthMsB[i] > periodMsB[i]) {
      pulseWidthMsB[i] = periodMsB[i];
    }

    dacValuesB[i] = (int)round((pulses[i].amplitude / 2.77) * 4095.0);
  }
}

void computeSineTable() {
  for (int i = 0; i < SINE_RESOLUTION; i++) {
    float angle = 2.0 * PI * i / SINE_RESOLUTION;

    // Full sine wave centered around sineAmplitude/2
    float voltage = (sineAmplitude / 2.0) * sin(angle) + (sineAmplitude / 2.0);

    voltage = constrain(voltage, 0.0, 2.77);
    sineTable[i] = calculateVoltage(voltage);
  }
}

void parsePulses(String line) {
  line.trim();
  int pulseIndex = 0;

  while (pulseIndex < MAX_PULSES) {
    int sepIndex = line.indexOf(';');
    String part = (sepIndex == -1) ? line : line.substring(0, sepIndex);
    line = (sepIndex == -1) ? "" : line.substring(sepIndex + 1);

    int c1 = part.indexOf(',');
    int c2 = part.indexOf(',', c1 + 1);
    int c3 = part.indexOf(',', c2 + 1);
    int c4 = part.indexOf(',', c3 + 1);

    if (c1 == -1 || c2 == -1 || c3 == -1 || c4 == -1) return;

    float a = part.substring(0, c1).toFloat();
    float f = part.substring(c1 + 1, c2).toFloat();
    float d = part.substring(c2 + 1, c3).toFloat();
    float g = part.substring(c3 + 1, c4).toFloat();
    float ang = part.substring(c4 + 1).toFloat();

    pulses[pulseIndex].amplitude = a;
    pulses[pulseIndex].frequency = f;
    pulses[pulseIndex].dutyPct = d;
    pulses[pulseIndex].leadlagFrac = g;
    pulses[pulseIndex].angle = ang;

    pulseIndex++;
    if (line.length() == 0) break;
  }
  pulseCount = pulseIndex;
}

void setup() {
  analogWriteResolution(12);
  Serial.begin(9600);
  computeA();
  computeB();
  computeSineTable();
}

void loop() {
  static String inputLine = "";
  static unsigned long lastMicros = 0;
  static int sineIndex = 0;

  while (Serial.available()) {
    char c = Serial.read();

    if (c == '<') {
      inputLine = "";
    } else if (c == '>') {
      inputLine.trim();

      if (inputLine == "VERSION?") {
        Serial.print("<VERSION:");
        Serial.print(SKETCH_VERSION);
        Serial.println(">");
        inputLine = "";
        continue;
      }

      if (inputLine == "@A") {
        currentMode = PHASE_A;
      } else if (inputLine == "@B") {
        currentMode = PHASE_B;
      } else if (inputLine == "@C") {
        currentMode = PHASE_C;
      } else if (inputLine == "Invert:ON") {
        currentInvert = on;
      } else if (inputLine == "Invert:OFF") {
        currentInvert = off;
      } else if (currentMode == PHASE_A) {
        float a, f, d;
        if (sscanf(inputLine.c_str(), "%f,%f,%f", &a, &f, &d) == 3) {
          amplitude = a;
          frequency = f;
          dutyPct = d;
          computeA();
        }
      } else if (currentMode == PHASE_B) {
        parsePulses(inputLine);
        computeB();
      } else if (currentMode == PHASE_C) {
        float a, f;
        if (sscanf(inputLine.c_str(), "%f,%f", &a, &f) == 2) {
          sineAmplitude = constrain(a, 0.0, 2.77);
          sineFrequency = max(f, 0.01);
          computeSineTable();
        }
      }
    } else {
      inputLine += c;
    }
  }

  if (currentMode == PHASE_A) {
    analogWrite(DAC0, dacValue);
    delayMicroseconds(pulseWidthMs * 1000);
    analogWrite(DAC0, 0);
    delayMicroseconds((periodMs - pulseWidthMs) * 1000);
  }

  else if (currentMode == PHASE_B) {
    unsigned long periodUs[MAX_PULSES];
    unsigned long highTimeUs[MAX_PULSES];
    unsigned long lowTimeUs[MAX_PULSES];

    for (int i = 0; i < pulseCount; i++) {
      periodUs[i] = (unsigned long)(periodMsB[i] * 1000.0 + 0.5);
      highTimeUs[i] = (unsigned long)(pulseWidthMsB[i] * 1000.0 + 0.5);
      lowTimeUs[i] = periodUs[i] - highTimeUs[i];
    }

    for (int i = 0; i < pulseCount; i++) {
      long phaseShiftUs = (long)(-pulses[i].leadlagFrac * (float)periodUs[i]);
      int prev = (i == 0) ? pulseCount - 1 : i - 1;
      lowTimeUs[i] += phaseShiftUs;
      lowTimeUs[prev] -= phaseShiftUs;
      if ((long)lowTimeUs[i] < 0) lowTimeUs[i] = 0;
      if ((long)lowTimeUs[prev] < 0) lowTimeUs[prev] = 0;
    }

    for (int i = 0; i < pulseCount; i++) {
      int steps = 50;
      float delayPerStepUs = highTimeUs[i] / steps;
      float startVoltage = pulses[i].amplitude;
      float angleFraction = pulses[i].angle / 100.0;
      float endVoltage = startVoltage + (pulses[i].amplitude * angleFraction);

      for (int j = 0; j < steps; j++) {
        float voltage = startVoltage + (j * (endVoltage - startVoltage) / steps);

        voltage = constrain(voltage, 0.0, 2.77);

        if (currentInvert == on) {
          voltage = 2.77 - voltage;
        }

        analogWrite(DAC0, calculateVoltage(voltage));
        delayMicroseconds(delayPerStepUs);
      }

      analogWrite(DAC0, currentInvert == on ? 4095 : 0);
      delayMicroseconds(lowTimeUs[i]);
    }
  }

  else if (currentMode == PHASE_C) {
    float periodUs = 1e6 / sineFrequency;
    float stepDelayUs = periodUs / SINE_RESOLUTION;

    if (micros() - lastMicros >= stepDelayUs) {
      lastMicros = micros();
      analogWrite(DAC0, sineTable[sineIndex]);
      sineIndex = (sineIndex + 1) % SINE_RESOLUTION;
    }
  }
}
