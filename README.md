# Grocery Rush

A top-down 2D arcade game built in Unity where you race through a supermarket to collect every item on your shopping list before the clock runs out.

## Team

| Name | Role |
|------|------|
| Guido Fajardo | Developer |
| Gabriel Kleinschmidt | Developer |
| Trigg Newton | Developer |
| Hayden Aldridge | Developer |

## Gameplay Overview

You control a shopping cart navigating a supermarket floor. Your goal is to collect all 10 items on your shopping list and reach the checkout register before the 2-minute timer expires.

**Win condition:** Collect all items, then drive your cart to the register.  
**Lose condition:** Timer hits zero before you check out.

### Shopping List

Each round you must find and collect all 10 items scattered across the store:

> Milk, Bread, Eggs, Cheese, Apples, Juice, Butter, Yogurt, Cereal, Bananas

Items are checked off on a live HUD list as you collect them. If an item is knocked out of your cart, it respawns at its original location and must be collected again.

### Controls

| Action | Input |
|--------|-------|
| Move cart | WASD / Arrow Keys |

The cart rotates to face the direction of movement and uses physics-based momentum.

## Enemies & Hazards

### Shopper NPCs
Ordinary shoppers patrol the aisles along preset waypoints, pausing occasionally as if browsing. If one gets too close to your cart, they bump into you — slowing your movement and randomly knocking one item out of your cart.

### Kid NPCs
Kids are faster and more aggressive than shoppers. They actively chase the player once you enter their detection radius, and grow faster the more items you are carrying. There are four distinct chase behaviors:

| Style | Behavior |
|-------|----------|
| **Direct** | Charges straight at the player |
| **Ahead** | Leads the player's movement to cut them off |
| **Flank** | Approaches from the side to intercept |
| **Shy** | Chases from a distance but retreats when too close (Clyde-style) |

Kids also spread apart from each other using a separation force, so they can't be stacked in one corner.

Contact with a kid slows you for 1.5 seconds and drops a random item from your cart.

### Wet Floor Zones
Hazard zones on the floor slow your cart for as long as you remain inside them.

### Drop Obstacles
Solid obstacles that knock 1–2 items loose on collision. They flash white on impact and have a short cooldown before they can affect you again.

## HUD & Feedback

- **Timer** — counts down from 2:00. Turns yellow at 45s, orange at 25s, and red at 15s. Pulses when under 15 seconds. Plays a one-time "30 seconds left!" warning.
- **Shopping List** — live checklist; collected items are struck through, dropped items are highlighted red and briefly animate.
- **Items Remaining** — running count of uncollected items.
- **Notifications** — pop-up banners for bumps, drops, and key events (fade in/out, queued so none are missed).
- **Register Banner** — appears when all items are collected to prompt you to check out.
- **Win / Lose screens** — replace the HUD on game end with a restart option.

## Project Structure

```
Assets/
  Scripts/
    GameManager.cs       — singleton, timer, win/lose state, event bus
    CartController.cs    — player movement, slow effect
    ShoppingList.cs      — item tracking, collect/drop events
    ItemPickup.cs        — trigger-based pickup, respawn with flash
    Register.cs          — win trigger at checkout
    ShopperAI.cs         — waypoint patrol, proximity bump
    KidAI.cs             — multi-style chase, separation steering
    HazardZone.cs        — slow-on-stay trigger
    DropObstacle.cs      — collision-based item drop
    UIManager.cs         — HUD, notifications, win/lose screens
  Editor/
    SceneBuilder.cs      — editor tooling for scene construction
    SceneUpdater.cs      — editor tooling for scene updates
    Beautifier.cs        — editor scene beautification helpers
    UIBuilder.cs         — editor UI scaffolding
```

## Built With

- **Unity** (2D, Physics2D)
- **TextMesh Pro** — all in-game text rendering
- **C#** — all game logic