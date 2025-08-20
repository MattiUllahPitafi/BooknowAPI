using BooknowAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace BooknowAPI.Controllers
{
    [RoutePrefix("api/music")]
    public class MusicController : ApiController
    {
        private readonly newRestdbEntities2 db = new newRestdbEntities2();

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
