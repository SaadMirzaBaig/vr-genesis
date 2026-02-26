using System.Threading.Tasks;
using UnityEngine;
using GLTFast;
using UnityEngine.EventSystems;

public class RuntimeGlbURL : MonoBehaviour
{
    public async Task<GameObject> LoadOneGlb(
        string fullUrl,
        string bearerToken,
        string objectName,
        Vector3 position,
        Vector3 rotationEuler,
        Vector3 scale,
        Transform parent = null
    )
    {
        var wrapper = new GameObject(string.IsNullOrEmpty(objectName) ? "GLB_Instance" : objectName);

        if (parent != null)
            wrapper.transform.SetParent(parent, false);

        wrapper.transform.position = position;
        wrapper.transform.rotation = Quaternion.Euler(rotationEuler);
        wrapper.transform.localScale = scale;

        var downloadProvider = new BearerTokenDownloadProvider(bearerToken);
        var gltf = new GltfImport(downloadProvider);

        Debug.Log("[RuntimeGlbURL] Loading from: " + fullUrl);

        bool loaded = await gltf.Load(fullUrl);
        if (!loaded)
        {
            Debug.LogError("[RuntimeGlbURL] Load FAILED: " + fullUrl);
            Debug.LogError("[RuntimeGlbURL] bearerTokenLen=" + (bearerToken?.Length ?? 0));
            Debug.LogError("[RuntimeGlbURL] wrapper exists? " + (wrapper != null));
            Destroy(wrapper);
            return null;
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(wrapper.transform);
        if (!instantiated)
        {
            Debug.LogError("[RuntimeGlbURL] Instantiate FAILED: " + fullUrl);
            Destroy(wrapper);
            return null;
        }

        SanitizeImportedGlb(wrapper);
        ForceUrpLitOnMaterials(wrapper);

        return wrapper;
    }

    private void ForceUrpLitOnMaterials(GameObject root)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("[RuntimeGlbURL] URP/Lit shader not found. Skipping shader swap.");
            return;
        }

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;

                var mainTex = m.mainTexture;   
                m.shader = urpLit;             
                m.mainTexture = mainTex;
            }
        }

        Debug.Log("[RuntimeGlbURL] Forced URP/Lit on GLB materials.");
    }

    private void SanitizeImportedGlb(GameObject root)
    {
        // Disable all Lights imported from GLB 
        foreach (var l in root.GetComponentsInChildren<Light>(true))
        {
            Destroy(l.gameObject);
        }
        //Disable Cameras + remove AudioListeners
        foreach (var cam in root.GetComponentsInChildren<Camera>(true))
        {
            cam.enabled = false;
        }
        foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
        {
            Destroy(al);
        }
        //Remove extra EventSystems
        foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
        {
            Destroy(es.gameObject);
        }
        //remove XR/OVR managers
        var allBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in allBehaviours)
        {
            if (b == null) continue;

            var t = b.GetType().Name;

            if (t.Contains("OVRManager") ||
                t.Contains("OVRCameraRig") ||
                t.Contains("XRInteractionManager") ||
                t.Contains("XROrigin") ||
                t.Contains("XRRayInteractor") ||
                t.Contains("XRDirectInteractor"))
            {
                Destroy(b.gameObject);
            }
        }
    }
}
