using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoxUserFolders
{
	[JsonObject]
	class Config
	{
		[JsonProperty]
		public Dictionary<string, Maping> groupMapings = new Dictionary<string, Maping>();

		[JsonProperty]
		public string accessFile;

		[JsonProperty("internal.lastpointer")]
		public string groupAddedPointer;
		
		[JsonObject]

		public class Maping
		{
			[JsonProperty]
			public string folderId;

			[JsonProperty]
			public string adminId;
		}
	}
}
