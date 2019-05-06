﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using AquaparkSystemApi.Exceptions;
using AquaparkSystemApi.Models;
using AquaparkSystemApi.Models.Dtos;
using AquaparkSystemApi.Models.PassedParameters;

namespace AquaparkSystemApi.Controllers
{
    public class OrdersController : ApiController
    {
        private AquaparkDbContext _dbContext;

        public OrdersController()
        {
                _dbContext = new AquaparkDbContext();
        }

        [AcceptVerbs("POST")]
        [ActionName("MakeNewOrder")]
        public OrderDto MakeNewOrder(NewOrder newOrder)
        {
            bool success = false;
            string status = "Wrong token";
            OrderDto orderDto = new OrderDto()
            {
                Success = success,
                Status = status
            };

            try
            {
                int userId;
                if (Security.Security.UserTokens.Any(i => i.Value == newOrder.UserToken))
                {
                    userId = Security.Security.UserTokens.FirstOrDefault(i => i.Value == newOrder.UserToken).Key;

                    User user = _dbContext.Users.FirstOrDefault(i => i.Id == userId);
                    if (user == null)
                        throw new UserNotFoundException("There is no user with given data.");

                    List<Position> positionsToOrder = new List<Position>();
                    foreach (var item in newOrder.TicketsWithClassDiscounts)
                    {
                        positionsToOrder.Add(new Position()
                        {
                            Number = item.NumberOfTickets,
                            SocialClassDiscount = _dbContext.SocialClassDiscounts.FirstOrDefault(i => i.Id == item.SocialClassDiscountId),
                            Ticket = _dbContext.Tickets.Include(i=> i.Zone).FirstOrDefault(i => i.Id == item.TicketId),
                            PeriodicDiscount = _dbContext.PeriodicDiscounts.FirstOrDefault(i => i.StartTime >= DateTime.Now &&
                                                                                                i.FinishTime <= DateTime.Now)
                        });
                    }

                    Order order = new Order()
                    {
                        DateOfOrder = DateTime.Now,
                        Positions = positionsToOrder,
                        UserData = new UserData()
                        {
                            Email = newOrder.UserData.Email,
                            Name = newOrder.UserData.Name,
                            Surname = newOrder.UserData.Surname
                        }
                    };
                    user.Orders.Add(order);
                    _dbContext.SaveChanges();

                    success = true;
                    status = "";
                    orderDto.Status = status;
                    orderDto.Success = success;
                    orderDto.Tickets = positionsToOrder.Select(i => new TicketDto()
                    {
                        Id = i.Id,
                        Number = i.Number,
                        Name = i.Ticket.Name,
                        Price = i.Ticket.Price,
                        Zone = new ZoneWithAttractionsInformationDto()
                        {
                            ZoneId = i.Ticket.Zone.Id,
                            Name = i.Ticket.Zone.Name,
                            Attractions = _dbContext.Attractions.Where(j => j.Zone.Id == i.Ticket.Zone.Id).
                                Select(j =>
                                    new AttractionPrimaryInformationDto()
                                    {
                                        AttractionId = j.Id,
                                        Name = j.Name
                                    })
                        }
                    });
                    orderDto.OrderId = order.Id;
                }
            }
            catch (Exception ex)
            {
                orderDto.Status = ex.Message;
                orderDto.Success = false;
            }

            return orderDto;
        }
    }
}