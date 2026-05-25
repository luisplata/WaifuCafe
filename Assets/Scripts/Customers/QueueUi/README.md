# QueueUi

UI layer for `CustomerQueue` with interface-based abstractions.

## Files

- `IQueueUiView.cs`: UI contract (buttons, labels, status).
- `IQueueUiService.cs`: queue gameplay contract used by UI.
- `IQueueUiController.cs`: composition contract between view and service.
- `QueueUiView.cs`: Unity UI implementation using `Button` and `Text`.
- `CustomerQueueUiService.cs`: adapter from `CustomerQueue` to `IQueueUiService`.
- `QueueUiController.cs`: binds view events to service commands and updates UI.

## Queue Display Format

The queue label now renders each waiting customer as:

`[CustomerStats][Position]`

Example:

`[Regular | P:12.0s | W:3.1s | $:50][Pos:2]`

This lets you decide which position to serve first.

## Serving a Specific Position

Use `QueueUiController.ServeByIndex(int queueIndex)` from UI buttons.

- `queueIndex = 0` serves `Pos:1`
- `queueIndex = 1` serves `Pos:2`
- etc.

## Unity Setup

1. Create an empty GameObject named `QueueUiRoot`.
2. Add components:
   - `QueueUiView`
   - `CustomerQueueUiService`
   - `QueueUiController`
3. In `QueueUiView`, assign all button and text references from your Canvas.
4. In `CustomerQueueUiService`, assign `CustomerQueue` and set `Configured Max Queue Size`.
5. In `QueueUiController`:
   - `View Source` = component with `QueueUiView`
   - `Service Source` = component with `CustomerQueueUiService`

## Notes

- You can replace `QueueUiView` or `CustomerQueueUiService` with your own implementations as long as they implement the corresponding interfaces.
- This lets you migrate from basic UI to a custom UI framework without changing queue gameplay logic.
