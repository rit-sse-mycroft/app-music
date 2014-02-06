using SpotiFire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    /// <summary>
    /// Enum for the current mode the the queue should be in
    /// </summary>
    enum Mode {Repeat, RepeateOne, Normal};
    /// <summary>
    /// The class used for managing the play queue
    /// </summary>
    class TrackManager
    {
        private List<Track> tracks;
        private Mode currentMode;

        /// <summary>
        /// Create a new Track Maangager
        /// </summary>
        public TrackManager()
        {
            tracks = new List<Track>();
            currentMode = Mode.Normal;
        }

        /// <summary>
        /// Adds a track to the end of the play queue
        /// </summary>
        /// <param name="track">The track to add</param>
        public void AddTrack(Track track)
        {
            tracks.Add(track);
        }

        /// <summary>
        /// Adds the track to the beginning of the play queue
        /// </summary>
        /// <param name="track">The track to add</param>
        /// <returns>The track that you just added</returns>
        public Track PlayTrack(Track track)
        {
            tracks.Insert(0, track);
            return Dequeue();
        }

        /// <summary>
        /// Adds an album to the end of the play queue
        /// </summary>
        /// <param name="album">The album to add</param>
        /// <returns>A task</returns>
        public async Task AddAlbum(Album album)
        {
            AlbumBrowse b = await album.Browse();
            IList<Track> t = b.Tracks;
            tracks.AddRange(t);
        }

        /// <summary>
        /// Adds an album to the beginning of the play queue
        /// </summary>
        /// <param name="album">The album to add</param>
        /// <returns>The first track to play</returns>
        public async Task<Track> PlayAlbum(Album album)
        {
            AlbumBrowse b = await album.Browse();
            IList<Track> t = b.Tracks;
            tracks.InsertRange(0, t);
            return Dequeue();
        }

        /// <summary>
        /// Gets the next song to play in the play queue
        /// </summary>
        /// <returns>The next song to play</returns>
        public Track Dequeue()
        {
            Track track = tracks[0];
            tracks.RemoveAt(0);
            switch (currentMode)
            {
                case Mode.Normal:
                    break;
                case Mode.Repeat:
                    AddTrack(track);
                    break;
                case Mode.RepeateOne:
                    tracks.Insert(0, track);
                    break;
            }
            return track;
        }

        /// <summary>
        /// Is the play queue Empty?
        /// </summary>
        /// <returns>True if yes, False if no</returns>
        public bool IsEmpty()
        {
            return tracks.Count() == 0;
        }

        /// <summary>
        /// Clears the entire play queue
        /// </summary>
        public void Clear()
        {
            tracks.Clear();
        }

        /// <summary>
        /// Accessors for the current mode
        /// </summary>
        public Mode CurrentMode
        {
            set { currentMode = value; }
            get { return currentMode; }
        }
    }
}
