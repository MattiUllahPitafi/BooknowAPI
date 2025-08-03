using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BooknowAPI.Models;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/tables")]

    public class TablesController : ApiController
    {
        private newRestdbEntities2 db = new newRestdbEntities2();

        // GET: api/tables
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetTables()
        {
            var tables = db.Tables.Select(r => new
            {
                r.Name,
                r.Location,
                r.Floor,
                r.Price,
                r.Status,
            });
            return Ok(tables);
        }
        // GET: api/tables/5
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetTable(int id)
        {
            var tables = db.Tables.Find(id);
            if (tables == null)
                return NotFound();

            return Ok(new
            {
               tables.Name,
               tables.Location,
               tables.Floor,
               tables.Price,
               tables.Status,
            });

        }
        // GET: api/tables/restaurant/3
        [HttpGet]
        [Route("restaurant/{restaurantId:int}")]
        public IHttpActionResult GetTablesByRestaurant(int restaurantId)
        {
            var tables = db.Tables
                           .Where(t => t.RestaurantId == restaurantId)
                           .Select(t => new
                           {
                               t.TableId,
                               t.Name,
                               t.Location,
                               t.Floor,
                               t.Price,
                               t.Status,
                               t.RestaurantId
                           })
                           .ToList();

            if (!tables.Any())
                return NotFound();

            return Ok(tables);
        }


    }
}
