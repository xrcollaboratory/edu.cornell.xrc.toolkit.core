# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Package Overview

XRC Core (`edu.cornell.xrc.toolkit.core`) is the foundational package for the XRC Toolkit ecosystem. It provides base classes, interfaces, and utilities used by all other XRC packages such as Scale, Color, Mesh, Create, and AI tools.

**Version**: 0.2.2
**Unity**: 6000.2 minimum
**Dependencies**: XR Interaction Toolkit 3.2.0, Input System 1.14.0

## Build and Documentation Commands

```bash
# Build this package (from parent project root)
dotnet build XRC.Toolkit.Core.csproj

# Generate API documentation (from Documentation~ directory)
docfx docfx.json --serve

# View generated docs
# Opens at http://localhost:8080
```

## Core Architecture

### IEditTool Interface

The central interface for all object editing tools in the XRC ecosystem:

```csharp
public interface IEditTool
{
    GameObject editObject { get; set; }
    bool isInEditMode { get; }
    void OnEditObjectChanging(GameObject newObject);
    void EnterEditMode();
    void ExitEditMode();
    void ToggleEditMode();
}
```

### Base Classes

| Class | Purpose |
|-------|---------|
| `BaseEditToolLogic` | Abstract base for tool logic components. Handles edit mode state, events (`editModeEntered`/`editModeExited`), and lifecycle. Derived classes implement `EnterEditMode()`, `ExitEditMode()`, and `OnEditObjectChanging()`. |
| `BaseEditToolInput` | Abstract base for input handlers. Auto-detects `EditObjectProvider` and routes input appropriately. Supports toggle mode, separate enter/exit actions, or mixed mode. |
| `EditObjectProvider` | Manages object selection for editing. Tracks grabbed objects, saves positions for snap-back, coordinates with `IEditTool` implementations. |

### Component Pattern

XRC tools follow a three-component pattern on a single GameObject:

1. **Logic** (extends `BaseEditToolLogic`) - Core functionality, implements `IEditTool`
2. **Input** (extends `BaseEditToolInput`) - Handles user input via `InputActionProperty`
3. **Feedback** (optional) - Visual/audio responses to state changes

### Edit Mode Lifecycle

```
EnterEditMode() called
    ↓
EditObjectProvider (if present):
    1. Snap-back object to grab position
    2. Set editObject on IEditTool
    3. Call OnEditObjectChanging()
    4. Disable grab interaction
    5. Call IEditTool.EnterEditMode()
    ↓
BaseEditToolLogic:
    1. Set m_IsInEditMode = true
    2. Fire editModeEntered event
    3. Tool-specific logic in derived class
```

## Key Components

### XRCKey

ScriptableObject for storing API keys securely outside version control:

```csharp
[CreateAssetMenu(fileName = "XRCKey", menuName = "Scriptable Objects/XRC/XRC Key")]
public class XRCKey : ScriptableObject
{
    public string key;
}
```

### Utilities

- `AttachAtRuntime` - Attaches child objects to parents at runtime with configurable transform
- `ToggleObjects` - Simple toggle for GameObjects based on input actions

## Implementing a New Edit Tool

Derive from the base classes to create a new tool:

```csharp
// 1. Logic component
public class MyToolLogic : BaseEditToolLogic
{
    public override void EnterEditMode()
    {
        m_IsInEditMode = true;
        m_IsRunning = true;
        InvokeEditModeEntered();
        // Tool-specific enter logic
    }

    public override void ExitEditMode()
    {
        // Tool-specific exit logic
        m_IsInEditMode = false;
        m_IsRunning = false;
        InvokeEditModeExited();
    }

    public override void OnEditObjectChanging(GameObject newObject)
    {
        // Initialize for new edit target
    }
}

// 2. Input component
public class MyToolInput : BaseEditToolInput
{
    // Base class handles routing to EditObjectProvider or IEditTool
    // Configure m_ToggleEditModeAction, m_EnterEditModeAction, m_ExitEditModeAction
}
```

## File Structure

```
Runtime/
├── EditTools/
│   ├── IEditTool.cs              # Core interface
│   ├── BaseEditToolLogic.cs      # Abstract logic base
│   ├── BaseEditToolInput.cs      # Abstract input base
│   └── EditObjectProvider.cs     # Object selection manager
├── Utilities/
│   ├── XRCKey.cs                 # API key storage
│   ├── AttachAtRuntime.cs        # Runtime object attachment
│   └── ToggleObjects.cs          # Input-based object toggling
└── XRC.Toolkit.Core.asmdef       # Assembly definition

Samples~/XRC Starter Assets/       # Sample scene and prefabs
Documentation~/                    # DocFX documentation source
```

## Notes

- `IRunnable` interface was removed in v0.2.2 - use `IEditTool` with `isInEditMode` instead
- The parent project CLAUDE.md contains broader architecture context for the entire XRC Toolkit ecosystem
- Documentation is auto-generated via DocFX and deployed to GitHub Pages
