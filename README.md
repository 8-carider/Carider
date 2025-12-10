# Carider

A multiplayer racing game built with Unity featuring dynamic obstacles, collectibles, and cooperative gameplay mechanics.

## Overview

Carider is a racing game where players navigate through challenging levels while avoiding laser obstacles and collecting items. The game supports both single-player and multiplayer modes with Photon PUN2 networking.

## Key Features

### Multiplayer Support
- Real-time multiplayer using Photon PUN2
- Cooperative gameplay with seat-based control system
- Players can control throttle and steering independently
- Synchronized game state across all clients

### Dynamic Obstacles
- Laser beam obstacles with precise collision detection
- Alternating row and column laser patterns
- Real-time synchronization of laser timing across multiplayer
- Visual and audio feedback for laser activation

### Vehicle System
- Physics-based vehicle movement
- Dynamic engine audio that responds to vehicle speed
- Respawn system with visual effects
- Speed boost mechanics via collectibles

### Game Mechanics
- Collectable system with respawn functionality
- Multiple game levels (Level 1-5)
- Dialogue system with time freeze
- Audio management with background music and sound effects

### Audio Features
- Background music that starts after intro dialogue
- Engine sound effects that vary with vehicle speed
- Laser firing sound effects with spatial audio
- Context-aware audio muting during dialogues and seat selection

## Technical Highlights

### Laser System
- Particle-based visual effects using Polygon Arsenal
- Dynamic BoxCollider creation for precise hit detection
- Timed collision windows (0.7s active period)
- PhotonNetwork.Time synchronization for multiplayer consistency

### Vehicle Control
- Separate throttle and steering controls
- Network-synchronized seat assignment
- Camera management for local players
- Respawn mechanism with explosion effects

### Audio Management
- AudioSource pause/unpause during dialogues
- AudioListener volume control
- Spatial audio blending for environmental sounds
- Audio Mixer integration

## Requirements

- Unity 2022.3 or later
- Photon PUN2 package
- TextMeshPro package
- Input System package

## Networking

The game uses Photon PUN2 for multiplayer functionality:
- Master client handles game state
- All clients render laser effects locally with synchronized timing
- Seat-based control allows multiple players to control one vehicle
- RPC calls for respawn and game events

## Controls

- **Movement**: WASD or Arrow Keys
- **Respawn**: F key
- **Continue Dialogue**: Configured input action

## Game Flow

1. Players enter level and see intro dialogue
2. All audio pauses during dialogue
3. After dialogue ends, background music and game audio start
4. Players select seats (throttle/steering)
5. Laser obstacles begin cycling
6. Collect items while avoiding lasers
7. Reach checkpoints to progress

## Audio System

The game features a comprehensive audio system:
- Background music managed by MusicManager
- Engine sounds controlled by VehicleEngineAudioEffect
- Context-aware muting during seat selection and dialogues
- Laser firing sounds with spatial positioning

## Development Notes

- Laser collision timing is synchronized using PhotonNetwork.Time
- AudioSource components are paused/unpaused during dialogues instead of using AudioListener.volume
- Vehicle engine sounds are muted when no player is assigned to seats
- All prefab BoxColliders for lasers are disabled by default and created dynamically at runtime

## Credits

Built with Unity and Photon PUN2 networking.
Laser effects provided by Polygon Arsenal.
