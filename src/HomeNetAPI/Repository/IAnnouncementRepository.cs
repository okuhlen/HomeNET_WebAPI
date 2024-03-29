﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeNetAPI.Models;

namespace HomeNetAPI.Repository
{
    public interface IAnnouncementRepository
    {
        List<HouseAnnouncement> GetUserAnnoucements(int userID);
        List<HouseAnnouncement> GetHouseAnnouncements(int houseID);
        HouseAnnouncement AddHouseAnnouncement(HouseAnnouncement newAnnouncement);
        HouseAnnouncement GetHouseAnnouncement(int houseAnnouncementID);
        List<HouseAnnouncement> GetAnnouncementsByMembership(int houseMemberID);
    }
}
