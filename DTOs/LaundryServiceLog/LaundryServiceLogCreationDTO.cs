﻿namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogCreationDTO
    {
        public Guid LaundryShopId { get; set; }
        public List<Guid> ServiceIds { get; set; }
        public decimal? Price { get; set; }

    }
}
