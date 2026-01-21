# UI Panel Management System

A comprehensive, reusable UI system for managing panels and button events in Unity.
**Now powered by DOTween for smooth, performant animations!**

## Components

### 1. UIPanel.cs
Individual panel component that handles open/close animations and state management.
**Now uses DOTween for all animations!**

**Features:**
- Multiple animation types (Fade, Scale, Slide, etc.)
- DOTween-powered animations (smooth and performant)
- Customizable animation curves (converted to DOTween Ease)
- Audio support for open/close sounds
- Event callbacks (OnPanelOpened, OnPanelClosed, etc.)
- Background click to close option
- Immediate open/close methods

**Usage:**
1. Add `UIPanel` component to your panel GameObject
2. Configure animation settings in the inspector
3. Call `Open()`, `Close()`, or `Toggle()` methods

**Animation Types:**
- `None` - No animation
- `Fade` - Fade in/out
- `Scale` - Scale from zero
- `SlideDown` - Slide from top
- `SlideUp` - Slide from bottom
- `SlideLeft` - Slide from right
- `SlideRight` - Slide from left
- `FadeAndScale` - Combined fade and scale
- `FadeAndSlideDown` - Combined fade and slide

### 2. UIPanelManager.cs
Central singleton manager for all UI panels.

**Features:**
- Panel registration and management
- Single panel mode (only one panel open at a time)
- Open/close panels by name
- Escape key to close current panel
- Event system for panel state changes

**Usage:**
```csharp
// Get the manager instance
UIPanelManager panelManager = UIPanelManager.Instance;

// Open a panel
panelManager.OpenPanel("SettingsPanel");

// Close a panel
panelManager.ClosePanel("MainMenuPanel");

// Toggle a panel
panelManager.TogglePanel("InventoryPanel");

// Close all panels
panelManager.CloseAllPanels();
```

**Setup:**
1. Add `UIPanelManager` to a GameObject in your scene
2. **Optional**: Add your `UIPanel` components to the "Registered Panels" list
3. Panels will be automatically registered by name

**Is Registration Required?**
- **NO** - Registration is **optional** and only needed for specific features:
  - Opening/closing panels by **name** (string) - `OpenPanel("PanelName")`
  - Using **single panel mode** (auto-closing other panels)
  - Using **CloseAllPanels()** or **CloseCurrentPanel()**
  - Using **IsPanelOpen(string panelName)**
  - Receiving **UIPanelManager events** (OnPanelOpened, OnPanelClosed)
  
- **Registration NOT needed** when:
  - Using **direct panel references** - `OpenPanel(panelReference)` works without registration
  - Calling panel methods directly - `panel.Open()`, `panel.Close()`, `panel.Toggle()` work independently
  - Using **UIButtonHandler with panel references** - Direct references don't require registration
  - Panels work **standalone** - UIPanel component is fully functional on its own

**Quick Answer:** If you use **panel references** (drag UIPanel component), registration is **NOT required**. Only register if you want to use panel **names** (strings) or UIPanelManager features.

### 3. UIButtonHandler.cs
Efficient button event handler for managing multiple button clicks.

**Features:**
- Centralized button event management
- Multiple action types (OpenPanel, ClosePanel, TogglePanel, EnableFirstPersonController, etc.)
- **Works with or without panels** - Can handle button events independently
- **Secondary Elements**: Toggle additional GameObjects (works with any action type)
- UnityEvent support for custom actions
- Programmatic button registration
- Action name logging
- **No panel references required for non-panel actions** (EnableFirstPersonController, CustomEvent, etc.)

### 4. UIManager.cs ⭐ NEW!
Custom UI Manager for handling custom UI logic and interactions.

**Features:**
- Singleton pattern for easy access
- Custom UI element management (show/hide/toggle)
- **FirstPersonController control** - Enable/disable/toggle player controller
- Integration with UIPanelManager events
- Custom event system for UI state changes
- Extensible design - add your own custom methods
- Event callbacks for UI state changes

**Usage:**
1. Add `UIManager` component to a GameObject in your scene (or it will auto-create)
2. Optionally add custom UI elements to the "Custom UI Elements" list
3. Access via `UIManager.Instance` from anywhere
4. Add your custom logic by extending the class or adding methods

**Basic Usage:**
```csharp
// Get the UIManager instance
UIManager uiManager = UIManager.Instance;

// Show/hide/toggle custom elements
uiManager.ShowCustomElement("MyUIElement");
uiManager.HideCustomElement("MyUIElement");
uiManager.ToggleCustomElement("MyUIElement");

// Show/hide all custom elements
uiManager.ShowAllCustomElements();
uiManager.HideAllCustomElements();

// First Person Controller control
uiManager.EnableFirstPersonController();
uiManager.DisableFirstPersonController();
uiManager.ToggleFirstPersonController();

// Set player object reference (optional, will auto-find if not set)
uiManager.SetPlayerObject(playerGameObject);

// Trigger custom events
uiManager.TriggerCustomEvent("MyCustomEvent");

// Subscribe to custom events
UIManager.Instance.OnCustomUIEvent += (eventName) => {
    Debug.Log($"Custom event: {eventName}");
};
```

**Extending UIManager:**
```csharp
public class MyCustomUIManager : UIManager
{
    protected override void HandlePanelOpened(UIPanel panel)
    {
        base.HandlePanelOpened(panel);
        
        // Add your custom logic here
        if (panel.gameObject.name == "SettingsPanel")
        {
            ShowCustomElement("SettingsIcon");
        }
    }
    
    protected override void HandlePanelClosed(UIPanel panel)
    {
        base.HandlePanelClosed(panel);
        
        // Add your custom logic here
    }
    
    // Add your own custom methods
    public void MyCustomMethod()
    {
        // Your custom UI logic
    }
}
```

### 5. UIButtonAnimator.cs ⭐ NEW!
Advanced button animation component with DOTween support.

**Features:**
- **Button Animations**: Hover, click animations for the button itself
- **Element Animations**: Animate child elements inside buttons (icons, text, etc.)
- **Active/Inactive States**: Toggle animations for active/inactive button states
- **DOTween Powered**: All animations use DOTween for smooth performance
- **Multiple Elements**: Animate multiple elements independently
- **Event Handlers**: OnAnimationStart, OnAnimationComplete, OnHoverEnter, etc.

**Usage:**
1. Add `UIButtonAnimator` component to your button GameObject
2. Configure button hover/click animations
3. Add element animations for child elements (icons, text, etc.)
4. Optionally enable active/inactive state animations

**Element Animation Example:**
- Animate an icon to rotate 90° on hover
- Scale icon on click
- Slide icon position when button becomes active
- Different animations for active/inactive states

**UIButtonHandler Usage:**
1. Add `UIButtonHandler` component to a GameObject
2. In the inspector, add button actions:
   - Select the button
   - Choose action type
   - **Option A (Recommended)**: Drag the `UIPanel` component directly into "Panel Reference"
   - **Option B**: Type the panel name in "Panel Name" field (must match the GameObject's name exactly)
   - **Secondary Elements**: Drag additional GameObjects into the "Secondary Elements" list to toggle them on/off with the panel
   - Optionally add custom UnityEvents

**Secondary Elements:**
- **Works with ANY action type** - not just panel actions!
- When any button action is triggered, you can specify additional GameObjects to control
- These elements will automatically toggle on/off based on **their own current state**:
  - If an element is currently **active**, it becomes **inactive**
  - If an element is currently **inactive**, it becomes **active**
- Each element toggles independently, so you can have some elements on and some off, and they'll each flip their state
- Useful for controlling UI elements like icons, badges, indicators, or other visual feedback that should toggle independently
- Example: Enable FirstPersonController AND toggle a UI icon at the same time

**How Panel Identification Works:**
- **Panel Reference (Recommended)**: Drag the UIPanel component directly. This is type-safe and works even if the GameObject name changes. **No registration required!**
- **Panel Name**: Enter the exact GameObject name of the panel. The UIPanelManager registers panels by their GameObject name, so the name must match exactly (case-sensitive). **Requires registration in UIPanelManager.**

**Action Types:**
- `OpenPanel` - Opens a panel by name
- `ClosePanel` - Closes a panel by name
- `TogglePanel` - Toggles a panel by name
- `CloseAllPanels` - Closes all open panels
- `CloseCurrentPanel` - Closes the currently open panel
- `EnableFirstPersonController` - Enables FirstPersonController component
- `DisableFirstPersonController` - Disables FirstPersonController component
- `ToggleFirstPersonController` - Toggles FirstPersonController component
- `CustomEvent` - Triggers custom UnityEvent

**Programmatic Usage:**
```csharp
UIButtonHandler handler = GetComponent<UIButtonHandler>();

// Option 1: Using panel reference (recommended)
handler.AddButtonAction(
    myButton,
    UIButtonHandler.ButtonActionType.OpenPanel,
    settingsPanel, // UIPanel component reference
    "OpenSettings"
);

// Option 2: Using panel name
handler.AddButtonAction(
    myButton,
    UIButtonHandler.ButtonActionType.OpenPanel,
    "SettingsPanel", // Must match GameObject name
    "OpenSettings"
);
```

## How Panel Identification Works

Buttons identify panels in **two ways**:

### Method 1: Panel Reference (Recommended) ✅
- **Drag the UIPanel component** directly into the "Panel Reference" field in the inspector
- **Type-safe**: Works even if you rename the GameObject
- **No string matching**: Direct reference, so no risk of typos
- **No registration required**: Works without adding panel to UIPanelManager
- **Example**: Drag `SettingsPanel` GameObject's `UIPanel` component into the field

### Method 2: Panel Name (String-based)
- **Enter the exact GameObject name** in the "Panel Name" field
- **Must match exactly**: The name must match the panel GameObject's name (case-sensitive)
- **Registration required**: Panel must be added to UIPanelManager's "Registered Panels" list
- **How it works**: 
  1. UIPanelManager registers panels using `panel.gameObject.name` (line 91 in UIPanelManager.cs)
  2. When you call `OpenPanel("SettingsPanel")`, it looks up the panel in a dictionary by name
  3. The button uses this same name string to find the panel
- **Example**: If your panel GameObject is named "SettingsPanel", enter exactly "SettingsPanel"

**Important Notes:**
- Panel names are **case-sensitive** - "SettingsPanel" ≠ "settingspanel"
- Panel names must match the **GameObject name**, not the component name
- If using panel reference, the name field is automatically filled but not required
- If a panel reference is provided, it takes priority over the panel name
- **Panel Reference method does NOT require registration** - panels work standalone!

## Quick Start Guide

### Step 1: Setup UIPanelManager (Optional)
1. Create an empty GameObject named "UIPanelManager"
2. Add the `UIPanelManager` component
3. Configure settings (single panel mode, close on escape, etc.)
4. **Optional**: Add panels to "Registered Panels" list (only needed if using panel names or manager features)

**Note**: UIPanelManager is optional! Panels can work standalone without it.

### Step 2: Create a Panel
1. Create your UI panel GameObject (with Canvas, Image, etc.)
2. Add `UIPanel` component
3. Configure animation settings
4. **Optional**: Add the panel to UIPanelManager's "Registered Panels" list (only if you want to use panel names or manager features)

### Step 3: Setup Buttons
**Option A: Using UIButtonHandler**
1. Add `UIButtonHandler` component to a GameObject
2. In inspector, add button actions for each button
3. Configure action type
   - **For panel actions**: Set panel reference or panel name
   - **For non-panel actions**: No panel reference needed (e.g., EnableFirstPersonController, CustomEvent)

**Option B: Direct Code**
```csharp
myButton.onClick.AddListener(() => {
    UIPanelManager.Instance.OpenPanel("MyPanel");
});
```

**Option C: Button Events Only (No Panels Required)**
UIButtonHandler can be used for any button event, not just panels:
- Enable/Disable FirstPersonController
- Custom events
- Toggle secondary elements
- Any combination of the above
- **No panel references needed for non-panel actions!**

### Step 4: Setup UIManager (Optional)
1. Add `UIManager` component to a GameObject (or it will auto-create)
2. Add custom UI elements to the "Custom UI Elements" list if needed
3. Extend the class or add custom methods for your specific UI logic
4. Access via `UIManager.Instance` from anywhere in your code

## Example Scenarios

### Scenario 1: Main Menu with Settings
```csharp
// Open settings from main menu
UIPanelManager.Instance.OpenPanel("SettingsPanel");

// Close settings and return to main menu
UIPanelManager.Instance.ClosePanel("SettingsPanel");
```

### Scenario 4: Toggle Panel with Secondary Elements
```csharp
// In UIButtonHandler inspector:
// Button: SettingsToggleButton
// Action Type: TogglePanel
// Panel Reference: SettingsPanel
// Secondary Elements:
//   - SettingsIcon (GameObject)
//   - NotificationBadge (GameObject)
//   - BackgroundOverlay (GameObject)
//
// When the button is clicked:
// - Panel toggles open/closed
// - Each secondary element toggles based on its own current state:
//   * If SettingsIcon is active → becomes inactive
//   * If NotificationBadge is inactive → becomes active
//   * Each element toggles independently
```

### Scenario 5: Using UIManager for Custom Logic
```csharp
// Example: Custom UI logic when settings panel opens
public class MyUIManager : UIManager
{
    protected override void HandlePanelOpened(UIPanel panel)
    {
        base.HandlePanelOpened(panel);
        
        if (panel.gameObject.name == "SettingsPanel")
        {
            // Show settings icon
            ShowCustomElement("SettingsIcon");
            
            // Trigger custom event
            TriggerCustomEvent("SettingsOpened");
            
            // Your custom logic here
            Debug.Log("Settings panel opened - updating UI");
        }
    }
    
    // Custom method for your specific needs
    public void UpdatePlayerUI()
    {
        // Your custom UI update logic
    }
}

// Usage from other scripts:
UIManager.Instance.ShowCustomElement("HealthBar");
UIManager.Instance.TriggerCustomEvent("PlayerLevelUp");

// Control FirstPersonController from button click
// Option 1: Using UIButtonHandler
// In inspector: Action Type = EnableFirstPersonController

// Option 2: Direct code
UIManager.Instance.EnableFirstPersonController();
```

### Scenario 6: Enable FirstPersonController on Button Click (No Panel Required)
```csharp
// Method 1: Using UIButtonHandler (Recommended)
// 1. Add UIButtonHandler component to a GameObject
// 2. Add button action:
//    - Button: YourButton
//    - Action Type: EnableFirstPersonController (or DisableFirstPersonController, ToggleFirstPersonController)
//    - Action Name: "EnablePlayerMovement"
//    - NO PANEL REFERENCE NEEDED! Leave panel fields empty
//    - Optionally add secondary elements to toggle
// 3. When button is clicked, FirstPersonController will be enabled

// Method 2: Using UnityEvent in UIButtonHandler
// 1. Add UIButtonHandler component
// 2. Add button action with Action Type: CustomEvent
// 3. In the "On Button Click" UnityEvent, add:
//    - Drag UIManager GameObject
//    - Select: UIManager -> EnableFirstPersonController()

// Method 3: Direct code
myButton.onClick.AddListener(() => {
    UIManager.Instance.EnableFirstPersonController();
});
```

### Scenario 7: Button Events Without Panels
```csharp
// UIButtonHandler can be used for ANY button event, not just panels!

// Example 1: Enable controller + toggle UI icon
// 1. Add UIButtonHandler
// 2. Add button action:
//    - Action Type: EnableFirstPersonController
//    - Secondary Elements: [Drag UI Icon GameObject]
//    - No panel reference needed!

// Example 2: Custom event only
// 1. Add UIButtonHandler
// 2. Add button action:
//    - Action Type: CustomEvent
//    - On Button Click: Add your custom UnityEvent
//    - No panel reference needed!

// Example 3: Multiple actions on one button
// You can combine actions using secondary elements and UnityEvents
// without needing any panel references
```

### Scenario 2: Multiple Panels (Single Panel Mode)
```csharp
// Opening a new panel automatically closes the current one
UIPanelManager.Instance.OpenPanel("InventoryPanel"); // Closes any open panel first
```

### Scenario 3: Custom Button Actions
```csharp
// In UIButtonHandler inspector:
// Button: CloseButton
// Action Type: CloseCurrentPanel
// Custom Event: Add listener to save game, play sound, etc.
```

## Best Practices

1. **Naming**: Use consistent naming for panels (e.g., "SettingsPanel", "MainMenuPanel")
2. **Single Panel Mode**: Enable for modal dialogs, disable for HUD elements
3. **Animation Duration**: Keep animations short (0.2-0.5 seconds) for better UX
4. **Event Cleanup**: Unsubscribe from events in OnDestroy to prevent memory leaks
5. **Button Handler**: Use UIButtonHandler for complex UI with many buttons

## Integration with Existing Code

The system is designed to work alongside existing UI code. You can:
- Use UIPanelManager for new panels
- Keep existing ArtworkUI for artwork-specific panels
- Mix direct panel control with manager control
- Gradually migrate existing UI to use this system

## Performance Notes

- Panel animations use coroutines (lightweight)
- Button handlers cache references for efficiency
- Single panel mode reduces simultaneous animations
- Event system uses C# Actions (fast, no boxing)

## Button Animations Guide

### Setting Up Button Animations

1. **Add UIButtonAnimator Component**
   - Add `UIButtonAnimator` to your button GameObject
   - The component requires a `Button` component

2. **Configure Button Animations**
   - Enable "Animate On Hover" for hover effects
   - Enable "Animate On Click" for click effects
   - Set hover scale (e.g., 1.05 for 5% larger)
   - Set click scale (e.g., 0.95 for 5% smaller)

3. **Add Element Animations**
   - Click "+" to add element animations
   - Drag child elements (icons, images, text) into "Target Element"
   - Or enter element name to find automatically
   - Configure animations for:
     - **Hover**: Scale, rotation, position changes on hover
     - **Click**: Scale animation on click
     - **Active**: Animation when button becomes active
     - **Inactive**: Animation when button becomes inactive

### Example: Animated Menu Button

```
Button (UIButtonAnimator)
├── Icon (Image) - Rotates 90° on hover, scales on click
├── Text (TextMeshPro) - Slides up on hover
└── Badge (Image) - Scales on active state
```

**Configuration:**
- Button: Hover scale 1.05, Click scale 0.95
- Icon Element: Hover rotation (0, 0, 90), Click scale 0.9
- Text Element: Hover position (0, 5), Click scale 0.95
- Badge Element: Active scale 1.2, Inactive scale 0.8

### Active/Inactive State Usage

```csharp
UIButtonAnimator animator = button.GetComponent<UIButtonAnimator>();

// Set active state (triggers active animations)
animator.SetActiveState();

// Set inactive state (triggers inactive animations)
animator.SetInactiveState();

// Toggle state
animator.ToggleActiveState();
```

### Custom Element Animation

```csharp
UIButtonAnimator animator = button.GetComponent<UIButtonAnimator>();
RectTransform icon = transform.Find("Icon").GetComponent<RectTransform>();

animator.AddCustomElementAnimation(
    icon,
    targetScale: Vector3.one * 1.2f,
    targetRotation: new Vector3(0, 0, 45f),
    targetPosition: new Vector2(10, 0),
    duration: 0.3f,
    ease: Ease.OutBack,
    onComplete: () => Debug.Log("Animation complete!")
);
```

## Element Animation Explained

Element Animation in `UIButtonAnimator` allows you to animate **child elements** inside a button (like icons, text, badges, etc.) independently from the button itself.

### What is Element Animation?

Element Animation lets you animate individual child GameObjects within a button. While the button itself can scale on hover/click, you can make child elements (like icons) rotate, move, or scale separately for more dynamic effects.

### Button Animation vs Element Animation

- **Button Animation**: Animates the button GameObject itself (the root)
  - Example: Button scales to 1.05x on hover
  
- **Element Animation**: Animates child elements inside the button
  - Example: Icon inside button rotates 90° on hover, while button scales

### Element Animation Types

Each element can have **4 different animation types**:

#### 1. **Hover Animation** (On Mouse/Touch Enter)
- Triggers when: Mouse/touch enters the button
- Properties:
  - **Hover Scale**: Scale the element (e.g., 1.2x to make it bigger)
  - **Hover Rotation**: Rotate the element (e.g., (0, 0, 90) to rotate 90°)
  - **Hover Position**: Move the element (e.g., (10, 0) to slide right)
  - **Hover Duration**: How long the animation takes
  - **Hover Ease**: Animation curve (OutQuad, InOutBack, etc.)
- **Auto-reverses**: When mouse exits, element returns to original state

#### 2. **Click Animation** (On Button Click)
- Triggers when: Button is clicked
- Properties:
  - **Click Scale**: Scale on click (e.g., 0.9x for a "press" effect)
  - **Click Duration**: How long the animation takes
  - **Click Ease**: Animation curve
- **Auto-reverses**: Returns to original scale after click

#### 3. **Active Animation** (When Button Becomes Active)
- Triggers when: `SetActiveState()` is called programmatically
- Properties:
  - **Active Scale**: Scale when active (e.g., 1.2x)
  - **Active Rotation**: Rotation when active (e.g., (0, 0, 45))
  - **Active Position**: Position offset when active
  - **Active Duration**: Animation duration
  - **Active Ease**: Animation curve
- **Use case**: Visual feedback when button is selected/active

#### 4. **Inactive Animation** (When Button Becomes Inactive)
- Triggers when: `SetInactiveState()` is called programmatically
- Properties:
  - **Inactive Scale**: Scale when inactive
  - **Inactive Rotation**: Rotation when inactive
  - **Inactive Position**: Position offset when inactive
  - **Inactive Duration**: Animation duration
  - **Inactive Ease**: Animation curve
- **Use case**: Visual feedback when button is deselected/inactive

### How to Set Up Element Animation

1. **Add UIButtonAnimator** to your button GameObject
2. **Expand "Element Animations"** section in inspector
3. **Click "+"** to add a new element animation
4. **Set Target Element**:
   - **Option A**: Drag the child GameObject (with RectTransform) into "Target Element"
   - **Option B**: Enter the child's name in "Element Name" field (will auto-find)
5. **Enable animation types** you want:
   - ✅ Animate On Hover
   - ✅ Animate On Click
   - ✅ Animate On Active
   - ✅ Animate On Inactive
6. **Configure each enabled animation**:
   - Set scale, rotation, position values
   - Set duration and ease type

### Element Animation Examples

#### Example 1: Rotating Icon on Hover
```
Button (UIButtonAnimator)
└── Icon (Image)
    - Hover: Rotation (0, 0, 90) - Icon rotates 90° when hovered
    - Click: Scale 0.9x - Icon shrinks slightly on click
```

**Setup:**
- Target Element: Icon GameObject
- Animate On Hover: ✅
- Hover Rotation: (0, 0, 90)
- Animate On Click: ✅
- Click Scale: (0.9, 0.9, 0.9)

#### Example 2: Sliding Text on Hover
```
Button (UIButtonAnimator)
└── Text (TextMeshPro)
    - Hover: Position (0, 5) - Text slides up 5 pixels
```

**Setup:**
- Target Element: Text GameObject
- Animate On Hover: ✅
- Hover Position: (0, 5)
- Hover Duration: 0.2s

#### Example 3: Active/Inactive Badge
```
Button (UIButtonAnimator)
└── Badge (Image)
    - Active: Scale 1.2x - Badge grows when button is active
    - Inactive: Scale 0.8x - Badge shrinks when inactive
```

**Setup:**
- Target Element: Badge GameObject
- Animate On Active: ✅
- Active Scale: (1.2, 1.2, 1.2)
- Animate On Inactive: ✅
- Inactive Scale: (0.8, 0.8, 0.8)
- Use Active State: ✅ (in UIButtonAnimator settings)

#### Example 4: Multiple Elements
```
Button (UIButtonAnimator)
├── Icon (Image)
│   - Hover: Rotation (0, 0, 90)
│   - Click: Scale 0.9x
├── Text (TextMeshPro)
│   - Hover: Position (0, 5)
└── Badge (Image)
    - Active: Scale 1.2x
    - Inactive: Scale 0.8x
```

**Setup:**
- Add 3 Element Animations
- Configure each element independently

### Key Features

1. **Multiple Elements**: Add multiple element animations to animate different children
2. **Independent Control**: Each element animates independently
3. **Original Values Preserved**: System stores original scale/position/rotation and returns to them
4. **DOTween Powered**: Smooth, performant animations
5. **Auto-Reverse**: Hover animations automatically reverse when mouse exits
6. **Combined Animations**: Can combine scale + rotation + position in one animation

### Programmatic Control

```csharp
UIButtonAnimator animator = button.GetComponent<UIButtonAnimator>();

// Set active state (triggers active animations)
animator.SetActiveState();

// Set inactive state (triggers inactive animations)
animator.SetInactiveState();

// Toggle active state
animator.ToggleActiveState();

// Add custom animation programmatically
RectTransform icon = transform.Find("Icon").GetComponent<RectTransform>();
animator.AddCustomElementAnimation(
    icon,
    targetScale: Vector3.one * 1.2f,
    targetRotation: new Vector3(0, 0, 45f),
    targetPosition: new Vector2(10, 0),
    duration: 0.3f,
    ease: Ease.OutBack,
    onComplete: () => Debug.Log("Done!")
);
```

### Tips & Best Practices

1. **Use RectTransform**: Elements must have RectTransform component (UI elements)
2. **Start Small**: Begin with subtle animations (1.1x scale, 5px movement)
3. **Fast Animations**: Keep durations short (0.1-0.3s) for responsive feel
4. **Consistent Easing**: Use similar ease types (OutQuad) for consistency
5. **Test Interactions**: Make sure hover/click animations don't conflict
6. **Performance**: Multiple elements are fine, but avoid too many simultaneous animations

## Troubleshooting

**Panel not opening:**
- Check if panel is registered in UIPanelManager
- Verify panel name matches exactly
- Ensure panel GameObject is active

**Animation not working:**
- Check if CanvasGroup component exists (auto-added)
- Verify animation type is not set to "None"
- Check animation duration is > 0

**Button not responding:**
- Verify button is assigned in UIButtonHandler
- Check action type and panel name are correct
- Ensure UIPanelManager exists in scene
