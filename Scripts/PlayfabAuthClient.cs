using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Facebook.Unity;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
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
        GooglePlay
    }

    public UnityEvent onLoggingIn;
    public UnityEvent onSuccess;
    public UnityEvent onCancel;
    public FailEvent onFail;

    private LoginType autoLoginType;

    private bool isLoggingIn;

    private void Start()
    {
        autoLoginType = (LoginType)PlayerPrefs.GetInt(KEY_LOGIN_TYPE, (int)LoginType.None);
        // Init Facebook
        FB.Init(OnFacebookInitialized);

#if UNITY_ANDROID
        // Init google play services
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .AddOauthScope("profile")
            .RequestServerAuthCode(false)
            .Build();

        PlayGamesPlatform.InitializeInstance(config);

        // recommended for debugging:
        PlayGamesPlatform.DebugLogEnabled = true;

        if (autoLoginType == LoginType.GooglePlay)
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

    private void OnFacebookInitialized()
    {
        if (FB.IsLoggedIn && autoLoginType == LoginType.Facebook)
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

    public void LoginWithFacebook()
    {
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
    }

    public void LoginWithGooglePlay()
    {
#if UNITY_ANDROID
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        onLoggingIn.Invoke();
        PlayGamesPlatform.Instance.Authenticate(PlayGamesAuthenticateResult);
#endif
    }

    private void PlayGamesAuthenticateResult(bool success, string message)
    {
#if UNITY_ANDROID
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

    public void SaveLoginType(LoginType loginType)
    {
        PlayerPrefs.SetInt(KEY_LOGIN_TYPE, (int)loginType);
        PlayerPrefs.Save();
    }
}
