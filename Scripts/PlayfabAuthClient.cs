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
    public enum LoginType
    {
        None,
        Facebook,
        GooglePlay,
        PlayFab,
    }
    public bool autoLogin;
    public UnityEvent onLoggingIn;
    public UnityEvent onSuccess;
    public UnityEvent onCancel;
    public FailEvent onFail;
    public string username;
    public string password;

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
        // Init google play services
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .AddOauthScope("profile")
            .RequestServerAuthCode(false)
            .Build();

        PlayGamesPlatform.InitializeInstance(config);

        // recommended for debugging:
        PlayGamesPlatform.DebugLogEnabled = true;

        if (autoLogin && autoLoginType == LoginType.GooglePlay)
        {
            // Silent login
            if (isLoggingIn)
                return;
            isLoggingIn = true;
            onLoggingIn.Invoke();
            PlayGamesPlatform.Instance.Authenticate(PlayGamesAuthenticateResult, true);
        }
#endif
    }

#if PLAYFAB_FB
    private void OnFacebookInitialized()
    {
        if (autoLogin && FB.IsLoggedIn && autoLoginType == LoginType.Facebook)
        {
            isLoggingIn = true;
            onLoggingIn.Invoke();
            PlayFabClientAPI.LoginWithFacebook(new LoginWithFacebookRequest
            {
                TitleId = PlayFabSettings.TitleId,
                AccessToken = AccessToken.CurrentAccessToken.TokenString,
                CreateAccount = true,
            }, OnPlayfabFacebookAuthComplete, OnPlayfabFacebookAuthFailed);
        }
    }
#endif

    public void LoginWithFacebook()
    {
#if PLAYFAB_FB
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
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
        PlayGamesPlatform.Instance.Authenticate(PlayGamesAuthenticateResult);
#endif
    }

    private void PlayGamesAuthenticateResult(bool success, string message)
    {
#if UNITY_ANDROID && PLAYFAB_GPG
        if (success)
        {
            // Login Success
            var serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();
            PlayFabClientAPI.LoginWithGoogleAccount(new LoginWithGoogleAccountRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                ServerAuthCode = serverAuthCode,
                CreateAccount = true,
            }, OnPlayfabGooglePlayAuthComplete, OnPlayfabGooglePlayAuthFailed);
        }
        else
        {
            isLoggingIn = false;
            // Login Failed
            Debug.LogError("[GP Login Failed] " + message);
            onFail.Invoke(message);
        }
#endif
    }

    public void LoginWithPlayFab()
    {
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest()
        {
            Username = username,
            Password = password
        }, OnPlayfabLoginComplete, OnPlayfabLoginFailed);
    }

    public void RegisterPlayFabUser()
    {
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest()
        {
            Username = username,
            Password = password
        }, OnPlayfabRegisterComplete, OnPlayfabRegisterFailed);
    }

    private void OnPlayfabFacebookAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.Facebook);
    }

    private void OnPlayfabFacebookAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[FB Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabGooglePlayAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.GooglePlay);
    }

    private void OnPlayfabGooglePlayAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[GP Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabLoginComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.PlayFab);
    }

    private void OnPlayfabLoginFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[PF Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    private void OnPlayfabRegisterComplete(RegisterPlayFabUserResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
        SaveLoginType(LoginType.PlayFab);
    }

    private void OnPlayfabRegisterFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[PF Register Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }

    public void SaveLoginType(LoginType loginType)
    {
        PlayerPrefs.SetInt(KEY_LOGIN_TYPE, (int)loginType);
        PlayerPrefs.Save();
    }
}
