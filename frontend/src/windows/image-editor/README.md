# Image Editor Window

Editor used to produce the **template image** an `IMAGE_SEARCH` flow step searches
for. Electron opens it in its own window with a PNG (today: a screenshot of the
whole virtual desktop), the user crops / erases it, and the edited PNG is
returned to the caller.

Pure Canvas 2D, no image libraries.

---

## Files

```
frontend/src/windows/image-editor/
├── ImageEditorPage.tsx        # orchestration: IPC, tool state, shortcuts, layout
├── ImageEditorPage.css        # window chrome (the app has no utility CSS framework)
├── types.ts                   # shared types + the coordinate space contract
├── hooks/
│   ├── useImageCanvas.ts      # the image document + crop/erase operations
│   ├── useUndoRedo.ts         # snapshot stack with thumbnails and a memory budget
│   └── useViewTransform.ts    # zoom + pan maths
├── components/
│   ├── Canvas.tsx             # viewport: rendering + all pointer interaction
│   ├── Toolbar.tsx            # top bar: save/cancel, undo/redo, zoom, readouts
│   ├── ToolRail.tsx           # left tool picker
│   ├── OptionsPanel.tsx       # right sidebar: selection, eraser, view options
│   ├── Minimap.tsx            # navigation preview
│   └── HistoryPanel.tsx       # undo/redo list
└── utils/canvas-utils.ts      # base64 <-> blob/image, canvas helpers, geometry
```

## Coordinate spaces

Everything hangs off one transform, defined in `types.ts`:

```
viewport = image * scale + offset
image    = (viewport - offset) / scale
```

- **image space** — integer pixels of the image being edited
- **viewport space** — CSS pixels relative to the viewport element

The image is *drawn* through this transform onto viewport-sized canvases; it is
never a CSS-transformed DOM element. That keeps hit testing to a single formula
and means a 7680x2165 desktop screenshot does not become a 7680x2165 element.

`Canvas.tsx` stacks two canvases, both sized to the viewport (times
`devicePixelRatio`):

| layer     | contents                                   | repainted when              |
| --------- | ------------------------------------------ | --------------------------- |
| `scene`   | checkerboard, image, pixel grid            | image or view changes       |
| `overlay` | selection, lasso path, eraser brush circle | pointer moves               |

## The document and history

`useImageCanvas` owns one detached canvas (the "document"). Every edit produces a
new document canvas and pushes a snapshot onto `useUndoRedo`.

Snapshots are full canvases, so a full-desktop screenshot costs
`width * height * 4` bytes each. The stack evicts oldest-first once it exceeds
its memory budget (256 MB) or 40 entries.

## IPC contract

Images travel as **base64 PNG strings** — the shape .Net already produces for
`byte[]` in the JSON payload, so nothing has to be converted on the way through.

```
caller  ElectronApiService.imageEditor.openWindow(pngBase64)
          -> image-editor-handler.ts creates the window
editor  ElectronApiService.imageEditor.signalReady()          -> pngBase64
editor  ElectronApiService.imageEditor.signalCloseWindow(png) -> saved
        ElectronApiService.imageEditor.signalCloseWindow(null)-> cancelled
          -> handler closes the window and resolves openWindow()
```

Force-closing the window (Alt+F4) resolves `openWindow()` with `null`.

## Tools

| tool             | interaction                                                    |
| ---------------- | -------------------------------------------------------------- |
| Pan (H)          | drag                                                            |
| Rect crop (R)    | drag a rectangle, then Apply / Enter                            |
| Lasso crop (L)   | drag freehand, then Apply / Enter                               |
| Polygon crop (P) | click points, double-click or click the first point to close    |
| Eraser (E)       | drag; erased pixels become fully transparent in the export      |

Both lasso crops keep the polygon's bounding box and make everything outside the
polygon transparent.

Wheel zooms at the cursor, space or middle-drag pans with any tool, `Enter`
applies a selection, `Esc` clears it, `Ctrl+Z` / `Ctrl+Y` undo/redo, `0` fits,
`1` is 1:1.
