namespace LyrAutomate.Automation;

public static class Lyra
{
    public const string PlayerController = "/LyraPlayerController_0";
    public const string PlayerLocator = "//B_Hero_ShooterMannequin_C_0";
    public const string BotsLocator = "//*[@class='B_AI_Controller_LyraShooter_C']";

    public const string WeaponSpawnerLocator = "//*[@class='B_WeaponSpawner_C']";
    public const string GrenadeLocator = "//*[@class='B_Grenade_C']";

    public const string UIRespawnTimerLocator = "//W_RespawnTimer_C_0";
    
    public const string ScenePath = "/ShooterMaps/Maps/L_Expanse";
    public const string SceneName = "UEDPIE_0_L_Expanse";

    public const string InventoryOtherMagazinesTextLocator = "//TotalCountWidget/@Text";
    public const string InventoryCurrentMagazineTextLocator = "//AmmoLeftInMagazineWidget/@Text";
    public static string InventoryQuickBarAmmoTextLocator(int slot) => $"//*[contains(@name,'W_QuickBarSlot')][{slot}]/SelectionBorder/SizeBox_274/Overlay_0/AmmoCounterHB/WeaponAmmoCount/@CurrentNumericValue";
}