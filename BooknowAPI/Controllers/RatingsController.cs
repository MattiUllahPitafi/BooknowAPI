using System.Linq;
using System.Web.Http;
using BooknowAPI.Models;

namespace RestWAdvBook.Controllers
{
    [RoutePrefix("api/ratings")]
    public class RatingsController : ApiController
    {
        private newRestdbEntities4 db = new newRestdbEntities4();

        // GET: api/ratings/all
        [HttpGet]
        [Route("all")]
        public IHttpActionResult GetAllRatings()
        {
            var ratings = db.Ratings.ToList();
            return Ok(ratings);
        }

        // GET: api/ratings/bybooking/5
        [HttpGet]
        [Route("bybooking/{bookingId}")]
        public IHttpActionResult GetByBooking(int bookingId)
        {
            var rating = db.Ratings.FirstOrDefault(r => r.BookingId == bookingId);
            if (rating == null)
                return NotFound();
            return Ok(rating);
        }

        // POST: api/ratings/create
        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Rating rating)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            rating.RatedAt = System.DateTime.Now;
            db.Ratings.Add(rating);
            db.SaveChanges();
            return Ok(rating);
        }

        // PUT: api/ratings/update/5
        [HttpPut]
        [Route("update/{id}")]
        public IHttpActionResult Update(int id, Rating updatedRating)
        {
            var rating = db.Ratings.Find(id);
            if (rating == null)
                return NotFound();

            rating.Stars = updatedRating.Stars;
            rating.Comment = updatedRating.Comment;
            rating.RatedAt = System.DateTime.Now;

            db.SaveChanges();
            return Ok(rating);
        }

        // DELETE: api/ratings/delete/5
        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult Delete(int id)
        {
            var rating = db.Ratings.Find(id);
            if (rating == null)
                return NotFound();

            db.Ratings.Remove(rating);
            db.SaveChanges();
            return Ok("Deleted");
        }
    }
}
