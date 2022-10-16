using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if PLAYFAB_FB
using Facebook.Unity;
#endif
#if PLAYFAB_GPG
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using PlayFab;
using PlayFab.ClientModels;
using LoginResult = PlayFab.ClientModels.LoginResult;

public class PlayfabAuthClient : MonoBehaviour
{
    [System.Serializable]
    public class FailEvent : UnityEvent<string> { }

    public const string KEY_LOGIN_TYPE = "LoginType";
    public static bool IsLoggedIn { get; private set; }
    public static string PlayFabId { get; private set; }
    public static EntityTokenResponse EntityToken { get; private set; }
    public enum LoginType
    {
        None,
        Facebook,
        GooglePlay,
        PlayFab,
        Device,
    }
    public bool autoLogin;
    public UnityEvent onLoggingIn;
    public UnityEvent onSuccess;
    public UnityEvent onCancel;
    public FailEvent onFail;
    public string username;
    public string password;

    public string Username { get { return username; } set { username = value; } }
    public string Password { get { return password; } set { password = value; } }

    private LoginType autoLoginType;
    private bool isLoggingIn;

    private void Start()
    {
        autoLoginType = (LoginType)PlayerPrefs.GetInt(KEY_LOGIN_TYPE, (int)LoginType.None);
        // Init Facebook
#if PLAYFAB_FB
        FB.Init(OnFacebookInitialized);
#endif

#if UNITY_ANDROID && PLAYFAB_GPG
        // recommended for debugging:
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            // TODO: May do something
        });
#endif
    }

#if PLAYFAB_FB
    private void OnFacebookInitialized()
    {
        if (autoLogin && FB.IsLoggedIn && autoLoginType == LoginType.Facebook)
        {
            isLoggingIn = true;
            onLoggingIn.Invoke();
            LoginWithFacebookToken();
        }
    }
#endif

    protected void LoginWithFacebookToken()
    {
#if PLAYFAB_FB
        PlayFabClientAPI.LoginWithFacebook(new LoginWithFacebookRequest
        {
            TitleId = PlayFabSettings.TitleId,
            AccessToken = AccessToken.CurrentAccessToken.TokenString,
            CreateAccount = true,
        }, OnPlayfabFacebookAuthComplete, OnPlayfabFacebookAuthFailed);
#endif
    }

    public void LoginWithFacebook()
    {
#if PLAYFAB_FB
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        if (FB.IsLoggedIn)
        {
            // Logout to re-login
            FB.LogOut();
        }
        FB.LogInWithReadPermissions(null, (result) =>
        {
            // If result has no errors, it means we have authenticated in Facebook successfully
            if (result.Cancelled)
            {
                isLoggingIn = false;
                onCancel.Invoke();
                return;
            }

            if (result == null || string.IsNullOrEmpty(result.Error))
            {
                // Login Success
                PlayFabClientAPI.LoginWithFacebook(new LoginWithFacebookRequest
                {
                    TitleId = PlayFabSettings.TitleId,
                    AccessToken = AccessToken.CurrentAccessToken.TokenString,
                    CreateAccount = true,
                }, OnPlayfabFacebookAuthComplete, OnPlayfabFacebookAuthFailed);
            }
            else
            {
                isLoggingIn = false;
                // Login Failed
                Debug.LogError("[FB Login Failed] " + result.Error);
                onFail.Invoke(result.Error);
            }
        });
#endif
    }

    public void LoginWithGooglePlay()
    {
#if UNITY_ANDROID && PLAYFAB_GPG
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            GetGPGServerSideAccessAndLogin();
        }
        else
        {
            PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
            {
                switch (status)
                {
                    case SignInStatus.InternalError:
                        isLoggingIn = false;
                        // Login Failed
                        onFail.Invoke("Cannot login with Google Play Games Service");
                        break;
                    case SignInStatus.Canceled:
                        isLoggingIn = false;
                        onCancel.Invoke();
                        break;
                    default:
                        GetGPGServerSideAccessAndLogin();
                        break;
                }
            });
        }
#endif
    }

#if UNITY_ANDROID && PLAYFAB_GPG
    private void GetGPGServerSideAccessAndLogin()
    {
        // When google play login success, send login request to server
        PlayGamesPlatform.Instance.RequestServerSideAccess(false, (idToken) =>
        {
            // Login Success
            PlayFabClientAPI.LoginWithGoogleAccount(new LoginWithGoogleAccountRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                ServerAuthCode = idToken,
                CreateAccount = true,
            }, OnPlayfabGooglePlayAuthComplete, OnPlayfabGooglePlayAuthFailed);
        });
    }
#endif

    public void LoginWithPlayFab()
    {
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest()
        {
            Username = username,
            Password = password
        }, OnPlayfabLoginComplete, OnPlayfabLoginFailed);
    }

    public void RegisterPlayFabUser()
    {
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest()
        {
            Username = username,
            Password = password,
            RequireBothUsernameAndEmail = false
        }, OnPlayfabRegisterComplete, OnPlayfabRegisterFailed);
    }

    public void GuestLogin()
    {
#if UNITY_ANDROID
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
        {
            AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
        }, OnPlayfabDeviceAuthComplete, OnPlayfabDeviceAuthFailed);
#endif
#if UNITY_IOS
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        PlayFabClientAPI.LoginWithIOSDeviceID(new LoginWithIOSDeviceIDRequest()
        {
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
        }, OnPlayfabDeviceAuthComplete, OnPlayfabDeviceAuthFailed);
#endif
    }

    private void OnPlayfabFacebookAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.Facebook);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        EntityToken = result.EntityToken;
    }

    private void OnPlayfabFacebookAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[Facebook Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabGooglePlayAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.GooglePlay);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        EntityToken = result.EntityToken;
    }

    private void OnPlayfabGooglePlayAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[Google Play Games Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabLoginComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.PlayFab);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        EntityToken = result.EntityToken;
    }

    private void OnPlayfabLoginFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[PlayFab Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabRegisterComplete(RegisterPlayFabUserResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.PlayFab);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        EntityToken = result.EntityToken;
    }

    private void OnPlayfabRegisterFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[PlayFab Register Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabDeviceAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.Device);
        IsLoggedIn = true;
        PlayFabId = result.PlayFabId;
        EntityToken = result.EntityToken;
    }

    private void OnPlayfabDeviceAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[Device Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    public void SaveLoginType(LoginType loginType)
    {
        PlayerPrefs.SetInt(KEY_LOGIN_TYPE, (int)loginType);
        PlayerPrefs.Save();
    }
}
