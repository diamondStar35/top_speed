namespace SteamAudio
{
	/// <summary>
	/// Contains all of the Phonon Library's functions.
	/// </summary>
	public static partial class IPL
	{
		public const string Library = "phonon.dll";

		public partial struct Vector3
		{
			public Vector3(float x, float y, float z)
			{
				X = x;
				Y = y;
				Z = z;
			}
		}
	}
}
