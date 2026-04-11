# PetSim VR
### Virtual Reality Pet Simulation

---

## Unity Scene

The preliminary prototype is located at:

```
Assets/Scenes/Prototype
```

Open this scene in the Unity Editor to run, build, or modify the prototype. Ensure the **Build Platform** is set to **Android** before deploying to a device.

---

## Target Device

| Component | Details |
|-----------|---------|
| **Headset** | Google Cardboard (v2 or compatible) |
| **Platform** | Android (minimum API Level 26 / Android 8.0) |
| **Unity XR Package** | Google Cardboard XR Plugin for Unity |
| **Controller** | Generic Bluetooth Wireless Gamepad (provided with kit) |

---

##  GitHub

- **Repository:** [YOUR GITHUB REPO URL HERE]
- **Branch:** `main`
- **Clone:**
  ```bash
  git clone https://github.com/[username]/PetSim.git
  ```

All scenes, assets, and scripts are version-controlled in the repository.

---

## Interaction Techniques

### Android Phone (Inside Google Cardboard)

The smartphone acts as both the display and motion sensor via the phone's gyroscope and accelerometer.

| Interaction | How It Works |
|-------------|-------------|
| **Gaze Aiming** | Rotate your head to move the reticle over interactive objects |
| **Gaze Selection** | Hold the reticle on an object for ~2 seconds to trigger an action (pet, feed, etc.) |
| **Cardboard Button** | Press the magnet/button on the side of the headset to instantly confirm a selection |
| **Head Tilt Navigation** | Tilt head left/right to scroll through menus or cycle available actions |

### Wireless Bluetooth Controller

The wireless controller provides faster, more precise input mapped via Unity's Input System.

| Button | Action |
|--------|--------|
| **A / Confirm** | Interact with the currently gazed-at object (pet, feed, play) |
| **B / Back** | Cancel the current action or close menus |
| **D-Pad / Left Thumbstick** | Navigate in-game UI menu options (food, toys, settings) |
| **Start / Menu** | Open or close the main in-game menu overlay |
| **Trigger / Right Shoulder** | Throw a toy or initiate a timed mini-game interaction |

---

## 👤 Multiplayer

PetSim is a **single-player** experience. Multiplayer is not supported in this version. Each player runs an independent session on their own Android device.

---

## 🎥 Video Demonstration

> 📺 **YouTube Demo:** [ADD YOUR YOUTUBE LINK HERE]

The video covers scene navigation, gaze-based interaction, controller input, and the core pet care loop.

---

## ⚙️ Setup & Build Instructions

1. Clone the repository and open it in **Unity Hub**
2. Go to **File > Build Settings** and set the platform to **Android**
3. Open the scene at `Scenes/Prototype`
4. Install the **Google Cardboard XR Plugin** via the Package Manager
5. Enable **Developer Mode** on your Android device and connect via USB
6. Click **Build and Run** in Unity to deploy to the device
7. Pair the Bluetooth controller via **Android Settings > Bluetooth** before launching, then insert your phone into the Google Cardboard headset
