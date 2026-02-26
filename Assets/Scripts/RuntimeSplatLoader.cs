using System.Collections;
using System.Reflection;
using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.Networking;

public class RuntimeSplatLoader : MonoBehaviour
{
    [Header("Platform Splat Bundle URL (BASE, without suffix)")]
    [SerializeField] private string splatBundleUrl; 

    [Header("Renderer")]
    [SerializeField] private GaussianSplatRenderer splatRenderer;

    public IEnumerator LoadSplatFromPlatform(string accessToken)
    {
        //Renderer check
        if (splatRenderer == null)
            splatRenderer = GetComponent<GaussianSplatRenderer>();

        if (splatRenderer == null)
        {
            Debug.LogError("[SPLAT] No GaussianSplatRenderer found on this GameObject.");
            yield break;
        }

        //URL check
        if (string.IsNullOrWhiteSpace(splatBundleUrl))
        {
            Debug.LogError("[SPLAT] splatBundleUrl is empty.");
            yield break;
        }

        string finalUrl = splatBundleUrl.Trim();

#if UNITY_ANDROID && !UNITY_EDITOR
        finalUrl += "_android.assetbundle";
#else
        finalUrl += "_windows.assetbundle";
#endif

        Debug.Log("[SPLAT] Using bundle URL: " + finalUrl);

        //Clean token (handles raw token OR "Bearer <token>")
        var token = (accessToken ?? "").Trim();
        if (token.StartsWith("Bearer "))
            token = token.Substring("Bearer ".Length).Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("[SPLAT] Token is empty after cleanup.");
            yield break;
        }

        Debug.Log("[SPLAT] Using Authorization: Bearer " + token.Substring(0, Mathf.Min(10, token.Length)) + "...");

        // Instead of UnityWebRequestAssetBundle.GetAssetBundle(finalUrl),
        // do a plain GET, then load bundle from memory.
        using (UnityWebRequest req = UnityWebRequest.Get(finalUrl))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Authorization", "Bearer " + token);

            req.certificateHandler = new BypassCertificateHandler();
            req.disposeCertificateHandlerOnDispose = true;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SPLAT] Download failed: {req.responseCode} - {req.error}");

                string body = "";
                try { body = req.downloadHandler.text; } catch { }
                if (!string.IsNullOrEmpty(body))
                    Debug.LogError("[SPLAT] Response body: " + body);

                yield break;
            }

            byte[] bytes = req.downloadHandler.data;
            Debug.Log("[SPLAT] Downloaded bytes: " + (bytes != null ? bytes.Length : 0));

            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError("[SPLAT] Download returned empty bytes.");
                yield break;
            }

            //Load AssetBundle from memory
            AssetBundleCreateRequest bundleReq = AssetBundle.LoadFromMemoryAsync(bytes);
            yield return bundleReq;

            AssetBundle bundle = bundleReq.assetBundle;
            if (bundle == null)
            {
                Debug.LogError("[SPLAT] AssetBundle.LoadFromMemoryAsync returned null bundle.");
                yield break;
            }

            //Find GaussianSplatAsset inside bundle
            GaussianSplatAsset[] splatAssets = bundle.LoadAllAssets<GaussianSplatAsset>();
            if (splatAssets == null || splatAssets.Length == 0)
            {
                Debug.LogError("[SPLAT] No GaussianSplatAsset found in bundle.");

                //Keep bundle unloaded cleanly
                bundle.Unload(false);
                yield break;
            }

            var splatAsset = splatAssets[0];

            //Assign to renderer using reflection (same as your logic)
            var rendererType = typeof(GaussianSplatRenderer);
            FieldInfo assetField = null;

            foreach (var field in rendererType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(GaussianSplatAsset).IsAssignableFrom(field.FieldType))
                {
                    assetField = field;
                    Debug.Log("[SPLAT] Using field '" + field.Name + "' on GaussianSplatRenderer for runtime asset assignment.");
                    break;
                }
            }

            if (assetField == null)
            {
                var allFields = rendererType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var f in allFields)
                    Debug.Log("[SPLAT] Field on GaussianSplatRenderer: " + f.Name + " : " + f.FieldType);

                Debug.LogError("[SPLAT] Could not find any field of type GaussianSplatAsset on GaussianSplatRenderer.");
                bundle.Unload(false);
                yield break;
            }

            assetField.SetValue(splatRenderer, splatAsset);
            Debug.Log("[SPLAT] Runtime splat assigned successfully via reflection (field: " + assetField.Name + ").");

            // Optional: keep bundle loaded if asset needs it; if not, you can unload:
            // bundle.Unload(false);
        }
    }

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
