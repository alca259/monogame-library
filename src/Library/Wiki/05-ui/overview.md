# UI — Visión General

**Namespace:** `Alca.MonoGame.Kernel.UI`

El sistema de UI es un árbol de elementos visual en el que cada nodo hereda de `UIElement`. El árbol es gestionado por `UIRoot`, que ejecuta el ciclo completo Measure → Arrange → Update → Draw una vez por frame.

---

## Arquitectura del árbol

```
UIRoot (UIContainer)
├── UIOverlayManager         ← overlays modales (Tooltip, Dropdown, etc.)
├── StackPanel               ← layout vertical del HUD
│   ├── Label                ← puntuación
│   └── ProgressBar          ← barra de vida
└── Panel
    └── Button               ← pausa
```

- **`UIRoot`** es el nodo raíz. Gestiona el `SpriteBatch.Begin/End` y delega en los hijos.
- **`UIContainer`** es cualquier nodo intermedio que puede tener hijos.
- **`UIElement`** es el nodo hoja con lógica de Measure/Arrange/Update/Draw.

---

## Ciclo de vida por frame

```
1. SceneManager.Update()
        └── UIRoot.Update()
                ├── UIFocusManager.Update()     ← Tab / D-Pad
                ├── UIInteractionManager.Update() ← hit test + puntero
                └── UIContainer.Update()         ← propaga a hijos

2. SceneManager.Draw()
        └── UIRoot.DrawAll()
                ├── UIContainer.Draw()           ← árbol principal
                └── UIOverlayManager.Draw()      ← siempre encima
```

---

## Pipeline de layout

El layout sigue el patrón WPF en dos pasadas:

| Pasada | Método | Qué hace |
|---|---|---|
| **Measure** | `Measure(availableSize)` | Calcula `DesiredSize` bottom-up |
| **Arrange** | `Arrange(finalBounds)` | Establece `Bounds` top-down |

Una llamada a `Invalidate()` propaga `IsLayoutDirty = true` hacia los ancestros hasta `UIRoot`, que relanza ambas pasadas al inicio del siguiente frame.

---

## Relación con SceneManager

Cada `Scene` tiene una propiedad `UIRoot?`. Cuando se hace `RequestChange`, el SceneManager reemplaza la escena activa y con ella su árbol de UI. El `UIFocusManager` y el `UIInteractionManager` se registran en `Core` y acceden al árbol activo vía `Core.UIFocus` y `Core.UIInteraction`.

---

## Ver también

- [UIElement / UIContainer / UIRoot →](elements.md)
- [Controles →](controls.md)
- [Layouts →](layout.md)
- [Foco →](focus.md)
- [Interacción →](interaction.md)
- [Transiciones →](transitions.md)
