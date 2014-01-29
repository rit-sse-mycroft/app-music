using SpotiFire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    enum Mode {Repeat, RepeateOne, Normal};
    class TrackManager
    {
        private List<Track> tracks;
        private Mode currentMode;

        public TrackManager()
        {
            tracks = new List<Track>();
            currentMode = Mode.Normal;
        }

        public void AddTrack(Track track)
        {
            tracks.Add(track);
        }

        public Track PlayTrack(Track track)
        {
            tracks.Insert(0, track);
            return Dequeue();
        }

        public async Task AddAlbum(Album album)
        {
            AlbumBrowse b = await album.Browse();
            IList<Track> t = b.Tracks;
            tracks.AddRange(t);
        }

        public async Task<Track> PlayAlbum(Album album)
        {
            AlbumBrowse b = await album.Browse();
            IList<Track> t = b.Tracks;
            tracks.InsertRange(0, t);
            return Dequeue();
        }

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

        public bool IsEmpty()
        {
            return tracks.Count() == 0;
        }

        public void Clear()
        {
            tracks.Clear();
        }

        public Mode CurrentMode
        {
            set { currentMode = value; }
            get { return currentMode; }
        }
    }
}
