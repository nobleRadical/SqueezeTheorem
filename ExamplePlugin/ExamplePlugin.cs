using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;


namespace ThmPlugin
{
	//This is an example plugin that can be put in BepInEx/plugins/ThmPlugin/ThmPlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]
	
	//This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
	
	//We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI))]
	
	//This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class ThmPlugin : BaseUnityPlugin
	{
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "nobleRadical";
        public const string PluginName = "SqueezeTheorem";
        public const string PluginVersion = "1.0.0";

		//We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;
        //for assets or some BS
        public static PluginInfo PInfo { get; private set; }
        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            //asset stuff
            PInfo = Info;

            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "SQUEEZE_NAME";
            myItemDef.nameToken = "SQUEEZE_NAME";
            myItemDef.pickupToken = "SQUEEZE_PICKUP";
            myItemDef.descriptionToken = "SQUEEZE_DESC";
            myItemDef.loreToken = "SQUEEZE_LORE";

            //The tier determines what rarity the item is:
            //Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            //and finally NoTier is generally used for helper items, like the tonic affliction
            myItemDef.tier = ItemTier.Lunar;

            Assets.Init();
            //You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks
            myItemDef.pickupModelPrefab = Assets.mainBundle.LoadAsset<GameObject>("Assets/squeezeSimpleGameObject.prefab");
            myItemDef.pickupIconSprite = Assets.mainBundle.LoadAsset<Sprite>("Assets/squeeze_theorem_simple_render.png");

            //Can remove determines if a shrine of order, or a printer can take this item, generally true, except for NoTier items.
            myItemDef.canRemove = true;

            //Hidden means that there will be no pickup notification,
            //and it won't appear in the inventory at the top of the screen.
            //This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;
			
            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            //You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            //For this example, we are omitting them, as they are quite a pain to set up without tools like ItemDisplayPlacementHelper
            var displayRules = new ItemDisplayRuleDict(null);

            //Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            //But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            RecalculateStatsAPI.GetStatCoefficients += (self, args) =>
            {
                // Avoiding null reference calls for background birds and level hazards
                // Disabled for artifact boss
                if (self.inventory != null && self.name != "ArtifactShellBody" && self.name != "ArtifactShellBody(Clone)")
                {


                    // Get the ammount of the item currently on the Character Body
                    int squeezeCount = self.inventory.GetItemCount(myItemDef.itemIndex);

                    // Update HP
                    self.PerformAutoCalculateLevelStats();
                    // Convert to armor
                    if (squeezeCount > 0)
                    {
                        self.levelArmor += self.levelMaxHealth * (float)(0.2 * squeezeCount);
                        self.levelMaxHealth = 0;
                    }
                }

            };
            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }


        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("SQUEEZE_NAME", "Squeeze Theorem");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("SQUEEZE_PICKUP", "Converts Level-up Health Bonuses to Armor.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("SQUEEZE_DESC", "Health bonuses on leveling up are removed. Instead, 20% <style=cStack>(+20% per stack)</style> of bonus health is converted to Armor.");
            
            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("SQUEEZE_LORE", @"<style=cMono>W H A T  I S  T H I S  Y O U  B R I N G  M E?</style>

Yes, I have seen these before.

You wish to know of their function. Pathetic. 
Vermin like you should not convern yourself with higher matters.
Know your place.

...

Yes? What now?

Again? Again you come to me with curiosity? Disgusting. I should kill you for that. You are fortunate to be so loyal to me.

And yet... this is from him, is it not?

It has been so long since I have designed, delved into the ratios and coefficients of the universe.

...

Very Well, Vermin. You will learn.");
        }
    }
    public static class Assets
    {
        //The mod's AssetBundle
        public static AssetBundle mainBundle;
    //A constant of the AssetBundle's name.
        public const string bundleName = "squeezebundle";
        // Not necesary, but useful if you want to store the bundle on its own folder.
        public const string assetBundleFolder = "AssetBundles";

        //The direct path to your AssetBundle
        public static string AssetBundlePath
        {
            get
            {
                //This returns the path to your assetbundle assuming said bundle is on the same folder as your DLL. If you have your bundle in a folder, you can uncomment the statement below this one.
                //return System.IO.Path.Combine(ThmPlugin.PInfo.Location, bundleName);
                return System.IO.Path.Combine(ThmPlugin.PInfo.Location, "..", assetBundleFolder, bundleName);
            }
        }

        public static void Init()
        {
            //Loads the assetBundle from the Path, and stores it in the static field.
            mainBundle = AssetBundle.LoadFromFile(AssetBundlePath);
            Log.LogInfo("SqueezeThm Assets Initialized.");
        }
    }
}
