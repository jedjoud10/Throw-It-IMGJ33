using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// A custom voxel editor painter that allows us to create a pre-defined voxel map
// Comes with some voxel edit tools like raise / lower, add, flatten
// The only reason this is in it's own script is because this will handle all editing tool stuff
// Everything else like chunk gen and loading/saving are handled by the voxel terrain custom editor
[ExecuteInEditMode]
public class VoxelEditor : MonoBehaviour {
    private Vector3 point;
    private Vector3 normal;
    private Vector3 heldNormal;
    public bool heldShift = false;
    public bool heldCtrl = false;
    public float brushStrength;
    public float brushRadius;
    private float direction;
    public BrushType currentBrush;
    public NoiseVoxelEdit.NoiseType noiseType;
    public NoiseVoxelEdit.Dimensionality noiseDimensionality;
    public float noiseScale;
    public float targetDensity;
    public float targetHeight;

    public enum BrushType {
        AddRemove,
        RaiseLower,
        Sphere,
        Cube,
        Flatten,
        Noise,
        SetDensity,
        SetHeight,
    }


    public void Paint(Ray ray, Event guiEvent) {
        if (Physics.Raycast(ray, out RaycastHit info)) {
            point = info.point;
            normal = info.normal;
        }

        if (guiEvent.keyCode == KeyCode.LeftShift) {
            if (guiEvent.type == EventType.KeyDown && !heldShift) {
                heldShift = true;
                heldNormal = normal;
            } else if (guiEvent.type == EventType.KeyUp) {
                heldShift = false;
            }
        }

        if (guiEvent.keyCode == KeyCode.LeftControl) {
            if (guiEvent.type == EventType.KeyDown && !heldCtrl) {
                heldCtrl = true;
                heldNormal = normal;
            } else if (guiEvent.type == EventType.KeyUp) {
                heldCtrl = false;
            }
        }

        direction = 0.0f;
        if (heldShift) {
            direction = 1.0f;
        } else if (heldCtrl) {
            direction = -1.0f;
        }
        /*
        if (guiEvent.type == EventType.KeyDown) {
            switch (guiEvent.keyCode) {
                case KeyCode.LeftShift:
                    direction = 1.0f;
                    break;

                case KeyCode.LeftControl:
                    direction = -1.0f;
                    break;
            }
        }
        */

        VoxelTerrain terrain = GetComponent<VoxelTerrain>();

        IVoxelEdit edit = null;

        switch (currentBrush) {
            case BrushType.AddRemove:
                edit = new AddVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    strength = brushStrength * direction,
                    writeMaterial = true,
                    material = 0,
                };
                break;
            case BrushType.RaiseLower:
                edit = new RaiseVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    strength = brushStrength * direction,
                    writeMaterial = true,
                    material = 0,
                };
                break;
            case BrushType.Sphere:
                edit = new SphereVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    strength = brushStrength * direction,
                    writeMaterial = true,
                    material = 0,
                };
                break;
            case BrushType.Cube:
                edit = new CuboidVoxelEdit {
                    center = point,
                    halfExtents = Vector3.one * brushRadius,
                    strength = brushStrength * direction,
                    writeMaterial = true,
                    material = 0,
                };
                break;
            case BrushType.Flatten:
                edit = new FlattenVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    strength = brushStrength * direction,
                    normal = heldNormal,
                };
                break;
            case BrushType.Noise:
                edit = new NoiseVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    strength = brushStrength * direction,
                    noiseScale = noiseScale,
                    noiseType = noiseType,
                    dimensionality = noiseDimensionality,
                };
                break;
            case BrushType.SetDensity:
                edit = new SetDensityVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    targetDensity = targetDensity,
                };
                break;
            case BrushType.SetHeight:
                edit = new SetHeightVoxelEdit {
                    center = point,
                    radius = brushRadius,
                    targetHeight = targetHeight,
                };
                break;
            default:
                break;
        }

        if (direction != 0.0f) {
            terrain.ApplyVoxelEdit(edit, false, false);
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = direction < 0.0f ? Color.green : Color.red;

        switch (currentBrush) {
            case BrushType.AddRemove:
                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.RaiseLower:
                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.Sphere:
                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.Cube:
                Gizmos.DrawWireCube(point, Vector3.one * brushRadius);
                break;
            case BrushType.Flatten:
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(point, Quaternion.LookRotation(direction == 0.0 ? normal : heldNormal), new Vector3(1, 1, 0.1f));
                Gizmos.DrawWireSphere(Vector3.zero, brushRadius * 0.9f);
                Gizmos.matrix = oldMatrix;

                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.Noise:
                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.SetDensity:
                Gizmos.DrawWireSphere(point, brushRadius);
                break;
            case BrushType.SetHeight:
                Gizmos.DrawWireSphere(point, brushRadius);
                Gizmos.DrawWireCube(new Vector3(point.x, targetHeight, point.z), new Vector3(1, 0.05f, 1.0f) * brushRadius);
                break;
            default:
                break;
        }

        
    }
}
