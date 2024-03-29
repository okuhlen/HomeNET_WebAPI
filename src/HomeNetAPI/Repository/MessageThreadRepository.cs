﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeNetAPI.Models;


namespace HomeNetAPI.Repository
{
    public class MessageThreadRepository : IMessageThreadRepository
    {
        private HomeNetContext dbContext;

        public MessageThreadRepository(HomeNetContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public MessageThread CreateMessageThread(MessageThread newMessage)
        {
            var thread = dbContext.MessageThreads.Add(newMessage);
            dbContext.SaveChanges();
            if (thread != null)
            {
                return thread.Entity;
            }
            else
            {
                return null;
            }
        }

        public List<MessageThread> GetHouseMessages(int houseMemberID)
        {
            var result = dbContext.MessageThreads.Where(i => i.HouseMemberID == houseMemberID).ToList();
            return result;
        }

        public MessageThread RemoveMessageThread(MessageThread oldThread)
        {
            var result = dbContext.MessageThreads.First(i => i.MessageThreadID == oldThread.MessageThreadID);
            if (result != null)
            {
                dbContext.MessageThreads.Remove(result);
                dbContext.SaveChanges();
                return result;
            }
            else
            {
                return null;
            }
        }

        public MessageThread GetMessageThread(int messageThreadID)
        {
            var result = dbContext.MessageThreads.First(i => i.MessageThreadID == messageThreadID);
            return result;
        }

        public List<MessageThread> GetMessageThreadForMembership(int houseMemberID)
        {
            List<MessageThread> finalList = new List<MessageThread>();
            var result = dbContext.MessageThreads.Where(i => i.HouseMemberID == houseMemberID).ToList();
            var others = dbContext.MessageThreadParticipants.Where(i => i.HouseMemberID == houseMemberID).ToList();
            if (others != null)
            {
                foreach (MessageThreadParticipant participant in others)
                {
                    var thread = dbContext.MessageThreads.First(i => i.MessageThreadID == participant.MessageThreadID);
                    if (thread != null)
                    {
                        finalList.Add(thread);
                    }
                }
            }
            if (result != null)
            {
                foreach (MessageThread thread in result)
                {
                    if (!finalList.Contains(thread))
                    {
                        finalList.Add(thread);
                    }
                }
            }
            if (finalList.Count > 0)
            {
                return finalList;
            } else
            {
                return null;
            }
        }
    }
}
