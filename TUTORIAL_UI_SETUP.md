# Tutorial System - UI Setup Guide

This guide explains how to set up the UI components for the enhanced tutorial system with hold duration and cue display features.

## Overview

The tutorial system now includes:
- **Hold Duration**: Users must hold keys for a specified duration (default 1.0s) to complete steps
- **Cue Display**: Shows preparation information from backend (step.cue field)
- **Progress Tracking**: Visual feedback showing hold progress
- **Cumulative Hold**: Consecutive same actions require longer holds (2x)

## Required UI Components

### 1. Step Instruction Text (TextMeshProUGUI)
**Purpose**: Display the main instruction for the current step

**Setup**:
1. Create a TextMeshProUGUI component in your Canvas
2. Name it: `StepInstructionText`
3. Configure:
   - Font Size: 20-24
   - Alignment: Center or Left
   - Color: White or high-contrast color
   - Text: (leave empty, will be set by script)

**Example**: "Move forward 2 meters"

### 2. Step Cue Text (TextMeshProUGUI)
**Purpose**: Display note/preparation information from backend

**Setup**:
1. Create a TextMeshProUGUI component below instruction text
2. Name it: `StepCueText`
3. Configure:
   - Font Size: 16-18
   - Alignment: Center or Left
   - Color: Yellow or light blue (to stand out)
   - Text: (leave empty, will be set by script)

**Example**: "💡 Make sure the wheelchair is on level ground before starting"

### 3. Step Input Hint Text (TextMeshProUGUI)
**Purpose**: Show which key to press and for how long

**Setup**:
1. Create a TextMeshProUGUI component
2. Name it: `StepInputHintText`
3. Configure:
   - Font Size: 18-20
   - Alignment: Center
   - Color: Cyan or bright color
   - Text: (leave empty, will be set by script)

**Example**: "Hold W for 1.0s"

### 4. Hold Progress Text (TextMeshProUGUI)
**Purpose**: Show real-time hold progress

**Setup**:
1. Create a TextMeshProUGUI component
2. Name it: `HoldProgressText`
3. Configure:
   - Font Size: 16-18
   - Alignment: Center
   - Color: Green when progressing
   - Text: (leave empty, will be set by script)

**Example**: "Hold: 0.7s / 1.0s"

### 5. Hold Progress Bar (Image)
**Purpose**: Visual progress indicator

**Setup**:
1. Create a UI Image component
2. Name it: `HoldProgressBarBackground`
3. Configure background:
   - Image Type: Filled
   - Fill Method: Horizontal
   - Color: Dark gray (RGBA: 0.2, 0.2, 0.2, 0.8)
   - Width: 300-400px
   - Height: 20-30px

4. Create child UI Image component
5. Name it: `HoldProgressBar`
6. Configure fill:
   - Image Type: Filled
   - Fill Method: Horizontal
   - Fill Origin: Left
   - Color: Green (RGBA: 0, 1, 0, 1)
   - RectTransform: Match parent

**Important**: Assign the child Image to the `holdProgressBar` field in the Inspector.

## Unity Inspector Setup

1. Find the GameObject with `RealtimeCoachTutorial` component
2. In the Inspector, locate the **UI References** section
3. Drag and drop the UI components:
   - Step Instruction Text → `stepInstructionText`
   - Step Cue Text → `stepCueText`
   - Step Input Hint Text → `stepInputHintText`
   - Hold Progress Text → `holdProgressText`
   - Hold Progress Bar (child) → `holdProgressBar`

## Hold Settings Configuration

In the **Hold Settings** section:

- **Required Hold Duration**: Duration in seconds to hold key (default: 1.0)
  - Set to 0.5 for easier tutorial
  - Set to 2.0 for more challenging tutorial

- **Cumulative Hold For Same Action**: Enable for progressive difficulty (default: true)
  - If enabled: W → W requires 2.0s on second step
  - If disabled: W → W requires 1.0s on both steps

## Example UI Layout

```
Canvas
├── TutorialPanel (Panel)
│   ├── StepInstructionText (TextMeshProUGUI)
│   │   └── "Move forward 2 meters"
│   ├── StepCueText (TextMeshProUGUI)
│   │   └── "💡 Ensure path is clear"
│   ├── StepInputHintText (TextMeshProUGUI)
│   │   └── "Hold W for 1.0s"
│   ├── HoldProgressText (TextMeshProUGUI)
│   │   └── "Hold: 0.7s / 1.0s"
│   └── HoldProgressBarBackground (Image)
│       └── HoldProgressBar (Image - FILLED)
```

## Behavior Examples

### Scenario A: Normal Step Completion
```
1. User sees: "Hold W for 1.0s"
2. User presses W
3. Progress updates: "Hold: 0.0s / 1.0s" → "Hold: 0.5s / 1.0s" → "Hold: 1.0s / 1.0s"
4. Progress bar fills from 0% to 100%
5. Step completes!
```

### Scenario B: Key Released Too Early
```
1. User sees: "Hold W for 1.0s"
2. User presses W
3. Progress updates: "Hold: 0.5s / 1.0s"
4. User releases W
5. Progress resets: "" (empty)
6. Progress bar resets to 0%
7. User must start over
```

### Scenario C: Consecutive Same Actions
```
Step 1: "Hold W for 1.0s" → Completed
Step 2: "Hold W for 2.0s" (doubled because previous was also W)
```

### Scenario D: Wrong Key Press
```
1. User sees: "Hold W for 1.0s"
2. User presses S (wrong key)
3. Tutorial fails immediately
4. Error logged to backend
```

### Scenario E: Cue Display
```
Backend response includes:
{
  "step_number": 1,
  "text": "Pop the front casters",
  "cue": "Lean back slightly before pressing X",
  "expected_actions": ["pop_casters"]
}

UI displays:
- Instruction: "Pop the front casters"
- Cue: "💡 Lean back slightly before pressing X"
- Input: "Hold X for 1.0s"
```

## Testing Checklist

After setup, test the following:

- [ ] Step instruction displays correctly
- [ ] Cue displays with 💡 emoji when present
- [ ] Cue is hidden when not present
- [ ] Input hint shows correct key and duration
- [ ] Hold progress text updates in real-time
- [ ] Progress bar fills smoothly
- [ ] Releasing key resets progress
- [ ] Holding for full duration completes step
- [ ] Wrong key press fails immediately
- [ ] W → W sequence requires 2x hold on second step

## Troubleshooting

**Issue**: UI elements not updating
- **Solution**: Ensure all UI references are assigned in Inspector

**Issue**: Progress bar not filling
- **Solution**: Check that you assigned the child Image (filled), not the background

**Issue**: Cue text always empty
- **Solution**: Backend must include `cue` field in step response

**Issue**: Hold duration too easy/hard
- **Solution**: Adjust `requiredHoldDuration` in Hold Settings

**Issue**: Same action doesn't increase hold time
- **Solution**: Enable `cumulativeHoldForSameAction` in Hold Settings

## Code Integration

The system automatically:
- Extracts `step.cue` from backend response
- Displays cue with 💡 prefix if present
- Calculates hold requirements (1x or 2x)
- Tracks hold time per frame
- Updates UI in real-time
- Resets on key release
- Validates completion

No additional code needed - just wire up the UI components!
