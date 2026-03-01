using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx.Logging;

using UnityEngine;

namespace Elin_Mod
{

	class DebugUtil
	{
		static System.Text.StringBuilder s_sb = null;
		private static ManualLogSource s_Logger;
		 
		public static void Initialize( ManualLogSource body )
		{
			s_Logger = body;
		}

		public static void Log(object message )
		{
			if (s_Logger != null)
			{
				s_Logger.LogInfo( (object)message );
			}
		}

		public static void LogError( object message, bool isDumpStackTrace=false )
		{
			if (s_Logger != null)
			{
				if (s_sb == null)
					s_sb = new System.Text.StringBuilder(100000);

				s_sb.AppendLine(message as string);
				if (isDumpStackTrace) {
					var stackTrace = new System.Diagnostics.StackTrace(true);
					s_sb.AppendLine(stackTrace.ToString());
				}
				s_Logger.LogError( (object)s_sb.ToString() );
				s_sb.Length = 0;
				//s_Logger.LogError((object)message );
			}
		}

		public static void LogWarning(object message )
		{
			if (s_Logger != null)
			{
				s_Logger.LogWarning( (object)message );
			}
		}

		public static void DumpText( string path, string text )
		{
			if (File.Exists( path ))
			{
				File.Delete( path );
			}
			File.WriteAllText( path, text );
		}

	}


}
