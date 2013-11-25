﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sinj.CommandLine
{
	class Pusher
	{
		private string _endpoint;
		private string[] _scripts;
		private bool _debug;

		public Pusher(string endpoint, string[] scripts, bool debug)
		{
			_endpoint = endpoint;
			_scripts = scripts;
			_debug = debug;
		}

		public int Execute()
		{
			ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertficate;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint);

			PostScript(request);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			ReadResponse(response);

			return response.StatusCode == HttpStatusCode.OK ? 0 : 1;
		}

		private static bool ValidateServerCertficate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		private string BuildScript()
		{
			StringBuilder builder = new StringBuilder();

			foreach (string script in _scripts)
			{
				string contents = File.ReadAllText(script);

				builder.AppendFormat("{0}={1}&", "path", HttpUtility.UrlEncode(script));
				builder.AppendFormat("{0}={1}&", "script", HttpUtility.UrlEncode(contents));
			}

			return builder.ToString();
		}

		private void PostScript(HttpWebRequest request)
		{
			request.KeepAlive = false;
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			string script = BuildScript();

			string data = script;

			if (_debug)
			{
				data = "debug=true&" + data;
			}

			byte[] requestData = Encoding.UTF8.GetBytes(data.ToString());

			request.ContentLength = requestData.Length;

			using (Stream requestStream = request.GetRequestStream())
			{
				requestStream.Write(requestData, 0, requestData.Length);
				requestStream.Close();
			}
		}

		private void ReadResponse(HttpWebResponse response)
		{
			byte[] buffer = new byte[64];

			using (Stream responseStream = response.GetResponseStream())
			{
				int length;

				while ((length = responseStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					string text = Encoding.ASCII.GetString(buffer, 0, length);

					Console.Write(text);
				}
			}
		}
	}
}
