using System;
using System.Threading.Tasks;
using Box.V2;
using Box.V2.Models;

namespace BoxUserFolders
{
	public static class UserFolders
	{
		//private BoxClient client;

		//public UserFolders(BoxClient client)
		//{
		//	this.client = client;
		//}

		public static async Task<BoxCollection<BoxUser>> GetUsers()
		{
			return await ClientManager.Get("0").UsersManager.GetEnterpriseUsersAsync(autoPaginate: true);
		}

		public static async Task<BoxEventCollection<BoxEnterpriseEvent>> GetNewUserEvents()
		{
			return await ClientManager.Get("0").EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "NEW_USER" });
		}

		public static async Task<BoxEventCollection<BoxEnterpriseEvent>> GetGroupAddEvents(string streamPos = null, DateTime? after = null)
		{
			return await ClientManager.Get("0").EventsManager.EnterpriseEventsAsync(eventTypes: new[] { "GROUP_ADD_USER" }, streamPosition: streamPos, createdAfter: after);
		}

		public static async void MakeFoldersForUser(string uid)
		{
			var userGroups = GetUserGroups(uid).Result;
			userGroups.Entries.ForEach((BoxGroupMembership membership) =>
			{
				if (membership.Group.Id == "123")
				{
					
				}
			});
		}

		public static async Task<BoxCollection<BoxGroupMembership>> GetUserGroups(string uid)
		{
			return await ClientManager.Get("0").GroupsManager.GetAllGroupMembershipsForUserAsync(uid, autoPaginate: true);
		}

		public static async Task<BoxCollection<BoxGroupMembership>> GetGroupUsers(string gid)
		{
			return await ClientManager.Get("0").GroupsManager.GetAllGroupMembershipsForGroupAsync(gid, autoPaginate: true);
		}

		public static async Task<BoxCollection<BoxItem>> GetFolderContents(BoxClient client, string id)
		{
			return await client .FoldersManager.GetFolderItemsAsync(id, 1000, autoPaginate: true);
		}
	}
}
