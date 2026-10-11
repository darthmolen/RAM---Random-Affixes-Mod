# RAM Affix Icon Art Overview

## Scope

The mod currently defines **863 affix modifiers** and assigns each one a unique icon file. Those are variations of **129 effect families**, mostly repeated across seven rarities.

Recommended visual system:

- **One master icon per effect family**, reused for every rarity.
- **Rarity is a frame/colour treatment**, not new icon art: Common, Uncommon, Rare, Epic, Legendary, Mythic, Godlike.
- Apply reusable **badges/sub-icons** for `%`, flat `+`, chance, kill trigger, headshot, power attack, sprint, night, and resource state.
- Compose category icons from a primary silhouette plus a reusable subject badge, rather than illustrating every variant from scratch.

This reduces the immediate art workload from 863 files to about **115 master compositions**, before further reuse of motifs and badges.

## Explicit shared master icons: flat versus percentage

Each pair below should use one primary motif with a small **flat `+`** or **`%`** badge.

| Shared motif | Flat variant | Percentage variant |
|---|---|---|
| Melee attack speed | Attacks/minute Base | Attacks/minute % |
| Block damage | Block Damage Base | Block Damage % |
| Body damage | Body Damage Base | Body Damage % |
| General entity damage | Base Entity Damage | Entity Damage % |
| Power-attack damage | Power Damage Base | Power Damage % |
| Durability | Durability Base | Durability % |
| Maximum health | Max HP Base | Max HP % |
| Headshot damage | Headshot Base | Headshot % |
| Magazine size | Magazine Base | Magazine % |
| Night damage | Night Damage Base | Night Damage % |
| Fire rate | Rounds/minute Base | Fire Rate % |
| Sneak damage | Sneak Base Bonus | Sneak Bonus % |
| Maximum stamina | Stamina Base | Stamina % |
| Loot stage | Loot Stage Base | Loot Stage % |

## Reusable sub-icon library

Create these once and reuse them as overlays or secondary silhouettes.

| Reusable set | Sub-icons |
|---|---|
| Damage delivery | generic weapon, power attack, headshot, sneak, sprint, attack-speed, firearm fire-rate, projectile, range, reload |
| Weapon class | baton, bow/crossbow, club, explosive, knife, knuckles, machine gun, motor tool, pistol, revolver/.44, robotic turret, shotgun, sledgehammer, sniper rifle, spear, tool |
| Enemy/target | generic zombie, feral zombie, radiated zombie, infernal/charged zombie, giant/boss, full-health target, wounded target, knocked-down target |
| Elements/status | fire, electric shock, bleed, infection, stun, slow, cripple, ragdoll/knockdown, dismemberment |
| Environment | blood moon, moon/night, indoor building, forest, burnt forest/fire, desert, snow/cold, wasteland/radiation |
| Player state | full HP, low HP, high food, low food, high water, low water, injured, kill streak, level-up |
| Attributes | Strength, Perception, Fortitude, Agility, Intellect |
| Survival | armor, shield/crit resistance, dodge, heat, cold, heat+cold, heart/health, stamina, carry capacity, durability |
| Economy/resources | loot bag, treasure/loot stage, XP, coins/bartering, arrow, casing, resource node, buff timer |
| Modifier badges | flat `+`, `%`, chance/starburst, on-kill skull, on-hit impact, conditional diamond, duration clock, stack counter |

## Master-icon families by effect

### Direct damage and weapon handling

- Armor Penetration
- Attack Speed — Base / %
- Block Damage — Base / %
- Body Damage — Base / %
- Entity Damage — Base / %
- Power Attack Damage — Base / %
- Headshot Damage — Base / %
- Cleave / Glancing Hit
- Accuracy
- Projectile Velocity
- Range Increase
- Reload Speed
- Fire Rate — Base / %
- Sneak Damage — Base / %
- Sprint Attack Damage

### Weapon-class damage

Use the **damage** primary motif with the relevant weapon-class sub-icon.

- Baton, Bow/Crossbow, Club, Explosives, Knife, Knuckles
- Machine Gun, Motor Tool, Pistol, Revolver/.44, Robotic Turret
- Shotgun, Sledgehammer, Sniper Rifle, Spear, Tool

### Target-specific and special damage

Use the **damage** primary motif with the target/state sub-icon.

- Feral Damage, Radiated Damage, Infernal/Charged Damage
- Giant Slayer, Bring It Down, Instakill
- Full-HP Target Damage, Low-HP Target Damage, Knocked-Down Target Damage
- Blood Moon Damage, Level Damage
- Stoked to Fury / injured-player damage

### Environment and state-conditional damage

Use the **damage** primary motif with one condition sub-icon.

- Forest, Burnt Forest, Desert, Snow, Wasteland
- Indoors, Night, Night Sneak
- High Food, Low Food, High Water, Low Water
- Player at Max HP, Player at Low HP
- Max Strength, Perception, Fortitude, Agility, and Intellect damage

### Damage-over-time, crowd control, and enemy manipulation

- Bleed
- Burn
- Shock
- Slow Power Headshots
- Stun Headshots
- Cripple Headshots
- Ragdoll Headshots
- Dismemberment

All seven can share the same **zombie** silhouette. Fire, lightning, blood drop, broken limb, stun stars, falling body, and severed-limb sub-icons remain reusable across other content.

### Defense, health, and recovery

- Armor Rating
- Crit Resistance
- Dodge Chance
- Durability — Base / %
- Max HP — Base / %
- Health Regeneration
- Lifesteal
- Infection Resistance
- Stun Resistance
- Heat Resistance, Cold Resistance, Elemental Resistance
- Resurgence / cleanse injury on hit
- Kill-Streak Max HP

### Stamina and movement

- Carry Capacity
- Crouch Speed
- Jump Strength
- Movement Speed, Run Speed, Walk Speed
- Rapid Response / movement speed after being hit
- Stamina on Kill
- Stamina Regeneration
- Stamina Regeneration While Sprinting
- Max Stamina — Base / %
- Stamina Cost, Light-Attack Stamina Cost, Power-Attack Stamina Cost

### Loot, resources, and utility

- Arrow Recovery
- Bullet Casing Return
- Resource Bonus
- Harvest Count
- Bonus Kills
- XP Bonus
- Loot Bag Chance
- Loot Stage — Base / %
- Bartering
- Buff Duration Increase

### Attributes and high-rarity specials

- Strength Attribute
- Perception Attribute
- Fortitude Attribute
- Agility Attribute
- Intellect Attribute
- Kill-Streak Damage

## Recommended production order

1. **System assets:** seven rarity frames/colours, flat/%/chance/trigger badges, generic damage, zombie, item, and player silhouettes.
2. **Most reused families:** damage, weapon class, enemy type, element/status, biome, survival, and stamina sub-icons.
3. **Compose all common 7-tier affixes** from those assets.
4. **Finish the low-count special affixes**: attributes, lifesteal, instakill, kill-streak, and Giant Slayer.

## Important consistency rules

- Use one zombie silhouette across Feral, Radiated, Infernal, headshot, bleed, burn, shock, stun, cripple, ragdoll, and dismemberment. Change only its overlay or glow.
- Use one fire motif for Burn, Burnt Forest, and Heat Resistance; use one snowflake/cold motif for Snow Damage and Cold Resistance.
- Use one heart motif for health, regen, lifesteal, full/low HP conditions, and kill-streak HP; distinguish with small overlays.
- Use one stamina/lightning motif for all stamina families; distinguish general, light attack, power attack, sprinting, on-kill, and maximum with badges.
- Use one loot/chest motif for Loot Stage, Loot Bag, XP, Bartering, arrows/casings, and harvest; distinguish with the relevant resource overlay.
