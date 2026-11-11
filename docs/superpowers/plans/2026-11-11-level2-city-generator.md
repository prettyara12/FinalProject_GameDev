# City Level 2 Custom Town Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a brand new, original custom city layout for Level 2 (`Module 2_Stage2.unity`) using the project's existing SimpleTown & SimpleBuildings assets, featuring a dual-corridor and winding alley design, Golden Hour lighting, moving avenue traffic, and 5 delivery missions in 7 minutes, while leaving `Module 2.unity` completely untouched.

**Architecture:** A dedicated Editor builder `Assets/Editor/Level2CityBuilder.cs` constructs the entire Level 2 city layout into `Assets/Module 2_Stage2.unity` with roads, sidewalks, buildings, street lamps, and barriers. Runtime bootstrap (`PlayerBootstrap.cs`) positions Rian and the 5 NPCs at their new locations in the new town, with `DeliveryHUD.cs` and `DeliveryGameManager.cs` supporting the 7-minute timer and updated navigation clues.

**Tech Stack:** Unity 6000.6.2f1, C#, SimpleTown Asset Pack, SimpleBuildings Asset Pack, Unity URP/Standard lighting.

---

## File Structure

- **`Assets/Editor/Level2CityBuilder.cs`** (New):
  - Editor utility script with `[MenuItem("Kurir Terakhir/Bangun Kota Baru Level 2")]`.
  - Clears legacy layout from `Module 2_Stage2.unity` and assembles:
    1. Dual-corridor asphalt road grid (North Avenue & South Avenue) with sidewalks.
    2. Central Boulevard & Hub Plaza (Mega Mall + starting position).
    3. Distinct commercial, civic, and residential building blocks with winding alleyways.
    4. Street lamps and props.
    5. Golden Hour warm directional sunlight (`Color(1.0f, 0.70f, 0.38f)`).
  - Saves the scene to `Assets/Module 2_Stage2.unity`.
- **`Assets/Scripts/PlayerBootstrap.cs`** (Update):
  - Updates `SetupCityLevel2()` to spawn and configure Player, Pak Beni, Clara, Pak Joko, Dimas, Petugas Parkir, Dropped Package, and Naya at the new town coordinates.
  - Configures traffic cars running on the North and South avenues.
- **`Assets/Scripts/DeliveryHUD.cs`** (Update):
  - Updates Level 2 mission clues and denah peta `[M]` to reflect the dual-corridor and alley layout.
- **`Assets/Scripts/DeliveryGameManager.cs`** (Update):
  - Supports Level 2 delivery clues, dropped package pickup, and 7-minute timer.
- **`Assets/Module 2.unity`** (Untouched):
  - Preserved 100% without changes.

---

## Task Breakdown

### Task 1: Create Level 2 City Builder Editor Script
- [ ] Create `Assets/Editor/Level2CityBuilder.cs`.
- [ ] Define modular layout coordinate matrix:
  - North Avenue: Z = +20, X from -40 to +40.
  - South Avenue: Z = -20, X from -40 to +40.
  - Central Boulevard: X = 0, connecting Z = -20 to +20.
  - West & East Alleys: X = -25 and X = +25.
  - Building placements:
    - Center Plaza: `Building_Mall` at `(0, 0, -5)`.
    - Coffee Shop: `Building_CoffeeShop` at `(-25, 0, 20)`.
    - Drug Store: `Building_StoreCorner_Drug` at `(-25, 0, -20)`.
    - Fire Station: `fire_station` at `(25, 0, 20)`.
    - Donut Shop: `donut_shop` at `(10, 0, 5)`.
    - Residential Apartment: `Building_ApartmentLarge_Red` at `(25, 0, -20)`.
    - Additional varied buildings, street lamps, trees, and road barricades.
- [ ] Configure Golden Hour lighting (low-angle warm sunlight + street lights).
- [ ] Provide menu execution method and automated scene setup.

### Task 2: Execute City Generation on `Module 2_Stage2.unity`
- [ ] Run the city builder to populate `Assets/Module 2_Stage2.unity`.
- [ ] Verify that all building prefabs, roads, and colliders are instantiated with proper layers and materials.
- [ ] Verify `Assets/Module 2.unity` is completely unchanged.

### Task 3: Update Runtime Bootstrap & Gameplay Coordinates
- [ ] Update `PlayerBootstrap.cs` (`SetupCityLevel2`) with exact coordinates in the new town:
  - Rian & Pak Beni at Center Plaza: `(0, 0.02, -2)`.
  - Clara at Coffee Shop: `(-25, 0.02, 17)`.
  - Pak Joko at Drug Store: `(-25, 0.02, -17)`.
  - Dimas at Fire Station: `(25, 0.02, 17)`.
  - Petugas Parkir at Central Crossroads: `(0, 0.02, 10)`.
  - Dropped Package near Donut Shop in alley: `(10, 0.026, 3)`.
  - Naya at Apartment: `(25, 0.02, -17)`.
- [ ] Setup traffic waypoints for cars patrolling the North and South avenues.

### Task 4: Update HUD & Map Navigation
- [ ] Update `DeliveryHUD.cs` denah peta `[M]` with the new dual-corridor map layout.
- [ ] Update mission clues in `DeliveryHUD.cs` and `DeliveryGameManager.cs`.

### Task 5: Verification & Version Control
- [ ] Verify scene loading, compilation, build settings, and gameplay flow.
- [ ] Confirm `git status` shows `Module 2.unity` is untouched.
- [ ] Commit and push to GitHub.
