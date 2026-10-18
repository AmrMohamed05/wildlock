# Assignment 2: Game World and Game Mechanics
**Course:** Video Game Design & Development  
**Project:** Wildlock (2v2 Multiplayer Hero Arena Shooter)  
**Student:** Amr Mohamed Salah (ID: 58-12817)  
**Engine & Pipeline:** Unity 6 (`6000.3.24f1`), Universal Render Pipeline (URP), Cinemachine 3, Input System  
**Repository:** [https://github.com/AmrMohamed05/wildlock.git](https://github.com/AmrMohamed05/wildlock.git)  

---

## Executive Summary & Alignment
*Wildlock* is an intense, competitive 2v2 3D Hero Arena Shooter set in an atmospheric, stylized Wild West frontier town. Teams of two gunslingers battle over a contested central gold mine cart that regularly unearths radiant ruby gemstones. To secure victory, a duo must collect a combined total of **10 gems** and survive a frantic **30-second Showdown countdown** while the opposing team mounts desperate counter-attacks to eliminate the gem carrier, spill their hoarded gems, and reset the clock.

This document represents the complete, exhaustive specification for **Part 1 (Game World)** and **Part 2 (Game Mechanics)** of Assignment 2, directly expanding upon the foundational concepts established in Assignment 1 and implemented in the Unity 6 URP working prototype (**Part 3**).

---

# Part 1: Game World

```
+--------------------------------------------------------------------------------------------------------+
|                                      OUTER CANYON VISTA TERRAIN                                        |
|                          (900m x 900m Terraced Red-Rock Mesas & Distant Peaks)                         |
|                                                                                                        |
|            +------------------------------------------------------------------------------+            |
|            |                 NATURAL SANDSTONE BOULDER PERIMETER BOUNDARY                 |            |
|            |                                                                              |            |
|            |   +--------------------------+                +--------------------------+   |            |
|            |   |   GENERAL STORE DEPOT    |                |    GOLD MINE CAVERN      |   |            |
|            |   | (Rooftop Vantage & Crate |                | (Dark-Zone Cavern Choke, |   |            |
|            |   |     Supply Stacks)       |                |   Torchlit Ore Tracks)   |   |            |
|            |   +--------------------------+                +--------------------------+   |            |
|            |                |                                            |                |            |
|            |                v                                            v                |            |
|            |   ========================================================================   |            |
|            |                      CENTRAL THOROUGHFARE / MAIN STREET                      |            |
|            |          [West Barrier]     [CENTRAL RUBY MINE CART]     [East Barrier]      |            |
|            |                             (Spawns Gem every 7.0s)                          |            |
|            |          [Jump Pad 2]       [RISING SNIPER PLATFORM]      [Jump Pad 1]       |            |
|            |   ========================================================================   |            |
|            |                ^                                            ^                |            |
|            |                |                                            |                |            |
|            |   +--------------------------+                +--------------------------+   |            |
|            |   |  SILVER HORSESHOE SALOON |                |  SHERIFF'S ARMORY VAULT  |   |            |
|            |   |  (Swinging Batwing Doors,|                |  (Fortified Brass Gate,  |   |            |
|            |   |   High-Ground Balcony)   |                |  Unlocks at 5 Gems)      |   |            |
|            |   +--------------------------+                +--------------------------+   |            |
|            |                                                                              |            |
|            +------------------------------------------------------------------------------+            |
|                                                                                                        |
+--------------------------------------------------------------------------------------------------------+
```

---

## 1. Locations: Distinct Environments, Landscapes & Transitions

The game world of *Wildlock* is centered around **"Saloon Gulch"**, an iconic frontier mining settlement situated in an arid, sun-bleached desert basin surrounded by towering red-rock mesas. The world features six interconnected, tactically distinct zones that seamlessly transition between blinding sunlight, indoor shadows, and underground subterranean darkness:

### 1.1. The Central Thoroughfare & Mining Chokepoint
* **Terrain & Landscape:** A flat, wide dirt thoroughfare compacted by carriage traffic and boots, blanketed in fine red-ochre dust. Symmetrically positioned at the center of the town arena at coordinates `(-124.8, 0, -10)`.
* **Environmental Characteristics:** Directly exposed to blazing desert sunlight during daytime. Flanked on either side by boardwalk storefronts. Stacks of weathered pine shipping crates, iron-banded powder barrels, and abandoned stagecoach carts break line-of-sight across the 16-meter-wide thoroughfare.
* **Tactical Role:** Houses the primary objective—the **Central Ruby Mine Cart**. All combat naturally funnels into this high-stakes corridor. Controlling the ground around the cart is essential for gathering newly unearthed gems.

### 1.2. The Silver Horseshoe Saloon & Elevated Balconies
* **Terrain & Landscape:** A weathered two-story timber saloon featuring a covered front porch, dual swinging batwing louver doors, and an exterior wrap-around upper balcony standing 4.8 meters above ground level.
* **Environmental Characteristics:** The interior is a recognized **Dark Zone**; stepping across the threshold immediately triggers an eye-adaptation ambient darkening, while warm incandescent oil lanterns cast deep amber shadows across the mahogany bar counter and poker tables.
* **Transitions & Flow:** Players can enter through the front swinging doors at street level, drop down from the second-story balcony rail, or launch directly onto the balcony from the ground using the adjacent steam-powered jump pad.
* **Tactical Role:** Provides critical high-ground sniper vantage over the Central Mine Cart. Defenders on the balcony enjoy partial waist-high wooden railing cover against ground-level revolver fire.

### 1.3. Rattlesnake Gulch Gold Mine & Subterranean Caverns
* **Terrain & Landscape:** A deep, jagged rock excavation reinforced with heavy oak timber shoring timbers, rail tracks, and abandoned iron ore carts emerging from the northern sandstone bluffs.
* **Environmental Characteristics:** Cold, claustrophobic subterranean environment. Entering the mine cavern triggers an acute environmental lighting transition where exterior sunlight cuts off abruptly, leaving visibility dependent on flickering kerosene wall sconces and the player's personal oil lantern.
* **Transitions & Flow:** Narrow 3.5-meter tunnel width creates a brutal chokepoint connecting the northern boundary directly to the central mine cart clearing.
* **Tactical Role:** Flanking corridor and emergency retreat zone. Gem carriers frequently pull back into the mine's cavern mouth where long sightlines are broken, forcing enemies into close-quarters shotgun and dynamite range.

### 1.4. The Frontier General Store & Depot Overlook
* **Terrain & Landscape:** A sturdy, flat-topped clapboard mercantile building on the western perimeter, surrounded by burlap sacks, whiskey barrel clusters, and wooden shipping pallets.
* **Environmental Characteristics:** Shaded boardwalk porch with corrugated tin overhangs. Offers multiple jump-pad approach routes onto its reinforced rooftop.
* **Transitions & Flow:** Connects the western spawn corridor to the main thoroughfare. The alleyway between the General Store and the Saloon forms a tight secondary lane ideal for ambush setups.
* **Tactical Role:** The flat rooftop provides an unobstructed 360-degree firing platform overlooking both the western flank and the generator switch station.

### 1.5. The Sheriff's Fortified Armory & Underground Vault
* **Terrain & Landscape:** A heavy stone-and-iron vault built into the eastern rock wall at `(-108.5, 0, 6.2)`. Blocked by a massive reinforced brass portcullis gate.
* **Environmental Characteristics:** Features a mechanical gem receptacle. The gate remains locked and barred until a team deposits at least **5 gems**.
* **Transitions & Flow:** Once unlocked, the heavy brass gates swing open with a resounding iron clatter, unlocking access to the vault interior.
* **Tactical Role:** Houses premium high-tier supply caches (Greater Health Elixir Flask and Full Ammo/Cooldown Refill Crate), rewarding teams that secure early gem dominance with late-game survival resources.

### 1.6. The Red-Rock Canyon Foothills & Mesa Vista (Non-Playable Vista Terrain)
* **Terrain & Landscape:** A massive 900-meter by 900-meter photorealistic desert mountain terrain utilizing Unity's PBR Terrain system. Features multi-tiered stepped sandstone mesas, jagged ridgelines, erosion gullies, and distant mountain peaks rising 135 meters above the valley floor.
* **Environmental Characteristics:** Non-playable background vista painted with 4 distinct PBR splatmap layers: fine windblown desert sand, coarse gravelly soil, layered red canyon rock, and sun-baked dry scrub grass. Populated with 75 procedural dry desert shrubs and heather foliage along the foothills.
* **Transitions & Flow:** The town arena sits seamlessly in a sculpted natural depression in the canyon floor. Sandstone boundary boulders physically contain players inside the 60m x 60m town while the majestic vista stretches uninterrupted to the horizon, ensuring complete visual immersion without artificial skybox seams.

---

## 2. Objects: Comprehensive Inventory of 32 In-Game Objects

| # | Object Name | Category | World Placement | Gameplay Role, Significance & Usability |
|---|---|---|---|---|
| **1** | **Peacemaker Revolver** | Weapon | Default Player Weapon | Six-shot single-action service revolver. Deals 200 hitscan damage per shot. Primary weapon for medium-range skirmishing. |
| **2** | **Frontier Repeater Rifle** | Weapon | Equipped by NPCs / Hero | Lever-action long rifle. Fires high-velocity rounds dealing 350 damage with minimal falloff. Ideal for balcony snipers. |
| **3** | **Frontier Shock Pilum** | Weapon | Carried by Outlaw Enforcer | Electrified frontier polearm used for melee thrusts and area denial, shocking anyone within a 2.5m arc. |
| **4** | **Frontier Dynamite Stick** | Weapon / Ability | Thrown via `[G]` Ability | Lit explosive ordnance thrown in an arc. Detonates after a 1.8s fuse, dealing 320 AoE damage and inflicting a 3.0s stun. |
| **5** | **Sheriff Auto-Turret** | Combat Deployable | Deployed via `[Q]` Ability | Clockwork tripod turret equipped with dual rotating barrels. Automatically tracks and shoots enemy outlaws within 18m. |
| **6** | **Frontier Healing Totem** | Support Deployable | Deployed via `[H]` Ability | Carved medicinal wooden pillar radiating a verdant light aura. Grants +120 instant HP and pulses +15 HP/sec in a 7.5m radius. |
| **7** | **Central Ruby Mine Cart** | Objective Container | Map Center `(-134, 0, 5)` | Heavy cast-iron ore cart on rails. Holds the pulsating mother ruby and automatically spawns loose gems every 7.0 seconds. |
| **8** | **Radiant Ruby Gemstone** | Primary Resource | Spawns from Cart / Crates | Floating, rotating red crystalline gem. Collecting 10 triggers the 30-second Showdown countdown for victory. |
| **9** | **Golden Mineral Nugget** | Secondary Resource | Drops from destroyed props | Collectible mineral worth 50 bonus team score points, accelerating ultimate ability charge meters. |
| **10** | **Emerald Relic Shard** | Objective Key Item | Hidden in Armory Vault | Rare ancient gemstone required to trigger the secret saloon emergency storm shutter lockdown. |
| **11** | **Raw Iron Ore Chunk** | Environmental Physics | Scattered along Mine Tracks | Dynamic rigidbodies that can be kicked, shot, or knocked into enemies to create minor physical distractions. |
| **12** | **Generator Lever Switch Station** | Interactive Device | Main Street `(-115, 0, 3.8)` | Industrial brass lever with LED indicator orb. Interacting (`[E]`) or shooting pulls lever, raising arena cover and platforms. |
| **13** | **Rising Central Sniper Platform** | Dynamic Environment | Center Chokepoint | Heavy iron motorized scissor platform. Rises from `y=-0.35m` to `y=2.5m` upon switch toggle, providing central high ground. |
| **14** | **Rising Iron Cover Barrier (East)** | Dynamic Environment | East Thoroughfare | 1.7m tall steel reinforced bulletproof barricade that elevates from beneath boardwalk planks to block long sightlines. |
| **15** | **Rising Iron Cover Barrier (West)** | Dynamic Environment | West Thoroughfare | Symmetrical 1.7m barricade providing crucial waist-high cover against balcony snipers during gem pickup runs. |
| **16** | **Saloon Batwing Louver Doors** | Interactive Door | Saloon Entrance | Dual swinging spring-loaded wooden louver doors. Swing open smoothly when walked through or shot, then oscillate shut. |
| **17** | **Sheriff Armory Vault Gate** | Interactive Gate | East Strongroom `(-108.5, 0, 6.2)` | Massive reinforced brass portcullis gate. Automatically unbars and swings open once player accumulates 5 gems. |
| **18** | **Saloon Balcony Steam Jump Pad** | Navigation Device | South Alley `(-117.5, 0, -15.5)` | Brass pneumatic launch pad glowing with cyan light. Stepping on pad launches gunslingers 7.5m vertically onto balcony. |
| **19** | **Mine Overlook Steam Jump Pad** | Navigation Device | North Bluff `(-135.5, 0, -3.5)` | High-pressure pneumatic launch disc propelling players 6.8m upward onto the gold mine timber scaffolding. |
| **20** | **Showdown Red Alarm Beacons (x4)** | Event Lighting | Encircling Mine Cart | High-intensity red beacon strobes. Remain dark until the 10-gem Showdown begins, then pulse rhythmically with sirens. |
| **21** | **Town High-Voltage Floodlights (x4)**| Interactive Lighting | Elevated Street Poles | Four 22-intensity industrial spot floodlights wired to the town generator. Switch on instantly when generator is pulled. |
| **22** | **Frontier Kerosene Porch Torches** | Static / Zone Lighting | Mine & Saloon Entrances | Flickering point lights (`intensity=5.5`, warm orange hue) that illuminate building thresholds and cast dynamic shadows. |
| **23** | **Brass Frontier Oil Lantern** | Player Equipment | Attached to Player Hip | Hip-mounted oil lantern toggled via `[F]`. Emits a 15m warm point-light sphere and a 32m directional aim spotlight. |
| **24** | **Frontier Wooden Supply Crate** | Destructible Prop | Scattered through town | Weathered pine shipping crate (200 HP). Absorbs bullet impacts; shatters with wood splinter particles and drops gems. |
| **25** | **Black-Powder Explosive Barrel** | Destructible Hazard | Red Barrels near Chokepoints | Volatile gunpowder barrel (150 HP). Detonates in a 6.8m fiery blast upon bullet penetration, damaging nearby players/NPCs. |
| **26** | **Frontier Health Elixir Flask** | Consumable | Armory & Saloon Porch | Glowing green glass flask. Instantly restores +250 HP upon collision and emits a restorative chime sound effect. |
| **27** | **Ammo & Energy Supply Crate** | Consumable | Armory & Mine Overlook | Golden brass ammunition container. Instantly refills revolver to max capacity (6/6) and resets all ability cooldowns. |
| **28** | **Covered Stagecoach Wagon** | Static Cover Prop | West Road Entrance | Full-scale horse-drawn frontier stagecoach. Provides full body cover against rifle crossfire between spawn points. |
| **29** | **Elevated Water Tanker Tower** | Architectural Prop | Northwest Town Plaza | 14m high wooden stilt tower with round water vat. Serves as a primary orienting landmark and visual focal point. |
| **30** | **Weathered Wanted Poster Signboard**| Narrative Prop | Center Crossroads | Wooden frontier signboard displaying bounties for "Outlaw Guard" and "Outlaw Lu", establishing narrative context. |
| **31** | **Perimeter Sandstone Barrier Boulders**| Boundary Prop | Town Ring Perimeter | Massive sculpted desert sandstone rock formations that physically block players from walking off the town floor. |
| **32** | **Dry Desert Brush & Heather Foliage** | Vista Prop | Outer Foothills | Procedural desert shrubbery and windblown scrub grass scattered across canyon slopes to enrich horizon realism. |

---

## 3. Scale: Physical Units, Dimensions & Spatial Consistency

The entire world of *Wildlock* is constructed strictly in real-world metric units ($1.0\text{ Unity Unit} = 1.0\text{ meter}$), ensuring authentic spatial proportions between character locomotion, camera frustums, and bullet ballistics:

### 3.1. Total Playable & World Dimensions
* **Playable Western Town Arena:** Exactly **$56.0\text{ m} \times 56.5\text{ m}$** ($3,164\text{ m}^2$), bounded on the X-axis from $X = -153.5\text{ m}$ to $X = -97.5\text{ m}$, and on the Z-axis from $Z = -38.5\text{ m}$ to $Z = +18.0\text{ m}$.
* **Vertical Bounds:** Ground datum level is $Y = 0.0\text{ m}$. Low ground mine trenches sit at $Y = -0.5\text{ m}$. Elevated saloon balconies sit at $Y = +4.8\text{ m}$. Rooftops reach $Y = +7.5\text{ m}$, and the Water Tower spire stands at $Y = +14.2\text{ m}$.
* **Outer Canyon Vista Terrain:** Spans **$900\text{ m} \times 900\text{ m}$** ($0.81\text{ km}^2$), centered at $X = -124.8\text{ m}, Z = -10.0\text{ m}$. Heightmap elevation ascends smoothly from $0.0\text{ m}$ at the arena rim to $+135.0\text{ m}$ at the perimeter mountain ridgeline.

### 3.2. Relative Sizes & Spatial Consistency
* **Characters (Heroes & NPCs):** Standard humanoid height of **$1.80\text{ m}$** with a collision capsule radius of **$0.32\text{ m}$** ($0.42\text{ m}$ for armored Outlaws). Eye level sits naturally at $Y = 1.66\text{ m}$.
* **Doorways & Archways:** Saloon and shop doorways measure $2.4\text{ m}$ high by $1.4\text{ m}$ wide, allowing two characters to pass through shoulder-to-shoulder with comfortable clearance.
* **Cover Objects:** Supply crates are uniform $1.1\text{ m}$ cubes (ideal half-cover when crouching or waist-high protection while running). Barrels measure $0.85\text{ m}$ diameter by $0.95\text{ m}$ height.
* **Streets & Engagement Ranges:** The central thoroughfare is $16.0\text{ m}$ wide, matching the optimal effective spread of the Peacemaker Revolver ($12\text{ m} - 25\text{ m}$). The distance from the Saloon Balcony to the Central Mine Cart is exactly $24.5\text{ m}$, creating a perfectly balanced sniper engagement zone.

---

## 4. Spatial Boundaries: Physical, Conceptual & Technical Containment

```
[Outer Vista Mountains (900m)] --> [Sandstone Boulder Ring (60m)] --> [Code Bounding Box Clamp] --> [Town Arena]
```

To maintain high-intensity 2v2 tactical gameplay without immersion-breaking invisible barriers, *Wildlock* utilizes a multi-layered boundary philosophy:

1. **Natural Perimeter Containment (Physical):** The outer boundary of the playable town is ringed by massive, interlocking natural desert sandstone boulder props (`Rock (12)`, `Rock (6)`, `Rock (3)`). These rock walls rise 4 to 8 meters into the air, preventing players from jumping or glitching out of the town settlement.
2. **Architectural Choke Point Borders (Physical):** The eastern and western borders are hemmed in by solid building exterior walls (General Store, Stables, Carriage Depots), naturally terminating player traversal and channeling all flow toward the center.
3. **Conditional Vault Obstacles (Mechanics-Gated):** The Sheriff's Armory Vault on the eastern bluff is physically blocked by an iron-barred gate with a hard trigger condition (`requiredGemsToUnlock = 5`). Players cannot enter until collecting 5 gems, creating a mid-match map expansion.
4. **Code-Level Safety Enclosure (Game Logic):** In [`WildlockPlayerController.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockPlayerController.cs#L233-L248), a strict hardware boundary clamp runs every frame:
   $$\text{clampedX} = \text{Mathf.Clamp}(\text{pos.x}, -153.5\text{f}, -97.5\text{f})$$
   $$\text{clampedZ} = \text{Mathf.Clamp}(\text{pos.z}, -38.5\text{f}, 18.0\text{f})$$
   If any physics glitch or blast knockback pushes a character past the perimeter rocks, the controller smoothly constrains position without freezing movement. A fail-safe respawn triggers if $Y < -12.0\text{ m}$.
5. **Non-Playable Vista Illusion (Conceptual):** The surrounding 900m terrain has a flat 58m inner radius basin where it hugs the underside of the town floor (`y = -0.35m`), then rolls upward into canyon bluffs. Players can clearly see the horizon, mesas, and vegetation across the perimeter rocks, eliminating any sense of an artificial "floating skybox box".

---

## 5. Temporal Dimension: Time Flow, Cycles & Showdown Countdown

Time in *Wildlock* is a dynamic gameplay mechanic operating on three distinct, interconnected layers:

```
[Match Clock (Real-Time 3-5m)] ---> [Gem Spawner (7.0s Cycles)] ---> [Showdown Countdown (30.0s Climax)]
                                                                               |
                                                                               v
                                                    [Dynamic Lighting Shift: Day -> Sunset -> Night]
```

### 5.1. Real-Time Flow & Match Pacing
* Matches are designed for rapid, high-adrenaline sessions lasting between **3 and 5 minutes**. Time advances uniformly in real-time ($1.0\text{ game second} = 1.0\text{ real second}$).
* The moment-to-moment cadence is governed by the **7-second Gem Spawn Pulse**. Every 7 seconds, the central mine cart emits a metallic chime and heaves upward, ejecting a new Ruby Gem into the contested arena.

### 5.2. The 30-Second Showdown Climax
* When either team’s combined inventory reaches **10 gems**, the game state shifts instantly into the **Showdown Phase**.
* A persistent 30.0-second countdown timer appears on the central HUD alongside a blaring frontier siren.
* **Tug-of-War Reset Mechanic:** If the opposing duo manages to eliminate a gem carrier, their inventory spills across the ground. If the leading team's active gem total drops below 10, the 30-second countdown **immediately aborts and resets to 30.0 seconds**, reverting the match to neutral territory control.

### 5.3. Dynamic Day/Night Lighting Shift
* Players can accelerate or shift world time manually via `[T]` or watch time transition dynamically as the match progresses:
  * **High Noon (Day):** Golden, harsh desert sun ($65^\circ$ elevation, warm $5500\text{K}$ color, sharp contact shadows).
  * **Twilight Sunset (Dusk):** Long, dramatic purple and amber shadows ($15^\circ$ elevation, deep orange directional light).
  * **Desert Starlight (Night):** Cool indigo ambient illumination ($0.15\text{ lux}$), where player oil lanterns, kerosene torches, and town floodlights become primary tactical navigation tools.
* During the Showdown countdown, four pulsing red alarm beacons strobe in sync with the siren audio, visually saturating the central mine cart in emergency red light.

---

# Part 2: Game Mechanics

---

## 1. Mechanics Count: Detailed Analysis of 22 Distinct Game Mechanics

```
+---------------------------------------------------------------------------------------------------------+
|                                    22 CORE GAME MECHANICS ARCHITECTURE                                  |
+------------------------------------+------------------------------------+-------------------------------+
| LOCOMOTION & TRAVERSAL             | COMBAT & ABILITIES                 | OBJECTIVES & WORLD            |
| 1. Hitscan Locomotion & Strafing   | 7. Peacemaker Revolver Hitscan     | 13. Mine Cart Spawner Engine  |
| 2. Sprinting & Velocity Scaling    | 8. Revolver Ammo & Manual Reload   | 14. Proximity Gem Pickup      |
| 3. Parabolic Jumping & Gravity     | 9. Dynamite Arc Trajectory Physics | 15. 10-Gem Showdown Climax    |
| 4. Pneumatic Jump Pad Launch       | 10. Concussive AoE Stun & Knockback| 16. Inventory Spilling On Death|
| 5. Camera-Decoupled Aim Facing     | 11. Autonomous Turret AI Tracking  | 17. Universal Raycast Interact|
| 6. Boundary Spatial Clamping       | 12. Healing Totem Pulse Radiance   | 18. Environment Switch Toggle |
|                                    |                                    | 19. Saloon Door Hinge Physics |
|                                    |                                    | 20. 5-Gem Armory Gate Unlock  |
|                                    |                                    | 21. Destructible Prop Fracture|
|                                    |                                    | 22. Explosive Barrel Detonation|
+------------------------------------+------------------------------------+-------------------------------+
```

### M01: Hitscan Locomotion & Ground Kinematics
* **System:** CharacterController kinematic movement driven by Unity Input System.
* **Parameters:** Walk velocity $= 4.6\text{ m/s}$, Sprint velocity $= 7.8\text{ m/s}$, grounded downward velocity snap $= -2.0\text{ m/s}$.
* **Details:** Evaluates horizontal input vectors relative to camera orientation, computing forward and strafe vectors that preserve momentum over uneven terrain without jitter.

### M02: Dynamic Camera-Aim Body Alignment
* **System:** Procedural torso and body yaw orientation.
* **Parameters:** Free 3rd-person turning during forward run; hard aim-lock to camera yaw (`_yaw`) during firing, ADS (`[RMB]`), FPS mode (`[V]`), or backward backpedaling (`[S]`).
* **Details:** Prevents the gunslinger from ever turning around to stare back at the camera when retreating. A $0.6\text{s}$ post-fire aim timer maintains an authentic over-the-shoulder shooting stance.

### M03: Parabolic Jump Physics
* **System:** Velocity-based vertical impulse using kinematic equations:
  $$v_0 = \sqrt{2 \cdot |g| \cdot h_{\text{jump}}}$$
* **Parameters:** Jump height $h = 1.65\text{ m}$, Gravity $g = -18.0\text{ m/s}^2$. Initial launch velocity $= 7.71\text{ m/s}$.
* **Details:** Requires solid ground contact; sets Animator boolean `Jump` and triggers airborne free-fall state if vertical velocity drops below $-3.0\text{ m/s}$.

### M04: Pneumatic Steam Jump Pad Launch
* **System:** External vertical force override via `WildlockJumpPad`.
* **Parameters:** Saloon Balcony Pad launch height $= 7.5\text{ m}$; Mine Overlook Pad launch height $= 6.8\text{ m}$.
* **Details:** Triggers a steam puff particle burst, plays a pneumatic air hiss sound effect, and instantly sets player vertical velocity to $\sqrt{2 \cdot 18 \cdot 7.5} \approx 16.43\text{ m/s}$, catapulting players onto high-ground perches.

### M05: Peacemaker Revolver Ballistic Hitscan
* **System:** Camera viewport center raycast with weapon muzzle tracer synchronization.
* **Parameters:** Damage $= 200\text{ HP}$, Cooldown $= 0.28\text{s}$, Max Range $= 75.0\text{ m}$, Ammo Capacity $= 6$ rounds.
* **Details:** Fires a raycast from `(0.5, 0.5)` viewport coordinates through the crosshair. Renders a two-point glowing tracer line for $0.065\text{s}$ from the revolver barrel to the impact point, spawning spark particles and audio upon hit.

### M06: Six-Round Cylinder Reload System
* **System:** Timed ammo replenishment coroutine.
* **Parameters:** Reload Duration $= 1.0\text{s}$.
* **Details:** Pressing `[R]` (or firing with an empty cylinder) triggers cylinder ejection audio, locks firing capability for $1.0\text{s}$, and restores ammo to $6/6$.

### M07: Physics-Based Dynamite Arc Trajectory
* **System:** 3D ballistic rigidbody projectile launch.
* **Parameters:** Throw velocity $= 13.5\text{ m/s}$, Upward arc bias $= +0.35$, Angular spin torque $= 8.0\text{ rad/s}$.
* **Details:** Instantiates a lit dynamite stick with an active point light and spark emitter. Adds combined forward and upward impulse, tumbling dynamically over crates and obstacles.

### M08: Dynamite Concussive AoE Detonation & Stun
* **System:** Spherical damage and status infliction overlap check.
* **Parameters:** Fuse $= 1.8\text{s}$, Blast Radius $= 6.8\text{ m}$, Damage $= 320\text{ HP}$, Stun Duration $= 3.0\text{s}$.
* **Details:** Detonates in a 50-particle fiery blast. Queries all colliders in radius: shatters destructible crates, ignites nearby explosive barrels, deals 280 damage to Outlaw NPCs, and paralyzes their AI state machine for 3 seconds.

### M09: Autonomous Sheriff Auto-Turret AI
* **System:** SphereCast target acquisition and tracking state machine.
* **Parameters:** Range $= 18.0\text{ m}$, Burst rate $= 0.35\text{s}$, Damage per bullet $= 45\text{ HP}$, Duration $= 25.0\text{s}$.
* **Details:** Deployed via `[Q]`. Employs horizontal turret head rotation targeting the closest enemy Outlaw within line-of-sight, firing tracer bursts until the target is destroyed or leaves range.

### M10: Frontier Healing Totem Pulse Radiance
* **System:** Area-of-effect restorative aura logic.
* **Parameters:** Radius $= 7.5\text{ m}$, Instant heal $= +120\text{ HP}$, Continuous regeneration $= +15\text{ HP/s}$, Duration $= 15.0\text{s}$.
* **Details:** Deployed via `[H]`. Projects a verdant light aura and periodic rising healing particles. Any friendly player within the cylinder receives continuous health ticks up to the $1000\text{ HP}$ ceiling.

### M11: Mine Cart Continuous Gem Spawning Engine
* **System:** Periodic objective generator script.
* **Parameters:** Interval $= 7.0\text{s}$, Cart position $= (-134, 0.05, 5.1)$.
* **Details:** Evaluates real-time clock; every 7 seconds, plays a mechanical clank, elevates the central ruby model, and instantiates a collectible gem physics entity that gently bobs above the cart.

### M12: Proximity Gem Resource Collection
* **System:** Trigger collider collection and inventory bookkeeping.
* **Parameters:** Pickup radius $= 1.8\text{ m}$, Bob amplitude $= 0.22\text{ m}$, Bob frequency $= 2.4\text{ rad/s}$.
* **Details:** Walking over a spawned gem destroys the world entity, increments the team's gem inventory count on the HUD, plays a crystalline chime sound effect, and triggers an assessment of the 10-gem victory threshold.

### M13: 10-Gem Showdown Climax State Machine
* **System:** Game-mode objective state evaluator in `WildlockGameManager`.
* **Parameters:** Activation Threshold $= 10$ gems, Countdown Duration $= 30.0\text{s}$.
* **Details:** Once team gem count $\ge 10$, initiates the Showdown phase. Activates the 4 pulsing red emergency beacons around the cart, accelerates background audio tempo, and displays the top-screen countdown timer.

### M14: Gem Dropping & Inventory Spilling on Elimination
* **System:** Death penalty resource scatter logic.
* **Parameters:** Gem drop $= 100\%$ of carried inventory.
* **Details:** When a gunslinger carrying gems is eliminated, all carried gems are scattered radially around their death location as physical pickups. If the team total drops below 10, the Showdown countdown immediately aborts.

### M15: Universal Aim & Proximity Interaction System
* **System:** Two-tier raycast + overlap sphere query (`IWildlockInteractable`).
* **Parameters:** Raycast reach $= 6.5\text{ m}$, Overlap radius $= 4.2\text{ m}$, Interact key $= [E]$.
* **Details:** Highlights hovered interactable objects on the HUD. Pressing `[E]` triggers context-sensitive actions: pulling levers, pushing doors, opening gates, or looting caches.

### M16: Generator Lever Environmental State Toggle
* **System:** Multi-object mechanical animator in `WildlockEnvironmentSwitch`.
* **Parameters:** Lever rotation $= 42^\circ \leftrightarrow -42^\circ$, Indicator color $=$ Red $\leftrightarrow$ Green.
* **Details:** Activating or shooting the switch station smoothly raises the Central Sniper Platform ($Y: -0.35\text{m} \rightarrow +2.5\text{m}$) and both East/West iron barricades ($Y: -0.8\text{m} \rightarrow +0.9\text{m}$), while illuminating the 4 high-voltage street floodlights.

### M17: Spring-Hinged Saloon Door Simulation
* **System:** Dual-pivot angular swing simulation in `WildlockSaloonDoor`.
* **Parameters:** Swing angle $= \pm 85^\circ$, Damping return $= 4.5\text{ rad/s}$.
* **Details:** Passing through the saloon entrance pushes both door wings outward in the direction of traversal. Releasing the trigger causes the wings to oscillate back and forth with realistic wooden spring decay.

### M18: 5-Gem Armory Strongroom Gated Access
* **System:** Inventory-validated boundary lock.
* **Parameters:** Unlock requirement $= 5$ gems.
* **Details:** Interacting with the Armory Vault gate queries `WildlockGameManager.playerGemCount`. If $<5$, the gate remains locked with an audio buzz and HUD alert; if $\ge 5$, the gate swings open permanently.

### M19: Destructible Prop Damage & Fracture System
* **System:** Health-based structural destruction in `WildlockDestructibleProp`.
* **Parameters:** Wooden crate HP $= 200$, Barrel HP $= 150$.
* **Details:** Absorbs revolver and dynamite impacts. Upon reaching $0\text{ HP}$, disables colliders and mesh renderers, spawns an explosion of wooden splinters/debris, and rolls a drop table for guaranteed ruby gems or health elixirs.

### M20: Explosive TNT Barrel Chain Reaction
* **System:** Hazardous explosive propagation logic.
* **Parameters:** Blast Radius $= 6.8\text{ m}$, Damage $= 320\text{ HP}$, Chain delay $= 0.1\text{s}$.
* **Details:** When an explosive red barrel reaches $0\text{ HP}$, it triggers a secondary explosion that immediately damages all nearby entities, detonating adjacent barrels in a cascading chain reaction.

### M21: Interactive Dark Zone Ambient Occlusion
* **System:** Volume-triggered ambient and light adaptation.
* **Parameters:** Mine entrance trigger box $= (8\text{m}, 4.5\text{m}, 8\text{m})$, Saloon porch box $= (11\text{m}, 4.5\text{m}, 9\text{m})$.
* **Details:** Entering the trigger volume dims global sunlight intensity and boosts local kerosene torches, mimicking human pupil dilation in dark interiors.

### M22: Multi-Perspective Cinemachine Camera Blending
* **System:** Cinemachine 3 virtual camera priority management.
* **Parameters:** Modes: 3rd-Person Follow (Priority 20), 1st-Person POV (Priority 5), Cutscene Transition (Priority 5), Tactical Overview (Priority 5).
* **Details:** Pressing `[V]` or `[RMB]` smoothly blends into FPS mode, hiding player character meshes to prevent clipping. Pressing `[C]` activates the cinematic mine cart zoom, and `[B]` transitions to the high-angle tactical overview.

---

## 2. Movement Mechanics: Locomotion, Controls, Physics & Immersion

```
[W/A/S/D Input] ---> [Relative to Camera Yaw] ---> [Sprint Mod (7.8m/s)] ---> [CharacterController.Move]
                                                        |                                   |
                                                        v                                   v
                                                [Jump Height (1.65m)]               [Perimeter Clamping]
```

### 2.1. Locomotion Modes & Control Schemes
Locomotion is mapped to standard PC shooter conventions using the Unity New Input System:
* **Forward / Backward / Strafe:** `W`, `A`, `S`, `D` or Arrow Keys. Vector input is normalized to prevent diagonal speed boosting.
* **Sprint Toggle / Hold:** `Left Shift`. Increases base movement speed by $+69.5\%$ ($4.6\text{ m/s} \rightarrow 7.8\text{ m/s}$).
* **Jump:** `Spacebar`. Available only when grounded (`_controller.isGrounded`).
* **Mouse Look:** High-precision raw mouse delta scaled by `mouseSensitivity = 0.16f`. Controls horizontal camera yaw ($\theta_{\text{yaw}} \in [0^\circ, 360^\circ]$) and clamped vertical pitch ($\phi_{\text{pitch}} \in [-45^\circ, +70^\circ]$).
* **Cursor Lock Toggle:** `Escape`. Toggles cursor between centered hardware capture and free OS pointer.

### 2.2. Physics Simulation, Gravity & Air Control
* **Simulated Gravity:** Calibrated at $-18.0\text{ m/s}^2$ (roughly $1.8\times$ Earth gravity). This elevated value is an intentional shooter design choice: it prevents floaty jumps and ensures crisp, responsive ground recovery.
* **Grounded Snap:** When grounded, a continuous downward velocity of $-2.0\text{ m/s}$ is applied to stick the character firmly to downhill slopes and wooden staircases without bouncing.
* **Airborne Trajectory:** In the air, players retain $85\%$ horizontal directional control, allowing skilled gunslingers to adjust landing positions or strafe onto elevated crates.

### 2.3. Terrain Challenges & Locomotion Immersion
* **Boardwalk Steps & Curbs:** The CharacterController `stepOffset` is set to $0.4\text{ m}$, allowing players to run seamlessly up saloon porch steps and sidewalk curbs without jumping.
* **Visual & Audio Feedback:** Forward velocity directly drives the Animator parameter `Speed`. When sprinting, camera follow damping dynamically tightens to convey speed, and rapid footstep dust puffs kick up from the player's boots.

---

## 3. Interaction Mechanics: Objects, NPCs & Player Agency

### 3.1. Dual-Tier Interaction Architecture
To ensure effortless object usability during fast-paced firefights, *Wildlock* implements a dual-tier scanning system:
1. **Precision Aim Raycast:** Casts from the camera center out to $6.5\text{ meters}$. If the crosshair is placed directly over an interactable (e.g. shooting or clicking the Generator Switch), it receives primary priority.
2. **Proximity Overlap Sphere:** If the aim raycast finds nothing, an automatic $4.2\text{-meter}$ overlap sphere around the player evaluates all nearby interactable triggers, selecting the nearest valid target.

### 3.2. Dialogue, Narrative & NPC Interactions
* **Friendly Duo Partner (Sheriff Adam):** Continuously monitors player distance. If the player moves more than $6.5\text{m}$ away, Adam dynamically switches from idle watch to `FollowPlayer` state, navigating through street cover. In combat, Adam tracks the same target as the player and provides covering fire.
* **Enemy Outlaws (Guard & Lu):** Feature distinct behavioral routines. Outlaw Guard patrols the southern thoroughfare between 4 waypoints, while Outlaw Lu guards the central mine cart perimeter. Inflicting damage interrupts their patrol and triggers aggressive combat pursuit.

---

## 4. Navigation Across Terrains: Verticality, Chokepoints & Mobility

```
[Ground Level (y=0m)] <==== Steam Jump Pad (7.5m) ====> [Saloon Balcony (y=4.8m)]
         |                                                              |
         v                                                              v
[Mine Trench (y=-0.5m)] <== Rising Platform (y=2.5m) ==> [General Store Roof (y=7.5m)]
```

### 4.1. Verticality & High-Ground Advantages
Vertical level design transforms *Wildlock* from a flat corridor brawler into a 3D tactical arena:
* **The Saloon Balcony:** Elevated $4.8\text{ meters}$ above the street. Grants full coverage of the mine cart while offering partial protection behind wooden balustrades.
* **The General Store Roof:** Accessible via crate parkour or high jumps, providing sniper dominance over the western road.
* **The Rising Sniper Platform:** A dynamic $4.5\text{m} \times 4.5\text{m}$ iron platform at the crossroads that raises gunslingers $2.5\text{ meters}$ into the air when the generator switch is pulled.

### 4.2. Overcoming Terrain Challenges
* **Steam Jump Pads:** Eliminate the slow climb up staircases. Stepping onto a pad catapults the player to balcony level in under $0.6\text{ seconds}$, enabling high-speed flanking routes.
* **Chokepoint Bypasses:** The northern Gold Mine cavern provides a subterranean bypass around the central street chokepoint, allowing duos to flank enemies who are heavily entrenched behind crates on the main road.

---

## 5. Weapon Usage: Profiles, Range, Ballistics & Cooldowns

```
+----------------------------------------------------------------------------------------------------+
|                                    WEAPON BALLISTIC MATRIX                                         |
+----------------------+-----------+---------+-----------+----------+---------------+----------------+
| WEAPON NAME          | TYPE      | DAMAGE  | COOLDOWN  | RANGE    | AMMO / FUSE   | SPECIAL EFFECT |
+----------------------+-----------+---------+-----------+----------+---------------+----------------+
| Peacemaker Revolver  | Hitscan   | 200 HP  | 0.28s     | 75m      | 6 Rounds (1s) | Snaps Aim Yaw  |
| Frontier Rifle       | Hitscan   | 350 HP  | 0.85s     | 150m     | 5 Rounds (2s) | Pinpoint Scope |
| Lit Dynamite Stick   | Projectile| 320 HP  | 5.00s     | Arc (25m)| 1.8s Fuse     | 3.0s AoE Stun  |
| Sheriff Auto-Turret  | AI Deploy | 45 HP/b | 10.00s    | 18m Rad. | 25s Lifetime  | 360° Tracking  |
| Frontier Shock Pilum | Melee Arc | 280 HP  | 1.10s     | 2.5m Arc | Infinite      | Micro-Stagger  |
+----------------------+-----------+---------+-----------+----------+---------------+----------------+
```

### 5.1. Peacemaker Six-Shooter Revolver
* **Attack Mechanics:** Single-action hitscan discharge. Bullet trajectories travel instantly to the crosshair with slight procedural recoil kick.
* **Damage & Falloff:** Deals $200\text{ damage}$ up to $35\text{ meters}$, with linear falloff reducing damage to $120\text{ damage}$ at maximum $75\text{ meters}$.
* **Reload Cycle:** Six chambers; reloading takes $1.0\text{s}$ and can be manually initiated anytime via `[R]`.

### 5.2. Lit Frontier Dynamite Stick (`[G]`)
* **Attack Mechanics:** Thrown physics projectile launched along a parabolic curve combining camera forward vector and vertical loft.
* **AoE Damage & Stun:** Deals $320\text{ damage}$ in a $6.8\text{-meter}$ radius upon expiration of the $1.8\text{s}$ fuse. Any enemy hit within the inner $4.5\text{m}$ suffers a **$3.0\text{-second}$ concussive stun**, interrupting ability usage and freezing locomotion.

### 5.3. Autonomous Sheriff Auto-Turret (`[Q]`)
* **Attack Mechanics:** Deploys an automated clockwork sentry onto the terrain at the player's feet.
* **Targeting AI:** Continuously rotates its head to acquire enemy Outlaws within an $18\text{-meter}$ radius, firing $45\text{-damage}$ hitscan bursts every $0.35\text{s}$. Remains active for $25\text{ seconds}$ or until destroyed.

---

## 6. Consumables and Resource Management: Survival & Economy

```
[Combat & Damage Taken] ---> [Seek Health Elixir (+250 HP)] ---> [Max 1000 HP Restored]
                                          ^
                                          |
[Abilities Expended]    ---> [Loot Ammo Crate (Refill 6/6)] ---> [Abilities Instantly Off Cooldown]
                                          ^
                                          |
[7.0s Spawner Tick]     ---> [Collect Ruby Gem (+1 Gem)]    ---> [10 Gems Trigger 30s Countdown]
```

### 6.1. Frontier Health Elixir Flask
* **Role & Function:** Crystalline emerald flask containing concentrated healing tonic. Positioned on the Saloon porch and inside the Sheriff's Armory Vault.
* **Gameplay Effect:** Restores **$+250\text{ HP}$** instantaneously upon contact (up to the $1000\text{ HP}$ health cap), accompanied by a restorative chime and green ascending particle swirl. Crucial for recovering health after surviving a dynamite blast.

### 6.2. Ammunition & Energy Supply Crate
* **Role & Function:** Reinforced brass ammunition cache containing revolver cartridges and clockwork springs.
* **Gameplay Effect:** Instantly refills revolver cylinder to $6/6$ rounds **and resets all ability cooldowns to zero** (instantly refreshing Dynamite `[G]`, Turret `[Q]`, and Healing Totem `[H]`). Provides massive tactical value for staging sudden counter-offensives.

### 6.3. Radiant Ruby Gemstones (Core Victory Resource)
* **Role & Function:** The central objective resource of *Wildlock*. Uniquely spawned from the central mine cart every 7 seconds, and dropped by destroyed supply crates.
* **Economic Loop & Strategy:**
  * **Gathering Phase:** Teams must coordinate to contest the mine cart, balancing aggression with cover.
  * **Holding Phase:** As a player accumulates gems, their personal value increases dramatically. Carrying 6+ gems makes a player the primary target for enemy dynamite and sniper focus.
  * **Showdown Defense:** Upon reaching 10 gems, the leading duo must transition from offensive push to defensive survival, using high ground, healing totems, and turret barricades to protect the gem carrier until the 30-second clock expires.

---

# Prototype Implementation Matrix (Part 3 Verification)

| Assignment Requirement | Prototype Script & Asset Reference | In-Game Trigger & Verification |
|---|---|---|
| **Action 1: Object Interaction** | [`WildlockGemPickup.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockGemPickup.cs) | Walk over Central Ruby Mine Cart or field gems to collect. |
| **Action 2: Scripted Event** | [`WildlockSaloonDoor.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockSaloonDoor.cs) | Walk through Saloon Batwing Doors or unlock Armory at 5 gems. |
| **Action 3: Environmental State** | [`WildlockEnvironmentSwitch.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockEnvironmentSwitch.cs) | Press `[E]` or shoot Generator Switch to raise Sniper Platform & Barricades. |
| **Action 4: Affect NPC State** | [`WildlockNpcAgent.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockNpcAgent.cs) | Shoot Outlaws Guard/Lu to damage; throw Dynamite `[G]` to stun for 3s. |
| **Action 5: Stats / Inventory** | [`WildlockPlayerController.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockPlayerController.cs) | Collect Health Elixir (+250 HP) or Ammo Crate; shoot & reload `[R]`. |
| **Camera 1: Third-Person Follow** | [`WildlockCameraDirector.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockCameraDirector.cs) | Default over-the-shoulder Cinemachine 3 virtual camera. |
| **Camera 2: First-Person POV** | [`WildlockCameraDirector.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockCameraDirector.cs) | Press `[V]` or hold `[RMB]` to aim down sights in true FPS perspective. |
| **Camera 3: Cinematic / Overview**| [`WildlockCameraDirector.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockCameraDirector.cs) | Press `[C]` for cutscene mine cart zoom; press `[B]` for tactical overview. |
| **Lighting 1: Day/Night Cycle** | [`WildlockLightingManager.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockLightingManager.cs) | Press `[T]` to transition sun between High Noon, Sunset, and Moonlit Night. |
| **Lighting 2: Player Lantern** | [`WildlockLightingManager.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockLightingManager.cs) | Press `[F]` to toggle hip-mounted oil lantern point light & aim spotlight. |
| **Lighting 3: Event/Zone Lighting**| [`WildlockLightingManager.cs`](file:///c:/Users/20114/Desktop/vgd/wildlock/Assets/Scripts/Wildlock/WildlockLightingManager.cs) | Mine/Saloon dark zone ambient shifts, street floodlights, red alarm beacons. |
| **Terrain: Outer Canyon Vista** | `Wildlock_OuterVistaTerrain.asset` | 900m x 900m sculpted mesa terrain surrounding the playable town. |
| **Skins: Swappable Characters** | `HeroSkin_*` (Guard, Adam, Lu, etc.) | Press `[M]` to cycle through all 5 textured PBR humanoid hero skins. |
