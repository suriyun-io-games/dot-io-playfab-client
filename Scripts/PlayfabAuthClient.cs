using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using PlayFab;
using PlayFab.ClientModels;
using LoginResult = PlayFab.ClientModels.LoginResult;

public class PlayfabAuthClient : MonoBehaviour
{
    private void Start()
    {
        FB.Init(OnFacebookInitialized);
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

    private void OnFacebookInitialized()
    {
    }

    public void LoginWithFacebook()
    {
        // We invoke basic login procedure and pass in the callback to process the result
        FB.LogInWithReadPermissions(null, OnFacebookLoggedIn);
    }

    private void OnFacebookLoggedIn(ILoginResult result)
    {
        // If result has no errors, it means we have authenticated in Facebook successfully
        if (result == null || string.IsNullOrEmpty(result.Error))
        {
            // Login Success
            PlayFabClientAPI.LoginWithFacebook(new LoginWithFacebookRequest
            {
                CreateAccount = true,
                AccessToken = AccessToken.CurrentAccessToken.TokenString
            }, OnPlayfabFacebookAuthComplete, OnPlayfabFacebookAuthFailed);
        }
        else
        {
            // Login Failed
        }
    }

    public void LoginWithGooglePlay()
    {
        Social.localUser.Authenticate((bool success) => {
            if (success)
            {
                // Login Success
                var serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();
                PlayFabClientAPI.LoginWithGoogleAccount(new LoginWithGoogleAccountRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    ServerAuthCode = serverAuthCode,
                    CreateAccount = true
                }, OnPlayfabGooglePlayAuthComplete, OnPlayfabGooglePlayAuthFailed);
            }
            else
            {
                // Login Failed
            }
        });
    }

    private void OnPlayfabFacebookAuthComplete(LoginResult result)
    {

    }

    private void OnPlayfabFacebookAuthFailed(PlayFabError error)
    {

    }

    private void OnPlayfabGooglePlayAuthComplete(LoginResult result)
    {

    }

    private void OnPlayfabGooglePlayAuthFailed(PlayFabError error)
    {

    }
}
