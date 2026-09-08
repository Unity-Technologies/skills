# Capturing the Editor for visual checks

There is no capture command in the Pipeline catalog. Run the capture as C# through `eval`, write
a PNG, then **read that file** — reading an image is something the agent does natively, so the
two-step version is equivalent to a single capture tool.

Both variants below were verified against a live Unity 6 Editor.

## Scene View

```csharp
var sv = UnityEditor.SceneView.lastActiveSceneView;
if (sv == null) throw new System.Exception("No active SceneView to capture.");
var cam = sv.camera;
var rt = new UnityEngine.RenderTexture(1280, 720, 24);
var prevTarget = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevTarget;
var prevActive = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = prevActive;
var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unity-sceneview.png");
System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(tex));
return path;
```

## Game camera

Same shape, rendering `Camera.main` instead:

```csharp
var cam = UnityEngine.Camera.main;
if (cam == null) throw new System.Exception("No Camera.main in the active scene.");
var rt = new UnityEngine.RenderTexture(1280, 720, 24);
var prevTarget = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevTarget;
var prevActive = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
UnityEngine.RenderTexture.active = prevActive;
var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unity-gameview.png");
System.IO.File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(tex));
return path;
```

## Several angles

Move the Scene View camera between captures and write a distinct filename each time. `sv.pivot`
sets what it looks at, `sv.rotation` the direction, `sv.size` the distance; call `sv.Repaint()`
after changing them, then capture as above.

```csharp
var sv = UnityEditor.SceneView.lastActiveSceneView;
sv.pivot = new UnityEngine.Vector3(0, 0, 0);
sv.rotation = UnityEngine.Quaternion.Euler(30, 45, 0);   // vary this per angle
sv.size = 10f;
sv.Repaint();
```

To frame specific objects rather than a fixed point, select them and use
`UnityEditor.SceneView.FrameLastActiveSceneView()`, or set `sv.pivot` to the centre of their
combined bounds.

**Then read the returned path.** Write each capture to a distinct filename so a later capture
doesn't get confused with an earlier one — comparing before and after a change is the whole point.

Temp files are the default here rather than `Assets/`: a PNG written into the project becomes an
imported asset the user then has to clean up.
