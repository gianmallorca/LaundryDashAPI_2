﻿namespace LaundryDashAPI_2.DTOs.LaundryServiceLog
{
    public class LaundryServiceLogDTO
    {
        public Guid LaundryServiceLogId { get; set; }
        public Guid LaundryShopId { get; set; }
        public List<Guid> ServiceIds { get; set; }
        public bool IsActive { get; set; }
        public string? ServiceDescription { get; set; }
        public decimal? Price { get; set; }
    }
}
