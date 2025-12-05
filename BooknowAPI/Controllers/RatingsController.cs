using BooknowAPI.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/ratings")]
    public class RatingsController : ApiController
    {
        private newRestdbEntities7 db = new newRestdbEntities7();

        // POST: api/ratings/submit
        [HttpPost]
        [Route("submit")]
        public IHttpActionResult SubmitRating([FromBody] RatingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate Stars
                if (request.Stars < 1 || request.Stars > 5)
                    return BadRequest("Rating must be between 1 and 5 stars");

                // Check if user exists (optional)
                var userExists = db.Users.Any(u => u.UserId == request.UserId);
                if (!userExists)
                    return BadRequest($"User with ID {request.UserId} not found");

                // IMPORTANT: Get RestaurantId from BookingId
                if (!request.BookingId.HasValue)
                    return BadRequest("BookingId is required");

                // Get booking to extract RestaurantId
                var booking = db.Bookings
                    .Where(b => b.BookingId == request.BookingId.Value)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.RestaurantId,
                        b.UserId
                    })
                    .FirstOrDefault();

                if (booking == null)
                    return BadRequest($"Booking with ID {request.BookingId} not found");

                // Verify booking belongs to the user
                if (booking.UserId != request.UserId)
                    return BadRequest("This booking does not belong to the specified user");

                // Check if restaurant exists
                var restaurantExists = db.Restaurants.Any(r => r.RestaurantId == booking.RestaurantId);
                if (!restaurantExists)
                    return BadRequest($"Restaurant with ID {booking.RestaurantId} not found");

                // Check if THIS booking has already been rated
                var existingRatingForBooking = db.Ratings.FirstOrDefault(r =>
                    r.BookingId == request.BookingId);

                if (existingRatingForBooking != null)
                    return Content(HttpStatusCode.Conflict, "This booking has already been rated.");

                // Create rating with RestaurantId from booking
                var rating = new Rating
                {
                    UserId = request.UserId,
                    RestaurantId = booking.RestaurantId, // Get from booking
                    BookingId = request.BookingId,
                    Stars = request.Stars
                };

                db.Ratings.Add(rating);
                db.SaveChanges();

                // Return created rating
                var result = new
                {
                    rating.RatingId,
                    rating.UserId,
                    rating.BookingId,
                    rating.Stars,
                    rating.RestaurantId,
                    Message = "Rating submitted successfully"
                };

                return CreatedAtRoute("DefaultApi", new { id = rating.RatingId }, result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST: api/ratings/submit/bybooking
        [HttpPost]
        [Route("submit/bybooking")]
        public IHttpActionResult SubmitRatingByBooking([FromBody] RatingByBookingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate Stars
                if (request.Stars < 1 || request.Stars > 5)
                    return BadRequest("Rating must be between 1 and 5 stars");

                // Get booking to extract UserId and RestaurantId
                var booking = db.Bookings
                    .Where(b => b.BookingId == request.BookingId)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.RestaurantId,
                        b.UserId
                    })
                    .FirstOrDefault();

                if (booking == null)
                    return BadRequest($"Booking with ID {request.BookingId} not found");

                // Check if user exists
                var userExists = db.Users.Any(u => u.UserId == booking.UserId);
                if (!userExists)
                    return BadRequest($"User with ID {booking.UserId} not found");

                // Check if restaurant exists
                var restaurantExists = db.Restaurants.Any(r => r.RestaurantId == booking.RestaurantId);
                if (!restaurantExists)
                    return BadRequest($"Restaurant with ID {booking.RestaurantId} not found");

                // Check if THIS booking has already been rated
                var existingRatingForBooking = db.Ratings.FirstOrDefault(r =>
                    r.BookingId == request.BookingId);

                if (existingRatingForBooking != null)
                    return Content(HttpStatusCode.Conflict, "This booking has already been rated.");

                // Create rating with UserId and RestaurantId from booking
                var rating = new Rating
                {
                    UserId = booking.UserId, // Get from booking
                    RestaurantId = booking.RestaurantId, // Get from booking
                    BookingId = request.BookingId,
                    Stars = request.Stars
                };

                db.Ratings.Add(rating);
                db.SaveChanges();

                // Get restaurant name for response
                var restaurantName = db.Restaurants
                    .Where(r => r.RestaurantId == booking.RestaurantId)
                    .Select(r => r.Name)
                    .FirstOrDefault();

                // Return created rating
                var result = new
                {
                    rating.RatingId,
                    rating.UserId,
                    rating.BookingId,
                    rating.Stars,
                    rating.RestaurantId,
                    RestaurantName = restaurantName,
                    Message = "Rating submitted successfully"
                };

                return CreatedAtRoute("DefaultApi", new { id = rating.RatingId }, result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/ratings/check/booking/{bookingId}
        [HttpGet]
        [Route("check/booking/{bookingId}")]
        public IHttpActionResult CheckBookingRating(int bookingId)
        {
            try
            {
                // Get booking info
                var booking = db.Bookings
                    .Where(b => b.BookingId == bookingId)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.RestaurantId,
                        b.UserId
                    })
                    .FirstOrDefault();

                if (booking == null)
                    return NotFound();

                var rating = db.Ratings.FirstOrDefault(r => r.BookingId == bookingId);

                if (rating == null)
                {
                    return Ok(new
                    {
                        bookingId = bookingId,
                        userId = booking.UserId,
                        restaurantId = booking.RestaurantId,
                        hasRated = false,
                        message = "Booking has not been rated yet"
                    });
                }

                return Ok(new
                {
                    bookingId = bookingId,
                    userId = booking.UserId,
                    restaurantId = booking.RestaurantId,
                    hasRated = true,
                    ratingId = rating.RatingId,
                    stars = rating.Stars,
                    message = "Booking already rated"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/ratings/bybooking/{bookingId}
        [HttpGet]
        [Route("bybooking/{bookingId}")]
        public IHttpActionResult GetRatingByBooking(int bookingId)
        {
            try
            {
                var rating = db.Ratings
                    .FirstOrDefault(r => r.BookingId == bookingId);

                if (rating == null)
                    return NotFound();

                // Get booking and restaurant info
                var bookingInfo = db.Bookings
                    .Where(b => b.BookingId == bookingId)
                    .Select(b => new
                    {
                        b.UserId,
                        b.RestaurantId,
                        RestaurantName = db.Restaurants
                            .Where(r => r.RestaurantId == b.RestaurantId)
                            .Select(r => r.Name)
                            .FirstOrDefault(),
                        RestaurantLocation = db.Restaurants
                            .Where(r => r.RestaurantId == b.RestaurantId)
                            .Select(r => r.Location)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                return Ok(new
                {
                    rating.RatingId,
                    rating.UserId,
                    rating.BookingId,
                    rating.Stars,
                    rating.RestaurantId,
                    BookingUserId = bookingInfo?.UserId,
                    RestaurantName = bookingInfo?.RestaurantName,
                    RestaurantLocation = bookingInfo?.RestaurantLocation
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // PUT: api/ratings/update/bybooking/{bookingId}
        [HttpPut]
        [Route("update/bybooking/{bookingId}")]
        public IHttpActionResult UpdateRatingByBooking(int bookingId, [FromBody] UpdateRatingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate Stars
                if (request.Stars < 1 || request.Stars > 5)
                    return BadRequest("Rating must be between 1 and 5 stars");

                // Find rating by BookingId
                var rating = db.Ratings.FirstOrDefault(r => r.BookingId == bookingId);
                if (rating == null)
                    return NotFound();

                // Update Stars only
                rating.Stars = request.Stars;

                db.Entry(rating).State = EntityState.Modified;
                db.SaveChanges();

                return Ok(new
                {
                    rating.RatingId,
                    rating.UserId,
                    rating.BookingId,
                    rating.Stars,
                    rating.RestaurantId,
                    Message = "Rating updated successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // DELETE: api/ratings/delete/bybooking/{bookingId}
        [HttpDelete]
        [Route("delete/bybooking/{bookingId}")]
        public IHttpActionResult DeleteRatingByBooking(int bookingId)
        {
            try
            {
                var rating = db.Ratings.FirstOrDefault(r => r.BookingId == bookingId);
                if (rating == null)
                    return NotFound();

                db.Ratings.Remove(rating);
                db.SaveChanges();

                return Ok(new
                {
                    Message = "Rating deleted successfully",
                    DeletedRatingId = rating.RatingId,
                    BookingId = bookingId,
                    RestaurantId = rating.RestaurantId
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET: api/ratings/booking/{bookingId}/details
        [HttpGet]
        [Route("booking/{bookingId}/details")]
        public IHttpActionResult GetBookingRatingDetails(int bookingId)
        {
            try
            {
                // Get booking with restaurant info
                var booking = db.Bookings
                    .Where(b => b.BookingId == bookingId)
                    .Select(b => new
                    {
                        b.BookingId,
                        b.UserId,
                        b.RestaurantId,
                        b.BookingDateTime,
                        b.TableId,
                        b.Status,
                        RestaurantName = db.Restaurants
                            .Where(r => r.RestaurantId == b.RestaurantId)
                            .Select(r => r.Name)
                            .FirstOrDefault(),
                        RestaurantImage = db.Restaurants
                            .Where(r => r.RestaurantId == b.RestaurantId)
                            .Select(r => r.ImageUrl)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (booking == null)
                    return NotFound();

                // Get rating if exists
                var rating = db.Ratings
                    .Where(r => r.BookingId == bookingId)
                    .Select(r => new
                    {
                        r.RatingId,
                        r.Stars
                    })
                    .FirstOrDefault();

                return Ok(new
                {
                    Booking = booking,
                    Rating = rating,
                    HasRating = rating != null
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //// GET: api/ratings/user/{userId}/bookings/ratings
        //[HttpGet]
        //[Route("user/{userId}/bookings/ratings")]
        //public IHttpActionResult GetUserBookingsWithRatings(int userId)
        //{
        //    try
        //    {
        //        // Get all bookings for user with their ratings
        //        var bookingsWithRatings = db.Bookings
        //            .Where(b => b.UserId == userId)
        //            .Select(b => new
        //            {
        //                b.BookingId,
        //                b.RestaurantId,
        //                b.BookingDateTime,
        //                b.TableId,
        //                b.Status,
        //                RestaurantName = db.Restaurants
        //                    .Where(r => r.RestaurantId == b.RestaurantId)
        //                    .Select(r => r.Name)
        //                    .FirstOrDefault(),
        //                Rating = db.Ratings
        //                    .Where(r => r.BookingId == b.BookingId)
        //                    .Select(r => new
        //                    {
        //                        r.RatingId,
        //                        r.Stars
        //                    })
        //                    .FirstOrDefault()
        //            })
        //            .ToList()
        //            .Select(b => new
        //            {
        //                b.BookingId,
        //                b.RestaurantId,
        //                b.RestaurantName,
        //                BookingDate = b.BookingDateTime.ToString("yyyy-MM-dd HH:mm"),
        //                b.TableId,
        //                b.Status,
        //                HasRating = b.Rating != null,
        //                Rating = b.Rating
        //            })
        //            .OrderByDescending(b => b.BookingDate)
        //            .ToList();

        //        return Ok(new
        //        {
        //            UserId = userId,
        //            TotalBookings = bookingsWithRatings.Count,
        //            RatedBookings = bookingsWithRatings.Count(b => b.HasRating),
        //            UnratedBookings = bookingsWithRatings.Count(b => !b.HasRating),
        //            Bookings = bookingsWithRatings
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}

        // Keep existing endpoints unchanged (they're fine as is)
        // ... [All other existing endpoints remain the same] ...

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Request Models
    public class RatingRequest
    {
        public int UserId { get; set; }
        public int? BookingId { get; set; }
        public int Stars { get; set; }
        // RestaurantId is not needed - will be extracted from BookingId
    }

    public class RatingByBookingRequest
    {
        public int BookingId { get; set; }
        public int Stars { get; set; }
        // UserId and RestaurantId will be extracted from BookingId
    }

    public class UpdateRatingRequest
    {
        public int Stars { get; set; }
    }
}