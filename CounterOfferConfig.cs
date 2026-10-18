using MelonLoader;

namespace BetterCounterOffer
{
    public static class CounterOfferConfig
    {

        public static MelonPreferences_Category Category { get; private set; }

        public static MelonPreferences_Entry<bool> DisableAllLabels { get; private set; }
        public static MelonPreferences_Entry<bool> DisableSuccessRate { get; private set; }
        public static MelonPreferences_Entry<bool> DisableMaxLimit { get; private set; }
        public static MelonPreferences_Entry<bool> DisableInitialOffer { get; private set; }
        public static MelonPreferences_Entry<bool> EnablePricePerUnit { get; private set; }

        public static void Initialize()
        {
            Category = MelonPreferences.CreateCategory(BuildInfo.Name);

            DisableAllLabels = Category.CreateEntry("DisableAllLabels", false, "Disable All Labels");
            DisableSuccessRate = Category.CreateEntry("DisableSuccessRate", false, "Disable Success Rate");
            DisableMaxLimit = Category.CreateEntry("DisableMaxLimit", false, "Disable Max Limit");
            DisableInitialOffer = Category.CreateEntry("DisableInitialOffer", false, "Disable Initial Offer");
            EnablePricePerUnit = Category.CreateEntry("EnablePricePerUnit", false, "Enable Price Per Unit");
        }
    }
}