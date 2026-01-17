using System;
using System.Numerics;

namespace TopSpeed.Tracks.Map
{
    internal static class MapMovement
    {
        public static MapMovementState CreateStart(TrackMap map)
        {
            var position = map.CellToWorld(map.StartX, map.StartZ);
            return new MapMovementState
            {
                CellX = map.StartX,
                CellZ = map.StartZ,
                Heading = map.StartHeading,
                WorldPosition = position,
                DistanceMeters = 0f,
                PendingMeters = 0f
            };
        }

        public static MapDirection DirectionFromYaw(float yawRadians)
        {
            var degrees = yawRadians * 180f / (float)Math.PI;
            while (degrees > 180f) degrees -= 360f;
            while (degrees < -180f) degrees += 360f;

            if (degrees >= -45f && degrees < 45f)
                return MapDirection.North;
            if (degrees >= 45f && degrees < 135f)
                return MapDirection.East;
            if (degrees >= -135f && degrees < -45f)
                return MapDirection.West;
            return MapDirection.South;
        }

        public static Vector3 DirectionVector(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => Vector3.UnitZ,
                MapDirection.East => Vector3.UnitX,
                MapDirection.South => -Vector3.UnitZ,
                MapDirection.West => -Vector3.UnitX,
                _ => Vector3.UnitZ
            };
        }

        public static bool TryMove(
            TrackMap map,
            ref MapMovementState state,
            float distanceMeters,
            MapDirection heading,
            out TrackMapCell cell,
            out bool boundaryHit)
        {
            cell = null!;
            boundaryHit = false;
            if (map == null)
                return false;

            if (!map.TryGetCell(state.CellX, state.CellZ, out var currentCell))
                return false;

            if (Math.Abs(distanceMeters) < 0.001f)
            {
                cell = currentCell;
                return false;
            }

            var sign = distanceMeters >= 0f ? 1f : -1f;
            var direction = distanceMeters >= 0f ? heading : TrackMap.Opposite(heading);
            var meters = state.PendingMeters + Math.Abs(distanceMeters);
            var steps = (int)Math.Floor(meters / map.CellSizeMeters);
            state.PendingMeters = meters - steps * map.CellSizeMeters;

            if (steps <= 0)
            {
                cell = currentCell;
                return false;
            }

            var x = state.CellX;
            var z = state.CellZ;
            var moved = false;

            for (var i = 0; i < steps; i++)
            {
                if (!TryStepLoose(map, x, z, direction, out var nextX, out var nextZ, out var nextCell))
                {
                    boundaryHit = true;
                    break;
                }

                x = nextX;
                z = nextZ;
                cell = nextCell;
                moved = true;
            }

            if (!moved)
            {
                cell = currentCell;
                return false;
            }

            state.CellX = x;
            state.CellZ = z;
            state.Heading = heading;
            state.WorldPosition = map.CellToWorld(x, z);
            state.DistanceMeters += steps * map.CellSizeMeters * sign;
            return true;
        }

        private static bool TryStepLoose(
            TrackMap map,
            int x,
            int z,
            MapDirection direction,
            out int nextX,
            out int nextZ,
            out TrackMapCell nextCell)
        {
            nextCell = null!;
            (nextX, nextZ) = Offset(x, z, direction);
            if (!map.TryGetCell(nextX, nextZ, out nextCell))
                return false;

            if (!map.TryGetCell(x, z, out var currentCell))
                return false;

            return AllowsTravel(currentCell, nextCell, direction);
        }

        private static bool AllowsTravel(TrackMapCell currentCell, TrackMapCell nextCell, MapDirection direction)
        {
            if (currentCell.Exits == MapExits.None || nextCell.Exits == MapExits.None)
                return true;

            var exit = TrackMap.ExitsFromDirection(direction);
            var entry = TrackMap.ExitsFromDirection(TrackMap.Opposite(direction));
            return (currentCell.Exits & exit) != 0 || (nextCell.Exits & entry) != 0;
        }

        private static (int X, int Z) Offset(int x, int z, MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => (x, z + 1),
                MapDirection.East => (x + 1, z),
                MapDirection.South => (x, z - 1),
                MapDirection.West => (x - 1, z),
                _ => (x, z)
            };
        }
    }
}
