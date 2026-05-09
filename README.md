# PetSim VR
### Virtual Reality Pet Simulation

---

## Group Members

- Roman Goff
- Logan Margabandu
- Romik Sarkar

---

## Purpose

PetSim is a virtual reality pet simulation designed to give users the experience of raising and caring for a pet. The application is aimed at those who may not be able to own a pet due to external circumstances, those who want to test out what it is like to have a pet before committing to one, or those who simply want to have fun in an interactive virtual environment.

---

## Advanced Requirements

### 1. Intelligent NPCs (Dynamic Pet Behavior)

Instead of relying on rigid, pre-scripted animation loops, the virtual cat operates as an intelligent NPC driven by internal state variables such as mood, hunger, and energy. The cat dynamically navigates the environment, reacting in real-time to the player's presence and spawned objects like food bowls or toys. Its movement and decision-making adapt based on its current stats — for example, actively pathfinding toward food when hungry or changing its interaction style based on its calculated mood level.

We chose this requirement because a pet simulation relies entirely on the believability of the animal. If the cat merely stood in one place or followed a fixed track, the illusion of life would break. Implementing adaptive, stat-driven behaviors creates a genuinely interactive and rewarding core gameplay loop, forcing the player to observe and respond to the pet's autonomous needs.

### 2. Conversational and Social Avatars (AI-Powered Virtual Vet)

We implemented a Virtual Vet NPC that serves as a socially aware, conversational avatar powered by OpenAI's APIs. Using the headset's microphone, the player can speak naturally to the Vet to describe the cat's symptoms or behavior. The audio is transcribed via the GPT-4o Audio API, and the text is processed by the GPT-4o-mini API to interpret the player's intent. The Vet then dynamically generates contextual advice and actionable suggestions on how to care for the cat based on the verbal input.

We selected this requirement to bypass cumbersome text input and rigid dialogue trees that can feel clunky in a Cardboard VR environment. By leveraging modern LLMs for real-time conversation, we provide a hands-free, immersive diagnostic tool that makes the educational and caretaking aspects of the game feel like a natural, human-to-human consultation.

---

The final build uses two scenes:

```
Scenes/MainMenu
Scenes/FinalScene
```

Open these scenes in the Unity Editor to run, build, or modify the project. Ensure the **Build Platform** is set to **Android** before deploying to a device.

---

## Target Device

| Component | Details |
|-----------|---------|
| **Headset** | Google Cardboard (v2 or compatible) |
| **Platform** | Android (minimum API Level 26 / Android 8.0) |
| **Unity XR Package** | Google Cardboard XR Plugin for Unity |
| **Controller** | Generic Bluetooth Wireless Gamepad (provided with kit) |

---

## GitHub

- **Repository:** (https://github.com/mloganu717/CS6334-Team17-PetSim/tree/roman-latest)
- **Branch:** `roman-latest`
- **Clone:**
  ```bash
  git clone (https://github.com/mloganu717/CS6334-Team17-PetSim.git)
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

## Multiplayer

PetSim is a **single-player** experience. Multiplayer is not supported in this version. Each player runs an independent session on their own Android device.

---

## Video Demonstration

**YouTube Demo:** (https://youtu.be/8iHGsO2qykA)

The video covers the Final Prototype which includes both advanced requirements and interactable objects in the scene.

---

## Setup and Build Instructions

1. Clone the repository and open it in **Unity Hub**
2. Go to **File > Build Settings** and set the platform to **Android**
3. Drag and drop both `Scenes/MainMenu` and `Scenes/FinalScene` into the Scenes in Build list — ensure **MainMenu is at index 0**
4. Install the **Google Cardboard XR Plugin** via the Package Manager
5. Enable **Developer Mode** on your Android device and connect via USB
6. Click **Build and Run** in Unity to deploy to the device, or alternatively transfer and install the pre-built `FinalTeam17.apk` directly to your phone
7. Pair the Bluetooth controller via **Android Settings > Bluetooth** before launching, then insert your phone into the Google Cardboard headset

---

## Third-Party SDKs and Open-Source Integrations

| Tool | Purpose |
|------|---------|
| **MiLabCardboardExtension** (GitHub) | Foundational VR input bridge. Extends Google Cardboard functionality to handle gaze-based raycasting and trigger inputs, allowing the player to interact with UI canvases and 3D objects using the headset button. |
| **OpenAI APIs** (GPT-4o Audio Transcribe and GPT-4o-mini) | Powers natural language interaction with the Virtual Vet. GPT-4o Audio captures and transcribes voice commands via the VR headset microphone; GPT-4o-mini interprets intent and dynamically updates game state. |
| **QuickOutline** (Asset Store) | Provides visual feedback for the gaze interaction system. When the player's VR pointer hovers over an interactable prop, a colored border is rendered around the mesh to clearly indicate selectability. |
| **Twemoji** (GitHub) | Serves as the visual mood indicator system. Specific open-source PNGs were packed into a custom TextMesh Pro Sprite Asset, allowing UI scripts to render mood indicators natively within the VR Canvas UI. |

---

## Credits, Assets, and Licensing

| Source | Assets Used | License |
|--------|-------------|---------|
| **Unity Asset Store** | 3D models (furniture, house, pet models) | Unity Asset Store EULA |
| **Pixabay** | Audio and video assets (BGM, animal noises, UI sounds) | Pixabay License (royalty-free) |
| **GitHub (MiLabCardboardExtension, Twemoji)** | VR input extension, mood emoji graphics | Open-source; Twemoji under CC-BY 4.0 |
| **OpenAI** | GPT-4o Audio API, GPT-4o-mini API | OpenAI API Terms of Service and Enterprise Privacy policies |
| **Unity Built-in Packages** | TextMesh Pro, UI Toolkit, XR Plugin, Plugins | Unity Companion License |
