/* This file is taken from ScrewTurn C# wiki. It is released under GPL v2.0. I believe there is no copyright infringement here
 * as I'm going to release my code as GPL as well. */

using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Globalization;

namespace ScrewTurn.Wiki
{
    /// <summary>
    /// Contains useful Tools.
    /// </summary>
    public static class Tools {
/*
        /// <summary>
        /// Gets all the included files for the HTML Head, such as CSS, JavaScript and Icon files.
        /// </summary>
        public static string Includes {
            get {
                StringBuilder result = new StringBuilder();

                string[] css = Directory.GetFiles(Settings.ThemesDirectory + Settings.Theme, "*.css");
				string firstChunk;
                for(int i = 0; i < css.Length; i++) {
					if(Path.GetFileName(css[i]).IndexOf("_") != -1) {
						firstChunk = Path.GetFileName(css[i]).Substring(0, Path.GetFileName(css[i]).IndexOf("_")).ToLowerInvariant();
						if(firstChunk.Equals("screen") || firstChunk.Equals("print") || firstChunk.Equals("all") ||
							firstChunk.Equals("aural") || firstChunk.Equals("braille") || firstChunk.Equals("embossed") ||
							firstChunk.Equals("handheld") || firstChunk.Equals("projection") || firstChunk.Equals("tty") || firstChunk.Equals("tv")) {
							result.Append(@"<link rel=""stylesheet"" media=""" + firstChunk + @""" href=""" + Settings.ThemePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
						}
						else {
							result.Append(@"<link rel=""stylesheet"" href=""" + Settings.ThemePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
						}
					}
					else {
						result.Append(@"<link rel=""stylesheet"" href=""" + Settings.ThemePath + Path.GetFileName(css[i]) + @""" type=""text/css"" />" + "\n");
					}
                }

                string[] js = Directory.GetFiles(Settings.ThemesDirectory + Settings.Theme, "*.js");
                for(int i = 0; i < js.Length; i++) {
                    result.Append(@"<script src=""" + Settings.ThemePath + Path.GetFileName(js[i]) + @""" type=""text/javascript""></script>" + "\n");
                }

                string[] icons = Directory.GetFiles(Settings.ThemesDirectory + Settings.Theme, "Icon.*");
				if(icons.Length > 0) {
					result.Append(@"<link rel=""shortcut icon"" href=""" + Settings.ThemePath + Path.GetFileName(icons[0]) + @""" type=""");
					switch(Path.GetExtension(icons[0]).ToLowerInvariant()) {
						case ".ico":
							result.Append("image/x-icon");
							break;
						case ".gif":
							result.Append("image/gif");
							break;
						case ".png":
							result.Append("image/png");
							break;
					}
					result.Append(@""" />" + "\n");
				}

				js = Directory.GetFiles(Settings.JsDirectory, "*.js");
				for(int i = 0; i < js.Length; i++) {
					result.Append(@"<script type=""text/javascript"" src=""" + Settings.JsDirectoryName + "/" + Path.GetFileName(js[i]) + @"""></script>" + "\n");
				}

				// Include HTML Head
				result.Append(LoadFile(Settings.HtmlHeadFile));

                return result.ToString();
            }
        }

        /// <summary>
        /// Returns the content of a file.
        /// </summary>
        /// <param name="path">The full path of the file to read.</param>
        /// <returns>The content of a file.</returns>
        public static string LoadFile(string path) {
			if(!File.Exists(path)) return null;
            FileStream fs = null;
            for(int i = 0; i < Settings.FileAccessTries; i++) {
                try {
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    break;
                }
                catch {
                    Thread.Sleep(Settings.FileAccessTryDelay);
                }
            }
            if(fs == null) throw new IOException("Unable to open the file: " + path);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string res = sr.ReadToEnd();
            sr.Close();
            return res;
        }

        /// <summary>
        /// Writes the content of a File.
        /// </summary>
        /// <param name="path">The full path of the file to write.</param>
        /// <param name="content">The content of the file.</param>
        public static void WriteFile(string path, string content) {
            FileStream fs = null;
            for(int i = 0; i < Settings.FileAccessTries; i++) {
                try {
                    fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                    break;
                }
                catch {
                    Thread.Sleep(Settings.FileAccessTryDelay);
                }
            }
            if(fs == null) throw new IOException("Unable to open the file: " + path);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            sw.Write(content);
            sw.Close();
            Log.LogEntry("File " + Path.GetFileName(path) + " written", EntryType.General, "SYSTEM");
        }

		/// <summary>
		/// Appends some content to a File. If the file doesn't exist, it is created.
		/// </summary>
		/// <param name="path">The full path of the file to append the content to.</param>
		/// <param name="content">The content to append to the file.</param>
		public static void AppendFile(string path, string content) {
			FileStream fs = null;
			for(int i = 0; i < Settings.FileAccessTries; i++) {
				try {
					fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None);
					break;
				}
				catch {
					Thread.Sleep(Settings.FileAccessTryDelay);
				}
			}
			if(fs == null) throw new IOException("Unable to open the file: " + path);
			StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
			sw.Write(content);
			sw.Close();
			Log.LogEntry("File " + Path.GetFileName(path) + " written", EntryType.General, "SYSTEM");
		}

        /// <summary>
        /// Converts a byte number into a KB number.
        /// </summary>
        /// <param name="bytes">The # of bytes.</param>
        /// <returns>The # of KB.</returns>
        public static float BytesToKiloBytes(long bytes) {
            return (float)bytes / 1024F;
        }

        /// <summary>
        /// Converts a byte number into a KB number, in a string format.
        /// </summary>
        /// <param name="bytes">The # of bytes.</param>
        /// <returns>The # of KB.</returns>
        public static string BytesToKiloBytesString(long bytes) {
            return string.Format("{0:N2}", BytesToKiloBytes(bytes));
        }

        /// <summary>
        /// Converts a byte number into a string, formatted using KB, MB or GB.
        /// </summary>
        /// <param name="bytes">The # of bytes.</param>
        /// <returns>The formatted string.</returns>
        public static string BytesToString(long bytes) {
            if(bytes < 1024) return bytes.ToString() + " B";
            else if(bytes < 1048576) return string.Format("{0:N2} KB", (float)bytes / 1024F);
            else if(bytes < 1073741824) return string.Format("{0:N2} MB", (float)bytes / 1048576F);
            else return string.Format("{0:N2} GB", (float)bytes / 1073741824F);
        }

        /// <summary>
        /// Computes the Disk Space Usage of a directory.
        /// </summary>
        /// <param name="dir">The directory.</param>
        /// <returns>The used Disk Space, in bytes.</returns>
        public static long DiskUsage(string dir) {
            string[] files = Directory.GetFiles(dir);
            string[] directories = Directory.GetDirectories(dir);
            long result = 0;

            FileInfo file;
            for(int i = 0; i < files.Length; i++) {
                file = new FileInfo(files[i]);
                result += file.Length;
            }
            for(int i = 0; i < directories.Length; i++) {
                result += DiskUsage(directories[i]);
            }
            return result;
        }

        /// <summary>
        /// Generates the standard 5-digit Page Version string.
        /// </summary>
        /// <param name="version">The Page version.</param>
        /// <returns>The 5-digit Version string.</returns>
        public static string GetVersionString(int version) {
            string result = version.ToString();
            int len = result.Length;
            for(int i = 0; i < 5 - len; i++) {
                result = "0" + result;
            }
            return result;
        }

		/// <summary>
		/// Gets the available Themes.
		/// </summary>
		public static string[] AvailableThemes {
			get {
				string[] dirs = Directory.GetDirectories(Settings.ThemesDirectory);
				string[] res = new string[dirs.Length];
				for(int i = 0; i < dirs.Length; i++) {
					if(dirs[i].EndsWith("\\")) dirs[i] = dirs[i].Substring(0, dirs[i].Length - 1);
					res[i] = dirs[i].Substring(dirs[i].LastIndexOf("\\") + 1);
				}
				return res;
			}
		}

		/// <summary>
		/// Gets the available Cultures.
		/// </summary>
		public static string[] AvailableCultures {
			get {
				List<string> c = new List<string>();
				c.Add("it-IT|Italiano (Italian)");
				c.Add("de-DE|Deutsch (German)");
				c.Add("fr-FR|Français (French)");
				c.Add("es-ES|Español (Spanish)");
				c.Add("ru-RU|Русский (Russian)");
				c.Add("pt-BR|Português, Brasil (Brazilian Portuguese)");
				c.Add("zh-CN|Simplified Chinese");
				c.Add("zh-TW|Traditional Chinese");
				c.Add("cs-CZ|Česky (Czech)");
				c.Add("sk-SK|Slovenčina (Slovak)");
				c.Add("hu-HU|Magyar (Hungarian)");
				c.Add("nl-NL|Nederlands (Dutch)");
				c.Add("tr-TR|Turkish");
				return c.ToArray();
			}
		}

		/// <summary>
		/// Aligns a Date and Time with the User's preferences (if any).
		/// </summary>
		/// <param name="dt">The Date and Time.</param>
		/// <param name="sh">The time shift in minutes respect to the Greenwich time.</param>
		/// <returns>The aligned Date and Time.</returns>
		public static DateTime AlignWithPreferences(DateTime dt, string sh) {
			int shift;
			if(sh== null || sh.Length == 0) {
				shift = int.Parse(Settings.DefaultTimezone);
			}
			else {
				shift = int.Parse(sh);
			}
			return dt.ToUniversalTime().AddMinutes(shift + (dt.IsDaylightSavingTime() ? 60 : 0));
		}

		/// <summary>
		/// Computes the Hash of a Username, mixing it with other data, in order to avoid illegal Account activations.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <returns>The secured Hash of the Username.</returns>
		public static string ComputeSecuredUsernameHash(string username) {
			return Hash.ComputeSecuredUsernameHash(username, Settings.MasterPassword);
		}

		/// <summary>
		/// Escapes bad characters in a string (pipes and \n).
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The escaped string.</returns>
		public static string EscapeString(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("\r", "");
			sb.Replace("\n", "%0A");
			sb.Replace("|", "%7C");
			return sb.ToString();
		}

		/// <summary>
		/// Unescapes bad characters in a string (pipes and \n).
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The unescaped string.</returns>
		public static string UnescapeString(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("%7C", "|");
			sb.Replace("%0A", "\n");
			return sb.ToString();
		}
		
		/// <summary>
		/// Generates a random 10-char Password.
		/// </summary>
		/// <returns>The Password.</returns>
		public static string GenerateRandomPassword() {
			Random r = new Random();
			string password = "";
			for(int i = 0; i < 10; i++) {
				if(i % 2 == 0)
					password += ((char)r.Next(65, 91)).ToString(); // Uppercase letter
				else password += ((char)r.Next(97, 123)).ToString(); // Lowercase letter
			}
			return password;
		}

		//private static bool negative = false;
		//private static int count = 0;

		/// <summary>
		/// Gets the System Uptime.
		/// </summary>
		public static TimeSpan SystemUptime {
			get {
				if(GetCurrentPermissionLevel() == AspNetHostingPermissionLevel.Unrestricted) {
					try {
						ManagementObjectSearcher q = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");
						foreach(ManagementObject result in q.Get()) {
							string s = Convert.ToString(result["LastBootUpTime"]);
							s = s.Substring(0, s.IndexOf("."));
							return DateTime.Now.Subtract(DateTime.ParseExact(s, "yyyyMMddHHmmss", CultureInfo.InvariantCulture));
						}
					}
					catch { }
					return new TimeSpan(0);
				}
				else {
					// Cannot use WMI because of unsufficient permissions
					// This code is experimental: TickCount counts up to 25 days, then goes to -25 and counts to 0, then restarts
					int t = Environment.TickCount;
					if(t < 0) t = t + int.MaxValue;
					//t = t + (count * int.MaxValue);
					t = t / 1000;
					return TimeSpan.FromSeconds(t);
				//}
			}
		}

		private static AspNetHostingPermissionLevel CurrentPermissionLevel = AspNetHostingPermissionLevel.None;

		private static AspNetHostingPermissionLevel GetCurrentPermissionLevel() {
			if(CurrentPermissionLevel == AspNetHostingPermissionLevel.None) {
				AspNetHostingPermissionLevel[] levels = new AspNetHostingPermissionLevel[] {
					AspNetHostingPermissionLevel.Unrestricted,
					AspNetHostingPermissionLevel.High,
					AspNetHostingPermissionLevel.Medium,
					AspNetHostingPermissionLevel.Low,
					AspNetHostingPermissionLevel.Minimal };
				for(int i = 0; i < levels.Length; i++) {
					try {
						new AspNetHostingPermission(levels[i]).Demand();
					}
					catch {
						continue;
					}
					return levels[i];
				}
				return AspNetHostingPermissionLevel.None;
			}
			else return CurrentPermissionLevel;
		}

		/// <summary>
		/// Converts a Time Span to string.
		/// </summary>
		/// <param name="span">The Time Span.</param>
		/// <returns>The string.</returns>
		public static string TimeSpanToString(TimeSpan span) {
			string result = span.Days.ToString() + "d ";
			result += span.Hours.ToString() + "h ";
			result += span.Minutes.ToString() + "m ";
			result += span.Seconds.ToString() + "s";
			return result;
		}

		/// <summary>
		/// Generates the Hash of an Image name.
		/// </summary>
		/// <param name="name">The image name.</param>
		/// <param name="big">True if the image is big.</param>
		/// <returns>The Hash.</returns>
		public static string GenerateImageNameHash(string name, bool big) {
			name = "Thumb-" + name.Replace("\\", "/").ToLowerInvariant() + (big ? "-Big" : "");
			return Hash.Compute(name);
		}

		/// <summary>
		/// Clears all the thumbnails (if any) of a file.
		/// </summary>
		/// <param name="name">The File Name.</param>
		public static void DeleteThumbnails(string name) {
			string n1 = GenerateImageNameHash(name, false) + ".jpg";
			string n2 = GenerateImageNameHash(name, true) + ".jpg";
			try {
				File.Delete(Settings.TempDirectory + n1);
			}
			catch { }
			try {
				File.Delete(Settings.TempDirectory + n2);
			}
			catch { }
		}*/

		/// <summary>
		/// Executes URL-encoding, avoiding to use '+' for spaces.
		/// </summary>
		/// <remarks>This method uses internally Server.UrlEncode.</remarks>
		/// <param name="input">The input string.</param>
		/// <returns>The encoded string.</returns>
		public static string UrlEncode(string input) {
			return HttpUtility.UrlEncode(input).Replace("+", "%20");
		}


        /// <summary>
        /// Executes URL-encoding, avoiding to use '+' for spaces.
        /// </summary>
        /// <remarks>This method uses internally Server.UrlEncode.</remarks>
        /// <param name="input">The input string.</param>
        /// <returns>The encoded string.</returns>
        public static string UrlDecode(string input)
        {
            return HttpUtility.UrlDecode(input);
        }
	}
}
