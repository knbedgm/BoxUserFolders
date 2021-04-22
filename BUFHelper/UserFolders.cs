using System;
using Box.V2;
using Box.V2.Models;

namespace BUFHelper
{
    public class UserFolders
    {
        private BoxClient client;

        public UserFolders(BoxClient client)
        {
            this.client = client;
        }

        public System.Collections.Generic.List<Box.V2.Models.BoxUser> getUsers()
        {
            var users = client.UsersManager.GetEnterpriseUsersAsync(limit: 20);
            users.Wait();
            return users.Result.Entries;
        }

        public BoxEventCollection<BoxEnterpriseEvent> getNewUserEvents()
        {
            var events = client.EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "NEW_USER" });
            return events.Result;
        }

        public BoxEventCollection<BoxEnterpriseEvent> getGroupAddEvents()
        {
            var events = client.EventsManager.EnterpriseEventsAsync(limit: 20, eventTypes: new[] { "GROUP_ADD_USER" });
            return events.Result;
        }

        public void makeFoldersForUser(string uid)
        {
            var userGroups = getUserGroups(uid);
            userGroups.Entries.ForEach((BoxGroupMembership membership) =>
            {
                if (membership.Group.Id == "123")
                {
                    
                }
            });
        }

        public BoxCollection<BoxGroupMembership> getUserGroups(string uid)
        {
            return client.GroupsManager.GetAllGroupMembershipsForUserAsync(uid, autoPaginate: true).Result;
        }

        public BoxCollection<BoxGroupMembership> getGroupUsers(string gid)
        {
            return client.GroupsManager.GetAllGroupMembershipsForGroupAsync(gid, autoPaginate: true).Result;
        }

        public BoxCollection<BoxItem> getFolderContents(string id)
        {
            return client.FoldersManager.GetFolderItemsAsync(id, 1000, autoPaginate: true).Result;
        }
    }
}
