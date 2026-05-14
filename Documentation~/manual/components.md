# Components

This document lists all MonoBehaviour components in the package.

## Edit Object Provider

Provides object selection management for editing operations. Tracks grabbed objects, saves their initial positions for snap-back, and coordinates with IEditTool implementations to ensure proper initialization order (snap-back → set object → fire events → start tool).

| Property | Description |
|----------|-------------|
| **Interactor** | *No description provided* |
| **Edit Tool Object** | *No description provided* |
| **Start Edit On Set** | *No description provided* |
| **Snap Back On Edit Mode** | *No description provided* |

## Gestural Float Feedback

Spatial feedback component for gestural float control. Renders a world-space line along the gesture's axis with a tick that slides between the target's min and max as the value changes, an optional text label that travels with the tick, and haptic pulses at configurable step intervals.

| Property | Description |
|----------|-------------|
| **Anchor** | *No description provided* |
| **Anchor Offset** | *No description provided* |
| **Anchor Smoothing** | *No description provided* |
| **Axis Length** | *No description provided* |
| **Line Material** | *No description provided* |
| **Line Color** | *No description provided* |
| **Line Width** | *No description provided* |
| **Tick Prefab** | *No description provided* |
| **Accent Color** | *No description provided* |
| **Tick Scale** | *No description provided* |
| **Label Prefab** | *No description provided* |
| **Label Offset** | *No description provided* |
| **Label Prefix** | *No description provided* |
| **Display Format** | *No description provided* |
| **Discrete Labels** | *No description provided* |
| **Do Haptic Feedback** | *No description provided* |
| **Haptic Controller** | *No description provided* |
| **Haptic Amplitude** | *No description provided* |
| **Haptic Duration** | *No description provided* |
| **Haptic Step Size** | *No description provided* |

## Gestural Float Input

Input component for gestural float control. Hold the assigned button to activate the gesture; release to end it.

| Property | Description |
|----------|-------------|
| **Gesture Action** | *No description provided* |

## Gestural Float Logic

Axis along which the gesture tracks controller displacement.

| Property | Description |
|----------|-------------|
| **Target** | The target that provides and receives the float value. |
| **Controller Transform** | The controller transform used for position tracking. |
| **Head** | The head/camera transform for axis reference and billboard orientation. |
| **XR Origin** | *No description provided* |
| **Gesture Axis** | The axis the gesture tracks displacement along. |
| **Sensitivity** | *No description provided* |
| **Min Value** | Minimum value clamp applied to the target. |
| **Max Value** | Maximum value clamp applied to the target. |
| **Dead Zone** | *No description provided* |

## Attach At Runtime

Attaches a child object to a parent object at runtime with configurable local transform settings.

| Property | Description |
|----------|-------------|
| **Parent Object** | *No description provided* |
| **Child Object** | *No description provided* |
| **Local Position** | *No description provided* |
| **Local Rotation** | *No description provided* |
| **Local Scale** | *No description provided* |
| **Is Active On Start** | *No description provided* |

## Fading World Icon

Generic transient floating icon that briefly appears head-locked in front of the user and fades out. Triggered by calling `Appear` from any system that wants a short visual notification (for example a play/pause cue wired to a playback toggle).

| Property | Description |
|----------|-------------|
| **Head** | *No description provided* |
| **Distance** | *No description provided* |
| **Head Offset** | *No description provided* |
| **Fade In Duration** | *No description provided* |
| **Hold Duration** | *No description provided* |
| **Fade Out Duration** | *No description provided* |

## Follow Scale

Mirrors another transform's world scale onto this GameObject every frame. Useful when a separate system (for example XRC Grab Move) drives scale changes on the player rig and unrelated objects — UI panels, auxiliary props — need to follow that scale even though they aren't parented under the rig.

| Property | Description |
|----------|-------------|
| **Source** | *No description provided* |

## Kiosk Mode Manager

Manages a kiosk mode for a scene by enabling or disabling a set of GameObjects. When kiosk mode is active, the configured objects are deactivated; when inactive, they are reactivated. Useful for restricting interactions in demo or exhibition contexts.

| Property | Description |
|----------|-------------|
| **Disabled In Kiosk Mode Objects** | Objects that are deactivated when kiosk mode is on and reactivated when kiosk mode is off. |
| **Enabled In Kiosk Mode Objects** | Objects that are activated when kiosk mode is on and deactivated when kiosk mode is off. |
| **Toggle Kiosk Mode Action** | Input action used to toggle kiosk mode on and off. |
| **Start In Kiosk Mode** | Whether kiosk mode should be active when the scene starts. |

## Toggle Objects

Simple script for toggling game objects based on user input. Used for enabling and disabling UIs etc.

| Property | Description |
|----------|-------------|
| **Toggle Objects** | List of game objects to be toggled. |
| **Toggle Action** | Input action used for toggling the associated game object. |

_This file is automatically generated. Do not edit manually._