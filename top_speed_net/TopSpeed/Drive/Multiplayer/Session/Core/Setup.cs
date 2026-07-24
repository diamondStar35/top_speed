using System;
using TopSpeed.Drive.Session;
using TopSpeed.Protocol;
using CommentarySubsystem = TopSpeed.Drive.Multiplayer.Session.Systems.Commentary;
using CoreRequestsSubsystem = TopSpeed.Drive.Session.Systems.CoreRequests;
using GeneralRequestsSubsystem = TopSpeed.Drive.Session.Systems.GeneralRequests;
using ProgressSubsystem = TopSpeed.Drive.Multiplayer.Session.Systems.Progress;
using SessionCommand = TopSpeed.Drive.Session.Command;
using SessionRuntime = TopSpeed.Drive.Session.Session;
using SyncSubsystem = TopSpeed.Drive.Multiplayer.Session.Systems.Sync;
using VehicleSubsystem = TopSpeed.Drive.Multiplayer.Session.Systems.Vehicle;
using ExitSubsystem = TopSpeed.Drive.Session.Systems.Exit;
using PanelsSubsystem = TopSpeed.Drive.Session.Systems.Panels;
using PlayerInfoSubsystem = TopSpeed.Drive.Session.Systems.PlayerInfo;
using PitStopSubsystem = TopSpeed.Drive.Session.Systems.PitStop;
using TrackAudioService = TopSpeed.Drive.Session.Systems.TrackAudio;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private sealed class SubsystemSet
        {
            public SubsystemSet(
                TrackAudioService trackAudio,
                PanelsSubsystem panels,
                CoreRequestsSubsystem coreRequests,
                GeneralRequestsSubsystem generalRequests,
                VehicleSubsystem vehicle,
                ProgressSubsystem progress,
                SyncSubsystem sync,
                CommentarySubsystem commentary,
                PlayerInfoSubsystem playerInfo,
                ExitSubsystem exit,
                PitStopSubsystem pitStop)
            {
                TrackAudio = trackAudio;
                Panels = panels;
                CoreRequests = coreRequests;
                GeneralRequests = generalRequests;
                Vehicle = vehicle;
                Progress = progress;
                Sync = sync;
                Commentary = commentary;
                PlayerInfo = playerInfo;
                Exit = exit;
                PitStop = pitStop;
            }

            public TrackAudioService TrackAudio { get; }
            public PanelsSubsystem Panels { get; }
            public CoreRequestsSubsystem CoreRequests { get; }
            public GeneralRequestsSubsystem GeneralRequests { get; }
            public VehicleSubsystem Vehicle { get; }
            public ProgressSubsystem Progress { get; }
            public SyncSubsystem Sync { get; }
            public CommentarySubsystem Commentary { get; }
            public PlayerInfoSubsystem PlayerInfo { get; }
            public ExitSubsystem Exit { get; }
            public PitStopSubsystem PitStop { get; }
        }

        private SubsystemSet CreateSubsystems()
        {
            var trackAudio = new TrackAudioService(
                _settings,
                GetRandomSoundBySlot,
                _soundTurnEndDing,
                QueueTrackInfoSound,
                (sessionEvent, delay) => _session!.QueueEvent(sessionEvent, delay));

            var pitStop = new PitStopSubsystem(
                "pitStop",
                135,
                _input,
                _car,
                _track,
                () => _started,
                () => _finished,
                _soundLetsPit,
                _soundRightTires,
                _soundLeftTires,
                _soundFuelingUp,
                _soundExitPitRoad,
                () => _currentState = Protocol.PlayerState.Pitting,
                () => _currentState = Protocol.PlayerState.Racing,
                SpeakText,
                s => QueueSound(s),
                (s, d) => _session!.QueueEvent(new Event(Events.PlaySound, s), d),
                road => trackAudio.AnnounceUpcomingCurve(road));

            return new SubsystemSet(
                trackAudio,
                new PanelsSubsystem("panels", 110, _input, _panelManager, _radioPanel, SpeakText),
                new CoreRequestsSubsystem(
                    "coreRequests",
                    105,
                    _input,
                    _settings,
                    _car,
                    _track,
                    () => _started,
                    () => _lap,
                    () => _lapLimit,
                    () => _lap <= _lapLimit ? _session!.Context.ProgressMilliseconds : _raceTime,
                    SpeakText,
                    () => !_hostPaused,
                    () => pitStop.IsActive),
                new GeneralRequestsSubsystem(
                    "generalRequests",
                    106,
                    _input,
                    _car,
                    () => _started,
                    () => _lap,
                    () => _lapLimit,
                    () => _session!.ApplyCommand(new SessionCommand(Commands.RequestPause))),
                new VehicleSubsystem(
                    "vehicle",
                    120,
                    _audio,
                    _car,
                    _input,
                    _track,
                    _settings,
                    trackAudio,
                    _localRadio,
                    _remotePlayers,
                    () => _currentRoad,
                    road => _currentRoad = road,
                    () => _started,
                    () => _finished,
                    () => _hostPaused,
                    GetSpatialTrackLength,
                    TrackLocalCrashState,
                    SpeakText,
                    skipCrashEval: () => pitStop.IsActive,
                    getListenerXOverride: () => pitStop.ListenerXOverride),
                new ProgressSubsystem(
                    "progress",
                    130,
                    _track,
                    _car,
                    _settings,
                    _remotePlayers,
                    _lapLimit,
                    GetLapSound,
                    LocalPlayerNumber,
                    () => _lap,
                    lap => _lap = lap,
                    () => _session!.Context.ProgressMilliseconds,
                    () => _lastCarState,
                    state => _lastCarState = state,
                    ApplyPlayerFinishState,
                    () => _hostPaused,
                    () => _sentFinish,
                    () => _sentFinish = true,
                    state => _currentState = state,
                    position => _position = position,
                    playerNumber =>
                    {
                        var positionFinish = _positionFinish;
                        AnnounceFinishOrder(playerNumber, ref positionFinish);
                        _positionFinish = positionFinish;
                    },
                    SpeakRaceInfoIfLoaded,
                    sendStarted => SendPlayerState(sendStarted),
                    SendCrash),
                new SyncSubsystem(
                    "sync",
                    140,
                    _liveTx,
                    _remoteLiveStates,
                    _remotePlayers,
                    _expiredLivePlayers,
                    () => _hostPaused,
                    () => _serverStopReceived,
                    () => _sendFailureAnnounced,
                    value => _sendFailureAnnounced = value,
                    () => _liveFailureAnnounced,
                    value => _liveFailureAnnounced = value,
                    () => _car.PositionX,
                    () => _car.PositionY,
                    ApplyBufferedRaceSnapshots,
                    SendPlayerData,
                    SpeakText),
                new CommentarySubsystem(
                    "commentary",
                    210,
                    _settings,
                    _input,
                    _car,
                    _remotePlayers,
                    GetPositionSoundByIndex,
                    GetPlayerNumberSoundByIndex,
                    GetRandomSoundBySlot,
                    () => _started,
                    () => _lap,
                    () => _lapLimit,
                    () => _positionComment,
                    value => _positionComment = value,
                    SpeakIfLoaded,
                    Speak),
                new PlayerInfoSubsystem(
                    "playerInfo",
                    220,
                    _input,
                    () => MaxPlayers - 1,
                    HasPlayerInRace,
                    GetPlayerNameForPlayer,
                    GetVehicleNameForPlayer,
                    () => _started,
                    SpeakText,
                    CalculatePlayerPerc,
                    HandleLocalPlayerNumberRequest,
                    () => _settings.BriefStatusReports,
                    getPlayerPosition: CalculatePlayerPosition,
                    track: _track,
                    getLapLimit: () => _lapLimit,
                    reportLapAndTurn: () => _settings.ReportLapAndTurn,
                    isPlayerFinished: CalculatePlayerFinished),
                new ExitSubsystem(
                    "exit",
                    300,
                    UpdateExitWhenQueueIdle,
                    () => _session!.ApplyCommand(new SessionCommand(Commands.RequestExit))),
                pitStop);
        }

        private SessionRuntime CreateSession()
        {
            var builder = new SessionBuilder(CreatePolicy());
            builder.AddSubsystems(_panels, _coreRequests, _generalRequests, _vehicle, _progress, _sync, _commentary, _playerInfo, _exit, _pitStop);
            builder.AddEventHandler(new HandlerId("multiplayer.events"), 100, HandleSessionEvent);
            builder.AddEventHandler(new HandlerId("multiplayer.phase"), 200, HandlePhaseEvent);
            builder.AddExternalEventHandler(new HandlerId("multiplayer.external"), 100, HandleExternalEvent);
            return builder.Build();
        }

        private Policy CreatePolicy()
        {
            var allowedExternalEvents = CreateAllowedExternalEvents();
            var allowedCommands = Defaults.StandardCommands;

            return new PolicyBuilder(Phase.Initializing, Phase.Countdown)
                .Add(Phase.Initializing, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Aborted })
                .Add(Phase.Countdown, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_panels, _coreRequests, _generalRequests, _vehicle, _sync, _playerInfo, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Running, Phase.Paused, Phase.Finishing, Phase.Aborted })
                .Add(Phase.Running, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_panels, _coreRequests, _generalRequests, _vehicle, _progress, _sync, _commentary, _playerInfo, _exit, _pitStop), allowedCommands, allowedExternalEvents, new[] { Phase.Paused, Phase.Finishing, Phase.Aborted })
                .Add(Phase.Paused, false, true, InputPolicy.Create(false, true, true), PhaseDefinition.Subsystems(_panels, _coreRequests, _vehicle, _sync, _playerInfo, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Running, Phase.Finishing, Phase.Aborted })
                .Add(Phase.Finishing, false, true, InputPolicy.Create(false, true, false), PhaseDefinition.Subsystems(_vehicle, _sync, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Finished, Phase.Aborted })
                .Add(Phase.Finished, false, true, InputPolicy.Create(false, true, false), PhaseDefinition.Subsystems(_vehicle, _sync, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Aborted })
                .Add(Phase.Aborted, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, Array.Empty<Phase>())
                .Build();
        }

        private static ExternalEventId[] CreateAllowedExternalEvents()
        {
            return
            [
                Incoming.RaceSnapshot,
                Incoming.PlayerBumped,
                Incoming.PlayerCrashed,
                Incoming.PlayerDisconnected,
                Incoming.RoomRacePlayerFinished,
                Incoming.RoomRaceCompleted,
                Incoming.RoomRaceAborted,
                Incoming.RoomParticipantSync,
                Incoming.LiveStart,
                Incoming.LiveFrame,
                Incoming.LiveStop,
                Incoming.MediaBegin,
                Incoming.MediaChunk,
                Incoming.MediaEnd
            ];
        }
    }
}
