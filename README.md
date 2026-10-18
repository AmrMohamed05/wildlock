# Wildlock — 2v2 Wild West Hero Arena Shooter (Gem Grab)

**Developer:** Amr Mohamed Salah (58-12817)  
**Engine:** Unity 6 (`6000.3.24f1`) — Universal Render Pipeline (URP) & Cinemachine 3  

---

## Overview

**Wildlock** is a fast-paced 2v2 3D Hero Arena Shooter set in a stylized Wild West frontier town surrounded by a vast red-rock canyon basin. Teams battle over a central **Ruby Mine Cart** that spawns Red Gems every 7 seconds. To win, a team must collect **10 or more Red Gems** and survive a frantic **30-second Showdown Countdown**.

---

##Prototype Features

### 1. Player Actions (5+ Distinct World-Altering Actions)
1. **Object Collection & Extraction (`Walk Over` / `E`):** Collect rotating Red Ruby Gems spawned around the central Mine Cart (or press `E` at the Mine Cart to extract a bonus vein).
2. **Destructible Cover & Explosive Barrels (`Left-Click` Shoot / `G` Dynamite):** Fire the Six-Shooter Revolver or throw lit Dynamite sticks to shatter **Wooden Supply Crates** (dropping hidden Red Gems) and detonate **Black-Powder TNT Barrels** in chain-reaction AoE blasts.
3. **Scripted World Events (`E` / Step Trigger):**
   - Swing open the **Saloon Batwing Doors** (`E`) to ignite interior chandeliers.
   - Unlock the **Sheriff's Armory Vault Gate** automatically upon collecting **5 Gems**.
   - Step onto **Steam-Powered Balcony Jump Pads** to launch onto elevated Saloon and Mine roofs.
   - Trigger the **30-Second Showdown Siren & Cutscene** upon reaching **10 Gems**.
4. **Environmental State Change (`E` / Shoot Generator Switch):** Flip the **Arena Generator & Defense Lever** near the Water Tower to dynamically raise the **Central Sniper Platform**, raise **Motorized Iron Cover Barriers**, and power on the **Overhead Town Floodlights**.
5. **Affecting NPC Behavior & Autonomous Units (`Left-Click` / `E` / `Q` / `H`):**
   - **Enemy Outlaw Bots (`Guard` & `Lu` from Adam Character Pack):** Patrol the town, transition to `AlertChase` when approached or shot, stun when pistol-whipped (`E`) or hit by dynamite, and drop 2 Red Gems when eliminated.
   - **Friendly Duo Partner (`Adam` from Adam Character Pack):** Press `E` near Adam to cycle his tactical AI state (`FollowPlayer`, `DefendMineCart`, `Patrol`).
   - **Deployable Sheriff Auto-Turret (`Q`) & Frontier Healing Totem (`H`):** Spawn autonomous combat and healing units on the battlefield.
6. **Modifying Player Stats, Inventory & HUD:** Collecting Red Gems, **Frontier Health Elixirs (+250 HP)**, and **Six-Shooter Ammo Crates** updates the live HUD, unlocks the 5-gem Armory Gate, and triggers the 10-gem countdown.

### 2. Camera Movements (Cinemachine 3)
- **Third-Person Follow View (`[1]` or `[C]`):** Over-the-shoulder `CinemachineThirdPersonFollow` camera with **Right-Click (`Mouse1`)** smooth ADS zoom.
- **First-Person POV View (`[2]` or `[C]`):** Eye-level `CinemachineCamera` for precision gunslinger aiming.
- **Cinematic / Objective Transition Camera (`[3]` or `[V]`):** Smoothly blends via `CinemachineBrain` onto the Central Mine Cart & Rising Sniper Platform when flipping the Generator Switch, reaching 10 Gems, or pressing `V`/`3`.
- **Tactical Arena & Outer Vista Overview (`[4]`):** High-angle `CinemachineCamera` showcasing the town arena and surrounding non-playable canyon terrain.

### 3. Lighting Behaviors
- **Day/Night Cycle & Time Shift (`[T]` Step / `[Y]` Auto-Cycle):** Smoothly transitions directional sun/moon angle, color temperature, ambient lighting, and atmospheric fog across **High Noon**, **Crimson Sunset**, and **Midnight Overtime**.
- **Player-Controlled Western Lantern & Aim Spotlight (`[F]` Toggle):** Toggleable personal oil lantern (`PointLight` + `SpotLight` cone) attached to the player hero.
- **Environment- & Event-Triggered Lighting:**
  - Entering the **Gold Mine Cavern** or **Saloon Interior** trigger zones dims exterior sunlight and boosts flickering wall torches.
  - Flipping the **Arena Generator Switch** powers on 4 overhead **Town Floodlights**.
  - Reaching **10 Gems** activates pulsing crimson **Showdown Alarm Beacons** around the central Mine Cart.

---

## Controls Summary

| Key / Input | Action |
| :--- | :--- |
| **W, A, S, D** | Move Gunslinger |
| **Left Shift** | Sprint |
| **Space** | Jump |
| **Left-Click** | Fire Six-Shooter Revolver |
| **Right-Click (Hold)** | Precision Aim Down Sights (Cinemachine FOV & Distance Blend) |
| **R** | Reload Revolver (6 Rounds) |
| **E** | Interact (Flip Generator Lever, Open Doors, Command Duo Partner, Mine Cart) |
| **G** | Throw Lit Dynamite Stick (AoE Explosion) |
| **Q** | Deploy Autonomous Sheriff Auto-Turret |
| **H** | Deploy Frontier Healing Totem (+HP Aura) |
| **F** | Toggle Player Western Lantern & Aim Spotlight |
| **C** (or **1 / 2**) | Toggle Cinemachine 3rd-Person / 1st-Person Camera |
| **V** (or **3 / 4**) | Trigger Cinemachine Objective Cutscene / Arena Overview Camera |
| **T** / **Y** | Step Time-of-Day Preset (Noon / Sunset / Midnight) / Toggle Fast Auto-Cycle |
| **M** | Swap Playable Hero Skin (`Guard`, `Lu`, `Adam`, `Kyle`) |
| **Tab** | Show / Hide Live Assignment 2 Verification Checklist |
| **Esc** | Lock / Unlock Mouse Cursor |
