# 🚀 Feature Showcase: Custom Level Tooling & Remote Economy System

> **Developer Portfolio Project**  
> **Engine:** Unity (C#) | **Backend / Cloud:** BunnyCDN, MongoDB, PlayFab  
> **Role:** Lead / Sole Developer (Built from scratch)

---

## 📌 Executive Summary

This project showcases a production-ready, full-stack workflow built completely from scratch for a **"Find the Differences"** mobile game. 

Instead of relying on hardcoded scenes or static build assets, I architected a **data-driven live-ops pipeline**. Content designers can author levels, set coordinate hitboxes, and publish assets directly to cloud storage using a **custom Unity Editor toolchain**, while the runtime client handles cloud syncing, touch gestures, and local caching gracefully.

---

## 🛠️ Key Technical Highlights & Capabilities

### 1. In-Editor Level & Catalog Creator Tool
* **Direct Cloud Pipeline:** Built a custom Unity Editor suite integrated with the **BunnyCDN Storage API** to compress, encode, and push level JSONs and assets directly to staging/production server environments.
* **Dual-Viewport Sync & Marker Scaling:** Designed an interactive image-marking system where raw images (`1440x900`) mirror pan/zoom actions in real time. Features mouse-scroll hitbox scaling and dynamic coordinate normalization.
* **Catalog Management & Integrity:** Implemented conflict checks for chapter/episode indices, automated catalog re-indexing, and local overwrite protections prior to deployment.

### 2. Multi-Touch Gesture & Pan/Zoom Pipeline
* **Unified Device Input:** Designed an abstraction layer using raw input and event handlers to unify mobile touch controls (single tap, double tap, hold) and editor mouse actions.
* **Anchor-Aware Pinch Zooming:** Integrated DOTween with custom screen-to-local calculations to enable smooth, anchor-aware zooming towards the user's touch center point with automated bound clamping.

### 3. Resilient Offline-First Economy
* **Cloud Sync & Local Cache Fallback:** Built an economy manager that attempts remote wallet sync via MongoDB API, smoothly falling back to local `PlayerPrefs` encryption/caching during network drops to ensure uninterrupted offline play.
* **Epoch-Based Cooldowns:** Created a robust energy system tracking attempt cooldowns via epoch timestamps, integrated with local notifications to notify players when attempts refill.

---

## 🏗️ System Architecture

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                            AUTHORING PIPELINE                               │
│  [Custom Unity Tool] ──(Base64/PNG)──> [BunnyCDN] ──> [Remote Catalog JSON] │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                             RUNTIME PIPELINE                                │
│  [Touch Input Layer] ──> [Pinch/Pan Handler] ──> [Synchronized UI Viewports] │
│  [Economy System] ────> [MongoDB / Cache]   ──> [Delegates / Event UI]     │
└─────────────────────────────────────────────────────────────────────────────┘
