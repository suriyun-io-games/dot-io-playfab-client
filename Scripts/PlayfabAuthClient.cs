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

    public UnityEvent onSuccess;
    public FailEvent onFail;

    private bool isLoggingIn;

    private void Start()
    {
        // Init Facebook
        FB.Init();

        // Init google play services
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .AddOauthScope("profile")
            .RequestServerAuthCode(false)
            .Build();

        PlayGamesPlatform.InitializeInstance(config);

        // recommended for debugging:
        PlayGamesPlatform.DebugLogEnabled = true;

        // Activate the Google Play Games platform
        PlayGamesPlatform.Activate();
    }

    public void LoginWithFacebook()
    {
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        FB.LogInWithReadPermissions(null, (result) =>
        {
            // If result has no errors, it means we have authenticated in Facebook successfully
            if (result == null || string.IsNullOrEmpty(result.Error))
            {
                isLoggingIn = true;
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
        if (isLoggingIn)
            return;
        isLoggingIn = true;
        Social.localUser.Authenticate((success, message) => {
            if (success)
            {
                isLoggingIn = true;
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
        });
    }

    private void OnPlayfabFacebookAuthComplete(LoginResult result)
    {
        isLoggingIn = false;
        onSuccess.Invoke();
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
    }

    private void OnPlayfabGooglePlayAuthFailed(PlayFabError error)
    {
        isLoggingIn = false;
        Debug.LogError("[GP Login Failed] " + error.ErrorMessage);
        onFail.Invoke(error.ErrorMessage);
    }
}
