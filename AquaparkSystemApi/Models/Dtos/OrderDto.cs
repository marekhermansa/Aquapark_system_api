﻿using System.Collections.Generic;

namespace AquaparkSystemApi.Models.Dtos
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public IEnumerable<TicketDto> Tickets { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
    }
}