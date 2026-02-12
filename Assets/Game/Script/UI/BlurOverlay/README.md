# Blur Overlay System - Quick Setup Guide

## 📋 Overview
Automatically fades blur overlay based on hunger and thirst levels. Built with SOLID principles and DOTween.

## ⚡ Quick Setup (5 minutes)

### 1. Prerequisites
- ✅ DOTween installed in project
- ✅ Blur overlay Image already in scene
- ✅ PlayerStats component on player GameObject

### 2. Add Component
1. Select your **Player GameObject** (must have PlayerStats component)
2. Add Component → **BlurOverlayController**
3. In Inspector:
   - Drag your blur overlay **Image** to the "Blur Overlay Image" field
   - Keep default settings or customize (see below)

### 3. Test It!
**In Play Mode:**
- Right-click BlurOverlayController component
- Select **"Test Max Blur"** → Blur fades in
- Select **"Test Clear Blur"** → Blur fades out
- Select **"Force Update"** → Update based on current stats

**Lower your hunger/thirst below 30%** → Blur automatically fades in!

## 🎛️ Configuration

### Thresholds
```
Hunger Critical: 30   ← Blur starts appearing
Hunger Severe:   10   ← Maximum blur intensity
Thirst Critical: 30   ← Blur starts appearing  
Thirst Severe:   10   ← Maximum blur intensity
```

### Intensity
```
Max Blur Intensity: 0.8   ← 80% blur at worst condition
Use Worst Stat:     ✓     ← Use most critical stat (not average)
```

### Animation
```
Fade In Duration:  0.67s   ← Gradual warning
Fade Out Duration: 0.4s    ← Quick relief feedback
Blur Method:       Alpha   ← Choose based on shader
```

### Blur Methods
- **Alpha**: Use Image alpha channel (simplest)
- **Material Property**: Control shader parameter (e.g., `_BlurAmount`)
- **Both**: Use alpha + material property together

### Update Rate
```
Update Interval: 0.1s   ← Checks stats 10x per second
```

## 📊 How It Works

```
Hunger/Thirst Level     Blur Intensity
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
100% (healthy)          0% (no blur)
↓
30% (critical)          0% → starts fading in
↓
10% (severe)            80% (maximum blur)
↓
0% (empty)              80% (maximum blur)
```

**Fade behavior:**
- Stats drop → Blur gradually fades in (0.67s)
- Consume food/water → Blur quickly fades out (0.4s)

## 🏗️ Architecture (SOLID Principles)

```
BlurOverlayController (Orchestrator)
├── IBlurIntensityCalculator (Calculation)
│   └── SurvivalStatBlurCalculator
└── IBlurEffect (Rendering)
    └── DOTweenBlurEffect
```

**Benefits:**
- ✅ Easy to test (mock interfaces)
- ✅ Easy to extend (new calculators/effects)
- ✅ Decoupled components
- ✅ Clean, maintainable code

## 🔧 Advanced Usage

### Manual Control
```csharp
var controller = GetComponent<BlurOverlayController>();

// Force specific blur intensity
controller.SetManualIntensity(0.5f, fadeIn: true);

// Get current blur level
float blur = controller.GetCurrentIntensity();

// Temporarily disable
controller.SetEnabled(false);
```

### Custom Calculator
Want blur based on health instead? Extend the system:
```csharp
public class HealthBlurCalculator : IBlurIntensityCalculator
{
    // Implement your custom logic
}
```

See [BLUR_OVERLAY_SYSTEM.md](BLUR_OVERLAY_SYSTEM.md) for full documentation.

## 🐛 Troubleshooting

### Blur not appearing?
1. Check PlayerStats component exists
2. Verify Image reference is assigned
3. Lower hunger/thirst below 30% in play mode
4. Use context menu "Force Update" to test

### Blur too strong/weak?
- Adjust **Max Blur Intensity** (0.0-1.0)
- Change **Critical Threshold** (when blur starts)

### Blur too fast/slow?
- Adjust **Fade In Duration** and **Fade Out Duration**

### Wrong blur method?
- **Alpha** works for any image with alpha
- **Material Property** needs shader with blur parameter
- Check material has the correct property name

## 📚 Files
- `BlurOverlayController.cs` - Main component (attach this!)
- `IBlurIntensityCalculator.cs` - Calculator interface
- `SurvivalStatBlurCalculator.cs` - Hunger/thirst calculator
- `IBlurEffect.cs` - Effect interface  
- `DOTweenBlurEffect.cs` - DOTween animator
- `BLUR_OVERLAY_SYSTEM.md` - Full technical documentation

## 🎯 Default Behavior
Works out of the box! Just attach and assign the Image reference. The system will:
- ✅ Monitor hunger and thirst automatically
- ✅ Fade blur in when stats drop below 30%
- ✅ Fade blur out when player consumes food/water
- ✅ Use smooth DOTween animations
- ✅ Scale intensity based on how low stats are

---

**Need help?** See [BLUR_OVERLAY_SYSTEM.md](BLUR_OVERLAY_SYSTEM.md) for detailed documentation.
