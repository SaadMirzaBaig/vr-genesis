using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class QuestTMPKeyboard : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private TMP_InputField input;
    private TouchScreenKeyboard kb;

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Force open quest keyboard
        var type = input.contentType == TMP_InputField.ContentType.Password
            ? TouchScreenKeyboardType.Default
            : TouchScreenKeyboardType.EmailAddress;

        kb = TouchScreenKeyboard.Open(input.text, type, false, false,
            input.contentType == TMP_InputField.ContentType.Password, false, "");
#endif
    
        input.ActivateInputField();
    }

    public void OnDeselect(BaseEventData eventData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        kb = null;
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (kb == null) return;

        if (input.text != kb.text)
        {
            input.text = kb.text;
            input.caretPosition = input.text.Length;
        }

        if (kb.status == TouchScreenKeyboard.Status.Done ||
            kb.status == TouchScreenKeyboard.Status.Canceled ||
            kb.status == TouchScreenKeyboard.Status.LostFocus)
        {
            kb = null;
        }
#endif
    }
}
