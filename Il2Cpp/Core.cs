using Il2CppScheduleOne.Persistence;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.Events;

[assembly: MelonInfo(typeof(BetterCounterOffer.CounterOfferUI), BetterCounterOffer.BuildInfo.Name, BetterCounterOffer.BuildInfo.Version, BetterCounterOffer.BuildInfo.Author, BetterCounterOffer.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterCounterOffer
{
    public static class BuildInfo
    {
        public const string Name = "Better Counter Offer UI";
        public const string Description = "A mod that improves the Counter Offer UI";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "3.3.1";
        public const string DownloadLink = "";
    }

    public class Core : MelonMod
	{
        public override void OnInitializeMelon()
        {
            Utility.Log("Initializing Better Counter Offer...Go Make that Money");
            Utility.Log("If you find any bugs,");
            Utility.Log("first check that your game is updated and on the main branch,");
            Utility.Log("Then if the bug persists message me on nexus");
            Utility.Log("- OverweightUnicorn\n");
            CounterOfferConfig.LoadConfig();
        }

        public override void OnLateInitializeMelon()
        {
            LoadManager.Instance.onLoadComplete.AddListener((UnityAction)CounterOfferUI.InitOnWake);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName.ToLower() == "main")
            {
                CounterOfferUI.labelCount = 0;
                CounterOfferUI.selectorTabControl = null;
            }
        }
    }
}