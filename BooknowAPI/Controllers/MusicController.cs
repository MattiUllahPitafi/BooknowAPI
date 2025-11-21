using BooknowAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/music")]
    public class MusicController : ApiController
    {
        private readonly newRestdbEntities7 db = new newRestdbEntities7();

        // GET: api/music/getall
        [HttpGet]
        [Route("getall")]
        public IHttpActionResult GetAll()
        {
            var musicList = db.Musics
                .Select(m => new
                {
                    m.MusicId,
                    m.Title,
                    m.Artist,
                    m.GenreId,
                    m.DurationInSeconds,
                    GenreName = m.MusicGenre != null ? m.MusicGenre.Name : null
                })
                .ToList();

            return Ok(musicList);
        }

        // GET: api/music/getbyid/5
        [HttpGet]
        [Route("getbyid/{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            var music = db.Musics
                .Where(m => m.MusicId == id)
                .Select(m => new
                {
                    m.MusicId,
                    m.Title,
                    m.Artist,
                    m.GenreId,
                    m.DurationInSeconds,
                    GenreName = m.MusicGenre != null ? m.MusicGenre.Name : null
                })
                .FirstOrDefault();

            if (music == null)
                return NotFound();

            return Ok(music);
        }
        [HttpGet]
        [Route("byday")]
       
        public IHttpActionResult GetMusicByDay(string day = null, string artistName = null)
        {
            // --- 0. Determine the Target Day ---
            DayOfWeek targetDay;

            if (string.IsNullOrWhiteSpace(day))
            {
                // If 'day' is not provided (or is null/empty), default to today
                targetDay = DateTime.Today.DayOfWeek;
            }
            else if (Enum.TryParse(day, true, out DayOfWeek parsedDay))
            {
                // If 'day' is provided and is a valid DayOfWeek name (e.g., "Friday", "3")
                targetDay = parsedDay;
            }
            else
            {
                // Invalid input for the day parameter
                return BadRequest("Invalid day parameter provided. Must be a valid DayOfWeek name (e.g., 'Monday') or an integer (0-6).");
            }

            // --- 1. Define the Genre Rotation Schedule (Business Logic) ---
            var dailyGenreMap = new Dictionary<DayOfWeek, string>
    {
        // Note: DayOfWeek returns Sunday=0, Monday=1, ..., Saturday=6
        { DayOfWeek.Monday, "Qawali" },
        { DayOfWeek.Tuesday, "Ghazal" },
        { DayOfWeek.Wednesday, "Pop" },
        { DayOfWeek.Thursday, "Rock" },
        { DayOfWeek.Friday, "Qawali" },
        { DayOfWeek.Saturday, "Ghazal" },
        { DayOfWeek.Sunday, "Pop" }
    };

            // --- 2. Determine the Required Genre for the Target Day ---
            if (!dailyGenreMap.TryGetValue(targetDay, out string requiredGenreName))
            {
                return NotFound($"Scheduled genre not found for the requested day: {targetDay}.");
            }

            // --- 3. Find the Genre ID ---
            var genre = db.MusicGenres.FirstOrDefault(g => g.Name == requiredGenreName);

            if (genre == null)
            {
                return BadRequest($"Genre '{requiredGenreName}' is scheduled for {targetDay} but not found in the database.");
            }

            int requiredGenreId = genre.GenreId;

            // --- 4. Start Base Query (Filter by Scheduled Genre) ---
            var query = db.Musics
                .Where(m => m.GenreId == requiredGenreId);

            // --- 5. Add Optional Artist Filter ---
            if (!string.IsNullOrWhiteSpace(artistName))
            {
                // Add case-insensitive filter for the artist name
                query = query.Where(m => m.Artist.ToLower().Contains(artistName.ToLower()));
            }

            // --- 6. Execute Query and Select Data ---
            var musicList = query
                .Select(m => new
                {
                    m.MusicId,
                    m.Title,
                    m.Artist,
                    m.GenreId,
                    m.DurationInSeconds,
                    GenreName = m.MusicGenre != null ? m.MusicGenre.Name : null
                })
                .ToList();

            if (!musicList.Any())
            {
                string message = $"No music tracks found for the scheduled genre '{requiredGenreName}' on {targetDay}";
                if (!string.IsNullOrWhiteSpace(artistName))
                {
                    message += $" by artist containing '{artistName}'";
                }
                return NotFound(message + ".");
            }

            return Ok(musicList);
        }
        private IHttpActionResult NotFound(string v)
        {
            throw new NotImplementedException();
        }

        // POST: api/music/create
        [HttpPost]
        [Route("create")]
        public IHttpActionResult Create(Music music)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Musics.Add(music);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.Created);
        }

        // PUT: api/music/update/5
        [HttpPut]
        [Route("update/{id:int}")]
        public IHttpActionResult Update(int id, Music music)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = db.Musics.Find(id);
            if (existing == null)
                return NotFound();

            existing.Title = music.Title;
            existing.Artist = music.Artist;
            existing.GenreId = music.GenreId;
            existing.DurationInSeconds = music.DurationInSeconds;

            db.SaveChanges();
            return Ok("Music updated successfully");
        }

        // DELETE: api/music/delete/5
        [HttpDelete]
        [Route("delete/{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            var music = db.Musics.Find(id);
            if (music == null)
                return NotFound();

            db.Musics.Remove(music);
            db.SaveChanges();
            return Ok("Music deleted successfully");
        }
    }
}
