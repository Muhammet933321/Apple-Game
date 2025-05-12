using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro; // If you use TextMeshPro for UI

public class FirebaseAuthManager : MonoBehaviour
{
    
    [Header("UI Elements")]
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;
    public TMP_Text feedbackText;
    
    public FirestoreAppointmentManager  appointmentManager;

    private FirebaseAuth auth;
    private AuthResult result;
    

    public TMP_InputField inputField; // assign in inspector or via code

    public void OnInputFieldSelected()
    {
        TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "Enter text");
    }
    
    private void Start()
    {
        //inputField.onSelect.AddListener(delegate { OnInputFieldSelected(); });
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        Debug.Log("Firebase Auth Initialized");
        //StartCoroutine(LoginUser("hasta@gmail.com", "123456"));
    }

    // Called by Register Button
    public void OnRegisterButtonClicked()
    {
        StartCoroutine(RegisterUser(emailInputField.text, passwordInputField.text));
    }

    // Called by Login Button
    public void OnLoginButtonClicked()
    {
        StartCoroutine(LoginUser(emailInputField.text, passwordInputField.text));
    }

    private IEnumerator RegisterUser(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Email and Password must not be empty.";
            yield break;
        }

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogWarning($"Registration Failed: {registerTask.Exception}");
            feedbackText.text = "Registration Failed: " + registerTask.Exception.GetBaseException().Message;
        }
        else
        {
            result = registerTask.Result;
            Debug.Log($"User Registered Successfully: {result.User.UserId}");
            feedbackText.text = "Registration Successful!";
        }
    }

    private IEnumerator LoginUser(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Email and Password must not be empty.";
            yield break;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogWarning($"Login Failed: {loginTask.Exception}");
            feedbackText.text = "Login Failed: " + loginTask.Exception.GetBaseException().Message;
        }
        else
        {
            result = loginTask.Result;
            Debug.Log($"User Logged In Successfully: {result.User.Email}");
            feedbackText.text = "Login Successful!";
            
            appointmentManager.StartFireStore(result.User);
        }
    }

    public void OnLogoutButtonClicked()
    {
        auth.SignOut();
        result = null;
        feedbackText.text = "Logged Out.";
        Debug.Log("User Logged Out.");
    }
}
