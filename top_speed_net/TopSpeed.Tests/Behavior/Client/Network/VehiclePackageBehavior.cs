using System.Collections.Generic;
using TopSpeed.Network;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class VehiclePackageBehaviorTests
{
    private static VehiclePackagePayload SamplePayload(string tsvText = "[meta]\nname = Rocket\nversion = 2\n")
    {
        return new VehiclePackagePayload
        {
            Manifest = new VehiclePackageManifest
            {
                VehicleId = "Rocket",
                Version = "2",
                DisplayName = "Rocket (2)"
            },
            TsvText = tsvText,
            AssetBlobs = new Dictionary<string, byte[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["sounds/engine.wav"] = new byte[] { 1, 2, 3, 4 },
                ["sounds/horn.wav"] = new byte[] { 9, 8, 7 }
            }
        };
    }

    [Fact]
    public void VehiclePackageCodec_ShouldRoundTrip_TextAndAssets()
    {
        var payload = SamplePayload();
        payload.Manifest.Hash = VehiclePackageCodec.ComputeHash(payload);

        var bytes = VehiclePackageCodec.Serialize(payload);
        Assert.True(VehiclePackageCodec.TryDeserialize(bytes, out var restored, out var error), error);

        Assert.Equal(payload.Manifest.VehicleId, restored.Manifest.VehicleId);
        Assert.Equal(payload.Manifest.Version, restored.Manifest.Version);
        Assert.Equal(payload.Manifest.DisplayName, restored.Manifest.DisplayName);
        Assert.Equal(payload.TsvText, restored.TsvText);
        Assert.Equal(2, restored.AssetBlobs.Count);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, restored.AssetBlobs["sounds/engine.wav"]);
        Assert.Equal(new byte[] { 9, 8, 7 }, restored.AssetBlobs["sounds/horn.wav"]);
    }

    [Fact]
    public void VehiclePackageCodec_Hash_ShouldBeDeterministic_AndContentSensitive()
    {
        var a = SamplePayload();
        var b = SamplePayload();
        Assert.Equal(VehiclePackageCodec.ComputeHash(a), VehiclePackageCodec.ComputeHash(b));

        var different = SamplePayload("[meta]\nname = Rocket\nversion = 3\n");
        Assert.NotEqual(VehiclePackageCodec.ComputeHash(a), VehiclePackageCodec.ComputeHash(different));
    }

    // Kept vehicles reproduce the server's folder layout, so the manifest carries a path like
    // "NASCAR/cup car dodge". The separators have to survive the wire intact.
    [Fact]
    public void VehiclePackageCodec_ShouldRoundTrip_NestedFolderName()
    {
        var payload = SamplePayload();
        payload.Manifest.FolderName = "NASCAR/cup car dodge";
        payload.Manifest.TsvFileName = "car.tsv";
        payload.Manifest.Hash = VehiclePackageCodec.ComputeHash(payload);

        var bytes = VehiclePackageCodec.Serialize(payload);
        Assert.True(VehiclePackageCodec.TryDeserialize(bytes, out var restored, out var error), error);
        Assert.Equal("NASCAR/cup car dodge", restored.Manifest.FolderName);
        Assert.Equal("car.tsv", restored.Manifest.TsvFileName);
    }

    // The hash must depend only on vehicle content, never on where the file happened to live. The
    // client reuses an already-present vehicle by matching this hash against locally rebuilt
    // packages, so folding the folder path into it would break that reuse the moment a vehicle was
    // kept under a different folder than the server used.
    [Fact]
    public void VehiclePackageHash_ShouldIgnore_FolderAndFileNames()
    {
        var nascar = SamplePayload();
        nascar.Manifest.FolderName = "NASCAR/cup car dodge";
        nascar.Manifest.TsvFileName = "car.tsv";

        var indy = SamplePayload();
        indy.Manifest.FolderName = "IndyCar/cup car dodge";
        indy.Manifest.TsvFileName = "different name.tsv";

        Assert.Equal(VehiclePackageCodec.ComputeHash(nascar), VehiclePackageCodec.ComputeHash(indy));
    }

    [Fact]
    public void VehiclePackageCatalog_ShouldRoundTrip_OverTheWire()
    {
        var packet = new PacketVehiclePackageCatalog
        {
            Vehicles = new[]
            {
                new PacketVehiclePackageCatalogEntry
                {
                    Vehicle = VehiclePackageRef.Custom("Rocket", "2", VehiclePackageRef.NormalizeHash("ABCDEF")),
                    DisplayName = "Rocket (2)",
                    SupportsAutomatic = false,
                    SupportsManual = true
                }
            }
        };

        var bytes = TopSpeed.Server.Protocol.PacketSerializer.WriteVehiclePackageCatalog(packet);
        Assert.True(ClientPacketSerializer.TryReadVehiclePackageCatalog(bytes, out var restored));
        Assert.Single(restored.Vehicles);
        Assert.Equal("Rocket", restored.Vehicles[0].Vehicle.VehicleId);
        Assert.Equal("2", restored.Vehicles[0].Vehicle.Version);
        Assert.Equal("abcdef", restored.Vehicles[0].Vehicle.Hash);
        Assert.Equal("Rocket (2)", restored.Vehicles[0].DisplayName);
        Assert.False(restored.Vehicles[0].SupportsAutomatic);
        Assert.True(restored.Vehicles[0].SupportsManual);
    }

    [Fact]
    public void RoomPlayerVehicle_ShouldRoundTrip_OverTheWire()
    {
        var bytes = TopSpeed.Server.Protocol.PacketSerializer.WriteRoomPlayerVehicle(new PacketRoomPlayerVehicle
        {
            PlayerNumber = 3,
            Hash = VehiclePackageRef.NormalizeHash("DEADBEEF")
        });

        Assert.True(ClientPacketSerializer.TryReadRoomPlayerVehicle(bytes, out var restored));
        Assert.Equal(3, restored.PlayerNumber);
        Assert.Equal("deadbeef", restored.Hash);
    }
}
