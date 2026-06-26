using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class LocalizationBehaviorTests
    {
        [Fact]
        public void MarkedTemplate_ShouldFormatWithoutTranslation()
        {
            LocalizationService.Format("Alice says: {0}", "Hello")
                .Should()
                .Be("Alice says: Hello");
        }

        [Fact]
        public void MarkedText_ShouldRoundTripWhenNoCatalogIsLoaded()
        {
            var marked = LocalizationService.Mark("Alice says: {0}");

            LocalizationService.Format(marked, "Hello")
                .Should()
                .Be("Alice says: Hello");
        }

        [Fact]
        public void SharedProtocolTexts_ShouldTranslateInClientScope()
        {
            using var scope = LocalizationScope.Map(new Dictionary<string, string>
            {
                [RoomTexts.RoomUnavailableFull] = "房间已满，无法加入。",
                [RoomTexts.NoBotsToRemove] = "没有可移除的电脑玩家。"
            });

            LocalizationService.Translate(RoomTexts.RoomUnavailableFull)
                .Should()
                .Be("房间已满，无法加入。");
            LocalizationService.Translate(RoomTexts.NoBotsToRemove)
                .Should()
                .Be("没有可移除的电脑玩家。");
        }

        [Fact]
        public void OfficialVehicleNames_ShouldTranslate()
        {
            using var scope = LocalizationScope.Map(new Dictionary<string, string>
            {
                ["Nissan GT-R Nismo"] = "日产 GT-R Nismo"
            });

            LocalizationService.Translate(OfficialVehicleCatalog.Vehicles[0].Name)
                .Should()
                .Be("日产 GT-R Nismo");
        }

        [Fact]
        public void OnlinePlayers_MainRoom_ShouldTranslate()
        {
            using var scope = LocalizationScope.Map(new Dictionary<string, string>
            {
                ["main room"] = "主房间"
            });

            var result = OnlineMap.ToList(new PacketOnlinePlayers
            {
                Players = new[]
                {
                    new PacketOnlinePlayer
                    {
                        PlayerId = 1,
                        PlayerNumber = 0,
                        Name = "Shuyue",
                        PresenceState = OnlinePresenceState.Available,
                        RoomName = "main room"
                    }
                }
            });

            result.Players.Should().HaveCount(1);
            result.Players[0].RoomName.Should().Be("主房间");
        }

        [Fact]
        public void TrackChangedHistory_ShouldTranslateBuiltInTrackName()
        {
            using var scope = LocalizationScope.Map(new Dictionary<string, string>
            {
                ["Track changed to {0}."] = "赛道已更改为 {0}。",
                [TrackList.RaceTracks[0].Display] = "美国"
            });

            var text = HistoryText.FromRoomEvent(new RoomEventInfo
            {
                Kind = RoomEventKind.TrackChanged,
                Track = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key),
                TrackName = TrackList.RaceTracks[0].Key
            });

            text.Should().Be("赛道已更改为 美国。");
        }

        [Fact]
        public void RoomTrackSelection_ShouldTranslateBuiltInTrackKey()
        {
            using var scope = LocalizationScope.Map(new Dictionary<string, string>
            {
                [TrackList.RaceTracks[0].Display] = "Localized America"
            });

            var text = MultiplayerCoordinator.FormatTrackDisplayName(
                TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key),
                string.Empty);

            text.Should().Be("Localized America");
        }

        private sealed class LocalizationScope : IDisposable
        {
            private LocalizationScope(ITextLocalizer localizer)
            {
                Set(localizer);
            }

            public static LocalizationScope Map(IReadOnlyDictionary<string, string> translations)
            {
                return new LocalizationScope(new MapLocalizer(translations));
            }

            public void Dispose()
            {
                Set(null);
            }

            private static void Set(ITextLocalizer? localizer)
            {
                var method = typeof(LocalizationService).GetMethod(
                    "SetLocalizer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                method.Should().NotBeNull();
                method!.Invoke(null, new object?[] { localizer });
            }
        }

        private sealed class MapLocalizer : ITextLocalizer
        {
            private readonly IReadOnlyDictionary<string, string> _translations;

            public MapLocalizer(IReadOnlyDictionary<string, string> translations)
            {
                _translations = translations;
            }

            public string Translate(string messageId)
            {
                return _translations.TryGetValue(messageId, out var translated) ? translated : messageId;
            }

            public string Translate(string context, string messageId)
            {
                return Translate(messageId);
            }
        }
    }
}
