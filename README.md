# Distributed Sensor System

A distributed system simulating temperature sensors with BFT consensus, cryptographic security, and real-time notifications. Built with .NET 9, SignalR, PostgreSQL, Docker, and Kubernetes.

---

## Architecture

```
SensorClient ──► IngestionService ──► Database 
                      │
                      ▼
              NotificationService (SignalR Hub)
                      │
              ConsensusService (BFT Worker)
```

| Service | Description |
|---|---|
| `SensorClient` | Simulates sensors. Sends encrypted, signed readings every 1–5s. |
| `IngestionService` | REST API that validates, decrypts, and stores incoming sensor data. |
| `NotificationService` | SignalR hub for broadcasting alarms and sensor status events. |
| `ConsensusService` | Background worker that calculates a BFT consensus temperature every minute. |
| `SensorNotificationClient` | Web dashboard for monitoring live sensor readings and alarms. |

---

## Security Measures

### 1. Payload Encryption (AES-256-CBC)
Each sensor encrypts its temperature payload (temperature, alarm priority, quality) with a unique AES-256 key before sending. The IV is randomly generated per message and included in the request. The server decrypts using the key registered at startup — plaintext never travels over the wire.

### 2. Digital Signatures (RSA / SHA-256)
Each message is signed with the sensor's RSA private key over the string `sensorId:messageId:timestamp:ciphertext`. The server verifies the signature using the sensor's registered public key. A tampered or forged message fails verification and the sensor is flagged as `BAD`.

### 3. Replay Attack Prevention
The server rejects messages if:
- The timestamp deviates more than 5 seconds from server time (clock skew check), or
- The `MessageId` is less than or equal to the last accepted ID for that sensor.

Replay attacks are simulated by `SensorClient` (option 3) by re-sending stale message IDs with a backdated timestamp.

### 4. DoS / Rate Limiting
A `SensorBlockManager` tracks request frequency per sensor. If a sensor exceeds the threshold in a short window, it is blocked for 30 seconds and gets `HTTP 429`. The flood attack simulation (option 5) fires 15 rapid requests, triggering this protection.

### 5. Sensor Timeout Detection
`SensorTimeoutWorker` polls every 2 seconds. Any active `GOOD` sensor that has not sent a reading in the last 10 seconds is marked `BAD` and `IsActive = false`. A `SensorInactive` event is broadcast over SignalR, and the client activates a standby replacement sensor automatically.

### 6. Byzantine Fault Tolerance (BFT)
`ConsensusService` runs every minute. It collects one averaged proposal per `GOOD` sensor, sorts them, and applies the standard BFT trimmed-mean: discard the `f` lowest and `f` highest values where `f = floor((N-1)/3)`, then average the rest. This tolerates up to ⌊(N−1)/3⌋ Byzantine (malicious/faulty) nodes — for example, with 7 sensors it tolerates 2.

### Key Exchange
Keys are generated on the client side at startup. The sensor registers its RSA public key and AES-256 symmetric key via `POST /api/ingest/register`. The server stores them in PostgreSQL. No private key ever leaves the sensor.

---

## Running the System

### Prerequisites
- Docker + Docker Compose
- OR: Minikube + kubectl + Helm

---

### Option A — Docker Compose (local)

```bash
# Clone the repo
git clone https://github.com/dejanaasevic/distributed-sensor-system
cd distributed-sensor-system

# Copy and configure environment
cp .env.example .env

# Start everything
docker compose up --build
```

Services will be available at:
- Ingestion API: `http://localhost:5001`
- Notification Hub: `http://localhost:5002/notificationHub`
- Dashboard: `http://localhost:5003`

To run the sensor client against a remote server (two-machine demo):

```bash
cd src/SensorClient
dotnet run http://<SERVER_IP>
```

---

### Option B — Kubernetes 

```bash
# Start Minikube
minikube start

# Enable Ingress
minikube addons enable ingress

# Deploy
kubectl apply -f k8s/

# Get the Ingress IP
minikube ip
```

Update your hosts file or pass the Minikube IP directly to the sensor client:

```bash
dotnet run http://<MINIKUBE_IP>
```

---

## Attack Simulation (SensorClient)

Once the client is running, use the interactive menu:

| Key | Attack |
|---|---|
| `1` | Out-of-bounds temperature (9999.9°C) |
| `2` | Tampered RSA signature |
| `3` | Replay attack (stale MessageId + backdated timestamp) |
| `4` | Inactivity dropout (sensor goes silent) |
| `5` | DoS burst flood (15 rapid requests) |
| `6` | Restore all sensors to normal |
| `7` | Exit |

---

## Project Structure

```
distributed-sensor-system/
├── docker-compose.yml
├── k8s/                        # Kubernetes manifests
├── scripts/                    # Helper scripts
└── src/
    ├── ConsensusService/       # BFT consensus worker
    ├── IngestionService/       # Sensor data ingestion API
    ├── NotificationService/    # SignalR hub
    ├── SensorClient/           # Sensor simulator + attack console
    └── SensorNotificationClient/ # Web dashboard
```

---

## Tech Stack
- .NET 9
- ASP.NET Core
- SignalR
- Entity Framework Core
- PostgreSQL
- Docker
- Kubernetes
- Minikube
- AES-256
- RSA/SHA-256
