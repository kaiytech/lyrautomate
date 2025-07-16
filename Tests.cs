using FluentAssertions;
using gdio.unreal_api;
using gdio.common.objects;
using LyrAutomate.Automation;
using LyrAutomate.Extensions;

namespace LyrAutomate;


public class Tests
{
    private ApiClient _api;
    
    [OneTimeSetUp]
    public void Setup()
    {
        _api = new ApiClient();
        _api.Connect("localhost");
        try
        {
            _api.StopEditorPlay();
            _api.Wait(1000); // giving it some time to exit play mode jic
        } catch {} //if we're not in play mode, StopEditorPlay will throw an exception.
        
        _api.StartEditorPlay();
        
        _api.LoadLevel(Lyra.ScenePath);
        Wait.Until(() => _api.GetSceneName(), "Loaded Scene").Is(Lyra.SceneName, 30);
        _api.WaitForObject(Lyra.BotsLocator);
        var bots = _api.GetObjectList(Lyra.BotsLocator);
        TestContext.Out.WriteLine($"Found {bots?.Count ?? 0} bots {(bots is not null && bots.Count > 0 ? ". Deleting bots..." : "")}");
        for (var i = 0; i < (bots?.Count ?? 0); i++)
            _api.ConsoleCommand("RemovePlayerBot");
        _api.Wait(2000); // hacky, wait for player spawn
    }

    #region PlayerMovement
    [Test, Order(1)]
    public void PlayerMovement()
    {
        var startingPlayerPos = new Vector3(-4000, 0, -369);
        
        _api.SetObjectPosition(Lyra.PlayerLocator, startingPlayerPos);
        _api.SetObjectRotation(Lyra.PlayerLocator, new Vector3(0, 0, 1));
        
        startingPlayerPos = _api.GetObjectPosition(Lyra.PlayerLocator);
        
        foreach (var direction in new List<(string, Vector3)>
                 {
                     new("left", new Vector3(0, 300, 0)),
                     new("right", new Vector3(0, -300, 0)),
                     new("backwards", new Vector3(300, 0, 0)),
                     new("forwards", new Vector3(-300, 0, 0))
                 })
        {
            try
            {
                TestContext.Out.WriteLine($"Moving {direction.Item1}...");
                _api.SetObjectPosition(Lyra.PlayerLocator, startingPlayerPos);
                var targetPlayerPos = startingPlayerPos.Add(direction.Item2);
                _api.NavAgentMoveToPoint(Lyra.PlayerLocator, targetPlayerPos, true);
                Wait.Until(() => _api.GetObjectPosition(Lyra.PlayerLocator).InRangeOf(targetPlayerPos, 50),
                    "Player Position Changed").Is(true, 10);
                TestContext.Out.WriteLine($"Successfully moved {direction.Item1}!");
            }
            catch (Exception e)
            {
                TestContext.Out.WriteLine($"Failed to move {direction.Item1}: {e}");
            }
        }
    }
    #endregion

    #region WeaponFunctional

    [Test, Order(3)]
    public void WeaponsFunctional()
    {
        _api.SetObjectPosition(Lyra.PlayerLocator, new Vector3(-4000, 0, -369));
        var inventory = GetInventory();
        inventory.CurrentMagazine.Should().BeGreaterThan(0);
        inventory.OtherMagazines.Should().BeGreaterThan(0);
        inventory.Slot1Ammo.Should().BeGreaterThan(0);
        (inventory.CurrentMagazine + inventory.OtherMagazines).Should().Be(inventory.Slot1Ammo);
        
        var currentInventory = inventory;
        _api.Click(MouseButtons.LEFT, 200, 200, 100);
        TestContext.Out.WriteLine($"Attempting to shoot...");
        Wait.Until((() =>
        {
            currentInventory = GetInventory();
            return currentInventory.CurrentMagazine == inventory.CurrentMagazine - 1;
        }), "Ammo decreased").Is(true, 10);
        
        currentInventory.CurrentMagazine.Should().Be(inventory.CurrentMagazine - 1);
        currentInventory.OtherMagazines.Should().Be(inventory.OtherMagazines);
        currentInventory.Slot1Ammo.Should().Be(inventory.Slot1Ammo - 1);
        
        TestContext.Out.WriteLine($"Attempting to reload...");
        Wait.Until((() =>
        {
            if (currentInventory.CurrentMagazine == inventory.CurrentMagazine)
                return true;
            _api.KeyPress(new[] { KeyCode.R }, 100);
            currentInventory = GetInventory();
            return false;
        }), "Ammo decreased").Is(true, 10);
        
        currentInventory.CurrentMagazine.Should().Be(inventory.CurrentMagazine);
        currentInventory.OtherMagazines.Should().Be(inventory.OtherMagazines - 1);
        currentInventory.Slot1Ammo.Should().Be(inventory.Slot1Ammo - 1);

        var weaponSpawners = _api.GetObjectList(Lyra.WeaponSpawnerLocator);
        weaponSpawners.Should().NotBeNull();
        TestContext.Out.WriteLine($"Found {weaponSpawners.Count} weapon spawners. Teleporting to them all.");
        foreach (var spawner in weaponSpawners)
        {
            TestContext.Out.WriteLine($"Teleporting player to {spawner.Position}...");
            _api.SetObjectPosition(Lyra.PlayerLocator, spawner.Position);
        }

        TestContext.Out.WriteLine("Teleporting player back to spawn.");
        _api.SetObjectPosition(Lyra.PlayerLocator, new Vector3(-4000, 0, -369));

        currentInventory = GetInventory();
        currentInventory.Slot1Ammo.Should().BeGreaterThan(0).And.BeLessThan(999);
        currentInventory.Slot2Ammo.Should().BeGreaterThan(0).And.BeLessThan(999);
        currentInventory.Slot3Ammo.Should().BeGreaterThan(0).And.BeLessThan(999);
        
        TestContext.Out.WriteLine("Attempting to throw a grenade...");
        _api.KeyPress([KeyCode.Q], 100);
        _api.WaitForObject(Lyra.GrenadeLocator);
        _api.Wait(1500); //hacky. wait a bit for the grenade to land somewhere
        var grenadePos = _api.GetObjectPosition(Lyra.GrenadeLocator);
        TestContext.Out.WriteLine("Teleporting player to grenade.");
        _api.SetObjectPosition(Lyra.PlayerLocator, grenadePos);
        Wait.Until(() => _api.CallMethod<bool>(Lyra.UIRespawnTimerLocator, "IsRendered", []),
            "Respawn Timer Visible").Is(true);
        Wait.Until(() => _api.CallMethod<bool>(Lyra.UIRespawnTimerLocator, "IsRendered", []),
            "Respawn Timer Visible").Is(false);
    }

    public PlayerInventory GetInventory()
    {
        var otherMagazines = _api.GetObjectFieldValue<string>(Lyra.InventoryOtherMagazinesTextLocator);
        var currentMagazine = _api.GetObjectFieldValue<string>(Lyra.InventoryCurrentMagazineTextLocator);
        
        var slot1 = _api.GetObjectFieldValue<float>(Lyra.InventoryQuickBarAmmoTextLocator(0));
        var slot2 = _api.GetObjectFieldValue<float>(Lyra.InventoryQuickBarAmmoTextLocator(1));
        var slot3 = _api.GetObjectFieldValue<float>(Lyra.InventoryQuickBarAmmoTextLocator(2));

        return new PlayerInventory()
        {
            // we assume these values are actually convertible to int.
            CurrentMagazine = Convert.ToInt32(currentMagazine),
            OtherMagazines = Convert.ToInt32(otherMagazines),
            Slot1Ammo = Convert.ToInt32(slot1) == 999 ? null : Convert.ToInt32(slot1),
            Slot2Ammo = Convert.ToInt32(slot2) == 999 ? null : Convert.ToInt32(slot2),
            Slot3Ammo = Convert.ToInt32(slot3) == 999 ? null : Convert.ToInt32(slot3)
        };
    }

    public struct PlayerInventory
    {
        public required int CurrentMagazine;
        public required int OtherMagazines;
        public required int? Slot1Ammo;
        public required int? Slot2Ammo;
        public required int? Slot3Ammo;
    }
    
    #endregion

    #region AimAtEnemy
    
    [Test, Order(2)]
    public void AimAtEnemy()
    {
        TestContext.Out.WriteLine("Adding a bot...");
        _api.ConsoleCommand("AddPlayerBot");
        _api.Wait(1000);
        TestContext.Out.WriteLine("Teleporting bot and player to each other.");
        _api.SetObjectPosition(Lyra.PlayerLocator, new Vector3(0, 0, -750));
        _api.SetObjectPosition("//B_Hero_ShooterMannequin_C_4", new Vector3(600, -600, -750));
        _api.Wait(1000);
        TestContext.Out.WriteLine("Aiming at the bot.");
        _api.CallMethod(Lyra.PlayerController, "AimAtActorTemp", [Convert.ToSingle(1f)]);
        _api.GetObjectFieldValue<bool>(Lyra.PlayerController, "IsAiming").Should().BeTrue();
        Wait.Until(() => _api.GetObjectFieldValue<bool>(Lyra.PlayerController, "IsAiming"), "Is aiming").Is(false);
        TestContext.Out.WriteLine("Done! Removing the enemy bot.");
        _api.ConsoleCommand("RemovePlayerBot");
    }
    
    #endregion
    
    [OneTimeTearDown]
    public void TearDown()
    {
        _api.Disconnect();
    }
}