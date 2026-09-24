# Scroll Guardian — Mobile App (React Native / Expo)

Cross-platform mobile application for **Scroll Guardian** supporting Android and iOS.

---

## Capabilities

1. **Native Share Target**:
   * Receive video links directly from the native Instagram, YouTube, or TikTok mobile apps via the OS Share Sheet (`Share → Scroll Guardian`).
   * Automatically extracts URLs, dispatches to the backend ingestion queue, and analyzes content quality.
2. **On-The-Go Spaced Recall Quizzes**:
   * Complete interactive knowledge check quizzes directly on mobile with immediate concept breakdowns.
3. **Daily Digital Diet Dashboard**:
   * Inspect active scrolling vs educational learning ratios, dominant categories, and recent consumption timelines.
4. **"Find Something Useful" Quick Action**:
   * One-tap instant recommendation matching your active career and personal growth goals.
5. **Configurable API Host**:
   * Seamlessly switch between local development emulators (`http://10.0.2.2:5000/api` for Android, `http://localhost:5000/api` for iOS) and production cloud endpoints.

---

## Setup & Running Locally

### 1. Install Dependencies
```bash
cd mobile
npm install
```

### 2. Start Expo Development Server
```bash
npx expo start
```

* Press `a` to launch the Android Emulator.
* Press `i` to launch the iOS Simulator (macOS only).
* Scan the QR code with the **Expo Go** app on your physical iOS/Android device.

---

## Connecting to Backend API

* If using the **Android Emulator**, the app connects to `http://10.0.2.2:5000/api` by default (which maps to your host machine's port 5000).
* If using a **physical mobile device**, ensure your phone is on the same local Wi-Fi network and enter your computer's local IP address (e.g. `http://192.168.1.50:5000/api`) under **Settings → Backend Server URL**.
