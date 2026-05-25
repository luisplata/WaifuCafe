# Drag and Drop: Staff -> Customer

Este modulo permite arrastrar un staff sobre un customer para confirmar la asignacion manual.

## Scripts incluidos

- `StaffDragItem.cs`: va en cada item visual de staff draggable.
- `CustomerDropSlot.cs`: va en cada item visual de customer (target de drop).

## Requisitos de escena

1. Tener un `EventSystem` en la escena.
2. Tener un `Canvas` para los elementos UI.
3. Tener un `StaffServiceUIController` con referencias asignadas a:
   - `StaffManager`
   - `CustomerQueue`
4. Asegurarte de que el `Graphic` del staff y del customer tengan `Raycast Target` activo.

## Setup paso a paso en Unity

1. **Crear panel de staff (draggables)**
   - En cada item de staff agregá:
     - `CanvasGroup`
     - `StaffDragItem`
   - En `StaffDragItem` configurá:
     - `Staff Index`: mismo index que usa tu `StaffFront` (`Staff.Index`).
     - `Staff Service UI Controller`: referencia al objeto con `StaffServiceUIController`.

2. **Crear panel de customers (drop targets)**
   - En cada item de customer agregá:
     - `CustomerDropSlot`
   - En `CustomerDropSlot` configurá:
     - `Queue Position`: 0 para el primero de la cola, 1 para el segundo, etc.
     - `Staff Service UI Controller`: referencia al objeto con `StaffServiceUIController`.

3. **(Opcional) Vincular eventos por codigo**
   - `StaffServiceUIController` ahora expone estos eventos:
     - `OnStaffSelected(int staffIndex)`
     - `OnCustomerSelected(int queuePosition)`
     - `OnAssignmentConfirmed(int staffIndex, int queuePosition)`
     - `OnAssignmentFailed(string message)`
   - Podés suscribirte desde cualquier script UI para mostrar feedback visual, texto o sonido.

## Flujo resultante

1. `OnBeginDrag` del staff llama `SelectStaff(staffIndex)`.
2. `OnDrop` del customer llama `TryAssignByIndices(staffIndex, queuePosition)`.
3. `StaffServiceUIController` hace:
   - Seleccion de staff
   - Seleccion de customer
   - Confirmacion de asignacion
4. Si la asignacion sale bien, dispara `OnAssignmentConfirmed`.
5. Si falla, dispara `OnAssignmentFailed`.

## Importante

- La asignacion manual ahora respeta el staff seleccionado.
- Si la cola cambia entre inicio de drag y drop, el `queuePosition` puede quedar desactualizado. Si tenes UI dinamica, actualizá `Queue Position` cuando refresques la lista.

