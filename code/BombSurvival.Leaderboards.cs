namespace BombSurvival;

public partial class BombSurvival
{
	/*
	[ClientRpc]
	internal static void SendScore( IClient client, int value )
	{
		if ( !Game.IsEditor )
		{
			if ( !client.IsValid() || client.IsBot ) return;

			Sandbox.Services.Stats.SetValue( client, $"{CurrentLevel} Highscores" , value );
			Log.Info( "Score sent" );
		}
	}

	[ConCmd.Admin]
	public static void TestScore( int value )
	{
		var caller = ConsoleSystem.Caller;

		SendScore( To.Single( caller ), caller, value );
	}*/
}
