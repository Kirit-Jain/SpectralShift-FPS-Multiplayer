# SpectralShift (Multiplayer Prototype)
**SpectralShift** is a high-performance, networked 1v1 FPS arena shooter built in Unity. The project focuses on solving the dual challenges of Real-Time State Synchronization and Procedural Environment Consistency across distributed clients. By utilizing a server-authoritative model, the game ensures fair play and visual consistency even under variable network conditions

##  Project Overview
**SpectralShift** is a multiplayer First-Person Shooter where two players (Host and Client) spawn into a procedurally generated maze. The core loop involves navigating a unique arena that changes every match and engaging in hitscan combat.

The primary goal of this project was to master **Unity Netcode for GameObjects (NGO)** and solve common challenges in networked physics and bandwidth optimization.

## Core Features

### 1. Server-Authoritative Networking
* **Architecture:** Built using Unity Netcode for GameObjects (NGO) and Unity Relay for seamless NAT traversal and cross-network connectivity.

* **Latency Mitigation:** Implemented Client-Side Prediction for local movement to ensure zero-input lag while maintaining the server as the "Source of Truth" for player positions.

* **State Sync:** Utilized NetworkVariables for persistent state (Health, Scores) and optimized RPCs (Remote Procedure Calls) for transient events like firing and impacts.

### 2. Seed-Based Procedural Generation
* **Bandwidth Optimization:** Instead of syncing thousands of individual wall positions over the network, the system only synchronizes a single Integer Seed.

* **Deterministic Environments:** Every client runs the same generation algorithm locally based on the shared seed, resulting in identical 1v1 arenas with near-zero network overhead.

* **Maze Complexity:** Features custom logic for room-and-corridor generation, ensuring valid spawn points and sightlines for competitive play.

### 3. Advanced Weapon & VFX Systems
* **Validated Hitscan:** Weapon fire uses server-side raycast validation to prevent client-side "ghost hits" or cheating.

* **Kinetic Feedback:** Features a custom-built muzzle flash system utilizing Object Pooling and dynamic point-light flashes for high-impact visual feedback without CPU spikes.

* **Multiplayer VFX:** Optimized particle systems that trigger via ClientRpc to ensure all players see tracers and impacts simultaneously.

### 4. Character Controller & Physics
* **Custom Kinematic Controller:** Designed to handle rapid movement and verticality while maintaining synchronization across the network.

* **Delta-Based Updates:** Uses delta-based transformation updates to reduce the data footprint of movement synchronization.

##  Technical Highlights

### 1. Bandwidth-Optimized Procedural Generation
*Most procedural games suffer from load-time lag when syncing map data. I implemented a seed-based approach to solve this.*

* **The Problem:** Spawning 400+ wall objects on the Server and attempting to sync them individually to the Client caused massive bandwidth spikes and connection timeouts.
* **The Solution:** Implemented **Seed Synchronization**. The Host generates a random integer seed which is synced via a `NetworkVariable`. The Client receives this seed and runs the exact same maze generation algorithm locally.
* **The Result:** Zero network lag during map generation and minimal bandwidth usage, as only a single integer is transmitted.

### 2. Networked Physics Jitter
* **Problem:** In early builds, player movement appeared "stuttery" on the opponent's screen due to the conflict between local physics calculation and incoming network position updates.
* **Solution:** Implemented a Linear Interpolation (Lerp) buffer. The client smooths the transition between the last known server position and the current predicted position, resulting in fluid 60 FPS movement visuals regardless of tick rate.

### 3. Performance-Heavy VFX
* **Problem:** Instantiating muzzle flashes and bullet impacts during high-intensity combat caused noticeable frame drops (garbage collection spikes).
* **Solution:** Developed a generic Object Pooler script. Visual effects are pre-instantiated at the start of the match and "cycled" in and out of the scene, reducing runtime memory allocation to near zero.

### 4. Networking & Architecture
* **Framework:** Built using **Unity Netcode for GameObjects (NGO)**.
* **Connection:** Integrated **Unity Relay** to allow players to connect across different networks without the need for port forwarding.
* **Architecture:** Strictly **Server-Authoritative**. Movement validation and game state are controlled by the server to prevent cheating and desynchronization.

### 5. Latency Handling & "Game Feel"
* **Client-Side Prediction:** Weapon visuals (e.g., bullet trails) trigger instantly on the local client to ensure responsive gameplay, while the actual hit logic is validated by the Server via RPCs.
* **Position Sync:** Uses `NetworkTransform` with interpolation to ensure enemy movement appears smooth even with minor latency.


##  How to Run

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/Kirit-Jain/Spectral-Shift.git
    ```
2.  **Open in Unity:** Version **2022.3** or later is recommended.
3.  **Build the Project:**
    * Go to `File > Build Settings`.
    * Select **Windows/Mac/Linux** and click **Build**.
4.  **Run Multiplayer:**
    * **Host:** Run the Unity Editor (or one instance of the build) and click **"Start Host"**. Copy the Join Code printed in the Console/UI.
    * **Client:** Run the standalone executable, paste the Join Code into the input field, and click **"Join"**.



##  Author
**Kirit Jain**

* [**GitHub**](https://github.com/Kirit-Jain/)
* [**LinkedIn**](https://www.linkedin.com/in/kirit-jain-019a60288/)
