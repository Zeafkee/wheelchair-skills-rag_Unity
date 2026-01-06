# Training Plan Panel Setup Guide

## Overview

This guide explains how to set up the Training Plan panel UI in Unity Editor. The Training Plan Manager allows users to:
- Generate personalized training plans based on their performance
- View global error statistics across all users
- Clear their training progress
- See detailed analytics and recommendations

## Prerequisites

- Unity Editor (2019.4 or later recommended)
- TextMesh Pro package installed
- Main game scene already set up with a Canvas

## Step-by-Step Setup

### 1. Create the Training Plan Panel

1. Open your main scene in Unity Editor
2. In the Hierarchy, locate your Canvas (or create one if it doesn't exist)
3. Right-click Canvas → UI → Panel
4. Rename the new panel to **TrainingPlanPanel**

#### Configure Panel:
- **RectTransform**: Set to stretch full screen or desired size
  - Anchors: Min (0, 0), Max (1, 1)
  - Left: 0, Top: 0, Right: 0, Bottom: 0
- **Image Component**:
  - Color: Black with alpha ~0.8-0.9 (semi-transparent)
  
### 2. Create Panel Title Text

1. Right-click **TrainingPlanPanel** → UI → Text - TextMeshPro
2. Rename to **PanelTitleText**

#### Configure:
- **RectTransform**:
  - Anchor Preset: Top Stretch
  - Height: ~60
  - Top: -10
- **TextMeshProUGUI**:
  - Text: "Training Plan"
  - Font Size: 32-36
  - Alignment: Center (Horizontal & Vertical)
  - Color: White
  - Font Style: Bold

### 3. Create Content Text Area

1. Right-click **TrainingPlanPanel** → UI → Text - TextMeshPro
2. Rename to **PlanContentText**

#### Configure:
- **RectTransform**:
  - Anchor Preset: Stretch (Full)
  - Left: 20, Top: -80, Right: -20, Bottom: 80
- **TextMeshProUGUI**:
  - Font Size: 14-16
  - Alignment: Top Left
  - Color: White
  - Overflow: Scroll (Enable scrolling if needed)
  - Enable Rich Text: ✓ (checked)
  - Enable Auto-sizing: Optional

#### Add Scroll View (Optional but Recommended):

For better readability with long content:

1. Right-click **TrainingPlanPanel** → UI → Scroll View
2. Rename to **ContentScrollView**
3. Delete the existing **PlanContentText** and create it inside the **Content** object of the Scroll View
4. Configure Scroll Rect component:
   - Vertical: ✓ (checked)
   - Horizontal: ✗ (unchecked)
   - Movement Type: Clamped

### 4. Create Action Buttons

Create a button container first:

1. Right-click **TrainingPlanPanel** → UI → Panel
2. Rename to **ButtonContainer**
3. Add **Horizontal Layout Group** component:
   - Spacing: 10
   - Child Alignment: Middle Center
   - Control Child Size: Width ✓, Height ✓
   - Padding: Left: 20, Right: 20, Top: 10, Bottom: 10

#### Create Buttons:

For each button (Generate Plan, View Global Stats, Clear Progress, Close):

1. Right-click **ButtonContainer** → UI → Button
2. Rename appropriately:
   - **GeneratePlanButton**
   - **ViewGlobalStatsButton**
   - **ClearProgressButton**
   - **ClosePanelButton**

3. Configure each button's child Text:
   - GeneratePlanButton text: "Generate Training Plan"
   - ViewGlobalStatsButton text: "View Global Stats"
   - ClearProgressButton text: "Clear Progress"
   - ClosePanelButton text: "Close"

4. Button styling (optional):
   - Normal Color: Dark blue/gray
   - Highlighted Color: Lighter shade
   - Pressed Color: Even lighter
   - Font Size: 14-16

Position **ButtonContainer**:
- Anchor Preset: Bottom Stretch
- Height: ~60
- Bottom: 10

### 5. Create Loading Indicator

1. Right-click **TrainingPlanPanel** → UI → Panel
2. Rename to **LoadingIndicator**

#### Configure:
- **RectTransform**:
  - Anchor Preset: Center
  - Width: 200, Height: 100
- **Image**: 
  - Color: Dark gray with alpha ~0.9

3. Add child Text - TextMeshPro:
   - Name: **LoadingText**
   - Text: "Loading..."
   - Font Size: 20
   - Alignment: Center
   - Color: White

4. (Optional) Add a rotating icon or animation for better UX

### 6. Wire Up the TrainingPlanManager Script

1. Find or create a GameObject to hold the **TrainingPlanManager** component
   - Suggested: Create an empty GameObject named "TrainingPlanManager" at root level
   
2. Add the **TrainingPlanManager** script component:
   - In Inspector, click "Add Component"
   - Search for "TrainingPlanManager"
   - Click to add

3. Configure Backend Settings:
   - **Backend Base Url**: "http://localhost:8000" (or your backend URL)
   - **User Id**: "sefa001" (or dynamic user ID)

4. Drag and drop UI references:

   **UI References - Buttons:**
   - **Generate Plan Button**: Drag GeneratePlanButton here
   - **View Global Stats Button**: Drag ViewGlobalStatsButton here
   - **Clear Progress Button**: Drag ClearProgressButton here
   - **Close Panel Button**: Drag ClosePanelButton here

   **UI References - Panel:**
   - **Training Plan Panel**: Drag TrainingPlanPanel here
   - **Panel Title Text**: Drag PanelTitleText here
   - **Plan Content Text**: Drag PlanContentText here

   **UI References - Loading:**
   - **Loading Indicator**: Drag LoadingIndicator here

### 7. Set Initial States

1. Select **TrainingPlanPanel** in Hierarchy
2. **Uncheck** the checkbox next to the GameObject name (make it inactive)
3. Select **LoadingIndicator** in Hierarchy
4. **Uncheck** the checkbox next to the GameObject name (make it inactive)

**Important**: Do NOT add OnClick events manually in the Inspector. The TrainingPlanManager script automatically binds button events in the `SetupEventListeners()` method.

### 8. Add Menu Buttons (Optional)

You can add buttons to your main menu to open the Training Plan panel:

1. In your main menu UI, create a button
2. Name it something like "TrainingPlanMenuButton"
3. Set the button's OnClick event to:
   - Target: TrainingPlanPanel GameObject
   - Function: GameObject.SetActive
   - Parameter: ✓ (checked/true)

## UI Hierarchy Summary

```
Canvas
├── TrainingPlanPanel (Panel) [Initially Inactive]
│   ├── PanelTitleText (TextMeshPro)
│   ├── ContentScrollView (Scroll View) [Optional]
│   │   └── Viewport
│   │       └── Content
│   │           └── PlanContentText (TextMeshPro)
│   ├── ButtonContainer (Panel with Horizontal Layout Group)
│   │   ├── GeneratePlanButton (Button)
│   │   ├── ViewGlobalStatsButton (Button)
│   │   ├── ClearProgressButton (Button)
│   │   └── ClosePanelButton (Button)
│   └── LoadingIndicator (Panel) [Initially Inactive]
│       └── LoadingText (TextMeshPro)
└── TrainingPlanManager (Empty GameObject with TrainingPlanManager script)
```

## Expected Behavior

### Generate Training Plan
1. User clicks "Generate Training Plan" button
2. Loading indicator appears
3. POST request sent to `/user/{userId}/generate-plan`
4. Panel displays formatted training plan with:
   - User info, phase, timestamp
   - Recommended skills to practice
   - Skills needing improvement
   - Common mistakes
   - Performance comparison vs global average
   - Session goals and notes

### View Global Stats
1. User clicks "View Global Stats" button
2. Loading indicator appears
3. GET request sent to `/analytics/global-errors`
4. Panel displays global statistics with:
   - Total attempts and users
   - Skill summary (attempts, success rates, errors)
   - Most problematic steps
   - Common action confusions

### Clear Progress
1. User clicks "Clear Progress" button
2. Loading indicator appears
3. DELETE request sent to `/user/{userId}/clear-progress`
4. Success/error message displayed

### Close Panel
1. User clicks "Close" button
2. Panel disappears (becomes inactive)

## Display Format Examples

### Training Plan Format:
```
═══════════════════════════════════════
  👤 User: sefa001
  📊 Phase: Foundation
  🕐 Generated: Jan 06, 2026 21:30
═══════════════════════════════════════

🎯 RECOMMENDED SKILLS TO PRACTICE
───────────────────────────────────────
  🔴 Rolls backwards 2m
      Reason: Low success rate: 40%
      Attempts: 5 | Success Rate: 40%

⚠️ SKILLS NEEDING IMPROVEMENT
───────────────────────────────────────
  ❌ a01_10m_forward
      Total Errors: 5
      Error Types: wrong_input, wrong_direction
```

## Integration with Error Recording

The Training Plan system works with the error recording feature in `RealTimeCoachAssistant.cs`:

1. When a user makes a wrong action during tutorial, the system:
   - Records the input via `RecordInput()`
   - Classifies the error type via `DetermineErrorType()`
   - Sends error details to backend via `RecordError()`

2. Error types classified:
   - `wrong_direction`: move_forward ↔ move_backward
   - `wrong_turn_direction`: turn_left ↔ turn_right
   - `stopped_instead_of_moving`: expected movement, got brake
   - `moved_instead_of_stopping`: expected brake, got movement
   - `missed_pop_casters`: expected pop_casters, got something else
   - `wrong_input`: default/other errors

3. Backend collects these errors and uses them to:
   - Generate personalized training plans
   - Track global error statistics
   - Identify problematic steps
   - Compare user performance

## Error Recording Configuration

In the **RealtimeCoachTutorial** component (RealTimeCoachAssistant.cs):
- **Record Errors**: ✓ (checked) to enable error recording
- This flag controls whether errors are sent to backend

## Testing Checklist

- [ ] Training Plan panel appears when Generate Plan button is clicked
- [ ] Loading indicator shows during API calls
- [ ] Generated training plan displays with proper formatting
- [ ] Global stats display correctly
- [ ] Clear progress button works and shows confirmation
- [ ] Close button hides the panel
- [ ] Error recording happens during wrong actions in tutorial
- [ ] Errors appear in training plan recommendations
- [ ] Panel is hidden on game start
- [ ] All buttons are properly wired up

## Troubleshooting

### Panel doesn't appear
- Check that TrainingPlanPanel is assigned in TrainingPlanManager Inspector
- Verify panel is a child of Canvas
- Check Canvas is set to Screen Space - Overlay

### Buttons don't work
- Verify all button references are assigned in Inspector
- Check console for error messages
- Ensure EventSystem exists in scene

### API calls fail
- Verify backend URL is correct
- Check backend server is running
- Look at console for network errors
- Verify user ID exists in backend

### Text appears cut off
- Increase panel size
- Enable scrolling
- Reduce font size
- Check RectTransform settings

### Loading indicator doesn't show
- Verify LoadingIndicator is assigned
- Check it's a child of TrainingPlanPanel
- Ensure it's positioned correctly (Center anchor)

## Backend API Endpoints

The system expects these endpoints to be available:

1. **POST** `/user/{userId}/generate-plan`
   - Generates personalized training plan
   - Returns: TrainingPlanResponse JSON

2. **GET** `/analytics/global-errors`
   - Retrieves global error statistics
   - Returns: GlobalErrorStats JSON

3. **DELETE** `/user/{userId}/clear-progress`
   - Clears user's training progress
   - Returns: Success/error message

4. **POST** `/attempt/{attemptId}/record-error`
   - Records error during tutorial
   - Body: `{ step_number, error_type, expected_action, actual_action }`

## Files Modified/Created

1. **Assets/Scripts/TrainingPlanManager.cs** - New file with all logic
2. **Assets/Scripts/RealTimeCoachAssistant.cs** - Updated with error recording
3. **TRAINING_PLAN_SETUP.md** - This documentation file

## Additional Notes

- The UI uses TextMesh Pro for better text rendering
- All API calls are asynchronous (coroutines)
- Error handling displays user-friendly messages
- Panel is designed to be modal (overlays other UI)
- Unicode characters (emojis) used for better visual appeal
- Timestamps are formatted from ISO 8601 to readable format
- Progress bars can be displayed using GetProgressBar() helper method

## Future Enhancements

Consider adding:
- Animated transitions when opening/closing panel
- More detailed error analytics
- Skill-specific drill recommendations
- Progress graphs/charts
- Export training plan to PDF
- Share achievements with other users
- Localization support for multiple languages
