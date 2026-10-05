using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string nextScene = "TitleScreen";
    private bool busy;
    private void Awake()
    {
        if (loginButton != null) loginButton.onClick.AddListener(Login);
        if (registerButton != null) registerButton.onClick.AddListener(Register);
        if (passwordInput != null) passwordInput.contentType = TMP_InputField.ContentType.Password;
    }
    private void OnDestroy()
    {
        if (loginButton != null) loginButton.onClick.RemoveListener(Login);
        if (registerButton != null) registerButton.onClick.RemoveListener(Register);
    }
    public void Login() { if (!busy) StartCoroutine(Submit(false)); }
    public void Register() { if (!busy) StartCoroutine(Submit(true)); }
    private IEnumerator Submit(bool registration)
    {
        if (idInput == null || passwordInput == null) { SetStatus("Assign both input fields."); yield break; }
        busy = true; SetButtons(false); SetStatus(registration ? "Registering…" : "Logging in…");
        string id = idInput.text, password = passwordInput.text;
        yield return null;
        if (registration)
        {
            bool registered = LocalAccountSession.Store.Register(id, password, out var error);
            SetStatus(registered ? "Registered. Press Login to sign in." : error);
        }
        else
        {
            if (LocalAccountSession.Login(id, password, out var error))
            {
                passwordInput.text = "";
                if (Application.CanStreamedLevelBeLoaded(nextScene)) SceneManager.LoadScene(nextScene);
                else SetStatus("Logged in. Add " + nextScene + " to Build Settings.");
            }
            else SetStatus(error);
        }
        if (!registration) passwordInput.text = ""; busy = false; SetButtons(true);
    }
    private void SetStatus(string text) { if (statusText != null) statusText.text = text; }
    private void SetButtons(bool enabled)
    {
        if (loginButton != null) loginButton.interactable = enabled;
        if (registerButton != null) registerButton.interactable = enabled;
    }
}
