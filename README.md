# Lyra Automation - Learning experience

# Table of contents
1. Introduction
2. Automated Tests project
3. -Tests
4. --Setup
5. --Movement test
6. --Weapon test
7. --Aim test
8. --Other classes
9. ---Wait
10. ---Vector3Extensions
11. Lyra code additions
12. -LyraPlayerController.h
13. -LyraPlayerController.cpp

# Introduction
This is a Test Automation project guided at learning on how to use [GameDriver](https://www.gamedriver.io/) with UnrealEngine 5. I have a few years of experience with AltTester and Unity, so giving this task a go should not be too hard. The code included in this repository represents only the automation project, but all changes to the game are listed below. This is based on the [UE5 Lyra example game](https://dev.epicgames.com/documentation/en-us/unreal-engine/lyra-sample-game-in-unreal-engine).

# Automated tests project
The Automated Tests project (LyrAutomate) consists of a few files:
- `Tests.cs` - all unit tests are defined here
- `Automation/Lyra.cs` - static locators for Lyra game objects
- `Automation/Wait.cs` - custom helper function to synchronously await on conditions
- `Extensions/Vector3Extensions.cs` - two handy Vector3 extensions

## Tests
All tests are defined in a `Tests.cs` file, and they are designed using NUnit. In some places a library FluentAssertions is used, to make the code look a bit nicer.

### Setup
There is a one-time setup for these tests. It stops the editor play mode (if possible), then starts it again, loads a specific level, kicks all bots and waits for 2 seconds (just in case). Because GameDriver doesn't let you check whether we're in the editor mode (and because GameDriver lacks proper exception handling, more on that later), I had to wrap it inside a dummy try/catch block. My approach loads the game normally (as if you loaded it yourself) and then deletes the bots. A good approach would also be to have our own gamemode that inherits from this one, and make it NOT spawn bots at all, but I opted for this approach to avoid modifying the game code too much.

### Movement test
This simple test is designed to test the character movement. The test teleports the player to a pre-set position and performs a movement in 4 different directions. These directions are labeled as 'left', 'right', 'backwards' and 'forwards', although the starting rotation of the player may have an impact if these labels correspond to actual directions (as they are pre-set coordinates). For each direction, the test awaits until the player reaches the desired coordinate (within a `50u` acceptable range)

```gherkin
Feature: Player Movement

Scenario: Player can move in all four directions
  Given the player is in the game world
  When the player moves <direction>
  Then the player's position should change
```

Example automated test output:
```
Moving left...
Waiting for Player Position Changed to be True (10s timeout)...
After waiting for 0.59058s, Player Position Changed is True.
Successfully moved left!
Moving right...
Waiting for Player Position Changed to be True (10s timeout)...
After waiting for 0.5836887s, Player Position Changed is True.
Successfully moved right!
Moving backwards...
Waiting for Player Position Changed to be True (10s timeout)...
After waiting for 0.5629081s, Player Position Changed is True.
Successfully moved backwards!
Moving forwards...
Waiting for Player Position Changed to be True (10s timeout)...
After waiting for 0.593584s, Player Position Changed is True.
Successfully moved forwards!
```


### Weapon test
This automated test is designed to test basic weapon features. Simulating input has been challenging, but apparently focusing the editor seems to work in most cases - in this case GameDriver seems very unreliable, but I'm happy to have taken a screen recording of the test working as expected. I had to loop the reload action until the result is achieved, because the game did not always record the key press. For the purpose of this test, I have created a PlayerInventory struct that returns the entire player's inventory. In some cases this could potentially slow down tests (because the amount of requests would increase, if we only cared about one value), but in my case it was a nice trade off for an easier to maintain code. 


```gherkin
Feature: Combat and Respawn Flow

Scenario: Player can shoot, reload, collect weapons, throw grenade, and respawn
  Given the player has a pistol with ammo
  When the player shoots
  Then the ammo count should decrease

  When the player reloads
  Then the magazine ammo count should increase to its full capacity

  When the player interacts with all weapon spawners
  Then the player should collect all available weapons

  When the player throws a grenade
  And the player is killed by the grenade
  Then the respawn timer should become visible
  And after it disappears, the player should respawn
```

This test contains one quirk: we wait for 1.5s for the grenade to land before teleporting the player to it. That *should* be enough time for the grenade to land. In an ideal world I would wait for the grenade to land by awaiting its position (see if it stopped changing between requests. Also limit the awaiter by the grenade explosion time), but GameDriver makes the game stutter when performing frequent requests, so I opted for this hack instead.

Example automated test output:

```
Attempting to shoot...
Waiting for Ammo decreased to be True (10s timeout)...
After waiting for 0.1897732s, Ammo decreased is True.
Attempting to reload...
Waiting for Ammo decreased to be True (10s timeout)...
After waiting for 2.1034448s, Ammo decreased is True.
Found 10 weapon spawners. Teleporting to them all.
Teleporting player to (3377.353, -1830, -380.59402)...
Teleporting player to (4586.512, -1008.41815, -380.41135)...
Teleporting player to (2330.2454, -903.5327, -480.1958)...
Teleporting player to (-3377.353, 1830, -380.59402)...
Teleporting player to (-2258.2454, 916.5327, -480.1958)...
Teleporting player to (-4586.512, 1008.41815, -380.41135)...
Teleporting player to (1472, -6.75, -1218.1145)...
Teleporting player to (-1472, -8.308535E-07, -1218.1145)...
Teleporting player to (0, -1623.652, 83.333786)...
Teleporting player to (12.109891, 1634.6387, 83.333786)...
Teleporting player back to spawn.
Attempting to throw a grenade...
Teleporting player to grenade.
Waiting for Respawn Timer Visible to be True (30s timeout)...
After waiting for 0.4411759s, Respawn Timer Visible is True.
Waiting for Respawn Timer Visible to be False (30s timeout)...
After waiting for 5.4055514s, Respawn Timer Visible is False.
```

### Aim test
This test is designed to showcase the aim helper method implemented (by me) in the game. The method takes over the input system and moves the camera towards the target (more on that later).
However, GameDriver doesn't let me push pointers to game objects with requests, so despite a proper method existing, I had to create a temporary helper method to workaround this issue and showcase "what would have been". So again, in an ideal world, this line:

```
_api.CallMethod(Lyra.PlayerController, "AimAtActorTemp", [Convert.ToSingle(1f)]);
```

would be replaced with:

```
_api.CallMethod(Lyra.PlayerController, "AimAtActor", [_api.GetObjectList(Lyra.EnemyLocator).Last(), Convert.ToSingle(1f)]);
```
But calling the latter throws an access violation creash in GameDriver's code. I believe it's a bug, so I opted for this temporary workaround.

Either way: after the target is centered, a boolean on the game side shifts, so we can easily detect when aiming is complete. About the ideal world again: I would love to check for a nullptr of the aim target instead, but I couldn't make it happen in GameDriver.

```gherkin
Feature: Aiming at Enemy Bot

Scenario: Player aims at a spawned bot
  Given a bot is added to the game
  And the bot and player are teleported to the same location
  When the player aims at the bot
  And the aiming is complete
  Then the bot is removed from the game
```

Example automated test output:
```
Adding a bot...
Teleporting bot and player to each other.
Aiming at the bot.
Waiting for Is aiming to be False (30s timeout)...
After waiting for 0.1722259s, Is aiming is False.
Done! Removing the enemy bot.
```

## Other classes
### Wait
This simple Wait class is here to help with synchronous awaiting for certain values to be as expected. Usage:
```cs
Wait.Until(() => <T>SomeMethod(), "Some Method").Is(<T>result, timeoutSeconds: 120);
```
It repeatedly performs `SomeMethod()` until its result is not `result`. After a timeout of `120` seconds is hit and the condition is not met, an exception is thrown.
I added a 100ms sleep in between checks, just to not overwhelm GameDriver too much. Usually this sleep wouldn't be there, or would be customisable.
The reason I like the `Wait` approach is because this eliminates the unnecessary hardcoded sleeps that could break the test if the game for some reason takes a bit longer to perform some action. We can also set timeouts. If something happens eventually after 120 seconds (when it should take only 10), it's still a valid reason to fail an automated test. Also, awaiting stuff often makes automated tests happen way quicker. 


### Vector3Extensions
Because the `gdio.common.objects`'s `Vector3` is only a very basic implementation, I had to add two helper methods: `Add` that lets you add or substract vectors from each other, and a `InRangeOf` method that compares two vectors with a specified acceptable range. I would love to add custom operators instead (so I could do `vector*vector`) but C# does not support this with class extensions. This could've been done by creating a new custom Vector3 class though (with custom casts to the native Vector3), but that would complicate code a bit too much.


# Lyra code additions
## LyraPlayerController.h
ALyraPlayerController:
```cpp
private:
  UFUNCTION()
  void AimAtActorProgressive();

  UPROPERTY()
  AActor* TargetActor = nullptr;

  UPROPERTY()
  float AimScale;

  UPROPERTY()
  bool IsAiming;

public:
  UFUNCTION()
  void AimAtActor(AActor* actor, float Scale);

  UFUNCTION()
  void AimAtActorTemp(float scale);
```
- `AimAtActorProgressive()` utilizes `TargetActor`, `AimScale` and `IsAiming` and is intended to be used internally inside a `Tick`.
- `AimAtActor` is the function that triggers the custom aim functionality
- `AimAtActorTemp` is the temporary function that works as a workaround to a misbehaving GameDriver (more on that above)

## LyraPlayerController.cpp
```cpp
ALyraPlayerController::ALyraPlayerController(const FObjectInitializer& ObjectInitializer)
{
	// [...]
	AimScale = 100;
	IsAiming = false;
	// [...]
}

// [...]

void ALyraPlayerController::PlayerTick(float DeltaTime)
{
  // [...]
  if (TargetActor)
  {
	  AimAtActorProgressive();
	  this->IsAiming = true;
  }
  else
  {
	  this->IsAiming = false;
  }
}

void ALyraPlayerController::AimAtActor(AActor* targetActor, float scale)
{
	this->TargetActor = targetActor;
	this->AimScale = scale;
}

void ALyraPlayerController::AimAtActorTemp(float scale)
{
	TArray<AActor*> FoundActors;


	AActor* LastMatch = nullptr;

	for (TActorIterator<AActor> It(GetWorld()); It; ++It)
	{
		if (It->GetName().Contains(TEXT("B_Hero_ShooterMannequin_C")))
		{
			LastMatch = *It;
		}
	}

	if (LastMatch)
	{
		this->AimAtActor(LastMatch, scale);
		this->IsAiming = true;
	}
}

void ALyraPlayerController::Tick(float DeltaSeconds)
{

	Super::Tick(DeltaSeconds);

	if (TargetActor)
	{
		AimAtActorProgressive();
		this->IsAiming = true;
	}
	else
	{
		this->IsAiming = false;
	}
}

void ALyraPlayerController::AimAtActorProgressive()
{
	if (!TargetActor) return;

	FVector CamLoc = PlayerCameraManager->GetCameraLocation();
	FRotator DesiredRot = (TargetActor->GetActorLocation() - CamLoc).Rotation();
	FRotator DeltaRot = (DesiredRot - GetControlRotation()).GetNormalized();

	if (DeltaRot.IsNearlyZero(0.05f))
	{
		TargetActor = nullptr;
		return;
	}


	FVector2D InputVec(DeltaRot.Yaw / AimScale, DeltaRot.Pitch / AimScale);

	FRotator NewControlRot = GetControlRotation();
	NewControlRot.Yaw += InputVec.X;
	NewControlRot.Pitch += InputVec.Y;
	SetControlRotation(NewControlRot);
}
```

