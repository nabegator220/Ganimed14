using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Client.Light.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.CCVar;
using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Client.Player;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared.Mobs;

namespace Content.Client._Ganimed.Audio;
/// <summary>
///     Handles all sound processing that needs to be done in immersive spacing, be it muffling or muting. A lot of this shit has basically been stolen from https://github.com/Monolith-Station/Monolith/pull/2377.
/// </summary>
public sealed class ISSoundProcessingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private bool _isEnabled = true;
    public override void Initialize()
    {
        base.Initialize();

        _configurationManager.OnValueChanged(CCVars.ImmersiveSpacingTesting, x => _isEnabled = x, invokeImmediately: true);

        SubscribeLocalEvent<AudioComponent, EntParentChangedMessage>(OnAudioParentChanged);
    }
    public bool IsAudioIsCompatible(AudioComponent audioComponent)
        => !audioComponent.Global && _isEnabled;
    private void ProcessSoundPlayerInSpace(Entity<AudioComponent> entity, EntityUid? oldparent)
    {
        Vector2 zeroVector = new Vector2(0, 0);
        Vector2 soundPos = entity.Comp.Position;
        /*Logger.Error(soundPos.ToString());
        Logger.Error(oldparent?.ToString() ?? "null");
        Logger.Error(entity.ToString());*/
        /*if (!oldparent.HasValue && soundPos == zeroVector) //evil nigger hack? maybe it will work
        {
            Logger.Error("gain setting 2");
            _audio.SetGain(entity, 0.1f);
        }*/
        if (soundPos.Length() > 1)
        {
            //Logger.Error("gain setting 3");
            _audio.SetGain(entity, 0.1f);
        }
    }
    private void LogAudioComponent(AudioComponent comp)
    {
        if (comp == null)
        {
            Logger.Info("AudioComponent is null");
            return;
        }

        var included = comp.IncludedEntities != null
            ? string.Join(", ", comp.IncludedEntities)
            : "null";

        var excluded = comp.ExcludedEntity?.ToString() ?? "null";

        Logger.Info($"""
        --- AudioComponent ---
        FileName: {comp.FileName}
        Flags: {comp.Flags}
        AudioStart: {comp.AudioStart}
        ExcludedEntity: {excluded}
        IncludedEntities: {included}
        State: {comp.State}
        Global: {comp.Global}
        Volume: {comp.Volume}
        Gain: {comp.Gain}
        Pitch: {comp.Pitch}
        MaxDistance: {comp.MaxDistance}
        RolloffFactor: {comp.RolloffFactor}
        ReferenceDistance: {comp.ReferenceDistance}
        Occlusion: {comp.Occlusion}
        PlaybackPosition: {comp.PlaybackPosition}
        Position: {comp.Position}
        Velocity: {comp.Velocity}
        ----------------------
        """);
    }
    private void OnAudioParentChanged(Entity<AudioComponent> entity, ref EntParentChangedMessage args)
    {
        if (!IsAudioIsCompatible(entity.Comp)) //check for system functionality, self explanatory
            return;

        /*string niggers = args.Transform.GridUid.ToString() ?? "no map youre in nullspace nigga";
        Logger.Error(niggers);*/

        //gets a bunch of info about the grids the player and sound are both on. trying to do this in ways where the return cascade saves as much perf as possible (read 2 cpu cycles)
        var player = _playerManager.LocalEntity;
        if (!player.HasValue)
            return;
        var playerGrid = Transform(player!.Value).GridUid;

        /*if (!playerGrid)
            ProcessSoundPlayerInSpace(entity, args.OldParent);*/

        var soundGrid = Transform(entity).GridUid;

        if (playerGrid != soundGrid) //no sound applied when player is on a different grid. MALF
        {
            LogAudioComponent(entity.Comp);
            _audio.SetGain(entity, 0.1f);
            return;
        }
    }
}
