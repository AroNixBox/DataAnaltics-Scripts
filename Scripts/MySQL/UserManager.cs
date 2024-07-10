using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MySQL {
    public class UserManager : MonoBehaviour {
        [SerializeField] private Canvas authenticationCanvas;
        
        [Header("References")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loginPanel;

        [Header("Register")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button switchRegisterLoginButton;
        [SerializeField] private Button registerButton;
        
        [Header("Login")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button switchLoginRegisterButton;
        
        public static DatabaseUser LoggedInUser;

        private void Start() {
            // Enable the Login Panel by default
            authenticationCanvas.gameObject.SetActive(true);
            loginPanel.SetActive(true);
            
            // Add Listeners to the Buttons
            registerButton.onClick.AddListener(OnRegisterPressed);
            loginButton.onClick.AddListener(OnLoginPressed);
            switchRegisterLoginButton.onClick.AddListener(OnSwitchRegisterLoginPressed);
            switchLoginRegisterButton.onClick.AddListener(OnSwitchLoginRegisterPressed);
        }

        private async void OnRegisterPressed() { // Called by the Register Button
            // Validation
            if (!ValidateInput(emailInput) || !ValidateInput(usernameInput) || !ValidateInput(passwordInput)) {
                return;
            }
            
            // Register the User if valid
            if(await MySQLManager.RegisterUser(emailInput.text, usernameInput.text, passwordInput.text)) {
                //Success
            } else {
                //Success
            }
        }
        
        private async void OnLoginPressed() { // Called by the Login Button
            // Validation
            if (!ValidateInput(loginEmailInput) || !ValidateInput(loginPasswordInput)) {
                return;
            }

            // This Line logs the user in.
            var userData = await MySQLManager.LoginUser(loginEmailInput.text, loginPasswordInput.text);
            LoggedInUser = userData;
            
            if(userData != null) { // Login the User isn't null
                //Success:
                Debug.Log("Logged in as: " + "<b>" + userData.Username + "</b>");
                
                // Disable the Authentication Canvas
                authenticationCanvas.gameObject.SetActive(false);
                
                BuildingSaveManager.Instance.LoadBuildings();
            } else {
                // Failed, Userdata is null
            }
        }
        
        private bool ValidateInput(TMP_InputField inputField) {
            if (string.IsNullOrEmpty(inputField.text) || inputField.text.Length < 5) {
                Debug.LogError("Please fill in all fields with at least 5 characters");
                return false;
            }
            return true;
        }
        
        private void OnSwitchRegisterLoginPressed() {
            registerPanel.SetActive(false);
            loginPanel.SetActive(true);
        }
        private void OnSwitchLoginRegisterPressed() {
            registerPanel.SetActive(true);
            loginPanel.SetActive(false);
        }
        
        private void OnDestroy() {
            registerButton.onClick.RemoveListener(OnRegisterPressed);
            loginButton.onClick.RemoveListener(OnLoginPressed);
            switchRegisterLoginButton.onClick.RemoveListener(OnSwitchRegisterLoginPressed);
            switchLoginRegisterButton.onClick.RemoveListener(OnSwitchLoginRegisterPressed);
        }
    }
}
