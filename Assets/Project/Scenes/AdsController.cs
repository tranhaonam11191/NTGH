using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdsController : MonoBehaviour
{

    public static AdsController instance;

    private void Awake()
    {
        instance = this;
    }
    private RewardedAd rewardedAd;
    private BannerView bannerView;

#if TEST
    string adReward = "ca-app-pub-3940256099942544/5224354917";
    string adOpenApp = "ca-app-pub-3940256099942544/3419835294";
    string adBanner = "ca-app-pub-3940256099942544/6300978111";
#else
        string adReward;
        string adOpenApp;
        string adBanner;
#endif


    [SerializeField] string adRewardAndroid, adRewardIOS, adOpenAppAndroid, adOpenAppIOS, adBannerAndroid, adBannerIOS;

    public void Start()
    {
        Debug.Log("AdsManager");

        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !TEST
            adReward = adRewardAndroid;
            adOpenApp = adOpenAppAndroid;
            adBanner = adBannerAndroid;
#elif UNITY_IOS && !TEST
            adReward = adRewardIOS;
            adOpenApp = adOpenAppIOS;
            adBanner = adBannerIOS;
#endif

        // Initialize the Mobile Ads SDK.
        MobileAds.Initialize((initStatus) =>
        {
            Debug.Log("init mobile ads complete: " + initStatus);
        });

        CreateAndLoadRewardedAd();

        //RequestBanner();
    }

    #region BANNER
    public void RequestBanner()
    {
        // Create a 320x50 banner at the top of the screen.
        this.bannerView = new BannerView(adBanner, AdSize.Banner, AdPosition.Bottom);
        AdRequest request = new AdRequest.Builder().Build();

        // Called when an ad request has successfully loaded.
        this.bannerView.OnAdLoaded += this.HandleOnAdLoaded;
        // Called when an ad request failed to load.
        this.bannerView.OnAdFailedToLoad += this.HandleOnAdFailedToLoad;
        // Called when an ad is clicked.
        this.bannerView.OnAdOpening += this.HandleOnAdOpened;
        // Called when the user returned from the app after an ad click.
        this.bannerView.OnAdClosed += this.HandleOnAdClosed;
        // Load the banner with the request.
        this.bannerView.LoadAd(request);
    }

    public void HandleOnAdLoaded(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleAdLoaded event received");
    }

    public void HandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        MonoBehaviour.print("HandleFailedToReceiveAd event received with message: ");
    }

    public void HandleOnAdOpened(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleAdOpened event received");
    }

    public void HandleOnAdClosed(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleAdClosed event received");
    }

    #endregion

    #region REWARD
    public void CreateAndLoadRewardedAd()
    {

        this.rewardedAd = new RewardedAd(adReward);

        // Called when an ad request has successfully loaded.
        this.rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        // Called when an ad request failed to load.
        this.rewardedAd.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        // Called when an ad is shown.
        this.rewardedAd.OnAdOpening += HandleRewardedAdOpening;
        // Called when an ad request failed to show.
        this.rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        // Called when the user should be rewarded for interacting with the ad.
        this.rewardedAd.OnUserEarnedReward += HandleUserEarnedReward;
        // Called when the ad is closed.
        this.rewardedAd.OnAdClosed += HandleRewardedAdClosed;

        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the rewarded ad with the request.
        this.rewardedAd.LoadAd(request);
    }

    public void HandleRewardedAdLoaded(object sender, EventArgs args)
    {
        Debug.Log("HandleRewardedAdLoaded event received");
    }

    public void HandleRewardedAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("HandleRewardedAdFailedToLoad event received with message: " + args.LoadAdError.GetMessage());
    }

    public void HandleRewardedAdOpening(object sender, EventArgs args)
    {
        Debug.Log("HandleRewardedAdOpening event received");
    }

    public void HandleRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
    {
        Debug.Log(
            "HandleRewardedAdFailedToShow event received with message: "
                             + args.Message);
    }

    public void HandleRewardedAdClosed(object sender, EventArgs args)
    {
        Debug.Log("HandleRewardedAdClosed event received");
        this.CreateAndLoadRewardedAd();
    }

    public void HandleUserEarnedReward(object sender, Reward args)
    {
        //string type = args.Type;
        //double amount = args.Amount;
        //Debug.Log(
        //   "HandleRewardedAdRewarded event received for "
        //                + amount.ToString() + " " + type);
        if (this.callback != null) callback();
    }

    public bool checkReward() => this.rewardedAd.IsLoaded();

    callback callback;

    public void UserChoseToWatchAd(callback callback)
    {
        if (this.rewardedAd.IsLoaded())
        {
            this.rewardedAd.Show();
            this.callback = callback;
        }
    }

    #endregion
}
